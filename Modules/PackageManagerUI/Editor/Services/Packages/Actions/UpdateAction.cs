// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal;

internal class UpdateAction : PackageAction
{
    private static readonly string k_UpdateToButtonTextFormat = L10n.Tr("Update to {0}");
    private static readonly string k_UpdatingToButtonTextFormat = L10n.Tr("Updating to {0}");
    private static readonly string k_UpdateToWithoutVersionButtonText = L10n.Tr("Update");
    private static readonly string k_UpdatingToWithoutVersionButtonText = L10n.Tr("Updating");

    private readonly bool m_ShowVersion;

    private readonly IPackageOperationDispatcher m_OperationDispatcher;
    private readonly IApplicationProxy m_Application;
    private readonly IPackageDatabase m_PackageDatabase;
    private readonly IPageManager m_PageManager;
    public UpdateAction(IPackageOperationDispatcher operationDispatcher,
        IApplicationProxy application,
        IPackageDatabase packageDatabase,
        IPageManager pageManager,
        bool showVersion = true)
    {
        m_OperationDispatcher = operationDispatcher;
        m_Application = application;
        m_PackageDatabase = packageDatabase;
        m_PageManager = pageManager;
        m_ShowVersion = showVersion;
    }

    // The target version refers to the version that will be installed when the user click on the `Update` button.
    // in the case where the user click this button directly in package details for an installed version, we want to
    // install the suggestedUpdate (if it exists), but for other non-installed versions in the version history tab,
    // we just want to install that version directly.
    public static IPackageVersion GetUpdateTarget(IPackageVersion version)
    {
        return version?.isInstalled == true ? version.package.versions.suggestedUpdate ?? version : version;
    }

    protected override bool TriggerActionImplementation(IList<IPackage> packages)
    {
        if (!m_OperationDispatcher.Install(packages.Select(p => GetUpdateTarget(p.versions.primary))))
            return false;
        // The current multi-select UI does not allow users to install non-recommended versions
        // Should this change in the future, we'll need to update the analytics event accordingly.
        PackageManagerWindowAnalytics.SendEvent("installUpdateRecommended", packages.Select(p => p.versions.primary));
        return true;
    }

    protected override bool TriggerActionImplementation(IPackageVersion version)
    {
        var installedVersion = version?.package.versions?.installed;
        var targetVersion = GetUpdateTarget(version);
        if (installedVersion != null && !installedVersion.isDirectDependency && installedVersion != targetVersion)
        {
            var featureSetDependents = m_PackageDatabase.GetFeaturesThatUseThisPackage(installedVersion);
            // if the installed version is being used by a Feature Set show the more specific
            //  Feature Set dialog instead of the generic one
            var title = string.Format(L10n.Tr("Updating {0}"), version.GetDescriptor());
            if (featureSetDependents.Any())
            {
                var message = string.Format(L10n.Tr("Changing a {0} that is part of a feature can lead to errors. Are you sure you want to proceed?"), version.GetDescriptor());
                if (!m_Application.DisplayDialog("updatePackagePartOfFeature", title, message, L10n.Tr("Yes"), L10n.Tr("No")))
                    return false;
            }
            else
            {
                var message = L10n.Tr("This version of the package is being used by other packages. Upgrading a different version might break your project. Are you sure you want to continue?");
                if (!m_Application.DisplayDialog("updatePackageUsedByOthers", title, message, L10n.Tr("Yes"), L10n.Tr("No")))
                    return false;
            }
        }

        IPackage[] packageToUninstall = null;
        if (targetVersion.HasTag(PackageTag.Feature))
        {
            var customizedDependencies = m_PackageDatabase.GetCustomizedDependencies(targetVersion, true);
            if (customizedDependencies.Any())
            {
                var packageNameAndVersions = string.Join("\n\u2022 ",
                    customizedDependencies.Select(package => $"{package.displayName} - {package.versions.recommended.version}").ToArray());

                var title = string.Format(L10n.Tr("Updating {0}"), version.GetDescriptor());
                var message = customizedDependencies.Length == 1 ?
                    string.Format(
                        L10n.Tr("This {0} includes a package version that is different from what's already installed. Would you like to reset the following package to the required version?\n\u2022 {1}"),
                        version.GetDescriptor(), packageNameAndVersions) :
                    string.Format(
                        L10n.Tr("This {0} includes package versions that are different from what are already installed. Would you like to reset the following packages to the required versions?\n\u2022 {1}"),
                        version.GetDescriptor(), packageNameAndVersions);

                var result = m_Application.DisplayDialogComplex("installAndReset", title, message, L10n.Tr("Install and Reset"), L10n.Tr("Cancel"), L10n.Tr("Install Only"));
                if (result == 1) // Cancel
                    return false;
                if (result == 0) // Install and reset
                    packageToUninstall = customizedDependencies;
            }
        }

        if (packageToUninstall?.Any() == true)
        {
            m_OperationDispatcher.InstallAndResetDependencies(targetVersion, packageToUninstall);
            PackageManagerWindowAnalytics.SendEvent("installAndReset", targetVersion);
        }
        else
        {
            if (!m_OperationDispatcher.Install(targetVersion))
                return false;

            var installRecommended = version.package.versions.recommended == targetVersion ? "Recommended" : "NonRecommended";
            var eventName = $"installUpdate{installRecommended}";
            PackageManagerWindowAnalytics.SendEvent(eventName, targetVersion);
        }
        return true;
    }

    public override bool IsVisible(IPackageVersion version)
    {
        var installed = version?.package.versions?.installed;
        var targetVersion = GetUpdateTarget(version);
        return installed?.HasTag(PackageTag.InstalledFromPath) == false
               && targetVersion?.HasTag(PackageTag.UpmFormat) == true
               && installed != targetVersion
               && !version.IsRequestedButOverriddenVersion
               && (version.isDirectDependency || version != installed)
               && !version.HasTag(PackageTag.Local)
               && m_PageManager.activePage.visualStates.Get(version.package?.uniqueId)?.isLocked != true;
    }

    public override string GetTooltip(IPackageVersion version, bool isInProgress)
    {
        if (isInProgress)
            return k_InProgressGenericTooltip;

        return string.Format(L10n.Tr("Click to update this {0} to the specified version."), version.GetDescriptor());
    }

    public override string GetText(IPackageVersion version, bool isInProgress)
    {
        if (!m_ShowVersion || m_PageManager.activePage.GetSelection().Count > 1)
            return isInProgress ? k_UpdatingToWithoutVersionButtonText : k_UpdateToWithoutVersionButtonText;

        return string.Format(isInProgress ? k_UpdatingToButtonTextFormat : k_UpdateToButtonTextFormat, GetUpdateTarget(version).version);
    }

    public override bool IsInProgress(IPackageVersion v) => m_OperationDispatcher.IsInstallInProgress(GetUpdateTarget(v));

    protected override IEnumerable<DisableCondition> GetAllTemporaryDisableConditions()
    {
        yield return new DisableIfInstallOrUninstallInProgress(m_OperationDispatcher);
        yield return new DisableIfCompiling(m_Application);
    }

    protected override IEnumerable<DisableCondition> GetAllDisableConditions(IPackageVersion version)
    {
        // We need to check the target version so that we don't disable the button in the details header
        yield return new DisableIfVersionDeprecated(GetUpdateTarget(version));
        yield return new DisableIfEntitlementsError(version);
    }
}
