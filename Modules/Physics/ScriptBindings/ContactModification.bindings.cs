// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;

namespace UnityEngine
{
    public partial class Physics
    {
        public static event Action<PhysicsScene, NativeArray<ModifiableContactPair>> ContactModifyEvent;
        public static event Action<PhysicsScene, NativeArray<ModifiableContactPair>> ContactModifyEventCCD;
        //initially hook contact modification to always be physx specific
        internal static event Action<PhysicsScene, IntPtr, int, bool> GenericContactModifyEvent = PhysXOnSceneContactModify;

        [RequiredByNativeCode]
        private static unsafe void OnSceneContactModify(PhysicsScene scene, IntPtr buffer, int count, bool isCCD)
        {
            GenericContactModifyEvent?.Invoke(scene, buffer, count, isCCD);
        }

        private static unsafe void PhysXOnSceneContactModify(PhysicsScene scene, IntPtr buffer, int count, bool isCCD)
        {
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<ModifiableContactPair>(buffer.ToPointer(), count, Allocator.None);

            var safety = AtomicSafetyHandle.Create();
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, safety);

            if (!isCCD)
                ContactModifyEvent?.Invoke(scene, array);
            else
                ContactModifyEventCCD?.Invoke(scene, array);

            AtomicSafetyHandle.Release(safety);
        }
    }


    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/Physics/PhysicsCollisionGeometry.h")]
    [NativeHeader("Modules/Physics/PhysXContactModification.h")]
    public struct ModifiableContactPair
    {
        private IntPtr actor;
        private IntPtr otherActor;

        private IntPtr shape;
        private IntPtr otherShape;

        public Quaternion rotation;
        public Vector3 position;

        public Quaternion otherRotation;
        public Vector3 otherPosition;

        private int numContacts;
        private IntPtr contacts;

        [FreeFunction("Physics::PhysxCompatibility::TranslateTriangleIndex", true)]
        extern internal static uint TranslateTriangleIndex(IntPtr shapePtr, uint rawIndex);

        [FreeFunction("Physics::PhysxCompatibility::ResolveShapeToInstanceID", true)]
        extern internal static int ResolveShapeToInstanceID(IntPtr shapePtr);

        [FreeFunction("Physics::PhysxCompatibility::ResolveActorToInstanceID", true)]
        extern internal static int ResolveActorToInstanceID(IntPtr actorPtr);

        [FreeFunction("Physics::PhysxCompatibility::GetActorLinearVelocity", true)]
        extern internal static Vector3 GetActorLinearVelocity(IntPtr actorPtr);

        [FreeFunction("Physics::PhysxCompatibility::GetActorAngularVelocity", true)]
        extern internal static Vector3 GetActorAngularVelocity(IntPtr actorPtr);

        public int colliderInstanceID => ResolveShapeToInstanceID(shape);
        public int otherColliderInstanceID => ResolveShapeToInstanceID(otherShape);
        public int bodyInstanceID => ResolveActorToInstanceID(actor);
        public int otherBodyInstanceID => ResolveActorToInstanceID(otherActor);

        public Vector3 bodyVelocity => GetActorLinearVelocity(actor);
        public Vector3 bodyAngularVelocity => GetActorAngularVelocity(actor);
        public Vector3 otherBodyVelocity => GetActorLinearVelocity(otherActor);
        public Vector3 otherBodyAngularVelocity => GetActorAngularVelocity(otherActor);

        public int contactCount => numContacts;

        public unsafe ModifiableMassProperties massProperties
        {
            get
            {
                return GetContactPatch()->massProperties;
            }

            set
            {
                var contactPatch = GetContactPatch();
                contactPatch->massProperties = value;
                contactPatch->internalFlags |= (byte)ModifiableContactPatch.Flags.HasModifiedMassRatios;
            }
        }

        public unsafe Vector3 GetPoint(int i)
        {
            return GetContact(i)->contact;
        }

        public unsafe void SetPoint(int i, Vector3 v)
        {
            GetContact(i)->contact = v;
        }

        public unsafe Vector3 GetNormal(int i)
        {
            return GetContact(i)->normal;
        }

        public unsafe void SetNormal(int i, Vector3 normal)
        {
            GetContact(i)->normal = normal;
            GetContactPatch()->internalFlags |= (byte)ModifiableContactPatch.Flags.RegeneratePatches;
        }

        public unsafe float GetSeparation(int i)
        {
            return GetContact(i)->separation;
        }

        public unsafe void SetSeparation(int i, float separation)
        {
            GetContact(i)->separation = separation;
        }

        public unsafe Vector3 GetTargetVelocity(int i)
        {
            return GetContact(i)->targetVelocity;
        }

        public unsafe void SetTargetVelocity(int i, Vector3 velocity)
        {
            GetContact(i)->targetVelocity = velocity;
            GetContactPatch()->internalFlags |= (byte)ModifiableContactPatch.Flags.HasTargetVelocity;
        }

        public unsafe float GetBounciness(int i)
        {
            return GetContact(i)->restitution;
        }

        public unsafe void SetBounciness(int i, float bounciness)
        {
            GetContact(i)->restitution = bounciness;
            GetContactPatch()->internalFlags |= (byte)ModifiableContactPatch.Flags.RegeneratePatches;
        }

        public unsafe float GetStaticFriction(int i)
        {
            return GetContact(i)->staticFriction;
        }

        public unsafe void SetStaticFriction(int i, float staticFriction)
        {
            GetContact(i)->staticFriction = staticFriction;
            GetContactPatch()->internalFlags |= (byte)ModifiableContactPatch.Flags.RegeneratePatches;
        }

        public unsafe float GetDynamicFriction(int i)
        {
            return GetContact(i)->dynamicFriction;
        }

        public unsafe void SetDynamicFriction(int i, float dynamicFriction)
        {
            GetContact(i)->dynamicFriction = dynamicFriction;
            GetContactPatch()->internalFlags |= (byte)ModifiableContactPatch.Flags.RegeneratePatches;
        }

        public unsafe float GetMaxImpulse(int i)
        {
            return GetContact(i)->maxImpulse;
        }

        public unsafe void SetMaxImpulse(int i, float value)
        {
            GetContact(i)->maxImpulse = value;
            GetContactPatch()->internalFlags |= (byte)ModifiableContactPatch.Flags.HasMaxImpulse;
        }

        public void IgnoreContact(int i)
        {
            SetMaxImpulse(i, 0);
        }

        public unsafe uint GetFaceIndex(int i)
        {
            if ((GetContactPatch()->internalFlags & (byte)ModifiableContactPatch.Flags.HasFaceIndices) != 0)
            {
                // See PxContactModifyCallback.h:150 for details on this
                var item = new IntPtr(contacts.ToInt64() + numContacts * sizeof(ModifiableContact) + (numContacts + i) * sizeof(int));
                uint rawIndex = *(uint*)item;

                return TranslateTriangleIndex(otherShape, rawIndex);
            }

            return 0xffffFFFF;
        }

        private unsafe ModifiableContact* GetContact(int index)
        {
            var item = new IntPtr(contacts.ToInt64() + index * sizeof(ModifiableContact));
            return (ModifiableContact*)item;
        }

        private unsafe ModifiableContactPatch* GetContactPatch()
        {
            var item = new IntPtr(contacts.ToInt64() - numContacts * sizeof(ModifiableContactPatch));
            return (ModifiableContactPatch*)item;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ModifiableMassProperties
    {
        public float inverseMassScale;
        public float inverseInertiaScale;
        public float otherInverseMassScale;
        public float otherInverseInertiaScale;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ModifiableContact
    {
        public Vector3 contact;
        public float separation;
        public Vector3 targetVelocity;
        public float maxImpulse;
        public Vector3 normal;
        public float restitution;
        public uint materialFlags;
        public ushort materialIndex;
        public ushort otherMaterialIndex;
        public float staticFriction;
        public float dynamicFriction;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ModifiableContactPatch
    {
        // values have to match PxContactPatchFlags from External/PhysX/builds/Include/PxContact.h
        public enum Flags
        {
            HasFaceIndices = 1,
            HasModifiedMassRatios = 8,
            HasTargetVelocity = 16,
            HasMaxImpulse = 32,
            RegeneratePatches = 64,
        };

        public ModifiableMassProperties massProperties;

        public Vector3 normal;
        public float restitution;

        public float dynamicFriction;
        public float staticFriction;
        public byte startContactIndex;
        public byte contactCount;

        public byte materialFlags;
        public byte internalFlags;
        public ushort materialIndex;
        public ushort otherMaterialIndex;
    }
}
