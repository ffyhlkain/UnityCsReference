// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.ShortcutManagement
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    interface IContextManager
    {
        void SetFocusedWindow(EditorWindow window);
        void RegisterToolContext(IShortcutContext context);
        void DeregisterToolContext(IShortcutContext context);
        bool HasAnyPriorityContext();
        bool HasPriorityContextOfType(Type type);
        bool HasActiveContextOfType(Type type, bool useActiveForHelperBar = false);
        bool DoContextsConflict(Type context1, Type context2);
        bool playModeContextIsActive { get; }
        object GetContextInstanceOfType(Type type, bool filterActive, bool useActiveForHelperBar = false);
        List<Type> GetActiveContexts();
        List<string> GetActiveTags();
        void RegisterTag(string tag);
        void RegisterTag(Enum e);
        void UnregisterTag(string tag);
        void UnregisterTag(Enum e);
        bool HasTag(string tag);
    }

    class ContextManager : IContextManager
    {
        class TagManager : ScriptableSingleton<TagManager>
        {
            [SerializeField] string[] m_SerializedTags;

            public HashSet<string> Tags { get; private set; }

            void OnEnable()
            {
                Tags = new HashSet<string>();

                if (m_SerializedTags == null) return;
                foreach (string tag in m_SerializedTags) Tags.Add(tag);
            }

            void OnDisable() => m_SerializedTags = Tags.ToArray();
        }

        internal class GlobalContext {}

        public static readonly GlobalContext globalContext = new GlobalContext();
        public static readonly Type globalContextType = typeof(GlobalContext);

        WeakReference m_FocusedWindow = new WeakReference(null);

        List<IShortcutContext> m_PriorityContexts = new List<IShortcutContext>();

        List<IShortcutContext> m_ToolContexts = new List<IShortcutContext>();
        static Dictionary<Type, bool> s_IsPriorityContextCache = new Dictionary<Type, bool>();

        public static Action onTagChange;

        public int activeContextCount => 1 + ((focusedWindow != null) ? 1 : 0) + m_PriorityContexts.Count(c => c.active) + m_ToolContexts.Count(c => c.active);

        public bool playModeContextIsActive => focusedWindow is GameView && EditorApplication.isPlaying && !EditorApplication.isPaused;

        private EditorWindow focusedWindow
        {
            get
            {
                if (m_FocusedWindow.Target != null && m_FocusedWindow.IsAlive)
                    return (EditorWindow)m_FocusedWindow.Target;
                return null;
            }
        }

        public void SetFocusedWindow(EditorWindow window)
        {
            m_FocusedWindow.Target = window;
        }

        static bool IsPriorityContext(IShortcutContext context)
        {
            return IsPriorityContext(context.GetType());
        }

        static bool IsPriorityContext(Type context)
        {
            bool result;
            if (!s_IsPriorityContextCache.TryGetValue(context, out result))
            {
                result = Attribute.GetCustomAttribute(context, typeof(PriorityContextAttribute)) != null;
                s_IsPriorityContextCache[context] = result;
            }

            return result;
        }

        public bool DoContextsConflict(Type context1, Type context2)
        {
            if (IsPriorityContext(context1) != IsPriorityContext(context2))
            {
                return false;
            }

            if (context1.IsAssignableFrom(context2) || context2.IsAssignableFrom(context1))
                return true;

            return false;
        }

        void RegisterPriorityContext(IShortcutContext context)
        {
            if (!m_PriorityContexts.Contains(context))
            {
                m_PriorityContexts.Add(context);
            }
        }

        void DeregisterPriorityContext(IShortcutContext context)
        {
            m_PriorityContexts.Remove(context);
        }

        public void RegisterToolContext(IShortcutContext context)
        {
            if (context == null)
                return;

            if (IsPriorityContext(context))
                RegisterPriorityContext(context);
            else
            {
                if (!m_ToolContexts.Contains(context))
                    m_ToolContexts.Add(context);
            }
        }

        public void DeregisterToolContext(IShortcutContext context)
        {
            if (context == null)
                return;

            if (m_PriorityContexts.Contains(context))
                DeregisterPriorityContext(context);
            else
            {
                if (m_ToolContexts.Contains(context))
                    m_ToolContexts.Remove(context);
            }
        }

        public bool HasAnyPriorityContext()
        {
            return m_PriorityContexts.Count > 0;
        }

        public bool HasPriorityContextOfType(Type type)
        {
            return GetPriorityContextOfType(type) != null;
        }

        public bool HasActiveContextOfType(Type type, bool useActiveForHelperBar = false)
        {
            return GetContextInstanceOfType(type, true, useActiveForHelperBar) != null;
        }

        internal object GetToolContextOfType(Type type, bool filterActive = true, bool useActiveForHelperBar = false)
        {
            foreach (var toolContext in m_ToolContexts)
            {
                if (!filterActive || (useActiveForHelperBar ? (toolContext is IHelperBarShortcutContext helperBarContext ? helperBarContext.helperBarActive : toolContext.active) : toolContext.active) && type.IsInstanceOfType(toolContext))
                    return toolContext;
            }

            return null;
        }

        internal object GetPriorityContextOfType(Type type, bool filterActive = true, bool useActiveForHelperBar = false)
        {
            foreach (var priorityContext in m_PriorityContexts)
            {
                if ((!filterActive || (useActiveForHelperBar ? (priorityContext is IHelperBarShortcutContext helperBarContext ? helperBarContext.helperBarActive : priorityContext.active) : priorityContext.active)) && type.IsInstanceOfType(priorityContext))
                {
                    return priorityContext;
                }
            }

            return null;
        }

        public object GetContextInstanceOfType(Type type, bool filterActive = true, bool useActiveForHelperBar = false)
        {
            if (type == null)
                return null;

            if (type == globalContextType)
                return globalContext;

            if (m_FocusedWindow != null && m_FocusedWindow.IsAlive && type.IsInstanceOfType(m_FocusedWindow.Target))
                return m_FocusedWindow.Target;

            object priorityContextType = GetPriorityContextOfType(type, filterActive, useActiveForHelperBar);
            if (priorityContextType != null)
            {
                return priorityContextType;
            }

            object toolContextType = GetToolContextOfType(type, filterActive, useActiveForHelperBar);
            if (toolContextType != null)
            {
                return toolContextType;
            }

            return null;
        }

        internal static string EnumTagFormat(Enum tag) => $"{tag.GetType().Name}.{tag.ToString()}";

        public void RegisterTag(string tag)
        {
            var change = TagManager.instance.Tags.Add(tag);
            if (change) onTagChange?.Invoke();
        }

        public void RegisterTag(Enum e)
        {
            foreach (var value in Enum.GetValues(e.GetType()))
            {
                UnregisterTag(EnumTagFormat((Enum)value));
            }
            RegisterTag(EnumTagFormat(e));
        }

        public void UnregisterTag(string tag)
        {
            var change = TagManager.instance.Tags.Remove(tag);
            if (change) onTagChange?.Invoke();
        }

        public void UnregisterTag(Enum e) => UnregisterTag(EnumTagFormat(e));

        public bool HasTag(string tag) => tag != null && TagManager.instance.Tags.Contains(tag);

        List<Type> IContextManager.GetActiveContexts()
        {
            var result = new List<Type>();

            result.Add(globalContextType);
            var targetType = m_FocusedWindow.Target?.GetType();
            if(targetType != null) result.Add(targetType);
            result.AddRange(m_PriorityContexts.Where(p => p.active).Select(p => p.GetType()));
            result.AddRange(m_ToolContexts.Where(c => c.active).Select(c => c.GetType()));

            return result;
        }

        public List<string> GetActiveTags() => TagManager.instance.Tags.ToList();
    }
}
