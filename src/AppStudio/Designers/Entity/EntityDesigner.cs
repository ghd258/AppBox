using System;
using System.Threading.Tasks;
using AppBoxClient;
using PixUI;

namespace AppBoxDesign
{
    internal sealed class EntityDesigner : View, IDesigner
    {
        public EntityDesigner(ModelNode modelNode)
        {
            _modelNode = modelNode;

            Child = new Column()
            {
                Children = new[]
                {
                    BuildActionBar(),
                    new Expanded()
                    {
                        //TODO: use FutureBuilder
                        Child = new IfConditional(_loaded, () => new Conditional<int>(_activePad,
                            new[]
                            {
                                new WhenBuilder<int>(t => t == 0,
                                    () => new MembersDesigner(_entityModel!, _membersController)),
                                new WhenBuilder<int>(t => t == 1,
                                    () => new SqlStoreOptionsDesigner(_entityModel!))
                            }), null)
                    },
                }
            };
        }

        private readonly ModelNode _modelNode;
        private readonly State<int> _activePad = 0; //当前的设计面板
        private bool _hasLoad = false;
        private readonly State<bool> _loaded = false;
        private EntityModelVO? _entityModel;

        private readonly DataGridController<EntityMemberVO> _membersController = new();

        private Widget BuildActionBar()
        {
            return new Container()
            {
                BgColor = new Color(0xFFF5F7FA), Height = 45,
                Padding = EdgeInsets.All(8),
                Child = new Row(VerticalAlignment.Middle, 10)
                {
                    Children = new Widget[]
                    {
                        new ButtonGroup()
                        {
                            Children = new[]
                            {
                                new Button("Members")
                                    { Width = 75, OnTap = _ => _activePad.Value = 0 },
                                new Button("Options")
                                    { Width = 75, OnTap = _ => _activePad.Value = 1 },
                                new Button("Data") { Width = 75 },
                            }
                        },
                        new IfConditional(_activePad.AsStateOfBool(i => i == 0),
                            () => new ButtonGroup()
                            {
                                Children = new[]
                                {
                                    new Button("Add", Icons.Filled.Add),
                                    new Button("Remove", Icons.Filled.Delete),
                                    new Button("Rename", Icons.Filled.Edit),
                                    new Button("Usages", Icons.Filled.Link),
                                }
                            }),
                    }
                }
            };
        }

        protected override void OnMounted()
        {
            base.OnMounted();
            TryLoadEntityModel();
        }

        private async void TryLoadEntityModel()
        {
            if (_hasLoad) return;
            _hasLoad = true;

            try
            {
                _entityModel = (EntityModelVO)await Channel.Invoke(
                    "sys.DesignService.OpenEntityModel",
                    new object[] { _modelNode.Id });
                _membersController.DataSource = _entityModel!.Members;
                _loaded.Value = true;
            }
            catch (Exception e)
            {
                Notification.Error("无法加载实体模型");
            }
        }

        public Task SaveAsync()
        {
            throw new System.NotImplementedException();
        }
    }
}