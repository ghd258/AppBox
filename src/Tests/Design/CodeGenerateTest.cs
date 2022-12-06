using System;
using System.Threading.Tasks;
using AppBoxCore;
using AppBoxDesign;
using NUnit.Framework;

namespace Tests.Design;

public class CodeGenerateTest
{
    [Test(Description = "测试生成实体模型的Web代码")]
    public async Task GenEntityWebCodeTest()
    {
        var designHub = await TestHelper.MockSession();
        var entityNode = designHub.DesignTree.FindModelNodeByFullName("sys.Entities.OrgUnit")!;
        var code = EntityJsGenerator.GenWebCode((EntityModel)entityNode.Model, designHub, true);
        Console.Write(code);
    }

    [Test(Description = "测试生成视图模型的Web预览代码")]
    public async Task GetViewWebCodeTest()
    {
        var designHub = await TestHelper.MockSession();
        var modelNode = designHub.DesignTree.FindModelNodeByFullName("sys.Views.HomePage")!;

        var res = await ViewJsGenerator.GenViewWebCode(designHub, modelNode.Id, true);
        Console.Write(res);
    }

    [Test]
    public async Task GenEntityCodeTest()
    {
        var designHub = await TestHelper.MockSession();
        var entityNode = designHub.DesignTree.FindModelNodeByFullName("sys.Entities.OrgUnit")!;
        var code = EntityCsGenerator.GenRuntimeCode(entityNode);
        Console.Write(code);
    }

    [Test(Description = "测试生成权限模型虚拟代码")]
    public async Task GenPermissionCodeTest()
    {
        var designHub = await TestHelper.MockSession();
        var node = designHub.DesignTree.FindModelNodeByFullName("sys.Permissions.Admin")!;
        var model = (PermissionModel)node.Model;
        var code = PermissionCodeGenerator.GenDummyCode(model, node.AppNode.Model.Name);
        Console.WriteLine(code);
    }

    [Test(Description = "测试生成服务的运行时代码")]
    public async Task GenServiceRuntimeCodeTest()
    {
        var designHub = await TestHelper.MockSession();
        var serviceModel = (ServiceModel)
            designHub.DesignTree.FindModelNodeByFullName("sys.Services.OrgUnitService")!.Model;
        var res = await PublishService.CompileServiceAsync(designHub, serviceModel);
        Assert.True(res != null);
    }
}