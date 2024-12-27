// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Interface for Manipulator objects.
    /// </summary>
    /// <remarks>For more information, refer to [[wiki:UIE-manipulators|Manipulators]] in the User Manual.</remarks>
    public interface IManipulator
    {
        /// <summary>
        /// VisualElement being manipulated.
        /// </summary>
        /// <remarks>For more information, refer to [[wiki:UIE-manipulators|Manipulators]] in the User Manual.</remarks>
        VisualElement target { get; set; }
    }

    /// <summary>
    /// Base class for all Manipulator implementations.
    /// </summary>
    /// <remarks>For more information, refer to [[wiki:UIE-manipulators|Manipulators]] in the User Manual.</remarks>
    public abstract class Manipulator : IManipulator
    {
        /// <summary>
        /// Called to register event callbacks on the target element.
        /// </summary>
        /// <remarks>For more information, refer to [[wiki:UIE-manipulators|Manipulators]] in the User Manual.</remarks>
        protected abstract void RegisterCallbacksOnTarget();
        /// <summary>
        /// Called to unregister event callbacks from the target element.
        /// </summary>
        /// <remarks>For more information, refer to [[wiki:UIE-manipulators|Manipulators]] in the User Manual.</remarks>
        protected abstract void UnregisterCallbacksFromTarget();

        VisualElement m_Target;
        /// <summary>
        /// VisualElement being manipulated.
        /// </summary>
        /// <remarks>For more information, refer to [[wiki:UIE-manipulators|Manipulators]] in the User Manual.</remarks>
        public VisualElement target
        {
            get { return m_Target; }
            set
            {
                if (target != null)
                {
                    UnregisterCallbacksFromTarget();
                }
                m_Target = value;
                if (target != null)
                {
                    RegisterCallbacksOnTarget();
                }
            }
        }
    }
}
