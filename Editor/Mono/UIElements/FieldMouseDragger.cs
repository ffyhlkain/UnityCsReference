// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Provides the base class for field mouse draggers.
    /// </summary>
    public abstract class BaseFieldMouseDragger
    {
        /// <summary>
        /// Sets the drag zone for the driven field.
        /// </summary>
        /// <param name="dragElement">The target of the drag operation.</param>
        public void SetDragZone(VisualElement dragElement)
        {
            SetDragZone(dragElement, new Rect(0, 0, -1, -1));
        }

        /// <summary>
        /// Sets the drag zone for the driven field.
        /// </summary>
        /// <param name="dragElement">The target of the drag operation.</param>
        /// <param name="hotZone">The rectangle that contains the drag zone.</param>
        public abstract void SetDragZone(VisualElement dragElement, Rect hotZone);
    }

    /// <summary>
    /// Provides dragging on a visual element to change a value field.
    /// </summary>
    /// <description>
    /// To create a field mouse dragger use <see cref="FieldMouseDragger{T}.FieldMouseDragger(IValueField{T})"/>
    /// and then set the drag zone using <see cref="BaseFieldMouseDragger.SetDragZone(VisualElement)"/>
    /// </description>
    public class FieldMouseDragger<T> : BaseFieldMouseDragger
    {
        public FieldMouseDragger(IValueField<T> drivenField)
        {
            m_DrivenField = drivenField;
            m_DragElement = null;
            m_DragHotZone = new Rect(0, 0, -1, -1);
            dragging = false;
        }

        IValueField<T> m_DrivenField;
        VisualElement m_DragElement;
        Rect m_DragHotZone;

        public bool dragging { get; set; }
        public T startValue { get; set; }

        /// <inheritdoc />
        public sealed override void SetDragZone(VisualElement dragElement, Rect hotZone)
        {
            if (m_DragElement != null)
            {
                m_DragElement.UnregisterCallback<MouseDownEvent>(UpdateValueOnMouseDown);
                m_DragElement.UnregisterCallback<MouseMoveEvent>(UpdateValueOnMouseMove);
                m_DragElement.UnregisterCallback<MouseUpEvent>(UpdateValueOnMouseUp);
                m_DragElement.UnregisterCallback<KeyDownEvent>(UpdateValueOnKeyDown);
            }

            m_DragElement = dragElement;
            m_DragHotZone = hotZone;

            if (m_DragElement != null)
            {
                dragging = false;
                m_DragElement.RegisterCallback<MouseDownEvent>(UpdateValueOnMouseDown);
                m_DragElement.RegisterCallback<MouseMoveEvent>(UpdateValueOnMouseMove);
                m_DragElement.RegisterCallback<MouseUpEvent>(UpdateValueOnMouseUp);
                m_DragElement.RegisterCallback<KeyDownEvent>(UpdateValueOnKeyDown);
            }
        }

        void UpdateValueOnMouseDown(MouseDownEvent evt)
        {
            if (evt.button == 0 && (m_DragHotZone.width < 0 || m_DragHotZone.height < 0 || m_DragHotZone.Contains(m_DragElement.WorldToLocal(evt.mousePosition))))
            {
                m_DragElement.CaptureMouse();

                // Make sure no other elements can capture the mouse!
                evt.StopPropagation();

                dragging = true;
                startValue = m_DrivenField.value;

                m_DrivenField.StartDragging();
                EditorGUIUtility.SetWantsMouseJumping(1);
            }
        }

        void UpdateValueOnMouseMove(MouseMoveEvent evt)
        {
            if (dragging)
            {
                DeltaSpeed s = evt.shiftKey ? DeltaSpeed.Fast : (evt.altKey ? DeltaSpeed.Slow : DeltaSpeed.Normal);
                m_DrivenField.ApplyInputDeviceDelta(evt.mouseDelta, s, startValue);
            }
        }

        void UpdateValueOnMouseUp(MouseUpEvent evt)
        {
            if (dragging)
            {
                dragging = false;
                IPanel panel = (evt.target as VisualElement)?.panel;
                panel.ReleasePointer(PointerId.mousePointerId);
                EditorGUIUtility.SetWantsMouseJumping(0);
                m_DrivenField.StopDragging();
            }
        }

        void UpdateValueOnKeyDown(KeyDownEvent evt)
        {
            if (dragging && evt.keyCode == KeyCode.Escape)
            {
                dragging = false;
                m_DrivenField.value = startValue;
                m_DrivenField.StopDragging();
                IPanel panel = (evt.target as VisualElement)?.panel;
                panel.ReleasePointer(PointerId.mousePointerId);
                EditorGUIUtility.SetWantsMouseJumping(0);
            }
        }
    }
}
