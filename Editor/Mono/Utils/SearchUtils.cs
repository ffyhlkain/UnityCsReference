// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor
{
    interface ISearchableContainer
    {
        string searchText { get; }
    }

    static class SearchUtils
    {
        internal static bool MatchSearch(string searchContext, string content)
        {
            return content != null && searchContext != null && content.IndexOf(searchContext, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static bool MatchSearchGroups(string searchContext, string content)
        {
            int dummyStart;
            int dummyEnd;
            return MatchSearchGroups(searchContext, content, out dummyStart, out dummyEnd);
        }

        internal static bool MatchSearchGroups(string searchContext, string content, out int startIndex, out int endIndex)
        {
            startIndex = endIndex = -1;
            if (searchContext == null || content == null)
                return false;

            if (searchContext == content)
            {
                endIndex = content.Length - 1;
                return true;
            }

            // Each search group is space separated
            // Search group must match in order and be complete.
            var searchGroups = searchContext.Split(' ');
            var startSearchIndex = 0;
            foreach (var searchGroup in searchGroups)
            {
                if (searchGroup.Length == 0)
                    continue;

                startSearchIndex = content.IndexOf(searchGroup, startSearchIndex, StringComparison.CurrentCultureIgnoreCase);
                if (startSearchIndex == -1)
                {
                    return false;
                }

                startIndex = startIndex == -1 ? startSearchIndex : startIndex;
                startSearchIndex = endIndex = startSearchIndex + searchGroup.Length - 1;
            }

            return startIndex != -1 && endIndex != -1;
        }

        internal static bool MatchNonConsecutive(string searchContext, string content)
        {
            int dummyStart;
            int dummyEnd;
            return MatchNonConsecutive(searchContext, content, out dummyStart, out dummyEnd);
        }

        internal static bool MatchNonConsecutive(string searchContext, string content, out int startIndex, out int endIndex)
        {
            startIndex = endIndex = -1;
            if (searchContext == null || content == null || searchContext.Length > content.Length)
                return false;

            if (searchContext == content)
            {
                endIndex = content.Length - 1;
                return true;
            }

            // Ensure we find all letters of searchContext but not necessarily consecutively:
            var contentIndex = 0;
            var searchContextIndex = 0;
            for (; searchContextIndex < searchContext.Length && contentIndex < content.Length; ++searchContextIndex)
            {
                var ch = Char.ToLowerInvariant(searchContext[searchContextIndex]);
                while (contentIndex < content.Length)
                {
                    if (ch == Char.ToLowerInvariant(content[contentIndex++]))
                    {
                        startIndex = startIndex != -1 ? startIndex : (contentIndex - 1);
                        endIndex = searchContextIndex == searchContext.Length - 1 ? (contentIndex - 1) : endIndex;
                        break;
                    }
                }
            }

            return startIndex != -1 && endIndex != -1;
        }

        // Faster version of String.EndsWith
        public static bool EndsWith(string a, string b)
        {
            int ap = a.Length - 1;
            int bp = b.Length - 1;
            while (ap >= 0 && bp >= 0 && char.ToUpperInvariant(a[ap]) == char.ToUpperInvariant(b[bp]))
            {
                ap--;
                bp--;
            }
            return (bp < 0 && a.Length >= b.Length) || (ap < 0 && b.Length >= a.Length);
        }

        static int? s_DebounceThresholdMs;
        const string k_DebounceThresholdKeyName = "Search.DebounceThresholdMs";
        public static int debounceThresholdMs
        {
            get
            {
                if (!s_DebounceThresholdMs.HasValue)
                    s_DebounceThresholdMs = EditorPrefs.GetInt(k_DebounceThresholdKeyName, 250);
                return s_DebounceThresholdMs.Value;
            }

            set
            {
                s_DebounceThresholdMs = Math.Min(Math.Max(0, value), 1000);
                EditorPrefs.SetInt(k_DebounceThresholdKeyName, s_DebounceThresholdMs.Value);
            }
        }
    }
}
