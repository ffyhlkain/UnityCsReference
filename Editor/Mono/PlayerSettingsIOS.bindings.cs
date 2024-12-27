// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEditor.PlatformSupport;
using UnityEngine.Bindings;

using UnityEngine;
using UnityEditor.Build;

namespace UnityEditor
{
    // AppleMobile CPU architecture.
    // Matches enum in EditorOnlyPlayerSettings.h.
    // For Device
    [Flags]
    public enum AppleMobileArchitecture : uint
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("ARMv7 is no longer supported", true)]
        ARMv7 = 0,

        ARM64 = 1 << 0,
        Universal = 1 << 1
    }

    // For Simulator
    public enum AppleMobileArchitectureSimulator
    {
        X86_64 = 0,
        ARM64 = 1 << 0,
        Universal = 1 << 1
    }

    // Supported iOS SDK versions
    public enum iOSSdkVersion
    {
        // Device SDK
        DeviceSDK = 988,
        // Simulator SDK
        SimulatorSDK = 989
    }

    // Target iOS device
    public enum iOSTargetDevice
    {
        // iPhone/iPod Only
        iPhoneOnly = 0,

        // iPad Only
        iPadOnly = 1,

        // Universal : iPhone/iPod + iPad
        iPhoneAndiPad = 2,
    }

    // "Show Loading Indicator" enums for the Player Settings
    // Keep in sync with iOSDevice.bindings.cs ActivityIndicatorStyle which is the user script API
    public enum iOSShowActivityIndicatorOnLoading
    {
        // Don't Show
        DontShow = -1,

        // WhiteLarge - Deprecated
        [Obsolete("WhiteLarge Activity Indicator has been deprecated by Apple. Use Large instead (UnityUpgradable) -> Large", true)]
        WhiteLarge = 0,

        // White - Deprecated
        [Obsolete("White Activity Indicator has been deprecated by Apple. Use Medium instead (UnityUpgradable) -> Medium", true)]
        White = 1,

        // Gray - Deprecated
        [Obsolete("Gray Activity Indicator has been deprecated by Apple. Use Medium instead (UnityUpgradable) -> Medium", true)]
        Gray = 2,

        // Medium (Old White and Gray)
        Medium = 100,

        // Large (Old WhiteLarge)
        Large = 101,
    }

    // iOS status bar style
    public enum iOSStatusBarStyle
    {
        // Default syle: iOS picks the automatically based on the user interface style.
        Default = 0,

        // A light status bar, intended for use on dark backgrounds
        LightContent = 1,

        // A dark status bar, intended for use on light backgrounds
        DarkContent = 2,

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("BlackTranslucent has no effect, use LightContent instead (UnityUpgradable) -> LightContent", true)]
        BlackTranslucent = -1,

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("BlackOpaque has no effect, use LightContent instead (UnityUpgradable) -> LightContent", true)]
        BlackOpaque = -1,
    }

    public enum iOSAppInBackgroundBehavior
    {
        Custom = -1,
        Suspend = 0,

        [Obsolete("UIApplicationExitsOnSuspend is no longer supported in iOS 13, use Custom or Suspend instead (UnityUpgradable) -> Custom", true)]
        Exit = 1,
    }

    [Flags]
    public enum iOSBackgroundMode: uint
    {
        None                            = 0,
        AudioAirplayPiP                 = 1 << 0,
        LocationUpdates                 = 1 << 1,
        VoiceOverIP                     = 1 << 2,
        NewsstandDownloads              = 1 << 3,
        ExternalAccessoryCommunication  = 1 << 4,
        UsesBluetoothLEAccessory        = 1 << 5,
        ActsAsABluetoothLEAccessory     = 1 << 6,
        BackgroundFetch                 = 1 << 7,
        RemoteNotifications             = 1 << 8,
        Processing                      = 1 << 9,
        NearbyInteraction               = 1 << 10,
        NetworkAuthentication           = 1 << 11,
        PushToTalk                      = 1 << 12,

        // Obsolete/renamed values
        [Obsolete("iOSBackgroundMode.Audio has been deprecated. Use AudioAirplayPiP instead (UnityUpgradable) -> AudioAirplayPiP", true)]
        Audio                           = AudioAirplayPiP,
        [Obsolete("iOSBackgroundMode.Location has been deprecated. Use LocationUpdates instead (UnityUpgradable) -> LocationUpdates", true)]
        Location                        = LocationUpdates,
        [Obsolete("iOSBackgroundMode.VoIP has been deprecated. Use VoiceOverIP instead (UnityUpgradable) -> VoiceOverIP", true)]
        VoIP                            = VoiceOverIP,
        [Obsolete("iOSBackgroundMode.NewsstandContent has been deprecated. Use NewsstandDownloads instead (UnityUpgradable) -> NewsstandDownloads", true)]
        NewsstandContent                = NewsstandDownloads,
        [Obsolete("iOSBackgroundMode.ExternalAccessory has been deprecated. Use ExternalAccessoryCommunication instead (UnityUpgradable) -> ExternalAccessoryCommunication", true)]
        ExternalAccessory               = ExternalAccessoryCommunication,
        [Obsolete("iOSBackgroundMode.BluetoothCentral has been deprecated. Use UsesBluetoothLEAccessory instead (UnityUpgradable) -> UsesBluetoothLEAccessory", true)]
        BluetoothCentral                = UsesBluetoothLEAccessory,
        [Obsolete("iOSBackgroundMode.BluetoothPeripheral has been deprecated. Use ActsAsABluetoothLEAccessory instead (UnityUpgradable) -> ActsAsABluetoothLEAccessory", true)]
        BluetoothPeripheral             = ActsAsABluetoothLEAccessory,
        [Obsolete("iOSBackgroundMode.Fetch has been deprecated. Use BackgroundFetch instead (UnityUpgradable) -> BackgroundFetch", true)]
        Fetch                           = BackgroundFetch,
        [Obsolete("iOSBackgroundMode.RemoteNotification has been deprecated. Use RemoteNotifications instead (UnityUpgradable) -> RemoteNotifications", true)]
        RemoteNotification              = RemoteNotifications,
    }

    public enum iOSLaunchScreenImageType
    {
        iPhonePortraitImage = 0,
        iPhoneLandscapeImage = 1,
        iPadImage = 2,
    }

    // extern splash screen type (on iOS)
    public enum iOSLaunchScreenType
    {
        Default = 0,
        ImageAndBackgroundRelative = 1,
        ImageAndBackgroundConstant = 4,
        CustomStoryboard = 5,

        [Obsolete("CustomXib is no longer supported. Use CustomStoryboard instead", true)]
        CustomXib = 2,

        [Obsolete("Launch Images are no longer supported by Apple. (UnityUpgradable) -> Default", true)]
        None = 3,
    }

    internal enum iOSAutomaticallySignValue
    {
        AutomaticallySignValueNotSet = 0,
        AutomaticallySignValueTrue  = 1,
        AutomaticallySignValueFalse = 2
    }

    public enum ProvisioningProfileType
    {
        Automatic,
        Development,
        Distribution
    }

    public class iOSDeviceRequirement
    {
        SortedDictionary<string, string> m_Values = new SortedDictionary<string, string>();
        public IDictionary<string, string> values { get { return m_Values; } }
    }

    [NativeHeader("Runtime/Misc/PlayerSettings.h")]
    [NativeHeader("Editor/Src/PlayerSettingsIOS.bindings.h")]
    internal partial class iOSDeviceRequirementGroup
    {
        private PlayerSettings m_PlayerSettings;
        private string m_VariantName;

        [FreeFunction("PlayerSettingsIOSBindings::SetOrAddDeviceRequirementForVariantNameImpl")]
        extern private static void SetOrAddDeviceRequirementForVariantNameImpl(PlayerSettings playerSettings, string name, int index, [Unmarshalled] string[] keys, [Unmarshalled] string[] values);

        [FreeFunction("PlayerSettingsIOSBindings::RemoveIOSDeviceRequirementForVariantNameImpl")]
        extern private static void RemoveIOSDeviceRequirementForVariantNameImpl(PlayerSettings playerSettings, string name, int index);

        internal iOSDeviceRequirementGroup(string variantName)
            : this(null, variantName)
        {}

        internal iOSDeviceRequirementGroup(PlayerSettings playerSettings, string variantName)
        {
            m_PlayerSettings = playerSettings;
            m_VariantName = variantName;
        }

        [FreeFunction("PlayerSettingsIOSBindings::GetDeviceRequirementForVariantNameImpl")]
        extern private static void GetDeviceRequirementForVariantNameImplInternal(PlayerSettings playerSettings, string name, int index, [Out] string[] keys, [Out] string[] values);

        [FreeFunction("PlayerSettingsIOSBindings::GetDeviceRequirementForVariantCount")]
        extern private static int GetDeviceRequirementForVariantCount(PlayerSettings playerSettings, string name, int index);

        private static void GetDeviceRequirementForVariantNameImpl(PlayerSettings playerSettings, string name, int index, out string[] keys, out string[] values)
        {
            int requirementCount = GetDeviceRequirementForVariantCount(playerSettings, name, index);
            keys = new string[requirementCount];
            values = new string[requirementCount];
            GetDeviceRequirementForVariantNameImplInternal(playerSettings, name, index, keys, values);
        }

        public int count
        {
            get
            {
                return (m_PlayerSettings != null)
                    ? PlayerSettings.iOS.GetIOSDeviceRequirementCountForVariantName_Internal(m_PlayerSettings, m_VariantName)
                    : PlayerSettings.iOS.GetIOSDeviceRequirementCountForVariantName(m_VariantName);
            }
        }

        public iOSDeviceRequirement this[int index]
        {
            get
            {
                string[] keys;
                string[] values;
                GetDeviceRequirementForVariantNameImpl(m_PlayerSettings, m_VariantName, index, out keys, out values);
                var result = new iOSDeviceRequirement();
                for (int i = 0; i < keys.Length; ++i)
                {
                    result.values.Add(keys[i], values[i]);
                }
                return result;
            }
            set
            {
                SetOrAddDeviceRequirementForVariantNameImpl(m_PlayerSettings, m_VariantName, index, value.values.Keys.ToArray(),
                    value.values.Values.ToArray());
            }
        }

        public void RemoveAt(int index)
        {
            RemoveIOSDeviceRequirementForVariantNameImpl(m_PlayerSettings, m_VariantName, index);
        }

        public void Add(iOSDeviceRequirement requirement)
        {
            SetOrAddDeviceRequirementForVariantNameImpl(m_PlayerSettings, m_VariantName, -1, requirement.values.Keys.ToArray(),
                requirement.values.Values.ToArray());
        }
    }


    // Player Settings is where you define various parameters for the final game that you will build in Unity. Some of these values are used in the Resolution Dialog that launches when you open a standalone game.
    public partial class PlayerSettings : UnityEngine.Object
    {
        // iOS specific player settings
        [NativeHeader("Runtime/Misc/PlayerSettings.h")]
        [NativeHeader("Editor/Src/EditorUserBuildSettings.h")]
        [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
        public partial class iOS
        {
            // iOS application display name
            [NativeProperty("ProductName")]
            public extern static string applicationDisplayName { get; set; }

            // iOS bundle build number
            public static string buildNumber
            {
                get { return PlayerSettings.GetBuildNumber(NamedBuildTarget.iOS.TargetName); }
                set { PlayerSettings.SetBuildNumber(NamedBuildTarget.iOS.TargetName, value); }
            }
            public extern static bool disableDepthAndStencilBuffers  { get; set; }

            // Script calling optimization
            private extern static int  scriptCallOptimizationInternal
            {
                [NativeMethod("GetiPhoneScriptCallOptimization")]
                get;
                [NativeMethod("SetiPhoneScriptCallOptimization")]
                set;
            }
            public static ScriptCallOptimizationLevel scriptCallOptimization
            {
                get { return (ScriptCallOptimizationLevel)scriptCallOptimizationInternal; }
                set { scriptCallOptimizationInternal = (int)value; }
            }

            // Active iOS SDK version used for build
            private extern static int sdkVersionInternal
            {
                [NativeMethod("GetiPhoneSdkVersion")]
                get;
                [NativeMethod("SetiPhoneSdkVersion")]
                set;
            }
            public static iOSSdkVersion sdkVersion
            {
                get { return (iOSSdkVersion)sdkVersionInternal; }
                set { sdkVersionInternal = (int)value; }
            }

            // Simulator Architectures
            private extern static int simulatorSdkArchitectureInternal
            {
                [NativeMethod("GetiOSSimulatorArchitecture")]
                get;
                [NativeMethod("SetiOSSimulatorArchitecture")]
                set;
            }
            public static AppleMobileArchitectureSimulator simulatorSdkArchitecture
            {
                get { return (AppleMobileArchitectureSimulator)simulatorSdkArchitectureInternal; }
                set { simulatorSdkArchitectureInternal = (int)value; }
            }

            [FreeFunction]
            private extern static string iOSTargetOSVersionObsoleteEnumToString(int val);

            [FreeFunction]
            private extern static int iOSTargetOSVersionStringToObsoleteEnum(string val);

            // Deployment minimal version of iOS
            [Obsolete("OBSOLETE warning targetOSVersion is obsolete, use targetOSVersionString")]
            public static iOSTargetOSVersion targetOSVersion
            {
                get
                {
                    return (iOSTargetOSVersion)iOSTargetOSVersionStringToObsoleteEnum(targetOSVersionString);
                }
                set
                {
                    targetOSVersionString = iOSTargetOSVersionObsoleteEnumToString((int)value);
                }
            }

            [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
            [NativeMethod("GetiOSMinimumVersionString")]
            static extern string GetMinimumVersionString();

            internal static readonly Version minimumOsVersion = new Version(GetMinimumVersionString());

            [NativeProperty("iOSTargetOSVersion")]
            public extern static string targetOSVersionString
            {
                [NativeMethod("GetiOSTargetOSVersion")]
                get;
                [NativeMethod("SetiOSTargetOSVersion")]
                set;
            }

            // Targeted device
            [NativeProperty("TargetDevice")]
            private extern static int  targetDeviceInternal { get; set; }


            // Active iOS SDK version used for build
            public static iOSTargetDevice targetDevice
            {
                get { return (iOSTargetDevice)targetDeviceInternal; }
                set { targetDeviceInternal = (int)value; }
            }

            // Icon is prerendered
            [NativeProperty("UIPrerenderedIcon")]
            public extern static bool prerenderedIcon { get; set; }

            // Application requires persistent WiFi
            [NativeProperty("UIRequiresPersistentWiFi")]
            public extern static bool requiresPersistentWiFi  { get; set; }

            // Require Full Screen on iOS for iOS 9.0 Multitasking support
            [NativeProperty("UIRequiresFullScreen")]
            public extern static bool requiresFullScreen  { get; set; }

            // Status bar style
            [NativeProperty("UIStatusBarStyle")]
            private extern static int  statusBarStyleInternal { get; set; }


            [NativeProperty("UIStatusBarStyle")]
            public static iOSStatusBarStyle statusBarStyle
            {
                get { return (iOSStatusBarStyle)statusBarStyleInternal; }
                set { statusBarStyleInternal = (int)value; }
            }

            // On iPhone 10 the home button is implemented as a system gesture. (swipe up
            // from the lower edge). This might interfere with games that use swipes as
            // an interaction method. iOS provides a way to reduce the chance of unwanted
            // interaction by marking edges as "protected" edges, so the system gesture
            // is not recognized on the first swipe, but on the second if it comes
            // immediately afterwards.
            [NativeProperty("DeferSystemGesturesMode")]
            private extern static int deferSystemGesturesModeInternal { get; set; }

            public static UnityEngine.iOS.SystemGestureDeferMode deferSystemGesturesMode
            {
                get { return (UnityEngine.iOS.SystemGestureDeferMode)deferSystemGesturesModeInternal; }
                set { deferSystemGesturesModeInternal = (int)value; }
            }

            [NativeProperty("HideHomeButton")]
            public extern static bool hideHomeButton { get; set; }

            [NativeProperty("IOSAppInBackgroundBehavior")]
            private extern static int  appInBackgroundBehaviorInternal
            {
                [FreeFunction("PlayerSettingsIOSBindings::GetAppInBackgroundBehavior")]
                get;

                [FreeFunction("PlayerSettingsIOSBindings::SetAppInBackgroundBehavior")]
                set;
            }

            public static iOSAppInBackgroundBehavior appInBackgroundBehavior
            {
                get { return (iOSAppInBackgroundBehavior)appInBackgroundBehaviorInternal; }
                set { appInBackgroundBehaviorInternal = (int)value; }
            }

            [NativeProperty("IOSBackgroundModes")]
            private extern static int  backgroundModesInternal { get; set; }

            [NativeProperty("IOSAppInBackgroundBehavior")]
            public static iOSBackgroundMode backgroundModes
            {
                get { return (iOSBackgroundMode)backgroundModesInternal; }
                set { backgroundModesInternal = (int)value; }
            }

            [NativeProperty("IOSMetalForceHardShadows")]
            public extern static bool forceHardShadowsOnMetal
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            [Obsolete("Use PlayerSettings.insecureHttpOption", false)]
            public static bool allowHTTPDownload
            {
                get { return PlayerSettings.insecureHttpOption == InsecureHttpOption.AlwaysAllowed; }
                set { PlayerSettings.insecureHttpOption = value ? InsecureHttpOption.AlwaysAllowed : InsecureHttpOption.NotAllowed; }
            }

            [NativeProperty("AppleDeveloperTeamID")]
            private extern static string appleDeveloperTeamIDInternal
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            public static string appleDeveloperTeamID
            {
                get
                {
                    return appleDeveloperTeamIDInternal.Length < 1 ?
                        EditorPrefs.GetString("DefaultiOSAutomaticSignTeamId") : appleDeveloperTeamIDInternal;
                }
                set { appleDeveloperTeamIDInternal = value; }
            }


            [NativeProperty("iOSManualProvisioningProfileID")]
            private extern static string iOSManualProvisioningProfileIDInternal
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            public static string iOSManualProvisioningProfileID
            {
                get
                {
                    return String.IsNullOrEmpty(iOSManualProvisioningProfileIDInternal) ?
                        EditorPrefs.GetString("DefaultiOSProvisioningProfileUUID") : iOSManualProvisioningProfileIDInternal;
                }
                set
                {
                    iOSManualProvisioningProfileIDInternal = value;
                }
            }


            [NativeProperty("tvOSManualProvisioningProfileID")]
            private extern static string tvOSManualProvisioningProfileIDInternal
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            public static string tvOSManualProvisioningProfileID
            {
                get
                {
                    return String.IsNullOrEmpty(tvOSManualProvisioningProfileIDInternal) ?
                        EditorPrefs.GetString("DefaulttvOSProvisioningProfileUUID") : tvOSManualProvisioningProfileIDInternal;
                }
                set
                {
                    tvOSManualProvisioningProfileIDInternal = value;
                }
            }


            [NativeProperty("VisionOSManualProvisioningProfileID")]
            private extern static string VisionOSManualProvisioningProfileIDInternal
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            public static string VisionOSManualProvisioningProfileID
            {
                get
                {
                    return String.IsNullOrEmpty(VisionOSManualProvisioningProfileIDInternal) ?
                        EditorPrefs.GetString("DefaultVisionOSProvisioningProfileUUID") : VisionOSManualProvisioningProfileIDInternal;
                }
                set
                {
                    VisionOSManualProvisioningProfileIDInternal = value;
                }
            }

            [NativeProperty("tvOSManualProvisioningProfileType")]
            public static extern ProvisioningProfileType tvOSManualProvisioningProfileType
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            [NativeProperty("iOSManualProvisioningProfileType")]
            public static extern ProvisioningProfileType iOSManualProvisioningProfileType
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            [NativeProperty("VisionOSManualProvisioningProfileType")]
            public static extern ProvisioningProfileType VisionOSManualProvisioningProfileType
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            [NativeProperty("AppleEnableAutomaticSigning")]
            private extern static int appleEnableAutomaticSigningInternal
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            public static bool appleEnableAutomaticSigning
            {
                get
                {
                    return appleEnableAutomaticSigningInternal == (int)iOSAutomaticallySignValue.AutomaticallySignValueNotSet ?
                        EditorPrefs.GetBool("DefaultiOSAutomaticallySignBuild") :
                        (iOSAutomaticallySignValue)appleEnableAutomaticSigningInternal == iOSAutomaticallySignValue.AutomaticallySignValueTrue;
                }
                set
                {
                    appleEnableAutomaticSigningInternal = value ?
                        (int)iOSAutomaticallySignValue.AutomaticallySignValueTrue :
                        (int)iOSAutomaticallySignValue.AutomaticallySignValueFalse;
                }
            }


            [NativeProperty("CameraUsageDescription")]
            public extern static string cameraUsageDescription { get; set; }

            [NativeProperty("LocationUsageDescription")]
            public extern static string locationUsageDescription { get; set; }

            [NativeProperty("MicrophoneUsageDescription")]
            public extern static string microphoneUsageDescription { get; set; }

            [NativeProperty("IOSShowActivityIndicatorOnLoading")]
            private extern static int  showActivityIndicatorOnLoadingInternal { get; set; }

            // Application should show ActivityIndicator when loading
            [NativeProperty("IOSAppInBackgroundBehavior")]
            public static iOSShowActivityIndicatorOnLoading showActivityIndicatorOnLoading
            {
                get { return (iOSShowActivityIndicatorOnLoading)showActivityIndicatorOnLoadingInternal; }
                set { showActivityIndicatorOnLoadingInternal = (int)value; }
            }

            [NativeProperty("UseOnDemandResources")]
            public extern static bool useOnDemandResources  { get; set; }

            // will be public
            [NativeMethod(Name = "GetIOSVariantsWithDeviceRequirements")]
            extern internal static string[] GetAssetBundleVariantsWithDeviceRequirements();
            [NativeMethod(Name = "GetIOSVariantsWithDeviceRequirements_Internal")]
            [StaticAccessor("PlayerSettings", StaticAccessorType.DoubleColon)]
            extern internal static string[] GetAssetBundleVariantsWithDeviceRequirements_Internal(PlayerSettings instance);

            internal static extern int GetIOSDeviceRequirementCountForVariantName(string name);
            internal static extern int GetIOSDeviceRequirementCountForVariantName_Internal(PlayerSettings instance, string name);

            private static bool CheckAssetBundleVariantHasDeviceRequirements(string name)
            {
                return GetIOSDeviceRequirementCountForVariantName(name) > 0;
            }
            private static bool CheckAssetBundleVariantHasDeviceRequirements_Internal(PlayerSettings instance, string name)
            {
                return GetIOSDeviceRequirementCountForVariantName_Internal(instance, name) > 0;
            }

            [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
            [NativeProperty("iOSLaunchScreenPortrait", TargetType.Field)]
            internal extern static Texture2D launchScreenPortrait { get; }

            [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
            [NativeProperty("iOSLaunchScreenLandscape", TargetType.Field)]
            internal extern static Texture2D launchScreenLandscape { get; }

            [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
            [NativeProperty("iOSLaunchScreeniPadImage", TargetType.Field)]
            internal extern static Texture2D launchScreeniPadImage { get; }

            [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
            [NativeMethod(Name = "SetiOSLaunchScreenImage")]
            private extern static void SetLaunchScreenImageInternal(Texture2D image, int type);

            public static void SetLaunchScreenImage(Texture2D image, iOSLaunchScreenImageType type)
            {
                SetLaunchScreenImageInternal(image, (int)type);
            }

            [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
            private extern static void SetiOSLaunchScreenType(int type, int device);

            public static void SetiPhoneLaunchScreenType(iOSLaunchScreenType type)
            {
                SetiOSLaunchScreenType((int)type, 0);
            }

            public static void SetiPadLaunchScreenType(iOSLaunchScreenType type)
            {
                SetiOSLaunchScreenType((int)type, 1);
            }

            // will be public
            internal static iOSDeviceRequirementGroup GetDeviceRequirementsForAssetBundleVariant(string name)
            {
                if (!CheckAssetBundleVariantHasDeviceRequirements(name))
                    return null;
                return new iOSDeviceRequirementGroup(name);
            }
            internal static iOSDeviceRequirementGroup GetDeviceRequirementsForAssetBundleVariant_Internal(PlayerSettings instance, string name)
            {
                if (!CheckAssetBundleVariantHasDeviceRequirements_Internal(instance, name))
                    return null;
                return new iOSDeviceRequirementGroup(instance, name);
            }

            // will be public
            internal static void RemoveDeviceRequirementsForAssetBundleVariant(string name)
            {
                var group = GetDeviceRequirementsForAssetBundleVariant(name);
                for (int i = 0; i < group.count; ++i)
                    group.RemoveAt(0);
            }

            // will be public
            internal static iOSDeviceRequirementGroup AddDeviceRequirementsForAssetBundleVariant(string name)
            {
                return new iOSDeviceRequirementGroup(name);
            }
            internal static iOSDeviceRequirementGroup AddDeviceRequirementsForAssetBundleVariant_Internal(PlayerSettings instance, string name)
            {
                return new iOSDeviceRequirementGroup(instance, name);
            }

            [NativeProperty("iOSURLSchemes", false, TargetType.Function)]
            public extern static string[] iOSUrlSchemes
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            [NativeProperty("iOSRequireARKit")]
            internal extern static bool requiresARKitSupport
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            internal extern static bool appleEnableProMotion
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }


            [NativeProperty("iOSAutomaticallyDetectAndAddCapabilities")]
            internal extern static bool automaticallyDetectAndAddCapabilities
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }


            internal static bool IsTargetVersionEqualOrHigher(Version requiredVersion)
            {
                Version requestedVersion;
                try
                {
                    requestedVersion = new Version(targetOSVersionString);
                }
                catch (Exception)
                {
                    requestedVersion = minimumOsVersion;
                }
                return requestedVersion >= requiredVersion;
            }

            internal static string[] GetURLSchemes()
            {
                return iOSUrlSchemes;
            }
        }
    }
}
