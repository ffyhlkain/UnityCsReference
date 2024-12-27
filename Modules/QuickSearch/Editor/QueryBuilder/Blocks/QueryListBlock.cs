// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Search.Providers;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    public abstract class QueryListBlock : QueryBlock
    {
        public readonly string id;
        public readonly string category;
        protected string label;
        public Texture2D icon { get; protected set; }

        protected bool alwaysDrawLabel;

        public abstract IEnumerable<SearchProposition> GetPropositions(SearchPropositionFlags flags = SearchPropositionFlags.None);

        protected QueryListBlock(IQuerySource source, string id, string value, QueryListBlockAttribute attr)
            : base(source)
        {
            this.id = id ?? attr.id;
            this.op = attr.op;
            this.name = attr.name;
            this.value = value;
            this.category = attr.category;
        }

        internal QueryListBlock(IQuerySource source, string id, string op, string value, string category = null)
            : base(source)
        {
            this.id = id;
            this.op = op;
            this.value = value;
            this.category = category;
        }

        internal string GetCategory(SearchPropositionFlags flags)
        {
            return flags.HasAny(SearchPropositionFlags.NoCategory) ? null : category;
        }

        internal override IBlockEditor OpenEditor(in Rect rect)
        {
            return QuerySelector.Open(rect, this);
        }

        internal override IEnumerable<SearchProposition> FetchPropositions()
        {
            return GetPropositions(SearchPropositionFlags.NoCategory);
        }

        public override void Apply(in SearchProposition searchProposition)
        {
            value = searchProposition.data?.ToString() ?? searchProposition.replacement;
            ApplyChanges();
        }

        internal override Color GetBackgroundColor()
        {
            return icon == null ? QueryColors.type : QueryColors.typeIcon;
        }

        internal IEnumerable<SearchProposition> GetEnumPropositions<TEnum>(SearchPropositionFlags flags, string helpTemplate) where TEnum : System.Enum
        {
            foreach (var obj in Enum.GetValues(typeof(TEnum)))
            {
                var e = (TEnum)obj;
                yield return new SearchProposition(category: GetCategory(flags), label: e.ToString(), help: $"{helpTemplate} {e}",
                        data: e, priority: 0, icon: null, type: GetType(), color: GetBackgroundColor());
            }
        }

        internal SearchProposition CreateProposition(SearchPropositionFlags flags, string label, object data, string help = "", int score = 0)
        {
            return new SearchProposition(category: GetCategory(flags), label: label, help: help,
                    data: data, priority: score, icon: icon, type: GetType(), color: GetBackgroundColor());
        }

        internal SearchProposition CreateProposition(SearchPropositionFlags flags, string label, object data, string help, Texture2D icon, int score = 0)
        {
            return new SearchProposition(category: GetCategory(flags), label: label, help: help,
                    data: data, priority: score, icon: icon, type: GetType(), color: GetBackgroundColor());
        }

        internal override void CreateBlockElement(VisualElement container)
        {
            if ((label == null && label != value) || alwaysDrawLabel)
            {
                base.CreateBlockElement(container);
                return;
            }

            if (icon != null)
                AddIcon(container, icon);
            AddLabel(container, label ?? value);
            AddOpenEditorArrow(container);
        }

        public override string ToString()
        {
            return $"{id}{op}{value}";
        }

        internal override void AddContextualMenuItems(GenericMenu menu)
        {
            menu.AddItem(EditorGUIUtility.TrTextContent($"Operator/Equal (=)"), string.Equals(op, "=", StringComparison.Ordinal), () => SetOperator("="));
            menu.AddItem(EditorGUIUtility.TrTextContent($"Operator/Contains (:)"), string.Equals(op, ":", StringComparison.Ordinal), () => SetOperator(":"));
        }

        internal virtual bool TryGetReplacement(string id, string type, ref Type blockType, out string replacement)
        {
            replacement = string.Empty;
            return false;
        }
    }

    class QueryListMarkerBlock : QueryListBlock
    {
        private QueryMarker m_Marker;
        Color? m_BackgroundColor;
        string m_IconName;
        string[] m_Choices;

        public QueryListMarkerBlock(IQuerySource source, string id, string name, string op, QueryMarker value)
            : base(source, id, op, value.value as string)
        {
            m_Marker = value;
            this.name = name ?? id;
            ExtractArguments(m_Marker);
        }

        public QueryListMarkerBlock(IQuerySource source, string id, QueryMarker value, QueryListBlockAttribute attr)
            : base(source, id, value.value as string, attr)
        {
            m_Marker = value;
            ExtractArguments(m_Marker);
        }

        internal override Color GetBackgroundColor()
        {
            return m_BackgroundColor ?? base.GetBackgroundColor();
        }

        public override IEnumerable<SearchProposition> GetPropositions(SearchPropositionFlags flags = SearchPropositionFlags.None)
        {
            var args = m_Marker.args;
            if (m_Choices == null || m_Choices.Length == 0)
                yield break;

            foreach (var choice in m_Choices)
            {
                yield return new SearchProposition(category: null, label: ObjectNames.NicifyVariableName(choice), replacement: choice, icon: icon, color: m_BackgroundColor ?? default);
            }
        }

        public override string ToString()
        {
            var markerReplacement = SearchUtils.GetListMarkerReplacementText(value, m_Choices, m_IconName, m_BackgroundColor);
            return $"{id}{op}{markerReplacement}";
        }

        void ExtractArguments(QueryMarker marker)
        {
            var args = m_Marker.EvaluateArgsNoSpread().ToList();
            if (args.Count < 2)
            {
                m_Choices = new string[]{};
                return;
            }

            m_Choices = ExtractChoices(args[1]).ToArray();

            foreach (var arg in args.Skip(2))
            {
                if (!(arg is IEnumerable<object> enumerable))
                    continue;

                var array = enumerable.ToArray();
                if (array.Length == 0)
                    continue;

                var argStr = (string)array[0];
                if (argStr.StartsWith("#"))
                {
                    m_BackgroundColor = ExtractBackgroundColor(argStr);
                }
                else
                {
                    icon = ExtractIcon(argStr);
                    if (icon)
                        m_IconName = argStr;
                }
            }
        }

        static Color? ExtractBackgroundColor(string colorArg)
        {
            if (!ColorUtility.TryParseHtmlString(colorArg, out var color))
                return null;
            return color;
        }

        static Texture2D ExtractIcon(string iconArg)
        {
            return Utils.LoadIcon(iconArg);
        }

        static IEnumerable<string> ExtractChoices(object choiceArg)
        {
            if (choiceArg == null || !(choiceArg is IEnumerable<object> enumerable))
                return Enumerable.Empty<string>();

            return enumerable.Cast<string>();
        }
    }

    [QueryListBlock("Types", "type", "t", ":")]
    class QueryTypeBlock : QueryListBlock
    {
        private Type type;

        public QueryTypeBlock(IQuerySource source, string id, string value, QueryListBlockAttribute attr)
            : base(source, id, value, attr)
        {
            SetType(GetValueType(value));
        }

        private void SetType(in Type type)
        {
            this.type = type;
            if (this.type != null)
            {
                value = type.Name;
                label = ObjectNames.NicifyVariableName(type.Name);
                icon = SearchUtils.GetTypeIcon(type);
            }
        }

        private static Type GetValueType(string value)
        {
            return TypeCache.GetTypesDerivedFrom<UnityEngine.Object>().FirstOrDefault(t => string.Equals(t.Name, value, StringComparison.OrdinalIgnoreCase) || string.Equals(t.ToString(), value, StringComparison.OrdinalIgnoreCase));
        }

        public override void Apply(in SearchProposition searchProposition)
        {
            if (searchProposition.data is Type t)
            {
                SetType(t);
                ApplyChanges();
            }
        }

        public override IEnumerable<SearchProposition> GetPropositions(SearchPropositionFlags flags)
        {
            yield return new SearchProposition(
                category: null,
                label: "Components",
                icon: EditorGUIUtility.LoadIcon("GameObject Icon"));

            var componentType = typeof(Component);
            var assetTypes = SearchUtils.FetchTypePropositions<UnityEngine.Object>().Where(p => !componentType.IsAssignableFrom((Type)p.data));
            var propositions = SearchUtils.FetchTypePropositions<Component>("Components").Concat(assetTypes);
            foreach (var p in propositions)
                yield return p;
        }
    }

    [QueryListBlock("Components", "component", "t", ":", 1)]
    class QueryComponentBlock : QueryTypeBlock
    {
        public QueryComponentBlock(IQuerySource source, string id, string value, QueryListBlockAttribute attr)
             : base(source, id, value, attr)
        {
        }

        public override IEnumerable<SearchProposition> GetPropositions(SearchPropositionFlags flags)
        {
            return SearchUtils.FetchTypePropositions<Component>("Components", GetType());
        }
    }

    [QueryListBlock("Labels", "label", "l", ":")]
    class QueryLabelBlock : QueryListBlock
    {
        static readonly Texture2D kLabelIcon = Utils.LoadIcon("QuickSearch/AssetLabelIconSquare");
        public static Texture2D GetLabelIcon()
        {
            return kLabelIcon;
        }

        public QueryLabelBlock(IQuerySource source, string id, string value, QueryListBlockAttribute attr)
            : base(source, id, value, attr)
        {
            icon = GetLabelIcon();
        }

        public override IEnumerable<SearchProposition> GetPropositions(SearchPropositionFlags flags)
        {
            foreach (var l in AssetDatabase.GetAllLabels())
            {
                yield return CreateProposition(flags, ObjectNames.NicifyVariableName(l.Key), l.Key, $"Assets with label: {l.Key}");
            }
        }
    }

    [QueryListBlock("Tags", "tag", "tag")]
    class QueryTagBlock : QueryListBlock
    {
        public QueryTagBlock(IQuerySource source, string id, string value, QueryListBlockAttribute attr)
            : base(source, id, value, attr)
        {
            icon = QueryLabelBlock.GetLabelIcon();
        }

        public override IEnumerable<SearchProposition> GetPropositions(SearchPropositionFlags flags)
        {
            foreach (var t in InternalEditorUtility.tags)
            {
                yield return CreateProposition(flags, ObjectNames.NicifyVariableName(t), t, $"Assets with tag: {t}");
            }
        }
    }

    [QueryListBlock("Layers", "layer", new[] { "layer", "#m_layer" })]
    class QueryLayerBlock : QueryListBlock
    {
        const int k_MaxLayerCount = 32;

        public QueryLayerBlock(IQuerySource source, string id, string value, QueryListBlockAttribute attr)
            : base(source, id, value, attr)
        {
            icon = Utils.LoadIcon("GUILayer Icon");

            if (QueryMarker.TryParse(value, out var marker) && marker.valid && marker.args.Length >= 2)
                this.value = marker.args[1].rawText.ToString();
        }

        public override IEnumerable<SearchProposition> GetPropositions(SearchPropositionFlags flags)
        {
            for (var i = 0; i < k_MaxLayerCount; i++)
            {
                var layerName = InternalEditorUtility.GetLayerName(i);
                if (!string.IsNullOrEmpty(layerName))
                {
                    yield return CreateProposition(flags, ObjectNames.NicifyVariableName(layerName), layerName, $"Assets with layer: {layerName}");
                }
            }
        }

        public override string ToString()
        {
            for (var i = 0; i < k_MaxLayerCount; i++)
            {
                var layerName = InternalEditorUtility.GetLayerName(i);
                if (layerName == value)
                    return $"{id}{op}{FormatValue(i, layerName)}";
            }

            return base.ToString();
        }

        internal override bool TryGetReplacement(string id, string type, ref Type blockType, out string replacement)
        {
            replacement = $"{id}{op}{GetDefaultMarker()}";
            return true;
        }

        static string FormatValue(int layerIndex, string layerName)
        {
            return $"<$layer:{layerIndex}, {layerName}$>";
        }

        public static string GetDefaultMarker()
        {
            var layerName = InternalEditorUtility.GetLayerName(0);
            return FormatValue(0, layerName);
        }
    }

    [QueryListBlock("Rendering Layers", "renderinglayer", "renderinglayer", ":")]
    class QueryRenderingLayerBlock : QueryListBlock
    {
        public QueryRenderingLayerBlock(IQuerySource source, string id, string value, QueryListBlockAttribute attr)
            : base(source, id, value, attr)
        {
            icon = Utils.LoadIcon("GUILayer Icon");
        }

        public override IEnumerable<SearchProposition> GetPropositions(SearchPropositionFlags flags)
        {
            var names = RenderingLayerMask.GetDefinedRenderingLayerNames();
            var values = RenderingLayerMask.GetDefinedRenderingLayerValues();

            for (var i = 0; i < names.Length; i++)
            {
                var layerName = names[i];
                if (!string.IsNullOrEmpty(layerName))
                {
                    yield return CreateProposition(flags, ObjectNames.NicifyVariableName(layerName), layerName, $"Objects with layer: {layerName}");
                }
            }
        }

        public static int GetValueForLayerName(string name)
        {
            var names = RenderingLayerMask.GetDefinedRenderingLayerNames();
            var values = RenderingLayerMask.GetDefinedRenderingLayerValues();
            for (var i = 0; i < names.Length; i++)
            {
                var layerName = names[i];
                if (layerName == name)
                    return values[i];
            }
            return -1;
        }
    }

    [QueryListBlock("Prefabs", "prefab", "prefab", ":")]
    class QueryPrefabFilterBlock : QueryListBlock
    {
        public QueryPrefabFilterBlock(IQuerySource source, string id, string value, QueryListBlockAttribute attr)
            : base(source, id, value, attr)
        {
            icon = Utils.LoadIcon("Prefab Icon");
        }

        public override IEnumerable<SearchProposition> GetPropositions(SearchPropositionFlags flags)
        {
            yield return CreateProposition(flags, "Any", "any", "Search prefabs");
            yield return CreateProposition(flags, "Base", "base", "Search base prefabs");
            yield return CreateProposition(flags, "Root", "root", "Search prefab roots");
            yield return CreateProposition(flags, "Top", "top", "Search top-level prefab root instances");
            yield return CreateProposition(flags, "Instance", "instance", "Search objects that are part of a prefab instance");
            yield return CreateProposition(flags, "Non asset", "nonasset", "Search prefab objects that are not part of an asset");
            yield return CreateProposition(flags, "Asset", "asset", "Search prefab objects that are part of an asset");
            yield return CreateProposition(flags, "Model", "model", "Search prefab objects that are part of a model");
            yield return CreateProposition(flags, "Regular", "regular", "Search regular prefab objects");
            yield return CreateProposition(flags, "Variant", "variant", "Search variant prefab objects");
            yield return CreateProposition(flags, "Modified", "modified", "Search modified prefab assets");
            yield return CreateProposition(flags, "Altered", "altered", "Search modified prefab instances");
        }
    }

    [QueryListBlock("Scene Filters", "is", "is", ":")]
    class QueryIsFilterBlock : QueryListBlock
    {
        public QueryIsFilterBlock(IQuerySource source, string id, string value, QueryListBlockAttribute attr)
            : base(source, id, value, attr)
        {
            icon = Utils.LoadIcon("Filter Icon");
            alwaysDrawLabel = false;
        }

        public override IEnumerable<SearchProposition> GetPropositions(SearchPropositionFlags flags)
        {
            yield return CreateProposition(flags, "Child", "child", "Search object with a parent");
            yield return CreateProposition(flags, "Leaf", "leaf", "Search object without children");
            yield return CreateProposition(flags,  "Root", "root", "Search root objects");
            yield return CreateProposition(flags, "Visible", "visible", "Search view visible objects");
            yield return CreateProposition(flags, "Hidden", "hidden", "Search hierarchically hidden objects");
            yield return CreateProposition(flags, "Static", "static", "Search static objects");
            yield return CreateProposition(flags, "Prefab", "prefab", "Search prefab objects");
            yield return CreateProposition(flags, "Main", "main", "Search main asset representation");
        }
    }

    [QueryListBlock("Missing", "missing", "missing", ":")]
    class QueryMissingBlock : QueryListBlock
    {
        public QueryMissingBlock(IQuerySource source, string id, string value, QueryListBlockAttribute attr)
            : base(source, id, value, attr)
        {
            icon = Utils.LoadIcon("Filter Icon");
            alwaysDrawLabel = false;
        }

        public override IEnumerable<SearchProposition> GetPropositions(SearchPropositionFlags flags)
        {
            foreach(var e in Enum.GetValues(typeof(MissingReferenceFilter)))
            {
                yield return CreateProposition(flags, e.ToString(), e, $"Missing {e}");
            }
        }
    }
}
