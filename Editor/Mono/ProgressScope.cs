// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor;

internal partial struct ProgressScope : IDisposable
{
    private UIntPtr scope;

    public ProgressScope(string title, string text, float length = 1f, bool cancellable = false, bool skippable = false, bool forceUpdate = false, bool forceDisplay = false)
    {
        scope = ProgressBarPushScope(title, text, length, cancellable, skippable, forceUpdate, forceDisplay);
    }

    public void SetText(string text, bool forceUpdate = false, bool forceDisplay = false)
    {
        ProgressBarSetText(text, forceUpdate, forceDisplay);
    }

    public void Dispose()
    {
        ProgressBarPopScope(scope);
    }
}
