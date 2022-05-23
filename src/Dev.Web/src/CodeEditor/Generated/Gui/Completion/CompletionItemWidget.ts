import * as CodeEditor from '@/CodeEditor'
import * as PixUI from '@/PixUI'

export class CompletionItemWidget extends PixUI.Widget {
    public constructor(item: CodeEditor.CompletionItem, isSelected: PixUI.State<boolean>) {
        super();
        this._item = item;
        this._isSelected = isSelected;
        this._iconPainter = new PixUI.IconPainter(() => this.Invalidate(PixUI.InvalidAction.Repaint));
    }

    private readonly _item: CodeEditor.CompletionItem;
    private readonly _isSelected: PixUI.State<boolean>;
    private readonly _iconPainter: PixUI.IconPainter;
    private _paragraph: Nullable<PixUI.Paragraph>; //TODO: use TextPainter

    public Layout(availableWidth: number, availableHeight: number) {
        this.SetSize(availableWidth, availableHeight);
    }

    public Paint(canvas: PixUI.Canvas, area: Nullable<PixUI.IDirtyArea> = null) {
        let fontSize: number = 13;
        let x: number = 2;
        let y: number = 3;
        this._iconPainter.Paint(canvas, fontSize, PixUI.Colors.Gray, CompletionItemWidget.GetIcon(this._item.Kind), x, y);
        if (this._paragraph == null) {
            let ts = PixUI.MakeTextStyle({color: PixUI.Colors.Black, fontSize: fontSize});
            let ps = PixUI.MakeParagraphStyle({maxLines: 1, textStyle: ts});
            let pb = PixUI.MakeParagraphBuilder(ps);

            pb.pushStyle(ts);
            pb.addText(this._item.Label);
            pb.pop();
            this._paragraph = pb.build();
            this._paragraph.layout(Number.POSITIVE_INFINITY);
            pb.delete();
        }

        canvas.drawParagraph(this._paragraph!, x + 20, y);
    }

    private static GetIcon(kind: CodeEditor.CompletionItemKind): PixUI.IconData {
        switch (kind) {
            case CodeEditor.CompletionItemKind.Function:
            case CodeEditor.CompletionItemKind.Method:
                return PixUI.Icons.Filled.Functions;
            case CodeEditor.CompletionItemKind.Event:
                return PixUI.Icons.Filled.Bolt;
            default:
                return PixUI.Icons.Filled.Title;
        }
    }

    public Init(props: Partial<CompletionItemWidget>): CompletionItemWidget {
        Object.assign(this, props);
        return this;
    }
}
