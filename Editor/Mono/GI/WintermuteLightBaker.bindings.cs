// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor.LightBaking
{
    [NativeHeader("Editor/Src/GI/LightBaker/LightBaker.Bindings.h")]
    internal static partial class LightBaker
    {
        [NativeMethod(IsThreadSafe = true)]
        internal static extern Result PopulateWorldWintermute(BakeInput bakeInput, LightmapRequests lightmapRequests, LightProbeRequests lightProbeRequests, UnityEngine.LightTransport.BakeProgressState progress,
            UnityEngine.LightTransport.WintermuteContext context, UnityEngine.LightTransport.IntegrationContext world);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern unsafe Result IntegrateProbeDirectRadianceWintermute(UnityEngine.Vector3* positions, UnityEngine.LightTransport.IntegrationContext integrationContext,
            int positionCount, float pushoff, int bounceCount, int directSampleCount, int giSampleCount, int envSampleCount,
            bool ignoreDirectEnvironment, bool ignoreIndirectEnvironment,
            UnityEngine.LightTransport.WintermuteContext context, UnityEngine.LightTransport.BakeProgressState progress, void* radianceBufferOut);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern unsafe Result IntegrateProbeIndirectRadianceWintermute(UnityEngine.Vector3* positions, UnityEngine.LightTransport.IntegrationContext integrationContext,
            int positionCount, float pushoff, int bounceCount, int directSampleCount, int giSampleCount, int envSampleCount,
            bool ignoreDirectEnvironment, bool ignoreIndirectEnvironment,
            UnityEngine.LightTransport.WintermuteContext context, UnityEngine.LightTransport.BakeProgressState progress, void* radianceBufferOut);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern unsafe Result IntegrateProbeValidityWintermute(void* positions, UnityEngine.LightTransport.IntegrationContext integrationContext,
            int positionCount, float pushoff, int bounceCount, int directSampleCount, int giSampleCount, int envSampleCount,
            UnityEngine.LightTransport.WintermuteContext context, UnityEngine.LightTransport.BakeProgressState progress, void* validityBufferOut);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern unsafe Result IntegrateProbeOcclusionWintermute(void* positions, void* probeLightIndices,
            int positionCount, float pushoff, int bounceCount, int directSampleCount, int giSampleCount, int envSampleCount,
            UnityEngine.LightTransport.WintermuteContext context, UnityEngine.LightTransport.BakeProgressState progress, void* occlusionBufferOut);
    }
}
