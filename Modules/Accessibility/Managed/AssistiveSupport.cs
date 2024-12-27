// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.Accessibility
{
    /// <summary>
    /// Access point to assistive technology support APIs.
    /// </summary>
    /// <remarks>
    /// The currently supported platforms are:
    ///
    ///- <see cref="RuntimePlatform.Android"/> (requires at least API level 26)
    ///- <see cref="RuntimePlatform.IPhonePlayer"/>
    ///
    /// This class contains static methods that allow users to support assistive technologies in the operating
    /// system (for example, the screen reader).
    /// </remarks>
    public static class AssistiveSupport
    {
        internal class NotificationDispatcher : IAccessibilityNotificationDispatcher
        {
            /// <summary>
            /// Sends the given notification to the operating system.
            /// </summary>
            /// <param name="context">The accessibility notification to be sent.</param>
            static void Send(in AccessibilityNotificationContext context)
            {
                AccessibilityManager.SendAccessibilityNotification(context);
            }

            public void SendAnnouncement(string announcement)
            {
                var notification = new AccessibilityNotificationContext
                {
                    notification = AccessibilityNotification.Announcement,
                    announcement = announcement
                };
                Send(notification);
            }

            /// <summary>
            /// Sends a notification to the screen reader conveying that a page was scrolled.
            /// </summary>
            /// <param name="announcement">The string containing a description of the new scroll position (for example,
            /// @@"Tab 3 of 5"@@ or @@"Page 19 of 27"@@).</param>
            public void SendPageScrolledAnnouncement(string announcement)
            {
                var notification = new AccessibilityNotificationContext
                {
                    notification = AccessibilityNotification.PageScrolled,
                    announcement = announcement
                };
                Send(notification);
            }

            public void SendScreenChanged(AccessibilityNode nodeToFocus = null)
            {
                var notification = new AccessibilityNotificationContext
                {
                    notification = AccessibilityNotification.ScreenChanged,
                    nextNodeId = nodeToFocus == null ? -1 : nodeToFocus.id
                };
                Send(notification);
            }

            public void SendLayoutChanged(AccessibilityNode nodeToFocus = null)
            {
                var notification = new AccessibilityNotificationContext
                {
                    notification = AccessibilityNotification.LayoutChanged,
                    nextNodeId = nodeToFocus == null ? -1 : nodeToFocus.id
                };
                Send(notification);
            }
        }

        /// <summary>
        /// Event that is invoked on the main thread when the screen reader focus changes.
        /// <para>For all the supported platforms, refer to <see cref="AssistiveSupport"/>.</para>
        /// </summary>
        public static event Action<AccessibilityNode> nodeFocusChanged;

        /// <summary>
        /// Event that is invoked on the main thread when the screen reader is enabled or disabled.
        /// <para>For all the supported platforms, refer to <see cref="AssistiveSupport"/>.</para>
        /// </summary>
        public static event Action<bool> screenReaderStatusChanged;

        /// <summary>
        /// Whether the screen reader is enabled on the operating system.
        /// <para>For all the supported platforms, refer to <see cref="AssistiveSupport"/>.</para>
        /// </summary>
        public static bool isScreenReaderEnabled { get; private set; }

        /// <summary>
        /// Service used to send accessibility notifications to the screen reader.
        /// <para>For all the supported platforms, refer to <see cref="AssistiveSupport"/>.</para>
        /// </summary>
        public static IAccessibilityNotificationDispatcher notificationDispatcher { get; } = new NotificationDispatcher();

        static ServiceManager s_ServiceManager;

        internal static void Initialize()
        {
            isScreenReaderEnabled = AccessibilityManager.IsScreenReaderEnabled();

            AccessibilityManager.screenReaderStatusChanged += ScreenReaderStatusChanged;
            AccessibilityManager.nodeFocusChanged += NodeFocusChanged;

            s_ServiceManager = new ServiceManager();
        }

        internal static T GetService<T>() where T : IService
        {
            if (s_ServiceManager == null)
            {
                return default;
            }

            return s_ServiceManager.GetService<T>();
        }

        internal static bool IsServiceRunning<T>() where T : IService
        {
            IService service = GetService<T>();

            return service != null;
        }

        internal static void SetApplicationAccessibilityLanguage(SystemLanguage language)
        {
            AccessibilityManager.SetApplicationAccessibilityLanguage(language);
        }

        static void ScreenReaderStatusChanged(bool screenReaderEnabled)
        {
            if (isScreenReaderEnabled == screenReaderEnabled)
            {
                return;
            }

            isScreenReaderEnabled = screenReaderEnabled;
            screenReaderStatusChanged?.Invoke(isScreenReaderEnabled);
        }

        static void NodeFocusChanged(AccessibilityNode currentNode)
        {
            nodeFocusChanged?.Invoke(currentNode);
        }

        /// <summary>
        /// The active AccessibilityHierarchy for the screen reader. May be @@null@@ if no hierarchy is active.
        /// <para>You need an active accessibility hierarchy to present any content to the user through the screen reader.</para>
        /// <para>If the screen reader is off, there is no active hierarchy. If the screen reader is turned off on the device
        /// while an active hierarchy is set, the active hierarchy is automatically set to @@null@@.</para>
        /// <para>For all the supported platforms, refer to <see cref="AssistiveSupport"/>.</para>
        /// </summary>
        /// <remarks>
        /// Throws @@PlatformNotSupportedException@@ if the screen reader support is not implemented for the
        /// platform and the code is not running in the Unity Editor.
        /// </remarks>
        /// <remarks>
        /// When the active hierarchy is assigned, a notification is sent to the operating system that the screen changed
        /// considerably. The notification is sent by calling <see
        /// cref="IAccessibilityNotificationDispatcher.SendScreenChanged"/> (with a @@null@@ parameter).
        /// </remarks>
        public static AccessibilityHierarchy activeHierarchy
        {
            set
            {
                CheckPlatformSupported();

                using var amlock = AccessibilityManager.GetExclusiveLock();
                var hierarchyService = GetService<AccessibilityHierarchyService>();
                if (hierarchyService != null)
                {
                    hierarchyService.hierarchy = value;
                    s_ActiveHierarchyChanged?.Invoke(value);
                }
            }
            get => GetService<AccessibilityHierarchyService>()?.hierarchy;
        }

        private static event Action<AccessibilityHierarchy> s_ActiveHierarchyChanged;

        /// <summary>
        /// Event sent when the active hierarchy is changed.
        /// </summary>
        internal static event Action<AccessibilityHierarchy> activeHierarchyChanged
        {
            [VisibleToOtherModules("UnityEditor.AccessibilityModule")]
            add { s_ActiveHierarchyChanged += value; }
            [VisibleToOtherModules("UnityEditor.AccessibilityModule")]
            remove { s_ActiveHierarchyChanged -= value; }
        }

        internal static void OnHierarchyNodeFramesRefreshed(AccessibilityHierarchy hierarchy)
        {
            if (activeHierarchy == hierarchy)
            {
                notificationDispatcher.SendLayoutChanged();
            }
        }

        static void CheckPlatformSupported()
        {
            // We accept Editor platform even though it is not actually supported yet in order to be able to debug
            // Accessibility hierarchy using the Accessibility Hierarchy Viewer.
            if (!Application.isEditor && !AccessibilityManager.isSupportedPlatform)
            {
                throw new PlatformNotSupportedException($"This API is not supported for platform {Application.platform}");
            }
        }
    }
}
