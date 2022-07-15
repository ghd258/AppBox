import * as AppBoxCore from '@/AppBoxCore'
import * as AppBoxDesign from '@/AppBoxDesign'
import * as PixUI from '@/PixUI'

export class SqlStoreOptionsDesigner extends PixUI.View {
    public constructor(entityModel: AppBoxDesign.EntityModelVO) {
        super();
        this._entityModel = entityModel;
        this._pkController.DataSource = this._entityModel.SqlStoreOptions.PrimaryKeys;

        this.Child = new PixUI.Container().Init(
            {
                Padding: PixUI.State.op_Implicit_From(PixUI.EdgeInsets.All(8)),
                Child: new PixUI.Column(PixUI.HorizontalAlignment.Left, 10).Init(
                    {
                        Children: [                    // Primary keys
                            new PixUI.Text(PixUI.State.op_Implicit_From("Primary Keys:")).Init({
                                FontSize: PixUI.State.op_Implicit_From(20),
                                FontWeight: PixUI.State.op_Implicit_From(CanvasKit.FontWeight.Bold)
                            }), new PixUI.ButtonGroup().Init(
                                {
                                    Children: [new PixUI.Button(PixUI.State.op_Implicit_From("Add"), PixUI.State.op_Implicit_From(PixUI.Icons.Filled.Add)), new PixUI.Button(PixUI.State.op_Implicit_From("Remove"), PixUI.State.op_Implicit_From(PixUI.Icons.Filled.Remove))
                                    ]
                                }), new PixUI.DataGrid<AppBoxCore.FieldWithOrder>(this._pkController).Init(
                                {
                                    Height: PixUI.State.op_Implicit_From(112),
                                    Columns: [new PixUI.DataGridTextColumn<AppBoxCore.FieldWithOrder>("Name", t => this._entityModel.Members.First(m => m.Id == t.MemberId).Name), new PixUI.DataGridCheckboxColumn<AppBoxCore.FieldWithOrder>("OrderByDesc", t => t.OrderByDesc)]
                                }),
                            //Indexes
                            new PixUI.Text(PixUI.State.op_Implicit_From("Indexes:")).Init({
                                FontSize: PixUI.State.op_Implicit_From(20),
                                FontWeight: PixUI.State.op_Implicit_From(CanvasKit.FontWeight.Bold)
                            }), new PixUI.ButtonGroup().Init(
                                {
                                    Children: [new PixUI.Button(PixUI.State.op_Implicit_From("Add"), PixUI.State.op_Implicit_From(PixUI.Icons.Filled.Add)), new PixUI.Button(PixUI.State.op_Implicit_From("Remove"), PixUI.State.op_Implicit_From(PixUI.Icons.Filled.Remove))
                                    ]
                                }), new PixUI.DataGrid<any>(this._idxController).Init(
                                {
                                    Height: PixUI.State.op_Implicit_From(112),
                                    Columns: [new PixUI.DataGridTextColumn<any>("Name", t => "TODO"), new PixUI.DataGridTextColumn<any>("Fields", t => "TODO"), new PixUI.DataGridCheckboxColumn<any>("Unique", t => true)]
                                })]
                    })
            });
    }

    private readonly _entityModel: AppBoxDesign.EntityModelVO;

    private readonly _pkController: PixUI.DataGridController<AppBoxCore.FieldWithOrder> = new PixUI.DataGridController<AppBoxCore.FieldWithOrder>();

    private readonly _idxController: PixUI.DataGridController<any> = new PixUI.DataGridController<any>();

    public Init(props: Partial<SqlStoreOptionsDesigner>): SqlStoreOptionsDesigner {
        Object.assign(this, props);
        return this;
    }
}