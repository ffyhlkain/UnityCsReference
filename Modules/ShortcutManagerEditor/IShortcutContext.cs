// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.ShortcutManagement
{
    abstract class ClutchShortcutContext : IShortcutContext
    {
        public bool active { get; internal set; }
    }

    public interface IShortcutContext
    {
        bool active { get; }
    }

    internal interface IHelperBarShortcutContext
    {
        bool helperBarActive { get; }
    }
}
