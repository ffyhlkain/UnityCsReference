// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using UnityEngine.UIElements.Experimental;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Interface for class capable of handling events.
    /// </summary>
    public interface IEventHandler
    {
        /// <summary>
        /// Sends an event to the event handler.
        /// </summary>
        /// <remarks>
        /// The event is forwarded to the event dispatcher for processing.
        /// For more information, refer to [[wiki:UIE-Events-Synthesizing|Synthesize and send events]].
        /// </remarks>
        /// <remarks>
        /// SA: [[IEventHandler.HandleEvent]], [[EventDispatcher]], [[EventBase]]
        /// </remarks>
        /// <param name="e">The event to send.</param>
        void SendEvent(EventBase e);

        /// <summary>
        /// Handles an event according to its propagation phase and current target, by executing the element's
        /// default action or callbacks associated with the event.
        /// </summary>
        /// <remarks>
        /// The <see cref="EventDispatcher"/> might invoke this method multiple times for the same event: once for each
        /// propagation phase and each target along the event's propagation path if it has matching callbacks or
        /// overrides default actions for the event.
        ///
        /// __Note:__ Do not use this method to intercept all events whose propagation path includes this element. There is no
        /// guarantee that it will or will not be invoked for a propagation phase or target along the propagation path
        /// if that target has no callbacks for the event and has no default action override that can receive the event.
        /// </remarks>
        /// <remarks>
        /// SA: [[CallbackEventHandler.RegisterCallback]], [[CallbackEventHandler.HandleEventBubbleUp]]
        /// </remarks>
        /// <param name="evt">The event to handle.</param>
        void HandleEvent(EventBase evt);

        /// <summary>
        /// Returns true if event handlers, for the event propagation TrickleDown phase, are attached to this object.
        /// </summary>
        /// <returns>True if the object already has event handlers for the TrickleDown phase.</returns>
        bool HasTrickleDownHandlers();

        /// <summary>
        /// Returns true if event handlers for the event propagation BubbleUp phase, have been attached to this object.
        /// </summary>
        /// <returns>True if the object has event handlers for the BubbleUp phase.</returns>
        bool HasBubbleUpHandlers();
    }

    /// <summary>
    /// Interface for classes capable of having callbacks to handle events.
    /// </summary>
    public abstract class CallbackEventHandler : IEventHandler
    {
        // IMGUIContainers are special snowflakes that need custom treatment regarding events.
        // This enables early outs in some dispatching strategies.
        internal bool isIMGUIContainer = false;

        internal EventCallbackRegistry m_CallbackRegistry;

        /// <summary>
        /// Adds an event handler to the instance. If the event handler has already been registered for the same phase (either TrickleDown or BubbleUp) then this method has no effect.
        /// </summary>
        /// <param name="callback">The event handler to add.</param>
        /// <param name="useTrickleDown">By default, this callback is called during the BubbleUp phase. Pass @@TrickleDown.TrickleDown@@ to call this callback during the TrickleDown phase.</param>
        public void RegisterCallback<TEventType>(EventCallback<TEventType> callback, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown) where TEventType : EventBase<TEventType>, new()
        {
            if (callback == null)
                throw new ArgumentException("callback parameter is null");

            (m_CallbackRegistry ??= new EventCallbackRegistry()).RegisterCallback(callback, useTrickleDown, default);

            GlobalCallbackRegistry.RegisterListeners<TEventType>(this, callback, useTrickleDown);

            AddEventCategories<TEventType>(useTrickleDown);
        }

        /// <summary>
        /// Adds an event handler to the instance. If the event handler has already been registered for the same phase (either TrickleDown or BubbleUp) then this method has no effect.
        /// The event handler is automatically unregistered after it has been invoked exactly once.
        /// </summary>
        /// <param name="callback">The event handler to add.</param>
        /// <param name="useTrickleDown">By default, this callback is called during the BubbleUp phase. Pass @@TrickleDown.TrickleDown@@ to call this callback during the TrickleDown phase.</param>
        public void RegisterCallbackOnce<TEventType>(EventCallback<TEventType> callback, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown) where TEventType : EventBase<TEventType>, new()
        {
            if (callback == null)
                throw new ArgumentException("callback parameter is null");

            (m_CallbackRegistry ??= new EventCallbackRegistry()).RegisterCallback(callback, useTrickleDown, InvokePolicy.Once);

            GlobalCallbackRegistry.RegisterListeners<TEventType>(this, callback, useTrickleDown);

            AddEventCategories<TEventType>(useTrickleDown);
        }

        private void AddEventCategories<TEventType>(TrickleDown useTrickleDown) where TEventType : EventBase<TEventType>, new()
        {
            if (this is VisualElement ve)
            {
                ve.AddEventCallbackCategories(1 << (int)EventBase<TEventType>.EventCategory, useTrickleDown);
            }
        }

        /// <summary>
        /// Adds an event handler to the instance. If the event handler has already been registered for the same phase (either TrickleDown or BubbleUp) then this method has no effect.
        /// </summary>
        /// <param name="callback">The event handler to add.</param>
        /// <param name="userArgs">Data to pass to the callback.</param>
        /// <param name="useTrickleDown">By default, this callback is called during the BubbleUp phase. Pass @@TrickleDown.TrickleDown@@ to call this callback during the TrickleDown phase.</param>
        public void RegisterCallback<TEventType, TUserArgsType>(EventCallback<TEventType, TUserArgsType> callback, TUserArgsType userArgs, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown) where TEventType : EventBase<TEventType>, new()
        {
            if (callback == null)
                throw new ArgumentException("callback parameter is null");

            (m_CallbackRegistry ??= new EventCallbackRegistry()).RegisterCallback(callback, userArgs, useTrickleDown, default);

            GlobalCallbackRegistry.RegisterListeners<TEventType>(this, callback, useTrickleDown);

            AddEventCategories<TEventType>(useTrickleDown);
        }

        /// <summary>
        /// Adds an event handler to the instance. If the event handler has already been registered for the same phase (either TrickleDown or BubbleUp) then this method has no effect.
        /// The event handler is automatically unregistered after it has been invoked exactly once.
        /// </summary>
        /// <param name="callback">The event handler to add.</param>
        /// <param name="userArgs">Data to pass to the callback.</param>
        /// <param name="useTrickleDown">By default, this callback is called during the BubbleUp phase. Pass TrickleDown.TrickleDown to call this callback during the TrickleDown phase.</param>
        public void RegisterCallbackOnce<TEventType, TUserArgsType>(EventCallback<TEventType, TUserArgsType> callback, TUserArgsType userArgs, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown) where TEventType : EventBase<TEventType>, new()
        {
            if (callback == null)
                throw new ArgumentException("callback parameter is null");

            (m_CallbackRegistry ??= new EventCallbackRegistry()).RegisterCallback(callback, userArgs, useTrickleDown, InvokePolicy.Once);

            GlobalCallbackRegistry.RegisterListeners<TEventType>(this, callback, useTrickleDown);

            AddEventCategories<TEventType>(useTrickleDown);
        }

        internal void RegisterCallback<TEventType>(EventCallback<TEventType> callback, InvokePolicy invokePolicy, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown) where TEventType : EventBase<TEventType>, new()
        {
            (m_CallbackRegistry ??= new EventCallbackRegistry()).RegisterCallback(callback, useTrickleDown, invokePolicy);

            GlobalCallbackRegistry.RegisterListeners<TEventType>(this, callback, useTrickleDown);

            AddEventCategories<TEventType>(useTrickleDown);
        }

        /// <summary>
        /// Remove callback from the instance.
        /// </summary>
        /// <param name="callback">The callback to remove. If this callback was never registered, nothing happens.</param>
        /// <param name="useTrickleDown">Set this parameter to true to remove the callback from the TrickleDown phase. Set this parameter to false to remove the callback from the BubbleUp phase.</param>
        public void UnregisterCallback<TEventType>(EventCallback<TEventType> callback, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown) where TEventType : EventBase<TEventType>, new()
        {
            if (callback == null)
                throw new ArgumentException("callback parameter is null");

            m_CallbackRegistry?.UnregisterCallback(callback, useTrickleDown);

            GlobalCallbackRegistry.UnregisterListeners<TEventType>(this, callback);
        }

        /// <summary>
        /// Remove callback from the instance.
        /// </summary>
        /// <param name="callback">The callback to remove. If this callback was never registered, nothing happens.</param>
        /// <param name="useTrickleDown">Set this parameter to true to remove the callback from the TrickleDown phase. Set this parameter to false to remove the callback from the BubbleUp phase.</param>
        public void UnregisterCallback<TEventType, TUserArgsType>(EventCallback<TEventType, TUserArgsType> callback, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown) where TEventType : EventBase<TEventType>, new()
        {
            if (callback == null)
                throw new ArgumentException("callback parameter is null");

            m_CallbackRegistry?.UnregisterCallback(callback, useTrickleDown);

            GlobalCallbackRegistry.UnregisterListeners<TEventType>(this, callback);
        }

        /// <summary>
        /// Sends an event to the event handler. 
        /// </summary>
        /// <remarks>
        /// UI Toolkit sends events to visual elements through the panel. The event is sent to the event handler's registered callbacks. 
        /// For more information, refer to [[wiki:UIE-Events-Synthesizing|Synthesize and send events]].
        /// </remarks>
        /// <remarks>
        /// SA: [[UIElements.IEventHandler]]
        /// </remarks>
        /// <param name="e">The event to send.</param>
        public abstract void SendEvent(EventBase e);

        internal abstract void SendEvent(EventBase e, DispatchMode dispatchMode);

        internal abstract void HandleEvent(EventBase e);

        void IEventHandler.HandleEvent(EventBase evt)
        {
            // This is only useful because HandleEvent is public and can be called from user code.
            if (evt == null)
                return;

            HandleEvent(evt);
        }

        /// <summary>
        /// Returns true if event handlers, for the event propagation TrickleDown phase, are attached to this object.
        /// </summary>
        /// <returns>True if the object has event handlers for the TrickleDown phase.</returns>
        public bool HasTrickleDownHandlers()
        {
            return m_CallbackRegistry != null && m_CallbackRegistry.HasTrickleDownHandlers();
        }

        /// <summary>
        /// Return true if event handlers for the event propagation BubbleUp phase have been attached to this object.
        /// </summary>
        /// <returns>True if the object has event handlers for the BubbleUp phase.</returns>
        public bool HasBubbleUpHandlers()
        {
            return m_CallbackRegistry != null && m_CallbackRegistry.HasBubbleHandlers();
        }

        /// <summary>
        /// Executes logic after the callbacks registered on each element in the BubbleUp phase have been executed,
        /// unless the event propagation is stopped by one of the callbacks.
        /// <see cref="EventBase{T}.PreventDefault"/>.
        /// </summary>
        /// <remarks>
        /// This method is designed to be overriden by subclasses. Use it to implement event handling without
        /// registering callbacks, which guarantees precedences of callbacks registered by users of the subclass.
        ///
        /// Use <see cref="EventInterestAttribute"/> on this method to specify a range of event types that this
        /// method needs to receive. Events that don't fall into the specified types might not be sent to this method.
        /// </remarks>
        /// <param name="evt">The event instance.</param>
        [EventInterest(EventInterestOptions.Inherit)]
        [Obsolete("Use HandleEventBubbleUp. Before proceeding, make sure you understand the latest changes to " +
                  "UIToolkit event propagation rules by visiting Unity's manual page " +
                  "https://docs.unity3d.com/Manual/UIE-Events-Dispatching.html")]
        protected virtual void ExecuteDefaultActionAtTarget(EventBase evt) {}

        /// <summary>
        /// Executes logic on this element during the BubbleUp phase, immediately before this element's
        /// BubbleUp callbacks.
        /// Calling StopPropagation will prevent further invocations of this method along the propagation path.
        /// </summary>
        /// <remarks>
        /// This method is designed to be overriden by subclasses. Use it to implement event handling without
        /// registering callbacks, which guarantees precedences of callbacks registered by users of the subclass.
        ///
        /// Use <see cref="EventInterestAttribute"/> on this method to specify a range of event types that this
        /// method needs to receive. Events that don't fall into the specified types might not be sent to this method.
        /// </remarks>
        /// <param name="evt">The event instance.</param>
        [EventInterest(EventInterestOptions.Inherit)]
        protected virtual void HandleEventBubbleUp(EventBase evt) {}

        [EventInterest(EventInterestOptions.Inherit)]
        internal virtual void HandleEventBubbleUpDisabled(EventBase evt) {}
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void HandleEventBubbleUpInternal(EventBase evt) => HandleEventBubbleUp(evt);
        internal const string HandleEventBubbleUpName = nameof(HandleEventBubbleUp);

        /// <summary>
        /// Executes logic on this element during the TrickleDown phase, immediately after this element's
        /// TrickleDown callbacks.
        /// Calling StopPropagation will prevent further invocations of this method along the propagation path.
        /// </summary>
        /// <remarks>
        /// This method is designed to be overriden by subclasses. Use it to implement event handling without
        /// registering callbacks, which guarantees precedences of callbacks registered by users of the subclass.
        ///
        /// Use <see cref="EventInterestAttribute"/> on this method to specify a range of event types that this
        /// method needs to receive. Events that don't fall into the specified types might not be sent to this method.
        /// </remarks>
        /// <param name="evt">The event instance.</param>
        [EventInterest(EventInterestOptions.Inherit)]
        protected virtual void HandleEventTrickleDown(EventBase evt) {}

        [EventInterest(EventInterestOptions.Inherit)]
        internal virtual void HandleEventTrickleDownDisabled(EventBase evt) {}
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void HandleEventTrickleDownInternal(EventBase evt) => HandleEventTrickleDown(evt);
        internal const string HandleEventTrickleDownName = nameof(HandleEventTrickleDown);

        /// <summary>
        /// Executes logic after the callbacks registered on the event target have been executed,
        /// unless the event has been marked to prevent its default behaviour.
        /// <see cref="EventBase{T}.PreventDefault"/>.
        /// </summary>
        /// <remarks>
        /// This method is designed to be overriden by subclasses. Use it to implement event handling without
        /// registering callbacks which guarantees precedences of callbacks registered by users of the subclass.
        /// Unlike <see cref="HandleEventBubbleUp"/>, this method is called after both the callbacks registered
        /// on the element and callbacks registered on its ancestors with <see cref="TrickleDown.NoTrickleDown"/>.
        ///
        /// Use <see cref="EventInterestAttribute"/> on this method to specify a range of event types that this
        /// method needs to receive. Events that don't fall into the specified types might not be sent to this method.
        /// </remarks>
        /// <param name="evt">The event instance.</param>
        [EventInterest(EventInterestOptions.Inherit)]
        [Obsolete("Use HandleEventBubbleUp. Before proceeding, make sure you understand the latest changes to " +
                  "UIToolkit event propagation rules by visiting Unity's manual page " +
                  "https://docs.unity3d.com/Manual/UIE-Events-Dispatching.html")]
        protected virtual void ExecuteDefaultAction(EventBase evt) {}

        [EventInterest(EventInterestOptions.Inherit)]
        [Obsolete("Use HandleEventBubbleUpDisabled.")]
        internal virtual void ExecuteDefaultActionDisabledAtTarget(EventBase evt) {}

        [EventInterest(EventInterestOptions.Inherit)]
        [Obsolete("Use HandleEventBubbleUpDisabled.")]
        internal virtual void ExecuteDefaultActionDisabled(EventBase evt) {}

#pragma warning disable 618
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ExecuteDefaultActionInternal(EventBase evt) => ExecuteDefaultAction(evt);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ExecuteDefaultActionDisabledInternal(EventBase evt) => ExecuteDefaultActionDisabled(evt);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ExecuteDefaultActionAtTargetInternal(EventBase evt) => ExecuteDefaultActionAtTarget(evt);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ExecuteDefaultActionDisabledAtTargetInternal(EventBase evt) => ExecuteDefaultActionDisabledAtTarget(evt);

        internal const string ExecuteDefaultActionName = nameof(ExecuteDefaultAction);
        internal const string ExecuteDefaultActionAtTargetName = nameof(ExecuteDefaultActionAtTarget);
#pragma warning restore 618

        /// <summary>
        /// Informs the data binding system that a property of a control has changed.
        /// </summary>
        /// <param name="property">The property that has changed.</param>
        protected void NotifyPropertyChanged(in BindingId property)
        {
            var element = this as VisualElement;
            if (null == element?.elementPanel)
                return;

            using var evt = PropertyChangedEvent.GetPooled(property);
            evt.target = this;
            SendEvent(evt);
        }
    }
}
