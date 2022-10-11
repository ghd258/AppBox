using System;
using System.Threading.Tasks;
using AppBoxCore;
using AppBoxDesign;
using NUnit.Framework;

namespace Tests.Design;

public class CodeGenerateTest
{
    [Test]
    public async Task GenEntityWebCodeTest()
    {
        var designHub = await TestHelper.MockSession();
        var entityNode = designHub.DesignTree.FindModelNodeByFullName("sys.Entities.Employee")!;
        var code = EntityCodeGenerator.GenWebCode((EntityModel)entityNode.Model, "sys", true);
        Console.Write(code);
    }


    [Test]
    public async Task GenEntityCodeTest()
    {
        var designHub = await TestHelper.MockSession();
        var entityNode = designHub.DesignTree.FindModelNodeByFullName("sys.Entities.OrgUnit")!;
        var code = EntityCodeGenerator.GenRuntimeCode(entityNode);
        Console.Write(code);
    }

    [Test]
    public async Task GenRxEntityCodeTest()
    {
        var designHub = await TestHelper.MockSession();
        var entityNode = designHub.DesignTree.FindModelNodeByFullName("sys.Entities.Employee")!;
        var code = EntityCodeGenerator.GenRxRuntimeCode((EntityModel)entityNode.Model,
            appId => designHub.DesignTree.FindApplicationNode(appId)!.Model.Name);
        Console.Write(code);
    }

    [Test]
    public async Task GenServiceRuntimeCodeTest()
    {
        var designHub = await TestHelper.MockSession();
        var serviceModel = (ServiceModel)
            designHub.DesignTree.FindModelNodeByFullName("sys.Services.HelloService")!.Model;
        var res = await PublishService.CompileServiceAsync(designHub, serviceModel);
        Assert.True(res != null);
    }
}