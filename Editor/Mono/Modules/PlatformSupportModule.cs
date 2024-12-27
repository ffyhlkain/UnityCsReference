// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.DeploymentTargets;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Build.Profile;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace UnityEditor.Modules
{
    internal interface IPlatformSupportModule
    {
        /// Returns name identifying a target, for ex., Metro, note this name should match prefix
        /// for extension module UnityEditor.Metro.Extensions.dll, UnityEditor.Metro.Extensions.Native.dll
        string TargetName { get; }

        /// Returns the filename of jam which should be executed when you're recompiling extensions
        /// from Editor using CTRL + L shortcut, for ex., WP8EditorExtensions, MetroEditorExtensions, etc
        string JamTarget { get; }

        IBuildTarget PlatformBuildTarget { get; }

        /// Returns an array of native libraries that are required by the extension and must be loaded
        /// by the editor.
        ///
        /// NOTE: If two different platform extensions return a native library with a same file name
        /// (regardless of the path), then only first one will be loaded. This is due to the fact that
        /// some platforms may require same native library, but we must ship a copy with both platforms,
        /// since our modularization and platform installers don't support shared stuff.
        string[] NativeLibraries { get; }

        /// Returns an array of assemblies that should be referenced by user's scripts. These will be
        /// referenced by editor scripts, and game scripts running in editor. Used to export additional
        /// platform specific editor API.
        string[] AssemblyReferencesForUserScripts { get; }

        // Returns an array of assemblies that should be included into C# project as references.
        // This is different from AssemblyReferencesForUserScripts by that most assembly references
        // are internal and not added to the C# project. On the other hand, certain assemblies
        // contain public API, and thus should be added to C# project.
        string[] AssemblyReferencesForEditorCsharpProject { get; }

        /// A human friendly version (eg. an incrementing number on each release) of the platform extension. Null/Empty if none available
        string ExtensionVersion { get; }

        // Names of displays to show in GameView and Camera inspector if the platform supports multiple displays. Return null if default names should be used.
        GUIContent[] GetDisplayNames();

        IBuildPostprocessor CreateBuildPostprocessor();

        // Returns an instance of IDeploymentTargetsExtension or null if not supported
        IDeploymentTargetsExtension CreateDeploymentTargetsExtension();

        // Returns an instance of IScriptingImplementations or null if only one scripting backend is supported
        IScriptingImplementations CreateScriptingImplementations();

        // Return an instance of ISettingEditorExtension or null if not used
        // See DefaultPlayerSettingsEditorExtension.cs for abstract implementation
        ISettingEditorExtension CreateSettingsEditorExtension();

        IAdaptiveVsyncSetting CreateAdaptiveSettingEditorExtension();

        // Return an instance of IPreferenceWindowExtension or null if not used
        IPreferenceWindowExtension CreatePreferenceWindowExtension();

        // Return an instance of IBuildWindowExtension or null if not used
        IBuildWindowExtension CreateBuildWindowExtension();

        ICompilationExtension CreateCompilationExtension();

        // Rather than null above, this returns a default extension if not used
        ITextureImportSettingsExtension CreateTextureImportSettingsExtension();

        IPluginImporterExtension CreatePluginImporterExtension();

        // Returns an instance of IBuildProfileExtension or null if not supported.
        IBuildProfileExtension CreateBuildProfileExtension();

        // Register platform specific Unity extensions
        // For ex., Metro specifc UnityEngine.Networking.dll which is different from the generic UnityEngine.Networking.dll
        void RegisterAdditionalUnityExtensions();

        // return valid object for this device. This ensures that API for certain operations is
        // still available even if device was removed, for example stopping remote support.
        IDevice CreateDevice(string id);

        // Called when build target supplied by this module is activated in the editor.
        //
        // NOTE: Keep in mind that due domain reloads and the way unity builds, calls on OnActive
        //     and OnDeactivate will be forced even if current build target isn't being changed.
        //
        // PERFORMANCE: This method will be called each time user starts the game, so use this
        //     only for lightweight code, like registering to events, etc.
        //
        // Currently (de)activation happens when:
        //     * User switches build target.
        //     * User runs build for current target.
        //     * Build is run through scripting API.
        //     * Scripts are recompiled and reloaded (due user's change, forced reimport, etc).
        //     * User clicks play in editor.
        //     * ... and possibly more I'm not aware of.
        void OnActivate();

        // Called when build target supplied by this module is deactivated in the editor.
        //
        // NOTE: Keep in mind that due domain reloads and the way unity builds, calls on OnActive
        //     and OnDeactivate will be forced even if current build target isn't being changed.
        //
        // PERFORMANCE: This method will be called each time user starts the game, so use this
        //     only for lightweight code, like unregistering from events, etc.
        //
        // For more info see OnActivate().
        void OnDeactivate();

        // Called when extension is loaded, on editor start or domain reload.
        //
        // PERFORMANCE: This will be called for all available platform extensions during each
        //     domain reload, including each time user starts the game, so use this only for
        //     lightweight code.
        void OnLoad();

        // Called when extension is unloaded, when editor is exited or before domain reload.
        //
        // PERFORMANCE: This will be called for all available platform extensions during each
        //     domain reload, including each time user starts the game, so use this only for
        //     lightweight code.
        void OnUnload();

        IEnumerable<ScriptAssemblyPlatform> GetExtraScriptAssemblyPlatforms(BuildTarget buildTarget);

        IEditorAnalyticsExtension GetEditorAnalyticsExtension();
    }

    internal interface IAdaptiveVsyncSetting
    {
        void AdaptiveVsyncUI(SerializedProperty currentSettings);
    }

    struct ScriptAssemblyPlatform
    {
        public string AssemblyNamePostfix;
        public string TargetNiceName;
        public int Subtarget;
    }

    internal interface IBuildPostprocessor
    {
        ILaunchReport LaunchPlayer(BuildLaunchPlayerArgs args);

        void PostProcess(BuildPostProcessArgs args, out BuildProperties outProperties);

        bool SupportsInstallInBuildFolder();

        bool SupportsLz4Compression();

        Compression GetDefaultCompression();

        // This is the place to make sure platform has everything it needs for the build.
        // Use EditorUtility.Display(Cancelable)ProgressBar when running long tasks (e.g. downloading SDK from internet).
        // Return non-empty string indicating error message to stop the build.
        /// <param name="buildOptions">Build details. Useful to get the location of the player to build, for instance.</param>
        string PrepareForBuild(BuildPlayerOptions buildOptions);

        void PostProcessCompletedBuild(BuildPostProcessArgs args);

        void UpdateBootConfig(BuildTarget target, BootConfigData config, BuildOptions options);

        // Return string.Empty if targeting a folder.
        string GetExtension(BuildTarget target, int subtarget, BuildOptions options);

        bool AddIconsToBuild(AddIconsArgs args);
    }

    internal interface IScriptingImplementations
    {
        // All supported scripting implementations. First is the default one.
        ScriptingImplementation[] Supported();

        // Scripting implementations exposed to the user.
        ScriptingImplementation[] Enabled();
        bool AllowIL2CPPCompilerConfigurationSelection();
    }

    internal class DefaultScriptingImplementations : IScriptingImplementations
    {
        public virtual ScriptingImplementation[] Supported()
        {
            return new[]
            {
                ScriptingImplementation.Mono2x,
                ScriptingImplementation.IL2CPP,
            };
        }

        public virtual ScriptingImplementation[] Enabled()
        {
            return Supported();
        }

        public virtual bool AllowIL2CPPCompilerConfigurationSelection()
        {
            return true;
        }
    }

    // Extension point to add/alter the SettingsEditorWindow class
    // If you are creating a new extension you should probably inherit from DefaultPlayerSettingsEditorExtension
    internal interface ISettingEditorExtension
    {
        void OnEnable(PlayerSettingsEditor settingsEditor);

        void ConfigurePlatformProfile(SerializedObject serializedProfile);

        bool CopyProjectSettingsPlayerSettingsToBuildProfile();

        bool IsPlayerSettingsDataEqualToProjectSettings();

        void OnActiveProfileChanged(BuildProfile previous, BuildProfile newProfile);

        bool HasPublishSection();

        // Leave blank if no contribution
        void PublishSectionGUI(float h, float midWidth, float maxWidth);

        bool HasIdentificationGUI();

        // Leave blank if no contribution
        void IdentificationSectionGUI();

        // Leave blank if no contribution
        void ConfigurationSectionGUI();

        // Leave blank if no contribution
        void RenderingSectionGUI();

        bool SupportsOrientation();

        bool SupportsStaticBatching();
        bool SupportsDynamicBatching();
        bool SupportsHighDynamicRangeDisplays();
        bool SupportsGfxJobModes();
        GraphicsJobMode AdjustGfxJobMode(GraphicsJobMode graphicsJobMode);

        bool CanShowUnitySplashScreen();

        void SplashSectionGUI();

        bool UsesStandardIcons();

        void IconSectionGUI();

        bool HasResolutionSection();

        void ResolutionSectionGUI(float h, float midWidth, float maxWidth);

        bool HasBundleIdentifier();

        bool SupportsMultithreadedRendering();

        void MultithreadedRenderingGUI(NamedBuildTarget targetGroup);

        bool SupportsCustomLightmapEncoding();

        bool SupportsCustomNormalMapEncoding();

        bool ShouldShowVulkanSettings();

        void VulkanSectionGUI();

        bool SupportsFrameTimingStatistics();

        void SerializedObjectUpdated();

        bool SupportsForcedSrgbBlit();

        bool SupportsStaticSplashScreenBackgroundColor();

        void AutoRotationSectionGUI();
    }


    // Extension point to add preferences to the PreferenceWindow class
    internal interface IPreferenceWindowExtension
    {
        // Called from PreferenceWindow whenever preferences should be read
        void ReadPreferences();

        // Called from PreferenceWindow whenever preferences should be written
        void WritePreferences();

        // True is this extension contributes an external application/tool preference(s)
        bool HasExternalApplications();

        // Called from OnGui - this function should draw any contributing UI components
        void ShowExternalApplications();
    }

    // NOTE: You probably want to inherit from DefaultBuildWindowExtension class
    internal interface IBuildWindowExtension
    {
        void ShowPlatformBuildOptions();

        void ShowPlatformBuildWarnings();

        // Use this for "developer" Unity builds
        void ShowInternalPlatformBuildOptions();

        bool EnabledBuildButton();

        bool EnabledBuildAndRunButton();

        void GetBuildButtonTitles(out GUIContent buildButtonTitle, out GUIContent buildAndRunButtonTitle);

        // Show path location dialog during Build or Build & Run click
        bool AskForBuildLocation();

        bool ShouldDrawRunLastBuildButton();
        void DoRunLastBuildButtonGui();

        bool ShouldDrawScriptDebuggingCheckbox();

        bool ShouldDrawProfilerCheckbox();

        bool ShouldDrawDevelopmentPlayerCheckbox();

        bool ShouldDrawExplicitNullCheckbox();

        bool ShouldDrawExplicitDivideByZeroCheckbox();

        bool ShouldDrawExplicitArrayBoundsCheckbox();

        // Force full optimisations for script complilation in Development builds.
        // Useful for forcing optimized compiler for IL2CPP when profiling.
        bool ShouldDrawForceOptimizeScriptsCheckbox();

        // Enables a dialog "Wait For Managed debugger", which halts program execution until managed debugger is connected
        bool ShouldDrawWaitForManagedDebugger();

        bool ShouldDrawManagedDebuggerFixedPort();

        // Grays out managed debugger options
        bool ShouldDisableManagedDebuggerCheckboxes();
    }

    // Extension point to add platform-specific texture import settings.
    // You probably want to inherit from DefaultTextureImportSettingsExtension
    internal interface ITextureImportSettingsExtension
    {
        void ShowImportSettings(BaseTextureImportPlatformSettings editor);
    }

    /// <summary>
    /// Describes a setting variant and if it should be selected in the UI initially.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
    internal class PreconfiguredSettingsVariant
    {
        public string Name { get; }
        public bool SelectedInitially { get; }
        public bool Selected { get; set; }

        public PreconfiguredSettingsVariant(string name, bool selectedInitially)
        {
            Name = name;
            SelectedInitially = selectedInitially;
            Selected = selectedInitially;
        }
    }

    // Interface for implementing platform specific setting in Build Profiles window.
    [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
    internal interface IBuildProfileExtension
    {
        BuildProfilePlatformSettingsBase CreateBuildProfilePlatformSettings();

        /// <summary>
        /// When editing a build profile asset, this method is invoked to render the UI for
        /// viewing and/or editing the platform specific settings. <see cref="BuildProfilePlatformSettingsBase"/>.
        /// </summary>
        /// <param name="serializedObject">
        /// Target Build Profile serialized object .
        /// </param>
        /// <param name="rootProperty">
        /// Property instance for <see cref="BuildProfile.platformBuildProfile"/>.
        /// </param>
        /// <param name="state">
        /// Workflow state for the Build Profile window.
        /// </param>
        /// <returns>
        /// Root visual element for the platform specific settings UI.
        /// </returns>
        VisualElement CreateSettingsGUI(
            SerializedObject serializedObject,
            SerializedProperty rootProperty,
            BuildProfileWorkflowState state);

        /// <summary>
        /// When editing a build profile asset, this method is invoked to render the
        /// warning help boxes for build related issues.
        /// </summary>
        /// <param name="serializedObject">
        /// Target Build Profile serialized object .
        /// </param>
        /// <param name="rootProperty">
        /// Property instance for <see cref="BuildProfile.platformBuildProfile"/>.
        /// </param>
        /// <returns>
        /// Root visual element for the platform specific warnings.
        /// </returns>
        VisualElement CreatePlatformBuildWarningsGUI(
            SerializedObject serializedObject,
            SerializedProperty rootProperty);

        /// <summary>
        /// Copy settings to the platform settings base we are passing. This is used, for example, when creating
        /// a new classic profile and we need to copy settings - that live in the managed side only - to it
        /// </summary>
        void CopyPlatformSettingsToBuildProfile(BuildProfilePlatformSettingsBase platformSettingsBase);

        /// <summary>
        /// Copy platform settings from build profile to platform specific setting asset.
        /// </summary>
        void CopyPlatformSettingsFromBuildProfile(BuildProfilePlatformSettingsBase platformSettings);

        /// <summary>
        /// The info message paired with the create build profile button.
        /// If platform does not override this message, TrText.sharedSettingsInfo will be used.
        /// </summary>
        /// <returns>
        /// Message to be displayed.
        /// </returns>
        string GetProfileInfoMessage();

        /// <summary>
        /// Provides the list of preconfigured settings variants to display in the platform browser window
        /// when creating a new build profile.
        /// </summary>
        PreconfiguredSettingsVariant[] GetPreconfiguredSettingsVariants();

        /// <summary>
        /// Gets called when a build profile is created from defaults or by duplication from a classic profile.
        /// </summary>
        void OnBuildProfileCreated(BuildProfile buildProfile, int preconfiguredSettingsVariant);

        void OnDisable();
    }

    // Interface for target device related operations
    internal interface IDevice
    {
        // Start remote support for this device
        RemoteAddress StartRemoteSupport();

        // Stop remote support for this device
        void StopRemoteSupport();

        // Start player connection support for this device. This only sets up ability to connect,
        // like setting up TCP tunneling over USB, getting remote device's IP, but it doesn't
        // actually make the connection. Only available if SupportsPlayerConnection is true.
        // Otherwise throws NotSupportedException.
        RemoteAddress StartPlayerConnectionSupport();

        // Stop player connection support for this device. Only available if SupportsPlayerConnection
        // is true. Otherwise throws NotSupportedException.
        void StopPlayerConnectionSupport();
    }

    internal struct RemoteAddress
    {
        public string ip;
        public int port;

        public RemoteAddress(string ip, int port)
        {
            this.ip = ip;
            this.port = port;
        }
    }

    internal interface IPluginImporterExtension
    {
        // Functions use by PluginImporterInspector
        void ResetValues(PluginImporterInspector inspector);
        bool HasModified(PluginImporterInspector inspector);
        void Apply(PluginImporterInspector inspector);
        void OnEnable(PluginImporterInspector inspector);
        void OnDisable(PluginImporterInspector inspector);
        void OnPlatformSettingsGUI(PluginImporterInspector inspector);

        // Called before building the player, checks if plugins don't overwrite each other
        string CalculateFinalPluginPath(string buildTargetName, PluginImporter imp);
        bool CheckFileCollisions(string buildTargetName, string[] buildDefineConstraints);
    }

    internal struct BuildLaunchPlayerArgs
    {
        public BuildTarget target;
        public string playerPackage;
        public string installPath;
        public string productName;
        public BuildOptions options;
        public Build.Reporting.BuildReport report;
    }

    internal struct BuildPostProcessArgs
    {
        public BuildTarget target;
        public int subtarget;
        public string stagingArea;
        public string stagingAreaData;
        public string stagingAreaDataManaged;
        public string playerPackage;
        public string installPath;
        public string companyName;
        public string productName;
        public Guid productGUID;
        public BuildOptions options;
        public Build.Reporting.BuildReport report;
        internal RuntimeClassRegistry usedClassRegistry;
    }

    internal struct AddIconsArgs
    {
        public string stagingArea;
    }

    internal interface ICompilationExtension
    {
        string[] GetCompilerExtraAssemblyPaths(bool isEditor, string assemblyPathName);

        // Returns an array of windows metadata files (.winmd) that should be referenced when compiling scripts.
        // Only WinRT based platforms need these references.
        IEnumerable<string> GetWindowsMetadataReferences();

        // Returns an array of managed assemblies that should be referenced when compiling scripts
        // Currently, only .NET scripting backend uses it to include WinRTLegacy.dll into compilation
        IEnumerable<string> GetAdditionalAssemblyReferences();

        // Returns an array of defines that should be used when compiling scripts
        IEnumerable<string> GetAdditionalDefines();

        // Returns an array of defines that should be used when compiling scripts for the editor
        IEnumerable<string> GetAdditionalEditorDefines();

        // Returns an array of C# source files that should be included into the assembly when compiling scripts
        IEnumerable<string> GetAdditionalSourceFiles();
    }

    internal interface IEditorAnalyticsExtension
    {
        void AddExtraBuildAnalyticsFields(IntPtr eventData, BuildOptions buildOptions);
    }
}
