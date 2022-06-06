using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using AppBoxCore;

namespace AppBoxStore;

/// <summary>
/// 存储初始化器，仅用于启动集群第一节点时初始化存储
/// </summary>
internal static class StoreInitiator
{
#if !FUTURE
    internal const ushort PK_Member_Id = 0; //暂为0
#endif

    internal static async Task InitAsync(
#if !FUTURE
        System.Data.Common.DbTransaction txn
#endif
    )
    {
        //TODO:判断是否已初始化
        //新建sys ApplicationModel
        var app = new ApplicationModel("AppBox", Consts.SYS);
#if FUTURE
            await ModelStore.CreateApplicationAsync(app);
#else
        await MetaStore.Provider.CreateApplicationAsync(app, txn);
#endif
        //新建默认文件夹
        var entityRootFolder = new ModelFolder(app.Id, ModelType.Entity);
        var entityOrgUnitsFolder = new ModelFolder(entityRootFolder, "OrgUnits");
        var entityDesignFolder = new ModelFolder(entityRootFolder, "Design");
        var viewRootFolder = new ModelFolder(app.Id, ModelType.View);
        var viewOrgUnitsFolder = new ModelFolder(viewRootFolder, "OrgUnits");
        var viewOperationFolder = new ModelFolder(viewRootFolder, "Operations");
        var viewMetricsFolder = new ModelFolder(viewOperationFolder, "Metrics");
        var viewClusterFolder = new ModelFolder(viewOperationFolder, "Cluster");

        //新建EntityModel
        var emploee = CreateEmploeeModel(app);
        emploee.FolderId = entityOrgUnitsFolder.Id;
        var enterprise = CreateEnterpriseModel(app);
        enterprise.FolderId = entityOrgUnitsFolder.Id;
        var workgroup = CreateWorkgroupModel(app);
        workgroup.FolderId = entityOrgUnitsFolder.Id;
        var orgunit = CreateOrgUnitModel(app);
        orgunit.FolderId = entityOrgUnitsFolder.Id;
        var staged = CreateStagedModel(app);
        staged.FolderId = entityDesignFolder.Id;
        var checkout = CreateCheckoutModel(app);
        checkout.FolderId = entityDesignFolder.Id;

        //新建默认组织
        var defaultEnterprise = new Enterprise(Guid.NewGuid());
        defaultEnterprise.Name = "Future Studio";

        //新建默认系统管理员及测试账号
        var admin = new Employee(Guid.NewGuid());
        admin.Name = admin.Account = "Admin";
        admin.Male = true;
        admin.Birthday = new DateTime(1977, 3, 16);

        var test = new Employee(Guid.NewGuid());
        test.Name = test.Account = "Test";
        test.Male = false;
        test.Birthday = new DateTime(1979, 1, 2);

        var itdept = new Workgroup(Guid.NewGuid());
        itdept.Name = "IT Dept";

        //新建默认组织单元
        var entou = new OrgUnit(Guid.NewGuid());
        entou.Base = defaultEnterprise;

        var itdeptou = new OrgUnit(Guid.NewGuid());
        itdeptou.Base = itdept;

        var adminou = new OrgUnit(Guid.NewGuid());
        adminou.Base = admin;

        var testou = new OrgUnit(Guid.NewGuid());
        testou.Base = test;

        //事务保存
#if FUTURE
            var txn = await Transaction.BeginAsync();
#endif
        await MetaStore.Provider.UpsertFolderAsync(entityRootFolder, txn);
        await MetaStore.Provider.UpsertFolderAsync(viewRootFolder, txn);

        await MetaStore.Provider.InsertModelAsync(emploee, txn);
        await MetaStore.Provider.InsertModelAsync(enterprise, txn);
        await MetaStore.Provider.InsertModelAsync(workgroup, txn);
        await MetaStore.Provider.InsertModelAsync(orgunit, txn);
        await MetaStore.Provider.InsertModelAsync(staged, txn);
        await MetaStore.Provider.InsertModelAsync(checkout, txn);

// #if FUTURE
//             await CreateServiceModel("OrgUnitService", 1, null, true, txn);
// #else
//         await CreateServiceModel("OrgUnitService", 1, null, false, txn);
// #endif
//         await CreateServiceModel("MetricService", 2, null, false, txn,
//             new List<string> { "Newtonsoft.Json", "System.Private.Uri", "System.Net.Http" });
//
//         await CreateViewModel("Home", 1, null, txn);
//         await CreateViewModel("EnterpriseView", 2, viewOrgUnitsFolder.Id, txn);
//         await CreateViewModel("WorkgroupView", 3, viewOrgUnitsFolder.Id, txn);
//         await CreateViewModel("EmploeeView", 4, viewOrgUnitsFolder.Id, txn);
//         await CreateViewModel("PermissionTree", 5, viewOrgUnitsFolder.Id, txn);
//         await CreateViewModel("OrgUnits", 6, viewOrgUnitsFolder.Id, txn);
//
//         await CreateViewModel("CpuUsages", 7, viewMetricsFolder.Id, txn);
//         await CreateViewModel("MemUsages", 8, viewMetricsFolder.Id, txn);
//         await CreateViewModel("NetTraffic", 9, viewMetricsFolder.Id, txn);
//         await CreateViewModel("DiskIO", 10, viewMetricsFolder.Id, txn);
//         await CreateViewModel("NodeMetrics", 11, viewMetricsFolder.Id, txn);
//         await CreateViewModel("InvokeMetrics", 12, viewMetricsFolder.Id, txn);
//
//         await CreateViewModel("GaugeCard", 13, viewClusterFolder.Id, txn);
//         await CreateViewModel("NodesListView", 14, viewClusterFolder.Id, txn);
//         await CreateViewModel("PartsListView", 15, viewClusterFolder.Id, txn);
//         await CreateViewModel("ClusterHome", 16, viewClusterFolder.Id, txn);
//
//         await CreateViewModel("OpsLogin", 17, viewOperationFolder.Id, txn, "ops");
//         await CreateViewModel("OpsHome", 18, viewOperationFolder.Id, txn);

        //插入数据前先设置模型缓存，以防止找不到
        var runtime = (IHostRuntimeContext)RuntimeContext.Current;
        runtime.InjectModel(emploee);
        runtime.InjectModel(enterprise);
        runtime.InjectModel(workgroup);
        runtime.InjectModel(orgunit);
        runtime.InjectModel(staged);
        runtime.InjectModel(checkout);

#if FUTURE
            await EntityStore.InsertEntityAsync(defaultEnterprise, txn);
            await EntityStore.InsertEntityAsync(itdept, txn);
            await EntityStore.InsertEntityAsync(admin, txn);
            await EntityStore.InsertEntityAsync(test, txn);
            await EntityStore.InsertEntityAsync(entou, txn);
            await EntityStore.InsertEntityAsync(itdeptou, txn);
            await EntityStore.InsertEntityAsync(adminou, txn);
            await EntityStore.InsertEntityAsync(testou, txn);
#else
        var ctx = new InitDesignContext(app);
        ctx.AddEntityModel(emploee);
        ctx.AddEntityModel(enterprise);
        ctx.AddEntityModel(workgroup);
        ctx.AddEntityModel(orgunit);
        ctx.AddEntityModel(staged);
        ctx.AddEntityModel(checkout);

        await SqlStore.Default.CreateTableAsync(emploee, txn, ctx);
        await SqlStore.Default.CreateTableAsync(enterprise, txn, ctx);
        await SqlStore.Default.CreateTableAsync(workgroup, txn, ctx);
        await SqlStore.Default.CreateTableAsync(orgunit, txn, ctx);
        await SqlStore.Default.CreateTableAsync(staged, txn, ctx);
        await SqlStore.Default.CreateTableAsync(checkout, txn, ctx);

        await SqlStore.Default.InsertAsync(defaultEnterprise, txn);
        await SqlStore.Default.InsertAsync(itdept, txn);
        await SqlStore.Default.InsertAsync(admin, txn);
        await SqlStore.Default.InsertAsync(test, txn);
        await SqlStore.Default.InsertAsync(entou, txn);
        await SqlStore.Default.InsertAsync(itdeptou, txn);
        await SqlStore.Default.InsertAsync(adminou, txn);
        await SqlStore.Default.InsertAsync(testou, txn);
#endif

        //添加权限模型在保存OU实例之后
//         var admin_permission = new PermissionModel(Consts.SYS_PERMISSION_ADMIN_ID, "Admin");
//         admin_permission.Remark = "System administrator";
// #if FUTURE
//             admin_permission.OrgUnits.Add(adminou.Id);
// #else
//         admin_permission.OrgUnits.Add(adminou.GetGuid(PK_Member_Id));
// #endif
//         var developer_permission =
//             new PermissionModel(Consts.SYS_PERMISSION_DEVELOPER_ID, "Developer");
//         developer_permission.Remark = "System developer";
// #if FUTURE
//             developer_permission.OrgUnits.Add(itdeptou.Id);
// #else
//         developer_permission.OrgUnits.Add(itdeptou.GetGuid(PK_Member_Id));
// #endif
//         await ModelStore.InsertModelAsync(admin_permission, txn);
//         await ModelStore.InsertModelAsync(developer_permission, txn);

#if FUTURE
            await txn.CommitAsync();
#endif
    }

    private static EntityModel CreateEmploeeModel(ApplicationModel app)
    {
#if FUTURE
            var emploee =
 new EntityModel(Consts.SYS_EMPLOEE_MODEL_ID, Consts.EMPLOEE, EntityStoreType.StoreWithMvcc);
#else
        var emploee = new EntityModel(Employee.MODELID, nameof(Employee));
        emploee.BindToSqlStore(SqlStore.DefaultSqlStoreId);

        var id = new DataFieldModel(emploee, nameof(Employee.Id), DataFieldType.Guid, false);
        emploee.AddSysMember(id, Employee.ID_ID);
        //add pk
        emploee.SqlStoreOptions!.SetPrimaryKeys(new[] { new FieldWithOrder(id.MemberId) });
#endif

        //Add members
        var name = new DataFieldModel(emploee, nameof(Employee.Name), DataFieldType.String, false);
#if !FUTURE
        name.Length = 20;
#endif
        emploee.AddSysMember(name, Employee.NAME_ID);

        var male = new DataFieldModel(emploee, nameof(Employee.Male), DataFieldType.Bool, false);
        emploee.AddSysMember(male, Employee.MALE_ID);

        var birthday = new DataFieldModel(emploee, nameof(Employee.Birthday),
            DataFieldType.DateTime, true);
        emploee.AddSysMember(birthday, Employee.BIRTHDAY_ID);

        var account =
            new DataFieldModel(emploee, nameof(Employee.Account), DataFieldType.String, true);
        emploee.AddSysMember(account, Employee.ACCOUNT_ID);

        var password =
            new DataFieldModel(emploee, nameof(Employee.Password), DataFieldType.Binary, true);
        emploee.AddSysMember(password, Employee.PASSWORD_ID);

        // var orgunits = new EntitySetModel(emploee, "OrgUnits", OrgUnit.MODELID, OrgUnit.BASE_ID);
        // emploee.AddSysMember(orgunits, Employee.);

        //Add indexes
#if FUTURE
            var ui_account = new EntityIndexModel(emploee, "UI_Account", true,
                                                       new FieldWithOrder[] { new FieldWithOrder(Consts.EMPLOEE_ACCOUNT_ID) },
                                                       new ushort[] { Consts.EMPLOEE_PASSWORD_ID });
            emploee.SysStoreOptions.AddSysIndex(emploee, ui_account, Consts.EMPLOEE_UI_ACCOUNT_ID);
#else
        var ui_account = new SqlIndexModel(emploee, "UI_Account", true,
            new[] { new FieldWithOrder(Employee.ACCOUNT_ID) },
            new[] { Employee.PASSWORD_ID });
        emploee.SqlStoreOptions.AddIndex(ui_account);
#endif

        return emploee;
    }

    private static EntityModel CreateEnterpriseModel(ApplicationModel app)
    {
#if FUTURE
            var model =
 new EntityModel(Consts.SYS_ENTERPRISE_MODEL_ID, Consts.ENTERPRISE, EntityStoreType.StoreWithMvcc);
#else
        var model = new EntityModel(Enterprise.MODELID, nameof(Enterprise));
        model.BindToSqlStore(SqlStore.DefaultSqlStoreId);

        var id = new DataFieldModel(model, nameof(Enterprise.Id), DataFieldType.Guid, false);
        model.AddSysMember(id, Enterprise.ID_ID);
        //add pk
        model.SqlStoreOptions!.SetPrimaryKeys(new[] { new FieldWithOrder(id.MemberId) });
#endif

        var name = new DataFieldModel(model, nameof(Enterprise.Name), DataFieldType.String, false);
#if !FUTURE
        name.Length = 100;
#endif
        model.AddSysMember(name, Enterprise.NAME_ID);

        var address =
            new DataFieldModel(model, nameof(Enterprise.Address), DataFieldType.String, true);
        model.AddSysMember(address, Enterprise.ADDRESS_ID);

        return model;
    }

    private static EntityModel CreateWorkgroupModel(ApplicationModel app)
    {
#if FUTURE
            var model =
 new EntityModel(Consts.SYS_WORKGROUP_MODEL_ID, Consts.WORKGROUP, EntityStoreType.StoreWithMvcc);
#else
        var model = new EntityModel(Workgroup.MODELID, nameof(Workgroup));
        model.BindToSqlStore(SqlStore.DefaultSqlStoreId);

        var id = new DataFieldModel(model, nameof(Workgroup.Id), DataFieldType.Guid, false);
        model.AddSysMember(id, Workgroup.ID_ID);
        //add pk
        model.SqlStoreOptions!.SetPrimaryKeys(new[] { new FieldWithOrder(id.MemberId) });
#endif

        var name = new DataFieldModel(model, nameof(Workgroup.Name), DataFieldType.String, false);
#if !FUTURE
        name.Length = 50;
#endif
        model.AddSysMember(name, Workgroup.NAME_ID);

        return model;
    }

    private static EntityModel CreateOrgUnitModel(ApplicationModel app)
    {
        DataFieldType fkType;
#if FUTURE
            var model =
 new EntityModel(Consts.SYS_ORGUNIT_MODEL_ID, Consts.ORGUNIT, EntityStoreType.StoreWithMvcc);
            fkType = EntityFieldType.EntityId;
#else
        fkType = DataFieldType.Guid;
        var model = new EntityModel(OrgUnit.MODELID, nameof(OrgUnit));
        model.BindToSqlStore(SqlStore.DefaultSqlStoreId);

        var id = new DataFieldModel(model, nameof(OrgUnit.Id), DataFieldType.Guid, false);
        model.AddSysMember(id, OrgUnit.ID_ID);
        //add pk
        model.SqlStoreOptions!.SetPrimaryKeys(new[] { new FieldWithOrder(id.MemberId) });
#endif

        var name = new DataFieldModel(model, nameof(OrgUnit.Name), DataFieldType.String, false);
#if !FUTURE
        name.Length = 100;
#endif
        model.AddSysMember(name, OrgUnit.NAME_ID);

        var baseId = new DataFieldModel(model, nameof(OrgUnit.BaseId), fkType, false);
        model.AddSysMember(baseId, OrgUnit.BASEID_ID);
        var baseType =
            new DataFieldModel(model, nameof(OrgUnit.BaseType), DataFieldType.Long, false);
        model.AddSysMember(baseType, OrgUnit.BASETYPE_ID);
        var Base = new EntityRefModel(model, nameof(OrgUnit.Base),
            new List<long> { Enterprise.MODELID, Workgroup.MODELID, Employee.MODELID },
            new short[] { baseId.MemberId }, baseType.MemberId);
        model.AddSysMember(Base, OrgUnit.BASE_ID);

        var parentId = new DataFieldModel(model, nameof(OrgUnit.ParentId), fkType, true);
        model.AddSysMember(parentId, OrgUnit.PARENTID_ID);
        var parent = new EntityRefModel(model, nameof(OrgUnit.Parent), OrgUnit.MODELID,
            new short[] { parentId.MemberId });
        model.AddSysMember(parent, OrgUnit.PARENT_ID);

        var children =
            new EntitySetModel(model, nameof(OrgUnit.Children), OrgUnit.MODELID, parent.MemberId);
        model.AddSysMember(children, OrgUnit.CHILDREN_ID);

        return model;
    }

    private static EntityModel CreateStagedModel(ApplicationModel app)
    {
#if FUTURE
            var model =
 new EntityModel(Consts.SYS_STAGED_MODEL_ID, "StagedModel", EntityStoreType.StoreWithoutMvcc);
#else
        var model = new EntityModel(StagedModel.MODELID, nameof(StagedModel));
        model.BindToSqlStore(SqlStore.DefaultSqlStoreId);

#endif

        var type = new DataFieldModel(model, "Type", DataFieldType.Byte, false);
        model.AddSysMember(type, StagedModel.TYPE_ID);

        var modelId = new DataFieldModel(model, "ModelId", DataFieldType.String, false);
#if !FUTURE
        modelId.Length = 100;
#endif
        model.AddSysMember(modelId, StagedModel.MODEL_ID);

        var devId = new DataFieldModel(model, "DeveloperId", DataFieldType.Guid, false);
        model.AddSysMember(devId, StagedModel.DEVELOPER_ID);

        var data = new DataFieldModel(model, "Data", DataFieldType.Binary, true);
        model.AddSysMember(data, StagedModel.DATA_ID);

#if !FUTURE
        //add pk
        model.SqlStoreOptions!.SetPrimaryKeys(new[]
        {
            new FieldWithOrder(devId.MemberId),
            new FieldWithOrder(type.MemberId),
            new FieldWithOrder(modelId.MemberId)
        });
#endif

        return model;
    }

    private static EntityModel CreateCheckoutModel(ApplicationModel app)
    {
#if FUTURE
            var model =
 new EntityModel(Consts.SYS_CHECKOUT_MODEL_ID, "Checkout", EntityStoreType.StoreWithoutMvcc);
#else
        var model = new EntityModel(Checkout.MODELID, nameof(Checkout));
        model.BindToSqlStore(SqlStore.DefaultSqlStoreId);
#endif

        var nodeType = new DataFieldModel(model, "NodeType", DataFieldType.Byte, false);
        model.AddSysMember(nodeType, Checkout.NODETYPE_ID);

        var targetId = new DataFieldModel(model, "TargetId", DataFieldType.String, false);
#if !FUTURE
        targetId.Length = 100;
#endif
        model.AddSysMember(targetId, Checkout.TARGET_ID);

        var devId = new DataFieldModel(model, "DeveloperId", DataFieldType.Guid, false);
        model.AddSysMember(devId, Checkout.DEVELOPER_ID);

        var devName = new DataFieldModel(model, "DeveloperName", DataFieldType.String, false);
#if !FUTURE
        devName.Length = 100;
#endif
        model.AddSysMember(devName, Checkout.DEVELOPERNAME_ID);

        var version = new DataFieldModel(model, "Version", DataFieldType.Int, false);
        model.AddSysMember(version, Checkout.VERSION_ID);

        //Add indexes
#if FUTURE
            var ui_nodeType_targetId = new EntityIndexModel(model, "UI_NodeType_TargetId", true,
                                                            new FieldWithOrder[]
            {
                new FieldWithOrder(Consts.CHECKOUT_NODETYPE_ID),
                new FieldWithOrder(Consts.CHECKOUT_TARGETID_ID)
            });
            model.SysStoreOptions.AddSysIndex(model, ui_nodeType_targetId, Consts.CHECKOUT_UI_NODETYPE_TARGETID_ID);
#else
        var ui_nodeType_targetId = new SqlIndexModel(model, "UI_NodeType_TargetId", true,
            new[]
            {
                new FieldWithOrder(Checkout.NODETYPE_ID),
                new FieldWithOrder(Checkout.TARGET_ID)
            });
        model.SqlStoreOptions!.AddIndex(ui_nodeType_targetId);

        //add pk
        model.SqlStoreOptions.SetPrimaryKeys(new[]
        {
            new FieldWithOrder(devId.MemberId),
            new FieldWithOrder(nodeType.MemberId),
            new FieldWithOrder(targetId.MemberId)
        });
#endif
        return model;
    }

//     private static async Task CreateServiceModel(string name, ulong idIndex, Guid? folderId,
//         bool forceFuture,
// #if FUTURE
//             Transaction txn,
// #else
//         System.Data.Common.DbTransaction txn,
// #endif
//         List<string> references = null)
//     {
//         var modelId = ((ulong)Consts.SYS_APP_ID << IdUtil.MODELID_APPID_OFFSET)
//                       | ((ulong)ModelType.Service << IdUtil.MODELID_TYPE_OFFSET) |
//                       (idIndex << IdUtil.MODELID_SEQ_OFFSET);
//         var model = new ServiceModel(modelId, name) { FolderId = folderId };
//         if (references != null)
//             model.References.AddRange(references);
//         await ModelStore.InsertModelAsync(model, txn);
//
//         var codeRes = forceFuture
//             ? $"Resources.Services.{name}_Future.cs"
//             : $"Resources.Services.{name}.cs";
//         var asmRes = forceFuture
//             ? $"Resources.Services.{name}_Future.dll"
//             : $"Resources.Services.{name}.dll";
//
//         var serviceCode = Resources.GetString(codeRes);
//         var codeData = ModelCodeUtil.EncodeServiceCode(serviceCode, null);
//         await ModelStore.UpsertModelCodeAsync(model.Id, codeData, txn);
//
//         var asmData = Resources.GetBytes(asmRes);
//         await ModelStore.UpsertAssemblyAsync(MetaAssemblyType.Service, $"sys.{name}", asmData, txn);
//     }

//     private static async Task CreateViewModel(string name, ulong idIndex, Guid? folderId,
// #if FUTURE
//             Transaction txn,
// #else
//         System.Data.Common.DbTransaction txn,
// #endif
//         string routePath = null)
//     {
//         var modelId = ((ulong)Consts.SYS_APP_ID << IdUtil.MODELID_APPID_OFFSET)
//                       | ((ulong)ModelType.View << IdUtil.MODELID_TYPE_OFFSET) |
//                       (idIndex << IdUtil.MODELID_SEQ_OFFSET);
//         var model = new ViewModel(modelId, name) { FolderId = folderId };
//         if (!string.IsNullOrEmpty(routePath))
//         {
//             model.Flag = ViewModelFlag.ListInRouter;
//             model.RoutePath = routePath;
//             await ModelStore.UpsertViewRoute($"sys.{model.Name}", model.RouteStoredPath, txn);
//         }
//
//         await ModelStore.InsertModelAsync(model, txn);
//
//         var templateCode = Resources.GetString($"Resources.Views.{name}.html");
//         var scriptCode = Resources.GetString($"Resources.Views.{name}.js");
//         var styleCode = Resources.GetString($"Resources.Views.{name}.css");
//         var codeData = ModelCodeUtil.EncodeViewCode(templateCode, scriptCode, styleCode);
//         await ModelStore.UpsertModelCodeAsync(model.Id, codeData, txn);
//
//         var runtimeCode = Resources.GetString($"Resources.Views.{name}.json");
//         var runtimeCodeData = ModelCodeUtil.EncodeViewRuntimeCode(runtimeCode);
//         await ModelStore.UpsertAssemblyAsync(MetaAssemblyType.View, $"sys.{name}", runtimeCodeData,
//             txn);
//     }
}

#if !FUTURE
/// <summary>
/// 仅用于初始化默认存储
/// </summary>
internal sealed class InitDesignContext : IDesignContext
{
    private readonly ApplicationModel _sysApp;
    private readonly Dictionary<long, EntityModel> _models;

    public InitDesignContext(ApplicationModel app)
    {
        _sysApp = app;
        _models = new Dictionary<long, EntityModel>(8);
    }

    internal void AddEntityModel(EntityModel model)
    {
        _models.Add(model.Id, model);
    }

    public ApplicationModel GetApplicationModel(int appId)
    {
        Debug.Assert(_sysApp.Id == appId);
        return _sysApp;
    }

    public EntityModel GetEntityModel(ModelId modelID)
    {
        return _models[modelID];
    }
}
#endif