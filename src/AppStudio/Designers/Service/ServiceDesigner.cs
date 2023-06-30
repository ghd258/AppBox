using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using AppBoxClient;
using CodeEditor;
using PixUI;

namespace AppBoxDesign
{
    internal sealed class ServiceDesigner : View, ICodeDesigner
    {
        public ServiceDesigner(ModelNodeVO modelNode)
        {
            ModelNode = modelNode;
            _codeEditorController = new CodeEditorController($"{modelNode.Label}.cs", "",
                RoslynCompletionProvider.Default, modelNode.Id);
            _codeEditorController.ContextMenuBuilder = ContextMenuService.BuildContextMenu;
            _codeSyncService = new ModelCodeSyncService(0, modelNode.Id);
            _delayDocChangedTask = new DelayTask(300, RunDelayTask);

            Child = BuildEditor(_codeEditorController);
        }

        public ModelNodeVO ModelNode { get; }
        private readonly CodeEditorController _codeEditorController;
        private readonly ModelCodeSyncService _codeSyncService;
        private readonly DelayTask _delayDocChangedTask;
        private bool _hasLoadSourceCode;

        private ReferenceVO? _pendingGoto;

        private Widget BuildEditor(CodeEditorController codeEditorController)
        {
            return new Column()
            {
                Children = new Widget[]
                {
                    BuildActionBar(),
                    new Expanded() { Child = new CodeEditorWidget(codeEditorController) },
                }
            };
        }

        private Widget BuildActionBar()
        {
            return new Container()
            {
                BgColor = new Color(0xFF3C3C3C), Height = 40,
                Padding = EdgeInsets.Only(15, 8, 15, 8),
                Child = new Row(VerticalAlignment.Middle, 10)
                {
                    Children = new Widget[]
                    {
                        new Button("Run") { Width = 75, OnTap = OnRunMethod },
                        new Button("Debug") { Width = 75 }
                    }
                }
            };
        }

        protected override void OnMounted()
        {
            base.OnMounted();
            TryLoadSourceCode();
        }

        private async void TryLoadSourceCode()
        {
            if (_hasLoadSourceCode) return;
            _hasLoadSourceCode = true;

            var srcCode = await Channel.Invoke<string>("sys.DesignService.OpenCodeModel",
                new object[] { ModelNode.Id });
            _codeEditorController.Document.TextContent = srcCode!;
            //订阅代码变更事件
            _codeEditorController.Document.DocumentChanged += OnDocumentChanged;

            if (_pendingGoto != null)
            {
                GotoDefinitionCommand.RunOnCodeEditor(_codeEditorController, _pendingGoto);
                _pendingGoto = null;
            }
        }

        private void OnDocumentChanged(DocumentEventArgs e)
        {
            //同步变更至服务端
            _codeSyncService.OnDocumentChanged(e);
            //TODO: check syntax error first.
            //启动延时任务
            _delayDocChangedTask.Run();
        }

        private async void RunDelayTask()
        {
            //检查代码错误，先前端判断语法，再后端判断语义，都没有问题刷新预览
            //if (_codeEditorController.Document.HasSyntaxError) return; //TODO:获取语法错误列表

            try
            {
                var problems = await Channel.Invoke<IList<CodeProblem>>(
                    "sys.DesignService.GetProblems", new object?[] { false, ModelNode.Id });
                DesignStore.UpdateProblems(ModelNode, problems!);
            }
            catch (Exception ex)
            {
                Notification.Error($"GetProblems error: {ex.Message}");
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            if (_hasLoadSourceCode)
            {
                _codeEditorController.Document.DocumentChanged -= OnDocumentChanged;
            }
        }

        public Widget? GetOutlinePad() => null;

        public void GotoDefinition(ReferenceVO reference)
        {
            if (reference.Offset < 0) return; //无需跳转

            var doc = _codeEditorController.Document;
            if (doc.TextLength == 0)
                _pendingGoto = reference;
            else
                GotoDefinitionCommand.RunOnCodeEditor(_codeEditorController, reference);
        }

        public void GotoProblem(CodeProblem problem)
        {
            _codeEditorController.SetCaret(problem.StartLine, problem.StartColumn);
            if (problem.StartLine == problem.EndLine && problem.StartColumn == problem.EndColumn)
                _codeEditorController.ClearSelection();
            else
                _codeEditorController.SetSelection(
                    new TextLocation(problem.StartColumn, problem.StartLine),
                    new TextLocation(problem.EndColumn, problem.EndLine));
        }

        public Task SaveAsync()
        {
            return Channel.Invoke("sys.DesignService.SaveModel", new object?[] { ModelNode.Id });
        }

        public async Task RefreshAsync()
        {
            var srcCode = await Channel.Invoke<string>("sys.DesignService.OpenCodeModel",
                new object[] { ModelNode.Id });
            _codeEditorController.Document.DocumentChanged -= OnDocumentChanged;
            _codeEditorController.Document.TextContent = srcCode!;
            _codeEditorController.Document.DocumentChanged += OnDocumentChanged;
        }

        /// <summary>
        /// 获取光标位置的服务方法
        /// </summary>
        private async Task<JsonObject?> GetMethodInfo()
        {
            try
            {
                var methodInfo = (await Channel.Invoke<string>("sys.DesignService.GetServiceMethod",
                    new object?[] { ModelNode.Id, _codeEditorController.GetCaretOffset() }))!;
                return (JsonObject)JsonNode.Parse(methodInfo)!;
            }
            catch (Exception ex)
            {
                Notification.Error($"无法获取服务方法: {ex.Message}");
                return null;
            }
        }

        private async void OnRunMethod(PointerEvent e)
        {
            var json = await GetMethodInfo();
            if (json == null) return;

            //TODO:暂简单实现且不支持带参数的调用(显示对话框设置参数并显示调用结果)
            try
            {
                var paras = (JsonArray)json["Args"]!;
                if (paras.Count > 0)
                    throw new Exception("暂未实现带参数的服务方法调用");

                var serviceMethod = $"{ModelNode.AppName}.{ModelNode.Label}.{json["Name"]}";
                var res = await Channel.Invoke<object?>(serviceMethod);
                if (res != null)
                {
                    Log.Info($"调用服务方法结果: {System.Text.Json.JsonSerializer.Serialize(res)}");
                }
            }
            catch (Exception ex)
            {
                Notification.Error($"调用服务方法错误: {ex.Message}");
            }
        }
    }
}