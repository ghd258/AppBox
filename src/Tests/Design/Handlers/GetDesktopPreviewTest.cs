using System;
using System.Threading.Tasks;
using AppBoxCore;
using AppBoxDesign;
using AppBoxServer;
using NUnit.Framework;

namespace Tests.Design.Handlers;

public class GetDesktopPreviewTest
{
    [Test]
    public async Task Test()
    {
        TestHelper.TryInitDefaultStore();
        
        var mockSession = new MockSession(12345);
        HostRuntimeContext.SetCurrentSession(mockSession);
        var designHub = mockSession.GetDesignHub();
        await designHub.DesignTree.LoadAsync();
        
        var modelNode = designHub.DesignTree.FindModelNodeByFullName("sys.Views.DemoPage")!;
        
        var handler = new GetDesktopPreview();
        var res = (string)await handler.Handle(designHub, InvokeArgs.Make(modelNode.Id));
        Console.Write(res);
    }
}