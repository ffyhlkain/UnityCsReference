// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Assertions;

namespace UnityEditor
{
    [NativeType(Header = "Editor/Src/InteractionContext.h")]
    internal partial class InteractionContext: IDisposable
    {
        [Flags]
        public enum Flags
        {
            DisableNone = 0,
            DisableUndo = 1,
            DisableDialogs = 2,

            DisableUndoAndDialogs = DisableUndo | DisableDialogs,
        }

        internal IntPtr m_NativePtr;

        public InteractionContext(Flags flags) : this (Internal_Create((int)flags))
        {
        }

        private InteractionContext(IntPtr nativePtr)
        {
            m_NativePtr = nativePtr;
        }

        ~InteractionContext()
        {
            if (m_NativePtr != IntPtr.Zero)
            {
                Dispose();
            }
        }

        public virtual void Dispose()
        {
            if (m_NativePtr != IntPtr.Zero)
            {
                Internal_Destroy(m_NativePtr);
                m_NativePtr = IntPtr.Zero;
            }
        }

        public extern bool IsUndoEnabled();
        public extern bool WasAnyUndoOperationRegisteredSinceCreation();

        public extern bool AreDialogsEnabled();
        public extern bool HasUnusedDialogResponses();
        public extern bool IsCurrentDialogResponse(string dialogTitle);
        public extern string GetCurrentDialogResponse();
        public extern string GetCurrentDialogResponseAndAvance();
        public extern void AppendDialogResponse(string dialogTitle, string dialogResponse);

        public extern string GetErrors();

        [FreeFunction("CreateInteractionContext", IsThreadSafe = false)]
        private static extern IntPtr Internal_Create(int flags);
        [FreeFunction("DestroyInteractionContext")]
        private static extern void Internal_Destroy(IntPtr m_NativePtr);

        public static InteractionContext UserAction = new InteractionContext(Flags.DisableNone);

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(InteractionContext context) => context.m_NativePtr;
            public static InteractionContext ConvertToManaged(IntPtr ptr) => new InteractionContext(ptr);
        }
    }

    internal class GlobalInteractionContext : InteractionContext, IDisposable
    {
        public GlobalInteractionContext(InteractionContext.Flags flags)
            : base(flags)
        {
            Assert.IsNull(GetGlobalInteractionContext());
            SetGlobalInteractionContext(this);
        }

        public override void Dispose()
        {
            try
            {
                Assert.IsFalse(HasUnusedDialogResponses());
                if (!IsUndoEnabled())
                {
                    Assert.IsFalse(WasAnyUndoOperationRegisteredSinceCreation(), "InteractionContext has unused dialog responses");
                }
            }
            finally
            {
                ClearGlobalInteractionContext();
            }
            base.Dispose();
        }

        [FreeFunction("SetGlobalInteractionContext")]
        private static extern void SetGlobalInteractionContext(InteractionContext interactionContext);

        [FreeFunction("GetGlobalInteractionContext")]
        private static extern InteractionContext GetGlobalInteractionContext();

        [FreeFunction("ClearGlobalInteractionContext")]
        private static extern void ClearGlobalInteractionContext();

        new internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(GlobalInteractionContext context) => context.m_NativePtr;
        }
    }
}
