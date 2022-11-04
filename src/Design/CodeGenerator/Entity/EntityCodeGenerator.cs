using System.Text;
using AppBoxCore;
using AppBoxStore;

namespace AppBoxDesign;

internal static class EntityCodeGenerator
{
    #region ====Web====

    /// <summary>
    /// 生成实体模型的Web代码
    /// </summary>
    internal static string GenWebCode(EntityModel model, string appName, bool forPreview)
    {
        var sb = StringBuilderCache.Acquire();
        if (forPreview)
#if DEBUG
            sb.Append("import * as AppBoxCore from '/src/AppBoxCore/index.ts'\n\n");
#else
            sb.Append("import * as AppBoxCore from '/AppBoxCore.js'\n\n");
#endif
        else
            throw new NotImplementedException("Not for preview");

        sb.Append($"export class {model.Name}");
        //根据存储配置继承不同的基类
        sb.Append(model.DataStoreKind == DataStoreKind.None
            ? " extends AppBoxCore.Entity"
            : " extends AppBoxCore.DbEntity");

        sb.Append("\n{\n"); //class start

        // 实体成员
        foreach (var member in model.Members)
        {
            switch (member.Type)
            {
                case EntityMemberType.EntityField:
                    GenWebEntityFieldMember((EntityFieldModel)member, sb);
                    break;
                default:
                    throw new NotImplementedException(member.Type.ToString());
            }
        }

        // override ModelId
        sb.Append("\n\tget ModelId() {return ");
        sb.Append(model.Id.Value.ToString());
        sb.Append("n;}\n");

        // override ReadFrom()
        sb.Append("\n\tReadFrom(rs){\n");
        sb.Append("\t\twhile(true){\n");
        sb.Append("\t\t\tlet mid=rs.ReadShort();\n");
        sb.Append("\t\t\tif(mid===0) break;\n");
        sb.Append("\t\t\tswitch(mid){\n");

        foreach (var member in model.Members)
        {
            sb.Append("\t\t\t\tcase ");
            sb.Append(member.MemberId.ToString());
            sb.Append(':');
            switch (member.Type)
            {
                case EntityMemberType.EntityField:
                    sb.Append("this.");
                    if (model.DataStoreKind != DataStoreKind.None)
                        sb.Append('_');
                    sb.Append(member.Name);
                    sb.Append("=rs.Read");
                    sb.Append(GetEntityMemberWriteReadType(member));
                    sb.Append("();break;\n");
                    break;
                default:
                    throw new NotImplementedException(member.Type.ToString());
            }
        }

        sb.Append("\t\t\t\tdefault: throw new Error();\n");

        sb.Append("\t\t\t}\n"); //end switch
        sb.Append("\t\t}\n"); //end while
        sb.Append("\t}\n"); //end ReadFrom()

        // override WriteTo()
        sb.Append("\n\tWriteTo(ws){\n");
        foreach (var member in model.Members)
        {
            sb.Append("\t\t");
            if (member.AllowNull)
            {
                sb.Append("if (this.");
                if (model.DataStoreKind != DataStoreKind.None)
                    sb.Append('_');
                sb.Append(member.Name);
                sb.Append(" != null){ ");
            }

            switch (member.Type)
            {
                case EntityMemberType.EntityField:
                    sb.Append("ws.WriteShort(");
                    sb.Append(member.MemberId.ToString());
                    sb.Append("); ");

                    sb.Append("ws.Write");
                    sb.Append(GetEntityMemberWriteReadType(member));
                    sb.Append("(this.");
                    if (model.DataStoreKind != DataStoreKind.None)
                        sb.Append('_');
                    sb.Append(member.Name);
                    sb.Append(");");
                    break;
                default:
                    throw new NotImplementedException(member.Type.ToString());
            }

            if (member.AllowNull) sb.Append(" }\n");
            else sb.Append('\n');
        }

        sb.Append("\t\tws.WriteShort(0);\n");
        sb.Append("\t}\n"); //end WriteTo()

        sb.Append("}\n"); //class end
        return StringBuilderCache.GetStringAndRelease(sb);
    }

    private static void GenWebEntityFieldMember(EntityFieldModel field, StringBuilder sb)
    {
        //TODO:默认值生成
        if (field.Owner.DataStoreKind == DataStoreKind.None)
        {
            sb.Append($"\t{field.Name};\n");
        }
        else
        {
            sb.Append($"\t_{field.Name}; ");
            sb.Append($"get {field.Name}() {{return this._{field.Name}}} ");
            sb.Append($"set {field.Name}(value) {{");
            //TODO: check equals and OnPropertyChanged
            sb.Append($"this._{field.Name}=value;");
            sb.Append("}\n");
        }
    }

    #endregion

    #region ====Entity for runtime====

    /// <summary>
    /// 生成实体模型的运行时代码
    /// </summary>
    internal static string GenRuntimeCode(ModelNode modelNode)
    {
        var appName = modelNode.AppNode.Model.Name;
        var model = (EntityModel)modelNode.Model;

        var sb = StringBuilderCache.Acquire();
        sb.Append("using System;\n");
        sb.Append("using System.Collections.Generic;\n");
        sb.Append("using System.Threading.Tasks;\n");
        sb.Append("using AppBoxCore;\n\n");
        sb.Append($"namespace {appName}.Entities;\n");

        sb.Append($"[EntityModelIdAttribute({model.Id.Value}L)]\n");
        sb.Append($"public sealed class {model.Name} : {GetEntityBaseClass(model)}");
        sb.Append("\n{\n"); //class start

        // 实体成员
        foreach (var member in model.Members)
        {
            switch (member.Type)
            {
                case EntityMemberType.EntityField:
                    GenEntityFieldMember((EntityFieldModel)member, sb);
                    break;
                case EntityMemberType.EntityRef:
                    GenEntityRefMember((EntityRefModel)member, sb, modelNode.DesignTree!);
                    break;
                case EntityMemberType.EntitySet:
                    GenEntitySetMember((EntitySetModel)member, sb, modelNode.DesignTree!);
                    break;
                default:
                    throw new NotImplementedException(member.Type.ToString());
            }
        }

        // override ModelId
        sb.Append($"public const long MODELID={model.Id.Value}L;\n");
        sb.Append("public override ModelId ModelId => MODELID;\n");

        // override AllMembers
        sb.Append("private static readonly short[] MemberIds={");
        for (var i = 0; i < model.Members.Count; i++)
        {
            if (i != 0) sb.Append(',');
            sb.Append(model.Members[i].MemberId.ToString());
        }

        sb.Append("};\nprotected override short[] AllMembers => MemberIds;\n");

        // override WriteMember & ReadMember
        GenOverrideWriteMember(model, sb);
        GenOverrideReadMember(model, sb, modelNode.DesignTree!);

        // 存储方法Insert/Update/Delete/Fetch
        GenStoreCRUDMethods(model, sb);

        sb.Append("}\n"); //class end
        return StringBuilderCache.GetStringAndRelease(sb);
    }

    private static void GenEntityFieldMember(EntityFieldModel field, StringBuilder sb)
    {
        var typeString = GetEntityFieldTypeString(field);
        if (field.Owner.DataStoreKind == DataStoreKind.None)
        {
            sb.Append($"\tpublic {typeString} {field.Name} {{get; set;}}\n");
        }
        else
        {
            sb.Append($"\tprivate {typeString} _{field.Name};\n");
            sb.Append($"\tpublic {typeString} {field.Name}\n");
            sb.Append("\t{\n"); //prop start
            sb.Append($"\t\tget => _{field.Name};\n");

            sb.Append("\t\tset\n");
            sb.Append("\t\t{\n"); //prop set start

            //暂判断是主键且非新建状态抛异常
            if (field.IsPrimaryKey)
            {
                sb.Append($"\t\t\tif (PersistentState != PersistentState.Detached) throw new NotSupportedException();\n");
            }

            sb.Append($"\t\t\tif (_{field.Name} == value) return;\n");
            sb.Append($"\t\t\t_{field.Name} = value;\n");
            //TODO:如果是主键调用OnPkChanged()跟踪旧值
            sb.Append($"\t\t\tOnPropertyChanged({field.MemberId});\n");
            sb.Append("\t\t}\n"); //prop set end

            sb.Append("\t}\n"); //prop end
        }
    }

    private static void GenEntityRefMember(EntityRefModel entityRef, StringBuilder sb,
        DesignTree tree)
    {
        var refModelNode = tree.FindModelNode(entityRef.RefModelIds[0])!;
        var typeString = entityRef.IsAggregationRef
            ? GetEntityBaseClass(entityRef.Owner)
            : $"{refModelNode.AppNode.Model.Name}.Entities.{refModelNode.Model.Name}";

        var fieldName = entityRef.Name;
        sb.Append($"\tprivate {typeString}? _{fieldName};\n");
        sb.Append($"\tpublic {typeString}? {fieldName}\n");
        sb.Append("\t{\n"); //prop start
        sb.Append($"\t\tget => _{fieldName};\n");

        sb.Append("\t\tset\n");
        sb.Append("\t\t{\n"); //prop set start
        sb.Append($"\t\t\t_{fieldName} = value");
        if (!entityRef.AllowNull) sb.Append(" ?? throw new ArgumentNullException()");
        sb.Append(";\n");

        //同步设置聚合引用类型的成员的值及外键成员的值
        if (entityRef.Owner.DataStoreKind == DataStoreKind.Sql)
        {
            if (entityRef.IsAggregationRef)
            {
                var typeMember = entityRef.Owner.GetMember(entityRef.TypeMemberId)!;
                sb.Append("\t\t\tswitch (value) {\n");
                foreach (var refModelId in entityRef.RefModelIds)
                {
                    refModelNode = tree.FindModelNode(refModelId)!;
                    var refModel = (EntityModel)refModelNode.Model;
                    var refPks = refModel.SqlStoreOptions!.PrimaryKeys;

                    sb.Append(
                        $"\t\t\tcase {refModelNode.AppNode.Model.Name}.Entities.{refModel.Name} _{refModel.Name}:\n");
                    sb.Append($"\t\t\t\t{typeMember.Name} = {refModel.Id.ToString()}L;\n");
                    for (var i = 0; i < entityRef.FKMemberIds.Length; i++)
                    {
                        var fkMember =
                            (EntityFieldModel)entityRef.Owner.GetMember(entityRef.FKMemberIds[i])!;
                        if (fkMember.IsPrimaryKey) continue; //TODO:暂OrgUnit特例
                        var pkMember = refModel.GetMember(refPks[i].MemberId)!;
                        sb.Append(
                            $"\t\t\t\t{fkMember.Name} = _{refModel.Name}.{pkMember.Name};\n");
                    }

                    sb.Append("\t\t\t\tbreak;\n");
                }

                sb.Append("\t\t\tdefault: throw new ArgumentException();\n");
                sb.Append("\t\t\t}\n");
            }
            else
            {
                var refModel = (EntityModel)refModelNode.Model;
                var refPks = refModel.SqlStoreOptions!.PrimaryKeys;
                for (var i = 0; i < entityRef.FKMemberIds.Length; i++)
                {
                    var fkMember = entityRef.Owner.GetMember(entityRef.FKMemberIds[i])!;
                    var pkMember = refModel.GetMember(refPks[i].MemberId)!;
                    sb.Append($"\t\t\t{fkMember.Name} = value.{pkMember.Name};\n");
                }
            }
        }
        else
        {
            throw new NotImplementedException("生成实体的EntityRef成员代码");
        }

        //TODO: DbEntity.OnPropertyChanged
        sb.Append("\t\t}\n"); //prop set end

        sb.Append("\t}\n"); //prop end
    }

    private static void GenEntitySetMember(EntitySetModel entitySet, StringBuilder sb,
        DesignTree tree)
    {
        var refNode = tree.FindModelNode(entitySet.RefModelId)!;
        var refModel = (EntityModel)refNode.Model;
        var typeString = $"{refNode.AppNode.Model.Name}.Entities.{refModel.Name}";
        var fieldName = entitySet.Name;

        sb.Append($"\tprivate IList<{typeString}>? _{fieldName};\n");
        sb.Append($"\tpublic IList<{typeString}>? {fieldName}\n");
        sb.Append("\t{\n"); //prop start
        sb.Append("\t\tget {\n");
        sb.Append(
            $"\t\t\tif (_{fieldName} == null && PersistentState == PersistentState.Detached)\n");
        sb.Append($"\t\t\t\t_{fieldName} = new List<{typeString}>();\n");
        sb.Append($"\t\t\treturn _{fieldName};\n");
        sb.Append("\t\t}\n");

        sb.Append("\t}\n"); //prop end
    }

    private static void GenOverrideWriteMember(EntityModel model, StringBuilder sb)
    {
        sb.Append(
            "protected override void WriteMember(short id, IEntityMemberWriter ws, int flags){\n");
        sb.Append("\tswitch(id){\n");
        foreach (var member in model.Members)
        {
            sb.Append("\t\tcase ");
            sb.Append(member.MemberId.ToString());
            sb.Append(": ws.Write");
            sb.Append(GetEntityMemberWriteReadType(member));
            sb.Append("Member(id,");
            if (model.StoreOptions != null) sb.Append('_');
            sb.Append(member.Name);
            sb.Append(",flags);break;\n");
        }

        sb.Append(
            "\t\tdefault: throw new SerializationException(SerializationError.UnknownEntityMember,nameof(");
        sb.Append(model.Name);
        sb.Append("));\n");
        sb.Append("\t}\n"); //end switch
        sb.Append("}\n"); //end WriteMember
    }

    private static void GenOverrideReadMember(EntityModel model, StringBuilder sb, DesignTree tree)
    {
        sb.Append(
            "protected override void ReadMember(short id, IEntityMemberReader rs, int flags){\n");
        sb.Append("\tswitch(id){\n");
        foreach (var member in model.Members)
        {
            sb.Append("\t\tcase ");
            sb.Append(member.MemberId.ToString());
            sb.Append(":");
            if (model.StoreOptions != null) sb.Append('_');
            sb.Append(member.Name);
            sb.Append("=rs.Read");
            sb.Append(GetEntityMemberWriteReadType(member));
            sb.Append("Member");
            switch (member.Type)
            {
                case EntityMemberType.EntityField:
                    sb.Append("(flags);break;\n");
                    break;
                case EntityMemberType.EntityRef:
                    {
                        var entityRef = (EntityRefModel)member;
                        if (entityRef.IsAggregationRef)
                        {
                            var typeMember = model.GetMember(entityRef.TypeMemberId)!;
                            sb.Append($"<{GetEntityBaseClass(model)}>");
                            sb.Append($"(flags, () => _{typeMember.Name} switch {{\n");
                            foreach (var refModelId in entityRef.RefModelIds)
                            {
                                var refNode = tree.FindModelNode(refModelId)!;
                                var refModel = (EntityModel)refNode.Model;
                                var refModelName =
                                    $"{refNode.AppNode.Model.Name}.Entities.{refModel.Name}";
                                sb.Append(
                                    $"\t\t\t{refModel.Id.ToString()}L => new {refModelName}(),\n");
                            }

                            sb.Append("\t\t\t_ => throw new Exception()\n");

                            sb.Append("\t\t\t});break;\n");
                        }
                        else
                        {
                            var refModelId = entityRef.RefModelIds[0];
                            var refNode = tree.FindModelNode(refModelId)!;
                            var refModel = (EntityModel)refNode.Model;
                            var refModelName = $"{refNode.AppNode.Model.Name}.Entities.{refModel.Name}";
                            sb.Append($"(flags, () => new {refModelName}());break;\n");
                        }

                        break;
                    }
                case EntityMemberType.EntitySet:
                    {
                        var entitySet = (EntitySetModel)member;
                        var refNode = tree.FindModelNode(entitySet.RefModelId)!;
                        var refModel = (EntityModel)refNode.Model;
                        var refModelName = $"{refNode.AppNode.Model.Name}.Entities.{refModel.Name}";
                        sb.Append($"(flags, () => new {refModelName}());break;\n");

                        break;
                    }
                default: throw new NotImplementedException();
            }
        }

        sb.Append(
            "\t\tdefault: throw new SerializationException(SerializationError.UnknownEntityMember,nameof(");
        sb.Append(model.Name);
        sb.Append("));\n");
        sb.Append("\t}\n"); //end switch
        sb.Append("}\n"); //end ReadMember
    }

    /// <summary>
    /// 生成服务端运行时的存储方法
    /// </summary>
    private static void GenStoreCRUDMethods(EntityModel model, StringBuilder sb)
    {
        if (model.SqlStoreOptions == null) return;

        sb.Append("#if __HOSTRUNTIME__\n");
        // GetSqlStore
        sb.Append("public SqlStore GetSqlStore() =>");
        GenSqlStoreGetMethod(model.SqlStoreOptions, sb);
        sb.Append(";\n\n");
        
        // InsertAsync
        sb.Append(
            "public Task<int> InsertAsync(System.Data.Common.DbTransaction? txn=null) =>\n");
        GenSqlStoreGetMethod(model.SqlStoreOptions, sb);
        sb.Append(".InsertAsync(this,txn);\n\n");

        // FetchAsync
        GenStoreFetchMethod(model, sb, true);

        //TODO: others
        sb.Append("#else\n");
        //生成internal版本的FetchAsync方法,防止前端工程看见此方法
        GenStoreFetchMethod(model, sb, false); 
        sb.Append("#endif\n");
    }

    private static void GenStoreFetchMethod(EntityModel model, StringBuilder sb, bool forRuntime)
    {
        if (!model.SqlStoreOptions!.HasPrimaryKeys) return;

        var pks = model.SqlStoreOptions.PrimaryKeys;

        sb.Append(forRuntime ? "public" : "internal");
        sb.Append($" static Task<{model.Name}?> FetchAsync(");
        for (var i = 0; i < pks.Length; i++)
        {
            if (i != 0) sb.Append(',');
            var dfm = (EntityFieldModel)model.GetMember(pks[i].MemberId)!;
            sb.Append(GetEntityFieldTypeString(dfm));
            sb.Append(' ');
            sb.Append(CodeUtil.ToLowCamelCase(dfm.Name));
        }
        sb.Append(", System.Data.Common.DbTransaction? txn=null) => \n");

        if (forRuntime)
        {
            GenSqlStoreGetMethod(model.SqlStoreOptions, sb);
            sb.Append(".FetchAsync(new ");
            sb.Append(model.Name);
            sb.Append("{");
            for (var i = 0; i < pks.Length; i++)
            {
                if (i != 0) sb.Append(',');
                var dfm = (EntityFieldModel)model.GetMember(pks[i].MemberId)!;
                sb.Append($"{dfm.Name} = {CodeUtil.ToLowCamelCase(dfm.Name)}");
            }
            sb.Append("}, txn);\n");
        }
        else
        {
            sb.Append("\tthrow new Exception();\n");
        }
    }

    /// <summary>
    /// 根据实体模型的存储配置获取继承的基类
    /// </summary>
    private static string GetEntityBaseClass(EntityModel model)
    {
        return model.DataStoreKind switch
        {
            DataStoreKind.None => nameof(Entity),
            DataStoreKind.Sql => nameof(SqlEntity),
            _ => throw new NotImplementedException(model.DataStoreKind.ToString())
        };
    }

    private static string GetEntityFieldTypeString(EntityFieldModel field)
    {
        var typeString = field.FieldType switch
        {
            EntityFieldType.String => "string",
            EntityFieldType.Bool => "bool",
            EntityFieldType.Byte => "byte",
            EntityFieldType.Short => "short",
            EntityFieldType.Int => "int",
            EntityFieldType.Long => "long",
            EntityFieldType.Float => "float",
            EntityFieldType.Double => "double",
            EntityFieldType.DateTime => "DateTime",
            EntityFieldType.Decimal => "decimal",
            EntityFieldType.Guid => "Guid",
            EntityFieldType.Binary => "byte[]",
            _ => throw new NotImplementedException(field.FieldType.ToString())
        };
        return field.AllowNull ? typeString + '?' : typeString;
    }

    private static void GenSqlStoreGetMethod(SqlStoreOptions sqlStoreOptions, StringBuilder sb)
    {
        var isDefaultStore = sqlStoreOptions.StoreModelId == SqlStore.DefaultSqlStoreId;
        if (isDefaultStore)
        {
            sb.Append("\tAppBoxStore.SqlStore.Default");
        }
        else
        {
            sb.Append("\tAppBoxStore.SqlStore.Get(");
            sb.Append(sqlStoreOptions.StoreModelId.ToString());
            sb.Append(')');
        }
    }

    private static string GetEntityMemberWriteReadType(EntityMemberModel member)
    {
        switch (member.Type)
        {
            case EntityMemberType.EntityField:
                var dfm = (EntityFieldModel)member;
                return dfm.FieldType == EntityFieldType.Enum ? "Int" : dfm.FieldType.ToString();
            case EntityMemberType.EntityRef: return "EntityRef";
            case EntityMemberType.EntitySet: return "EntitySet";
            default: throw new Exception();
        }
    }

    #endregion

    #region ===RxEntity for UI binding====

    /// <summary>
    ///  生成用于前端组件状态绑定的响应实体类
    /// </summary>
    internal static string GenRxRuntimeCode(EntityModel model,
        Func<int, string> appNameGetter, Func<ModelId, ModelBase> modelGetter)
    {
        var appName = appNameGetter(model.AppId);
        var className = $"Rx{model.Name}";

        var sb = StringBuilderCache.Acquire();
        var sb2 = StringBuilderCache.Acquire();
        sb.Append("using System;\n");
        sb.Append("using System.Collections.Generic;\n");
        sb.Append("using AppBoxCore;\n");
        sb.Append("using PixUI;\n\n");
        sb.Append($"namespace {appName}.Entities;\n");

        sb.Append($"public sealed class {className} : RxObject<{model.Name}>\n");
        sb.Append("{\n");

        sb.Append($"\tpublic {className}()\n");
        sb.Append("\t{\n");
        //生成实例化空目标对象
        sb.Append("#if __RUNTIME__\n");
        sb.Append($"\t\t_target = new {model.Name}();\n");
        sb.Append("#endif\n");

        foreach (var member in model.Members)
        {
            switch (member.Type)
            {
                case EntityMemberType.EntityField:
                    var entityField = (EntityFieldModel)member;
                    var fieldType = GetEntityFieldTypeString(entityField);
                    sb.Append(
                        $"\t\t{member.Name} = new RxProperty<{fieldType}>(() => Target.{member.Name}");
                    if (!entityField.IsPrimaryKey)
                        sb.Append($", v => Target.{member.Name} = v");
                    sb.Append(");\n");

                    sb2.Append($"\tpublic readonly RxProperty<{fieldType}> {member.Name};\n");
                    break;
                case EntityMemberType.EntityRef:
                    //TODO: gen RxObject?
                    // var entityRef = (EntityRefModel)member;
                    // var refTarget = modelGetter(entityRef.RefModelIds[0]);
                    // var refTypeString = entityRef.IsAggregationRef
                    //     ? GetEntityBaseClass(entityRef.Owner)
                    //     : $"{appNameGetter(refTarget.AppId)}.Entities.{refTarget.Name}";
                    // sb.Append(
                    //     $"\t\t{member.Name} = new RxProperty<{refTypeString}>(() => Target.{member.Name}, v => Target.{member.Name} = v);\n");
                    //
                    // sb2.Append($"\tpublic readonly RxProperty<{refTypeString}> {member.Name};\n");
                    break;
                case EntityMemberType.EntitySet:
                    //TODO: gen RxList?
                    // var entitySet = (EntitySetModel)member;
                    // var setTarget = modelGetter(entitySet.RefModelId);
                    // var setTypeString =
                    //     $"IList<{appNameGetter(setTarget.AppId)}.Entities.{setTarget.Name}>?";
                    // sb.Append(
                    //     $"\t\t{member.Name} = new RxProperty<{setTypeString}>(() => Target.{member.Name});\n");
                    //
                    // sb2.Append($"\tpublic readonly RxProperty<{setTypeString}> {member.Name};\n");
                    break;
                default:
                    throw new NotImplementedException(member.Type.ToString());
            }
        }

        sb.Append("\t}\n");

        sb.Append(StringBuilderCache.GetStringAndRelease(sb2));

        sb.Append("}");

        return StringBuilderCache.GetStringAndRelease(sb);
    }

    #endregion
}