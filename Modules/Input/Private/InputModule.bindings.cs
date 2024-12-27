// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngineInternal;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngineInternal.Input
{
    [NativeHeader("Modules/Input/Private/InputModuleBindings.h")]
    [NativeHeader("Modules/Input/Private/InputInternal.h")]
    internal partial class NativeInputSystem
    {
        internal static extern bool hasDeviceDiscoveredCallback { set; }

        [NativeProperty(IsThreadSafe = true)]
        public static extern double currentTime { get; }

        [NativeProperty(IsThreadSafe = true)]
        public static extern double currentTimeOffsetToRealtimeSinceStartup { get; }

        [FreeFunction("AllocateInputDeviceId")]
        public static extern int AllocateDeviceId();

        // C# doesn't allow taking the address of a value type because of pinning requirements for the heap.
        // And our bindings generator doesn't support overloading. So ugly code following here...
        public static unsafe void QueueInputEvent<TInputEvent>(ref TInputEvent inputEvent)
            where TInputEvent : struct
        {
            QueueInputEvent((IntPtr)UnsafeUtility.AddressOf<TInputEvent>(ref inputEvent));
        }

        [NativeMethod(IsThreadSafe = true)]
        public static extern void QueueInputEvent(IntPtr inputEvent);

        public static extern long IOCTL(int deviceId, int code, IntPtr data, int sizeInBytes);

        public static extern void SetPollingFrequency(float hertz);

        public static extern void Update(NativeInputUpdateType updateType);

        internal static extern ulong GetBackgroundEventBufferSize();

        [Obsolete("This is not needed any longer.")]
        public static void SetUpdateMask(NativeInputUpdateType mask)
        {
        }

        /// <summary>
        /// Allows creation of input devices from events.
        /// Used by input simulation package on Windows, where we can send the simulated events.
        /// Input simulation package doesn't create input devices, thus we must ask backend to created those when required.
        /// By default, such behavior is disabled.
        /// </summary>
        [NativeProperty("AllowInputDeviceCreationFromEvents")]
        internal static extern bool allowInputDeviceCreationFromEvents { get; set; }

        /// <summary>
        /// If this option is set to true, scrollWheel delta values will always be returned within a range of [-1, 1]
        /// regardless of the platform.
        /// If false, scrollWheel delta values will try to respect platform-specific values ranges.
        /// (For instance, on the Windows platform the scrollWheel delta values range would be [-120, 120].)
        /// </summary>
        [NativeProperty("NormalizeScrollWheelDelta")]
        internal static extern bool normalizeScrollWheelDelta { get; set; }

        internal static extern float GetScrollWheelDeltaPerTick();
    }
}
