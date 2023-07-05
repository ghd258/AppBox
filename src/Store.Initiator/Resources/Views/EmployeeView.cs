using sys.Entities;

namespace sys.Views;

public sealed class EmployeeView : View
{
    public static EmployeeView Preview() => new(new());

    public EmployeeView(RxEntity<Employee> state)
    {
        Child = new Form
        {
            Padding = EdgeInsets.All(10),
            LabelWidth = 50,
            Children =
            {
                new ("姓名:", new Input(state.Observe(e => e.Name))),
                new ("生日:", new DatePicker(state.Observe(e => e.Birthday))),
                new ("性别:", new Row { Children =
                {
                    new Text("男"),
                    new Radio(state.Observe(e => e.Male)),
                    new Text("女"),
                    new Radio(state.Observe(e => e.Male).ToStateOfBoolReversed())
                }})
            }
        };
    }

}