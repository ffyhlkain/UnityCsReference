// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Build.Content
{
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct BuildUsageTagGlobal
    {
        // Ensure changes here match changes in Runtime/Serialize/BuildUsageTags.h
        internal uint m_LightmapModesUsed;
        internal uint m_LegacyLightmapModesUsed;
        internal uint m_DynamicLightmapsUsed;
        internal uint m_FogModesUsed;
        internal uint m_BrgShaderStripModeMask;
        internal bool m_ForceInstancingStrip;
        internal bool m_ForceInstancingKeep;
        internal bool m_ShadowMasksUsed;
        internal bool m_SubtractiveUsed;
        internal bool m_HybridRendererPackageUsed;
        internal bool m_BuildForServer;
        internal bool m_LODFadeCrossfade;

        public static BuildUsageTagGlobal operator|(BuildUsageTagGlobal x, BuildUsageTagGlobal y)
        {
            var results = new BuildUsageTagGlobal();
            results.m_LightmapModesUsed = x.m_LightmapModesUsed | y.m_LightmapModesUsed;
            results.m_LegacyLightmapModesUsed = x.m_LegacyLightmapModesUsed | y.m_LegacyLightmapModesUsed;
            results.m_DynamicLightmapsUsed = x.m_LightmapModesUsed | y.m_DynamicLightmapsUsed;
            results.m_FogModesUsed = x.m_FogModesUsed | y.m_FogModesUsed;
            results.m_ForceInstancingStrip = x.m_ForceInstancingStrip | y.m_ForceInstancingStrip;
            results.m_ForceInstancingKeep = x.m_ForceInstancingKeep | y.m_ForceInstancingKeep;
            results.m_ShadowMasksUsed = x.m_ShadowMasksUsed | y.m_ShadowMasksUsed;
            results.m_SubtractiveUsed = x.m_SubtractiveUsed | y.m_SubtractiveUsed;
            results.m_HybridRendererPackageUsed = x.m_HybridRendererPackageUsed | y.m_HybridRendererPackageUsed;
            results.m_BrgShaderStripModeMask = x.m_BrgShaderStripModeMask | y.m_BrgShaderStripModeMask;
            results.m_BuildForServer = x.m_BuildForServer | y.m_BuildForServer;
            results.m_LODFadeCrossfade = x.m_LODFadeCrossfade | y.m_LODFadeCrossfade;
            return results;
        }
    }
}
