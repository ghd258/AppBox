using PixUI;
using AppBoxCore;

namespace PixUI
{
    public sealed class DynamicWidget : Widget
    {
        public DynamicWidget(long modelId, IDictionary<string, object?>? initProps = null) {}
    }
}

namespace AppBoxClient
{
    public interface IHomePage
    {
        void InjectRoute(RouteBase route);
    }

    public sealed class RxEntity<T> : RxObject<T> where T : Entity, new()
    {
        public State<TMember> Observe<TMember>(Func<T, TMember> getter)
            => throw new Exception();
    }

    public static class EntityExtensions
    {
        public static State<TMember> Observe<TEntity, TMember>(this TEntity entity, Func<TEntity, TMember> getter)
            where TEntity : Entity
            => throw new Exception();
    }

    public static class ObjectNotifierExtensions
    {
        public static void BindToRxEntity<T>(this ObjectNotifier<T> notifier, RxEntity<T> rxEntity)
            where T : Entity, new()
            => throw new Exception();
    }
}

