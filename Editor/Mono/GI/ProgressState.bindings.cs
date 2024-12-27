// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine.LightTransport
{
    [StructLayout(LayoutKind.Sequential)]
    public class BakeProgressState : IDisposable
    {
        [NativeMethod(IsThreadSafe = true)]
        static extern IntPtr Internal_Create();

        [NativeMethod(IsThreadSafe = true)]
        static extern void Internal_Destroy(IntPtr ptr);

        internal IntPtr m_Ptr;
        internal bool m_OwnsPtr;

        public BakeProgressState()
        {
            m_Ptr = Internal_Create();
            m_OwnsPtr = true;
        }
        private BakeProgressState(IntPtr ptr)
        {
            m_Ptr = ptr;
            m_OwnsPtr = false;
        }
        ~BakeProgressState()
        {
            Destroy();
        }
        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }
        void Destroy()
        {
            if (m_OwnsPtr && m_Ptr != IntPtr.Zero)
            {
                Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }
        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(BakeProgressState obj) => obj.m_Ptr;
        }

        [NativeMethod(IsThreadSafe = true)]
        public extern void Cancel();

        [NativeMethod(IsThreadSafe = true)]
        public extern float Progress();

        [NativeMethod(IsThreadSafe = true)]
        public extern void SetTotalWorkSteps(UInt64 total);

        [NativeMethod(IsThreadSafe = true)]
        public extern void IncrementCompletedWorkSteps(UInt64 steps);

        [NativeMethod(IsThreadSafe = true)]
        public extern bool WasCancelled();
    }
}
