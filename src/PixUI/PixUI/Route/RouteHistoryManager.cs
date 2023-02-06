using System;
using System.Collections.Generic;

namespace PixUI
{
    /// <summary>
    /// 路由历史项
    /// </summary>
    internal sealed class RouteHistoryEntry
    {
        internal RouteHistoryEntry(string path)
        {
            Path = path;
        }

        internal readonly string Path;
        //TODO: cache of keepalive widget, think about use Map<PathString, Widget>
    }

    internal sealed class BuildPathContext
    {
        public BuildPathContext()
        {
            LeafNamed = new StringMap<Navigator>(new (string, Navigator)[] { });
        }

        internal Navigator LeafDefault = null!;
        internal readonly StringMap<Navigator> LeafNamed;

        internal string GetFullPath()
        {
            var fullPath = LeafDefault.Path;
            if (LeafNamed.size > 0)
            {
                fullPath += "?";
                var first = true;
                foreach (var key in LeafNamed.keys())
                {
                    if (first) first = false;
                    else fullPath += "&";

                    fullPath += key + "=" + LeafNamed.get(key)!.Path;
                }
            }

            return fullPath;
        }
    }

    /// <summary>
    /// 路由历史管理，一个UIWindow对应一个实例
    /// </summary>
    public sealed class RouteHistoryManager
    {
        private readonly List<RouteHistoryEntry> _history = new List<RouteHistoryEntry>();
        private int _historyIndex = -1;

        internal readonly Navigator RootNavigator = new Navigator(Array.Empty<Route>());

        internal string? AssignedPath { get; set; }

        internal int Count => _history.Count;

        /// <summary>
        /// 获取当前路由的全路径
        /// </summary>
        internal string GetFullPath()
        {
            if (RootNavigator.Children == null || RootNavigator.Children.Count == 0)
                return "";

            var ctx = new BuildPathContext();
            BuildFullPath(ctx, RootNavigator);
            return ctx.GetFullPath();
        }

        private static void BuildFullPath(BuildPathContext ctx, Navigator navigator)
        {
            if (navigator.IsNamed)
            {
                ctx.LeafNamed.set(navigator.NameOfRouteView!, navigator);
            }
            else if (navigator.IsInNamed)
            {
                var named = navigator.GetNamedParent()!;
                ctx.LeafNamed.set(named.NameOfRouteView!, navigator);
            }
            else
            {
                ctx.LeafDefault = navigator;
            }

            if (navigator.Children != null)
            {
                foreach (var child in navigator.Children)
                {
                    BuildFullPath(ctx, child);
                }
            }
        }

        internal void PushEntry(RouteHistoryEntry entry)
        {
            //先清空之后的记录
            if (_historyIndex != _history.Count - 1)
            {
                //TODO: dispose will removed widgets
                _history.RemoveRange(_historyIndex + 1, _history.Count - _historyIndex - 1);
            }

            _history.Add(entry);
            _historyIndex++;
        }

        internal RouteHistoryEntry? Pop()
        {
            if (_historyIndex <= 0) return null;

            var oldEntry = _history[_historyIndex];
            Goto(_historyIndex - 1);
            return oldEntry;
        }

        internal void Goto(int index)
        {
            if (index < 0 || index >= this._history.Count)
                throw new Exception("index out of range");

            var action = index < _historyIndex ? RouteChangeAction.GotoBack : RouteChangeAction.GotoForward;
            _historyIndex = index;
            var newEntry = _history[_historyIndex];
            AssignedPath = newEntry.Path;

            NavigateTo(newEntry.Path, action);
        }

        public void Push(string fullPath)
        {
            //TODO: 验证fullPath start with '/' and convert to lowercase
            AssignedPath = fullPath;
            var newEntry = new RouteHistoryEntry(fullPath); //TODO:考虑已存在则改为Goto
            PushEntry(newEntry);

            NavigateTo(fullPath, RouteChangeAction.Push);
        }

        private void NavigateTo(string fullPath, RouteChangeAction action)
        {
            //从根开始比较
            var psa = fullPath.Split('?');
            var defaultPath = psa[0];
            var defaultPss = defaultPath.Split('/');

            //先比较处理默认路径
            var navigator = GetDefaultNavigator(RootNavigator);
            ComparePath(navigator, defaultPss, 1, action);
            //TODO: 再处理各命名路由的路径
        }

        private bool ComparePath(Navigator? navigator, string[] pss, int index, RouteChangeAction action)
        {
            if (navigator == null) return false;

            var name = pss[index];
            if (name == "")
                name = navigator.GetDefaultRoute().Name;
            string? arg = null;
            if (navigator.IsDynamic(name))
            {
                arg = pss[index + 1];
                index++;
            }

            if (name != navigator.ActiveRoute.Name || arg != navigator.ActiveArgument)
            {
                navigator.Goto(name, arg, action);
                return true;
            }

            if (index == pss.Length - 1)
                return false;
            return ComparePath(GetDefaultNavigator(navigator), pss, index + 1, action);
        }

        /// <summary>
        /// 获取默认路由（惟一的非命名的）
        /// </summary>
        private static Navigator? GetDefaultNavigator(Navigator navigator)
        {
            if (navigator.Children == null || navigator.Children.Count == 0)
                return null;

            for (var i = 0; i < navigator.Children.Count; i++)
            {
                if (!navigator.Children[i].IsNamed)
                    return navigator.Children[i];
            }

            return null;
        }
    }
}