using AppBoxCore;
using PixUI;

namespace AppBoxDesign;

internal sealed class RxEntityField : RxObject<EntityFieldVO>
{
    public RxEntityField(EntityFieldVO? target)
    {
        _target = target;

        Name = new RxProperty<string>(() => Target.Name);
        FieldType = new RxProperty<EntityFieldType>(() => Target.FieldType);
        Comment = new RxProperty<string>(() => Target.Comment ?? string.Empty, v => Target.Comment = v);
    }

    public readonly RxProperty<string> Name;
    public readonly RxProperty<EntityFieldType> FieldType;
    public readonly RxProperty<string> Comment;
}

/// <summary>
/// 实体模型的属性面板
/// </summary>
internal sealed class EntityPropertyPanel : View
{
    public EntityPropertyPanel(EntityModelVO entityModel, State<EntityMemberVO?> selectedMember)
    {
        _entityModel = entityModel;
        _selectedMember = Bind(selectedMember, BindingOptions.None);
        _rxEntityField = new RxEntityField((EntityFieldVO?)_selectedMember.Value);
        var isEntityField = _selectedMember
            .AsStateOfBool(v => v != null && v.Type == EntityMemberType.EntityField);

        Child = new Column(HorizontalAlignment.Left)
        {
            Children = new Widget[]
            {
                new Text("Entity Properties:") { FontWeight = FontWeight.Bold },
                new Form()
                {
                    LabelWidth = _labelWidth,
                    Children = new[]
                    {
                        new FormItem("DataStoreKind:", new Input("SqlStore") { Readonly = true }),
                        new FormItem("DataStoreName:", new Input("Default") { Readonly = true }),
                        new FormItem("Comment:", new Input("")),
                    }
                },
                new IfConditional(isEntityField,
                    () => new Text("EntityField Properties:") { FontWeight = FontWeight.Bold }),
                new IfConditional(isEntityField, () => new Form()
                {
                    LabelWidth = _labelWidth,
                    Children = new[]
                    {
                        new FormItem("Name:", new Input(_rxEntityField.Name)),
                        new FormItem("FieldType:",
                            new Input(_rxEntityField.FieldType.AsStateOfString(v => v.ToString()))),
                        new FormItem("Comment:", new Input(_rxEntityField.Comment))
                    }
                })
            }
        };
    }

    private const float _labelWidth = 120f;
    private readonly EntityModelVO _entityModel;
    private readonly State<EntityMemberVO?> _selectedMember;
    private readonly RxEntityField _rxEntityField;

    public override void OnStateChanged(StateBase state, BindingOptions options)
    {
        if (ReferenceEquals(state, _selectedMember))
        {
            if (_selectedMember.Value != null)
            {
                if (_selectedMember.Value.Type == EntityMemberType.EntityField)
                    _rxEntityField.Target = (EntityFieldVO)_selectedMember.Value;
            }

            return;
        }

        base.OnStateChanged(state, options);
    }
}