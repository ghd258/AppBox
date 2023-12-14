using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using AppBoxClient;
using AppBoxClient.Dynamic;
using PixUI.Dynamic;

namespace PixUI;

/// <summary>
/// 解析动态视图模型生成相应的组件
/// </summary>
public sealed class DynamicWidget : DynamicView, IDynamicContext
{
    public DynamicWidget(long viewModelId /*, IDictionary<string, object?>? initProps = null*/)
    {
        _viewModelId = viewModelId;
    }

    private readonly long _viewModelId;
    private bool _hasLoaded;
    private List<DynamicState>? _states;
    private DynamicBackground? _background;
    private Image? _cachedImage;

    DynamicState? IDynamicContext.FindState(string name) => _states?.SingleOrDefault(s => s.Name == name);

    protected override void OnMounted()
    {
        base.OnMounted();
        if (!_hasLoaded)
        {
            _hasLoaded = true;
            LoadAsync();
        }
    }

    public override void Paint(Canvas canvas, IDirtyArea? area = null)
    {
        if (_background != null && _background.ImageData != null)
        {
            _cachedImage ??= Image.FromEncodedData(_background.ImageData);
            canvas.DrawImage(_cachedImage!, Rect.FromLTWH(0, 0, W, H));
        }

        base.Paint(canvas, area);
    }

    private async void LoadAsync()
    {
        await DynamicInitiator.TryInitAsync();

        //TODO:考虑缓存加载过的json配置
        var json = await Channel.Invoke<byte[]?>(
            "sys.SystemService.LoadDynamicViewJson", new object?[] { _viewModelId });
        if (json == null || json.Length == 0)
        {
            ReplaceTo(new Text("Can't find dynamic view")); //TODO: ErrorWidget
            return;
        }

        try
        {
            var root = ParseJson(json);
            ReplaceTo(root);
        }
        catch (Exception e)
        {
            ReplaceTo(new Text($"Parse error: {e.Message}"));
        }
    }

    #region ====Parse Json====

    private Widget ParseJson(byte[] json)
    {
        var reader = new Utf8JsonReader(json);
        Widget? root = null;
        while (reader.Read())
        {
            if (reader.TokenType != JsonTokenType.PropertyName) continue;

            var propName = reader.GetString();
            switch (propName)
            {
                case "Background":
                    ReadBackground(ref reader);
                    break;
                case "State":
                    ReadStates(ref reader);
                    break;
                case "Root":
                    root = ReadWidget(ref reader, string.Empty);
                    break;
            }
        }

        if (root == null) throw new Exception();
        return root;
    }

    private void ReadBackground(ref Utf8JsonReader reader)
    {
        _background = JsonSerializer.Deserialize<DynamicBackground>(ref reader);
    }

    private void ReadStates(ref Utf8JsonReader reader)
    {
        _states = new List<DynamicState>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) continue;

            var propName = reader.GetString()!;
            ReadState(ref reader, propName, _states);
        }
    }

    private static void ReadState(ref Utf8JsonReader reader, string name, IList<DynamicState> states)
    {
        reader.Read(); //{
        reader.Read(); //Type prop
        reader.Read(); //Type value
        var type = Enum.Parse<DynamicStateType>(reader.GetString()!);
        var state = new DynamicState { Name = name, Type = type };

        if (type == DynamicStateType.DataSet)
        {
            reader.Read(); //Value prop
            var peekReader = reader;
            if (!(peekReader.Read() && peekReader.TokenType == JsonTokenType.Null))
            {
                var ds = new DynamicDataSetState();
                ds.ReadFrom(ref reader);
                state.Value = ds;
            }
        }
        else
        {
            //AllowNull
            reader.Read(); //AllowNull prop
            reader.Read(); //AllowNull value
            state.AllowNull = reader.GetBoolean();

            //Value
            reader.Read(); //Value prop
            var peekReader = reader;
            if (!(peekReader.Read() && peekReader.TokenType == JsonTokenType.Null))
            {
                var vs = new DynamicValueState();
                vs.ReadFrom(ref reader, state);
                state.Value = vs;
            }
        }

        reader.Read(); //}

        states.Add(state);
    }

    private Widget ReadWidget(ref Utf8JsonReader reader, string slotName)
    {
        Widget result = null!;
        DynamicWidgetMeta meta = null!;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) continue;

            var propName = reader.GetString()!;
            if (propName == "Type")
            {
                reader.Read();
                meta = DynamicWidgetManager.GetByName(reader.GetString()!);
                result = meta.CreateInstance();
            }
            else if (propName == "Events")
            {
                ReadEvents(ref reader, result);
            }
            else if (meta.IsSlot(propName, out var childSlot))
            {
                if (childSlot!.ContainerType == ContainerType.MultiChild)
                {
                    ReadWidgetArray(ref reader, result, childSlot);
                }
                else
                {
                    var child = ReadWidget(ref reader, childSlot!.PropertyName);
                    childSlot.SetChild(result, child);
                }
            }
            else
            {
                var propMeta = meta.GetPropertyMeta(propName);
                var propValue = DynamicValue.Read(ref reader, propMeta);
                propMeta.SetRuntimeValue(meta, result, propValue, this);
            }
        }

        return result;
    }
    
    private void ReadWidgetArray(ref Utf8JsonReader reader, Widget parent, ContainerSlot childrenSlot)
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray) break;
            if (reader.TokenType != JsonTokenType.StartObject) continue;

            var child = ReadWidget(ref reader, childrenSlot.PropertyName);
            childrenSlot.AddChild(parent, child);
        }
    }

    private void ReadEvents(ref Utf8JsonReader reader, Widget widget)
    {
        reader.Read(); //{

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) break;

            var eventName = reader.GetString()!;
            var eventAction = ReadEventAction(ref reader);
            //绑定事件
            
            // if (widget is Button button && eventName == nameof(Button.OnTap))
            // {
            //     button.OnTap = _ => eventAction.Run(this);
            // }
            // else
            // {
            //     throw new NotImplementedException("Bind Event to Widget");
            // }
        }
    }

    private static IEventAction ReadEventAction(ref Utf8JsonReader reader)
    {
        reader.Read(); //{
        reader.Read(); // Handler prop
        Debug.Assert(reader.GetString() == "Handler");
        reader.Read(); // Handler value
        var handler = reader.GetString()!;
        //根据类型创建实例
        var res = DynamicWidgetManager.EventActionManager.Create(handler);
        res.ReadProperties(ref reader);
        return res;
    }

    private void BindEventAction(Widget widget, string eventName, IEventAction eventAction)
    {
        var widgetType = widget.GetType();
        var eventPropInfo = widgetType.GetProperty(eventName, BindingFlags.Public | BindingFlags.Instance);
        if (eventPropInfo == null)
        {
            Notification.Error($"Can't find event: {widgetType.Name}.{eventName}");
            return;
        }

        var actionType = eventPropInfo.PropertyType;
        
    }

    #endregion
}