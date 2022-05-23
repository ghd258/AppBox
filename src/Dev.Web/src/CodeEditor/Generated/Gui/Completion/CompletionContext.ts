import * as System from '@/System'
import * as PixUI from '@/PixUI'
import * as CodeEditor from '@/CodeEditor'

export class CompletionContext {
    private static readonly STATE_IDLE: number = 0;
    private static readonly STATE_SHOW: number = 1;
    private static readonly STATE_SUSPEND_HIDE: number = 2;

    private readonly _controller: CodeEditor.CodeEditorController;
    private readonly _provider: Nullable<CodeEditor.ICompletionProvider>;

    private _completionStartOffset: number = -1; //开始自动完成时的光标位置
    private _startByTriggerChar: boolean = false; //是否由TriggerChar触发的
    private _completionWindow: Nullable<PixUI.ListPopup<CodeEditor.CompletionItem>>;
    private _state: number = CompletionContext.STATE_IDLE; //当前状态

    public constructor(controller: CodeEditor.CodeEditorController, provider: Nullable<CodeEditor.ICompletionProvider>) {
        this._controller = controller;
        this._provider = provider;
    }

    public RunCompletion(value: string) {
        if (this._provider == null) return;

        //Get word at caret position (end with caret position)
        let world = this.GetWortAtPosition((this._controller.TextEditor.Caret.Position).Clone());

        //是否已经显示
        if (this._state == CompletionContext.STATE_SHOW) {
            if (world == null) //if word is null, hide completion window
            {
                this.HideCompletionWindow();
            } else {
                this.UpdateFilter();
                return;
            }
        }

        //开始显示
        if (world != null) {
            this._completionStartOffset = world.Offset;
            this._startByTriggerChar = false;
            this._state = CompletionContext.STATE_SHOW;
            this.RunInternal(world.Word);
        } else {
            let triggerChar = value[value.length - 1];
            if (this._provider.TriggerCharacters.Contains(Number(triggerChar))) {
                this._completionStartOffset = this._controller.TextEditor.Caret.Offset;
                this._startByTriggerChar = true;
                this._state = CompletionContext.STATE_SHOW;
                this.RunInternal("");
            }
        }
    }

    public async RunInternal(filter: string): System.Task {
        let items = await this._provider!.ProvideCompletionItems(this._controller.Document, (this._controller.TextEditor.Caret.Position).Clone(), filter);
        this.ShowCompletionWindow(items, "");
    }

    private GetWortAtPosition(pos: CodeEditor.TextLocation): Nullable<CodeEditor.CompletionWord> {
        let lineSegment = this._controller.Document.GetLineSegment(pos.Line);
        let token = lineSegment.GetTokenAt(pos.Column);
        if (token == null) return null;

        //排除无需提示的Token类型
        let tokenType = CodeEditor.CodeToken.GetTokenType(token);
        if (tokenType == CodeEditor.TokenType.Comment || tokenType == CodeEditor.TokenType.Constant ||
            tokenType == CodeEditor.TokenType.LiteralNumber || tokenType == CodeEditor.TokenType.LiteralString ||
            tokenType == CodeEditor.TokenType.PunctuationBracket ||
            tokenType == CodeEditor.TokenType.PunctuationDelimiter ||
            tokenType == CodeEditor.TokenType.WhiteSpace || tokenType == CodeEditor.TokenType.Operator)
            return null;

        let tokenStartColumn = CodeEditor.CodeToken.GetTokenStartColumn(token);
        let len = pos.Column - tokenStartColumn;
        if (len <= 0) return null;
        let offset = lineSegment.Offset + tokenStartColumn;
        let tokenWord = this._controller.Document.GetText(offset, len);
        return new CodeEditor.CompletionWord(offset, tokenWord);
    }

    private ShowCompletionWindow(list: Nullable<System.IList<CodeEditor.CompletionItem>>, filter: string) {
        if (list == null || list.length == 0) {
            this._state = CompletionContext.STATE_IDLE;
            return;
        }

        if (this._completionWindow == null) {
            this._completionWindow = new PixUI.ListPopup<CodeEditor.CompletionItem>(this._controller.Widget.Overlay!, CompletionContext.BuildPopupItem, 250, 18, 8);
            this._completionWindow.OnSelectionChanged = this.OnCompletionDone.bind(this);
        }

        this._completionWindow.DataSource = list;
        this._completionWindow.TrySelectFirst();
        //TODO: set filter
        let caret = this._controller.TextEditor.Caret;
        let lineHeight = this._controller.TextEditor.TextView.FontHeight;
        let pt2Win = this._controller.Widget.LocalToWindow(0, 0);
        this._completionWindow.UpdatePosition(caret.CanvasPosX + pt2Win.X - 8, caret.CanvasPosY + lineHeight + pt2Win.Y);
        this._completionWindow.Show();
    }

    private HideCompletionWindow() {
        this._completionWindow?.Hide();
        this._state = CompletionContext.STATE_IDLE;
    }

    private UpdateFilter() {
        let filter = this._controller.Document.GetText(this._completionStartOffset, this._controller.TextEditor.Caret.Offset - this._completionStartOffset);
        this._completionWindow?.UpdateFilter(t => t.Label.startsWith(filter));
        this._completionWindow?.TrySelectFirst();
    }

    private ClearFilter() {
        this._completionWindow?.ClearFilter();
        this._completionWindow?.TrySelectFirst();
    }

    public OnCaretChangedByNoneTextInput() {
        if (this._state != CompletionContext.STATE_SUSPEND_HIDE) {
            this.HideCompletionWindow();
            return;
        }

        //由后退键触发的
        let caret = this._controller.TextEditor.Caret;
        if (caret.Offset <= this._completionStartOffset) {
            if (caret.Offset == this._completionStartOffset && this._startByTriggerChar) {
                this._state = CompletionContext.STATE_SHOW;
                this.ClearFilter();
            } else {
                this.HideCompletionWindow();
            }
        } else {
            this._state = CompletionContext.STATE_SHOW;
            this.UpdateFilter();
        }
    }

    public PreProcessKeyDown(e: PixUI.KeyEvent) {
        if (this._state == CompletionContext.STATE_SHOW) {
            if (e.KeyCode == PixUI.Keys.Back)
                this._state = CompletionContext.STATE_SUSPEND_HIDE;
        }
    }

    private OnCompletionDone(item: CodeEditor.CompletionItem) {
        this.HideCompletionWindow();

        this._controller.TextEditor.InsertOrReplaceString(item.InsertText ?? item.Label, this._controller.TextEditor.Caret.Offset - this._completionStartOffset);

        //TODO: force focus editor, maybe has lost focus by click someone.
    }

    private static BuildPopupItem(item: CodeEditor.CompletionItem, index: number, isHover: PixUI.State<boolean>, isSelected: PixUI.State<boolean>): PixUI.Widget {
        return new CodeEditor.CompletionItemWidget(item, isSelected);
    }

    public Init(props: Partial<CompletionContext>): CompletionContext {
        Object.assign(this, props);
        return this;
    }
}
