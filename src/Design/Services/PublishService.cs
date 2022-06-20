using System.Data.Common;
using System.IO.Compression;
using System.Text;
using AppBoxCore;
using AppBoxStore;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace AppBoxDesign;

internal static class PublishService
{
    internal static void ValidateModels(DesignHub hub, PublishPackage package)
    {
        //TODO:
    }

    internal static async Task CompileModelsAsync(DesignHub hub, PublishPackage package)
    {
        foreach (var item in hub.PendingChanges!)
        {
            if (item is ServiceModel sm && sm.PersistentState != PersistentState.Deleted)
            {
                var asmData = await CompileServiceAsync(hub, sm);
                var appName = hub.DesignTree.FindApplicationNode(sm.Id.AppId)!.Model.Name;
                var fullName = $"{appName}.{sm.Name}";
                //重命名的已不再需要加入待删除列表，保存模型时已处理
                package.ServiceAssemblies.Add(fullName, asmData!);
            }
            //TODO: 编译转换视图模型的Web代码
        }
    }

    /// <summary>
    /// 1. 保存模型(包括编译好的服务Assembly)，并生成EntityModel的SchemaChangeJob;
    /// 2. 通知集群各节点更新缓存;
    /// 3. 删除当前会话的CheckoutInfo;
    /// 4. 刷新DesignTree相应的节点，并删除挂起
    /// 5. 保存递交日志
    /// </summary>
    internal static async Task PublishAsync(DesignHub hub, PublishPackage package,
        string commitMessage)
    {
        //先根据依赖关系排序
        package.SortAllModels();

        //注意: 目前实现无法保证第三方数据库与内置模型存储的一致性,第三方数据库发生异常只能手动清理
        var otherStoreTxns = new Dictionary<long, DbTransaction>();

#if FUTURE
        var txn = await Transaction.BeginAsync();
#else
        var txn = await SqlStore.Default.BeginTransactionAsync();
        otherStoreTxns.Add(SqlStore.DefaultSqlStoreId, txn);
#endif

        //TODO:考虑发布锁
        try
        {
            await SaveModelsAsync(hub, package, txn, otherStoreTxns);

            await CheckoutService.CheckinAsync(txn);

            //注意必须先刷新后清除缓存，否则删除的节点在移除后会自动保存
            //刷新所有CheckoutByMe的节点项
            hub.DesignTree.CheckinAllNodes();
            //清除所有签出缓存
            await StagedService.DeleteStagedAsync(txn);

            //先尝试递交第三方数据库的DDL事务
            foreach (var sqlTxn in otherStoreTxns.Values)
            {
                var sqlConn = sqlTxn.Connection;
                await sqlTxn.CommitAsync();
                sqlConn.Dispose();
            }

#if FUTURE
            //再递交系统数据库事务
            await txn.CommitAsync();
#endif
        }
        finally
        {
#if FUTURE
            txn.Dispose();
#endif
            foreach (var sqlTxn in otherStoreTxns.Values)
            {
                var sqlConn = sqlTxn.Connection;
                await sqlTxn.DisposeAsync();
                sqlConn?.Dispose();
            }
        }

        //最后通知各节点更新模型缓存
        InvalidModelsCache(hub, package);
    }

    /// <summary>
    /// 发布或调试时编译服务模型
    /// </summary>
    /// <remarks>
    /// 发布时返回的是已经压缩过的
    /// </remarks>
    internal static async Task<byte[]?> CompileServiceAsync(DesignHub hub, ServiceModel model,
        string? debugFolder = null)
    {
        var forDebug = !string.IsNullOrEmpty(debugFolder);
        var designNode = hub.DesignTree.FindModelNode(ModelType.Service, model.Id)!;
        var appName = designNode.AppNode.Model.Name;

        //获取RoslynDocument并检测语义错误
        var doc =
            hub.TypeSystem.Workspace.CurrentSolution.GetDocument(designNode.RoslynDocumentId)!;
        var semanticModel = await doc.GetSemanticModelAsync();
        if (semanticModel == null) throw new Exception("Can't get SemanticModel");
        CheckSemanticErrors(semanticModel);

        //转换服务模型的虚拟代码为运行时代码
        var codegen = new ServiceCodeGenerator(hub, appName, semanticModel, model);
        var newRootNode = codegen.Visit(await semanticModel.SyntaxTree.GetRootAsync());
        //Log.Debug(newRootNode.ToFullString());

        var docName = $"{appName}.Services.{model.Name}";
        var newTree = SyntaxFactory.SyntaxTree(newRootNode,
            path: docName + ".cs", encoding: Encoding.UTF8);

        //生成服务模型依赖的其他模型的运行时代码
        var usagesTree = codegen.GetUsagesTree();

        //注意：必须添加并更改版本号，否则服务端Assembly.Load始终是旧版 
        var newModelVersion = model.Version + 1; //用于消除版本差
        var asmVersion =
            $"{newModelVersion >> 24}.{(newModelVersion >> 16) & 0xFF}.{newModelVersion & 0xFFFF}";
        var usingAndVersionTree = SyntaxFactory.ParseSyntaxTree(
            $"global using System;global using System.Linq;global using System.Collections.Generic;global using System.Threading.Tasks;using System.Reflection;using System.Runtime.CompilerServices;using System.Runtime.Versioning;[assembly:TargetFramework(\".NETStandard, Version = v2.1\")][assembly: AssemblyVersion(\"{asmVersion}\")]");
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, false)
            .WithNullableContextOptions(NullableContextOptions.Enable)
            .WithOptimizationLevel(forDebug ? OptimizationLevel.Debug : OptimizationLevel.Release);

        //开始编译运行时代码
        var compilation = CSharpCompilation.Create(docName)
            .AddReferences(GetServiceModelReferences(model))
            .AddSyntaxTrees(newTree, usingAndVersionTree)
            .WithOptions(options);
        if (usagesTree != null)
            compilation = compilation.AddSyntaxTrees(usagesTree);

        EmitResult emitResult;
        byte[]? asmData = null;
        if (forDebug)
        {
            await using var dllStream = new FileStream(Path.Combine(debugFolder!, docName + ".dll"),
                FileMode.CreateNew);
            var emitOpts = new EmitOptions(false, DebugInformationFormat.Embedded);
            //using var pdbStream = new FileStream(Path.Combine(debugFolder, docName + ".pdb"), FileMode.CreateNew);
            emitResult = compilation.Emit(dllStream, null, null, null, null, emitOpts);
        }
        else
        {
            using var dllStream = new MemoryStream(1024);
            await using (var cs = new BrotliStream(dllStream, CompressionMode.Compress, true))
            {
                emitResult = compilation.Emit(cs);
            }

            asmData = dllStream.ToArray();
        }

        if (!emitResult.Success)
        {
            var sb = new StringBuilder("编译错误:");
            sb.AppendLine();
            for (var i = 0; i < emitResult.Diagnostics.Length; i++)
            {
                var error = emitResult.Diagnostics[i];
                sb.AppendFormat("{0}. {1}", i + 1, error);
                sb.AppendLine();
            }

            throw new Exception(sb.ToString());
        }

        return forDebug ? null : asmData;
    }

    /// <summary>
    /// 检查服务模型的设计时代码是否有语义错误
    /// </summary>
    private static void CheckSemanticErrors(SemanticModel semanticModel)
    {
        var diagnostics = semanticModel.GetDiagnostics();
        if (diagnostics.Length <= 0) return;

        var hasError = false;
        var sb = new StringBuilder("语义错误:");
        sb.AppendLine();
        for (var i = 0; i < diagnostics.Length; i++)
        {
            var error = diagnostics[i];
            if (error.WarningLevel != 0) continue;

            hasError = true;
            sb.AppendFormat("{0}. {1} {2}{3}", i + 1, error.WarningLevel,
                error.GetMessage(), Environment.NewLine);
        }

        if (hasError)
            throw new Exception(sb.ToString());
    }

    /// <summary>
    /// 获取服务模型的依赖引用
    /// </summary>
    private static IEnumerable<MetadataReference> GetServiceModelReferences(ServiceModel model)
    {
        var deps = new List<MetadataReference>
        {
            MetadataReferences.CoreLib,
            MetadataReferences.NetstandardLib,
            //MetadataReferences.SystemCoreLib,
            // MetadataReferences.SystemCollectionsLib,
            // MetadataReferences.SystemLinqLib,
            MetadataReferences.SystemRuntimeLib,
            // MetadataReferences.SystemRuntimeExtLib,
            // MetadataReferences.SystemTasksLib,
            MetadataReferences.SystemDataLib,
            // MetadataReferences.ComponentModelPrimitivesLib,
            //MetadataReferences.ComponentModelLib,
            //MetadataReferences.SystemBuffersLib,
            MetadataReferences.AppBoxCoreLib,
            MetadataReferences.AppBoxStoreLib
        };

        if (model.HasReference) //添加其他引用
        {
            throw new NotImplementedException("ServiceModel has references");
            // for (int i = 0; i < model.References.Count; i++)
            // {
            //     deps.Add(MetadataReferences.Get($"{model.References[i]}.dll", appName));
            // }
        }

        return deps;
    }

    private static async ValueTask<DbTransaction> MakeOtherStoreTxn(long storeId,
        IDictionary<long, DbTransaction> txns)
    {
        if (!txns.TryGetValue(storeId, out var txn))
        {
            var sqlStore = SqlStore.Get(storeId);
            var conn = sqlStore.MakeConnection();
            await conn.OpenAsync();
            txn = await conn.BeginTransactionAsync();
            txns[storeId] = txn;
        }

        return txn;
    }

    private static async Task SaveModelsAsync(DesignHub hub, PublishPackage package,
#if FUTURE
            Transaction txn,
#else
        DbTransaction txn,
#endif
        IDictionary<long, DbTransaction> otherStoreTxns)
    {
        //保存文件夹
        foreach (var folder in package.Folders)
        {
            if (folder.IsDeleted)
                await MetaStore.Provider.DeleteFolderAsync(folder, txn);
            else
                await MetaStore.Provider.UpsertFolderAsync(folder, txn);
        }

        //保存模型，注意:
        //1.映射至系统存储的实体模型的变更与删除暂由MetaStore处理，映射至SqlStore的DDL暂在这里处理
        //2.删除的模型同时删除相关代码及编译好的组件，包括视图模型的相关路由
        foreach (var model in package.Models)
        {
            switch (model.PersistentState)
            {
                case PersistentState.Detached:
                {
                    await MetaStore.Provider.InsertModelAsync(model, txn);
                    if (model.ModelType == ModelType.Entity)
                    {
                        var em = (EntityModel)model;
                        if (em.SqlStoreOptions != null) //映射至第三方数据库的需要创建相应的表
                        {
                            var sqlStore = SqlStore.Get(em.SqlStoreOptions.StoreModelId);
                            var sqlTxn = await MakeOtherStoreTxn(em.SqlStoreOptions.StoreModelId,
                                otherStoreTxns);
                            await sqlStore.CreateTableAsync(em, sqlTxn, hub);
                        }
                        // else if (em.CqlStoreOptions != null)
                        // {
                        //     var cqlStore = CqlStore.Get(em.CqlStoreOptions.StoreModelId);
                        //     await cqlStore.CreateTableAsync(em);
                        // }
                    }
                    // else if (model.ModelType == ModelType.View) //TODO:暂在这里保存视图模型的路由
                    // {
                    //     var viewModel = (ViewModel)model;
                    //     if ((viewModel.Flag & ViewModelFlag.ListInRouter) ==
                    //         ViewModelFlag.ListInRouter)
                    //     {
                    //         var app = hub.DesignTree.FindApplicationNode(model.AppId);
                    //         var viewName = $"{app.Model.Name}.{viewModel.Name}";
                    //         await ModelStore.UpsertViewRoute(viewName, viewModel.RouteStoredPath,
                    //             txn);
                    //     }
                    // }

                    break;
                }
                case PersistentState.Unchanged: //TODO:临时
                case PersistentState.Modified:
                {
                    await MetaStore.Provider.UpdateModelAsync(model, txn, hub.GetApplicationModel);
                    if (model.ModelType == ModelType.Entity)
                    {
                        var em = (EntityModel)model;
                        if (em.SqlStoreOptions != null) //映射至第三方数据库的需要变更表
                        {
                            var sqlStore = SqlStore.Get(em.SqlStoreOptions.StoreModelId);
                            var sqlTxn = await MakeOtherStoreTxn(em.SqlStoreOptions.StoreModelId,
                                otherStoreTxns);
                            await sqlStore.AlterTableAsync(em, sqlTxn, hub);
                        }
                        // else if (em.CqlStoreOptions != null)
                        // {
                        //     var cqlStore = CqlStore.Get(em.CqlStoreOptions.StoreModelId);
                        //     await cqlStore.AlterTableAsync(em);
                        // }
                    }
                    //TODO:服务模型重命名删除旧的Assembly
                    // else if (model.ModelType == ModelType.View)
                    // {
                    //     var viewModel = (ViewModel)model;
                    //     var app = hub.DesignTree.FindApplicationNode(model.AppId);
                    //     if ((viewModel.Flag & ViewModelFlag.ListInRouter) ==
                    //         ViewModelFlag.ListInRouter)
                    //     {
                    //         var viewName = $"{app.Model.Name}.{viewModel.Name}";
                    //         //TODO:判断重命名删除旧的
                    //         await ModelStore.UpsertViewRoute(viewName, viewModel.RouteStoredPath,
                    //             txn);
                    //     }
                    //     else
                    //     {
                    //         var oldViewName = $"{app.Model.Name}.{viewModel.OriginalName}";
                    //         await ModelStore.DeleteViewRoute(oldViewName, txn);
                    //     }
                    // }

                    break;
                }
                case PersistentState.Deleted:
                {
                    await MetaStore.Provider.DeleteModelAsync(model, txn, hub.GetApplicationModel);

                    if (model.ModelType == ModelType.Entity)
                    {
                        var em = (EntityModel)model;
                        if (em.SqlStoreOptions != null) //映射至第三方数据库的需要删除相应的表
                        {
                            var sqlStore = SqlStore.Get(em.SqlStoreOptions.StoreModelId);
                            var sqlTxn = await MakeOtherStoreTxn(em.SqlStoreOptions.StoreModelId,
                                otherStoreTxns);
                            await sqlStore.DropTableAsync(em, sqlTxn, hub);
                        }
                        // else if (em.CqlStoreOptions != null)
                        // {
                        //     var cqlStore = CqlStore.Get(em.CqlStoreOptions.StoreModelId);
                        //     await cqlStore.DropTableAsync(em);
                        // }
                    }
                    //判断模型类型删除相关代码及编译好的组件
                    else if (model.ModelType == ModelType.Service)
                    {
                        var app = hub.DesignTree.FindApplicationNode(model.AppId)!;
                        await MetaStore.Provider.DeleteModelCodeAsync(model.Id, txn);
                        await MetaStore.Provider.DeleteAssemblyAsync(MetaAssemblyType.Service,
                            $"{app.Model.Name}.{model.OriginalName}", txn);
                    }
                    else if (model.ModelType == ModelType.View)
                    {
                        var app = hub.DesignTree.FindApplicationNode(model.AppId)!;
                        var oldViewName = $"{app.Model.Name}.{model.OriginalName}";
                        await MetaStore.Provider.DeleteModelCodeAsync(model.Id, txn);
                        // await ModelStore.DeleteAssemblyAsync(MetaAssemblyType.View, oldViewName,
                        //     txn);
                        // await ModelStore.DeleteViewRoute(oldViewName, txn);
                    }
                }
                    break;
            }
        }

        //保存模型相关的代码
        foreach (var modelId in package.SourceCodes.Keys)
        {
            var codeData = package.SourceCodes[modelId];
            await MetaStore.Provider.UpsertModelCodeAsync(modelId, codeData, txn);
        }

        //保存服务模型编译好的运行时组件
        foreach (var serviceName in package.ServiceAssemblies.Keys)
        {
            var asmData = package.ServiceAssemblies[serviceName];
            await MetaStore.Provider.UpsertAssemblyAsync(MetaAssemblyType.Service, serviceName,
                asmData, txn);
        }

        //保存视图模型编译好的运行时代码
        // foreach (var viewName in package.ViewAssemblies.Keys)
        // {
        //     var asmData = package.ViewAssemblies[viewName];
        //     await ModelStore.UpsertAssemblyAsync(MetaAssemblyType.View, viewName, asmData, txn);
        // }
    }

    /// <summary>
    /// 通知各节点模型缓存失效
    /// </summary>
    private static void InvalidModelsCache(DesignHub hub, PublishPackage package)
    {
        if (package.Models.Count == 0)
            return;

        var others = package.Models.Where(t => t.ModelType != ModelType.Service).Select(t => t.Id)
            .ToArray();
        var serviceModels = package.Models.Where(t => t.ModelType == ModelType.Service)
            .Cast<ServiceModel>().ToArray();
        var services = new string[serviceModels.Length];
        for (var i = 0; i < serviceModels.Length; i++)
        {
            var sm = serviceModels[i];
            var app = hub.DesignTree.FindApplicationNode(sm.AppId)!.Model;
            services[i] = serviceModels[i].IsNameChanged
                ? $"{app.Name}.{sm.OriginalName}"
                : $"{app.Name}.{sm.Name}";
        }

        RuntimeContext.Current.InvalidModelsCache(services, others, true);
    }
}