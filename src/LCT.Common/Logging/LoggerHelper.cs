// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.Common.Constants;
using LCT.Common.Model;
using LCT.Common.Runtime;
using log4net;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Level = log4net.Core.Level;

namespace LCT.Common.Logging
{
    public static class LoggerHelper
    {
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static IAnsiConsole _console;
        private static readonly object _consoleLock = new();

        private static IAnsiConsole ConsoleInstance
        {
            get
            {
                if (_console != null) return _console;
                lock (_consoleLock)
                {
                    if (_console == null)
                    {
                        var settings = BuildAnsiConsoleSettings();
                        _console = AnsiConsole.Create(settings);
                        _console.Profile.Width = GetAutoConsoleWidth();
                        _console.Profile.Capabilities.Ansi = true;
                    }
                }
                return _console;
            }
        }
        private static int GetAutoConsoleWidth()
        {           

            try
            {
                int width = Console.IsOutputRedirected ? 160 : Console.WindowWidth;
                if (width < 100) width = 160;
                if (width > 240) width = 240;
                return width;
            }
            catch (InvalidOperationException ex)
            {
                Logger.Debug($"GetAutoConsoleWidth(): InvalidOperationException - {ex.Message}");
            }
            catch (System.IO.IOException ex)
            {
                Logger.Debug($"GetAutoConsoleWidth(): IOException - {ex.Message}");
            }
            catch (PlatformNotSupportedException ex)
            {
                Logger.Debug($"GetAutoConsoleWidth(): PlatformNotSupportedException - {ex.Message}");
            }
            catch (System.Security.SecurityException ex)
            {
                Logger.Debug($"GetAutoConsoleWidth(): SecurityException - {ex.Message}");
            }

            return 160;
        }
        private static AnsiConsoleSettings BuildAnsiConsoleSettings()
        {
            EnvironmentType envType = RuntimeEnvironment.GetEnvironment();

            var colorSystem = GetColorSystem(envType);

            var settings = new AnsiConsoleSettings
            {
                Ansi = AnsiSupport.Yes,
                ColorSystem = colorSystem,
                Out = AnsiConsole.Profile.Out,
                Interactive = InteractionSupport.No
            };
            return settings;
        }
        private static ColorSystemSupport GetColorSystem(EnvironmentType envType)
        {
            // User override
            var env = Environment.GetEnvironmentVariable("SPECTRE_COLOR");
            if (!string.IsNullOrEmpty(env))
            {
                return env.ToLowerInvariant() switch
                {
                    "standard" => ColorSystemSupport.Standard,
                    "256" => ColorSystemSupport.EightBit,
                    "truecolor" or "24bit" => ColorSystemSupport.TrueColor,
                    _ => ColorSystemSupport.Standard
                };
            }

            if (envType == EnvironmentType.AzurePipeline)
            {
                return ColorSystemSupport.Standard;
            }
            return ColorSystemSupport.TrueColor;
        }

        // Wrapper helper so existing code only changes AnsiConsole.* to ConsoleInstance.*
        private static void ConsoleWrite(Action<IAnsiConsole> writeAction)
        {
            writeAction(ConsoleInstance);
        }

        private static readonly Dictionary<string, string> _colorCache = new Dictionary<string, string>();
        private static int _colorIndex = 0;


        public static void SafeSpectreAction(Action spectreAction, string fallbackMessage, string fallbackType = "Info")
        {
            try
            {
                spectreAction();
            }
            catch (Exception ex) when (
                ex is InvalidOperationException ||
                ex is ArgumentException ||
                ex is System.IO.IOException ||
                ex is TargetInvocationException ||
                ex is NullReferenceException ||
                ex is NotSupportedException
            )
            {
                WriteFallback(fallbackMessage, fallbackType);
                Logger.Debug($"SafeSpectreAction suppressed exception: {ex.GetType().Name} - {ex.Message}");
            }
        }
        public static void WriteComponentsWithoutDownloadURLByUseingSpectreToKpi(List<ComparisonBomData> componentInfo, List<Components> lstReleaseNotCreated, string sw360URL, List<Components> duplicateComponentsByPurlId)
        {
            if (componentInfo.Count > 0 || lstReleaseNotCreated.Count > 0)
            {
                SafeSpectreAction(() =>
                {
                    WriteInfoWithMarkup("[yellow]Action Item required by the user:[/]\n");

                    DisplayComponentsWithoutUrl(componentInfo, sw360URL);
                    DisplayNotCreatedComponents(lstReleaseNotCreated);
                    DisplayDuplicateComponentsByPurlId(duplicateComponentsByPurlId, sw360URL);

                    var environmentHelper = new EnvironmentHelper();
                    environmentHelper.CallEnvironmentExit(2);

                }, "Components Without Download URL", "Alert");
            }
        }
        private static void DisplayDuplicateComponentsByPurlId(List<Components> duplicateComponents, string sw360URL)
        {
            if (duplicateComponents == null || duplicateComponents.Count == 0)
                return;

            WriteLine();
            WriteInfoWithMarkup("[yellow]* Components or releases not created due to invalid PurlIds in SW360 ExternalID field[/]");
            WriteInfoWithMarkup("[yellow]  Component name exists in SW360 with a different package type PurlId. Manually update the component details.[/]");
            WriteLine();

            int consoleWidth = GetAutoConsoleWidth();

            var table = new Table()
                .BorderColor(Color.Yellow)
                .Border(TableBorder.Rounded)
                .Title("[yellow]Invalid / Duplicate Components (PurlId Mismatch)[/]")
                .Width(Math.Min(consoleWidth, 200))
                .Expand();

            table.AddColumn(new TableColumn("[green]Name[/]").Width(45));
            table.AddColumn(new TableColumn("[blue]Version[/]").Width(25));
            table.AddColumn(new TableColumn("[cyan]SW360 Component URL[/]").Width(120));

            foreach (var item in duplicateComponents)
            {
                string name = string.IsNullOrWhiteSpace(item.Name) ? "N/A" : item.Name;
                string version = string.IsNullOrWhiteSpace(item.Version) ? "N/A" : item.Version;
                string link = CommonHelper.Sw360ComponentURL(sw360URL, item.ComponentId);

                table.AddRow(
                    $"[white]{Markup.Escape(name)}[/]",
                    $"[white]{Markup.Escape(version)}[/]",
                    $"[cyan]{Markup.Escape(link)}[/]"
                );
            }

            ConsoleWrite(c => c.Write(table));
            WriteLine();
        }
        private static void DisplayComponentsWithoutUrl(List<ComparisonBomData> componentInfo, string sw360URL)
        {
            if (componentInfo.Count <= 0) return;

            WriteLine();
            WriteInfoWithMarkup("[yellow]* List of components without source download URL:[/]");
            WriteInfoWithMarkup("[yellow] Update the source download URL & Upload the source code manually if the SRC attachment is missing for the component[/]");
            WriteLine();

            var table = CreateComponentTable(true);
            PopulateComponentInfoTable(table, componentInfo, sw360URL);

            ConsoleInstance.Write(table);
            WriteLine();
        }

        private static void DisplayNotCreatedComponents(List<Components> lstReleaseNotCreated)
        {
            if (lstReleaseNotCreated.Count <= 0) return;

            WriteLine();
            WriteInfoWithMarkup("[yellow]* List of components or releases not created in SW360:[/]");
            WriteInfoWithMarkup("[yellow]  There could be network/SW360/FOSSology server problem. Check and Re-Run the pipeline.Check the logs for more details[/]");
            WriteLine();

            var table = CreateComponentTable(false);
            PopulateNotCreatedComponentsTable(table, lstReleaseNotCreated);

            ConsoleInstance.Write(table);
            WriteLine();
        }

        private static Table CreateComponentTable(bool includeUrl)
        {
            var table = new Table()
                .BorderColor(Color.Yellow)
                .Border(TableBorder.Rounded)
                .Width(Math.Min(GetAutoConsoleWidth(), includeUrl ? 200 : 120));

            table.AddColumn(new TableColumn("[green]Name[/]").Width(45));
            table.AddColumn(new TableColumn("[blue]Version[/]").Width(25));

            if (includeUrl)
            {
                table.AddColumn(new TableColumn("[cyan]SW360 Release URL[/]").Width(120));
                table.Expand();
            }

            return table;
        }

        private static void PopulateComponentInfoTable(Table table, List<ComparisonBomData> componentInfo, string sw360URL)
        {
            foreach (var item in componentInfo)
            {
                string link = CommonHelper.Sw360URL(sw360URL, item.ReleaseID);
                table.AddRow(
                    Markup.Escape(item.Name),
                    Markup.Escape(item.Version),
                    Markup.Escape(link)
                );
            }
        }

        private static void PopulateNotCreatedComponentsTable(Table table, List<Components> components)
        {
            foreach (var item in components)
            {
                table.AddRow(
                    Markup.Escape(item.Name),
                    Markup.Escape(item.Version)
                );
            }
        }
        public static void WriteComponentsNotLinkedListTableWithSpectre(List<Components> components)
        {
            if (components.Count > 0)
            {
                SafeSpectreAction(() =>
                {
                    WriteLine();
                    WriteInfoWithMarkup("[yellow]* Components Not linked to project:[/]");
                    WriteInfoWithMarkup("[yellow] Can be linked manually OR Check the Logs AND RE-Run[/]");
                    WriteLine();

                    int consoleWidth = GetAutoConsoleWidth();

                    var table = new Table()
                        .BorderColor(Color.Yellow)
                        .Border(TableBorder.Rounded)
                        .Title("[yellow]Unlinked Components[/]")
                        .Width(Math.Min(consoleWidth, 120));

                    table.AddColumns(
                        new TableColumn("[green]Name[/]").Width(45),
                        new TableColumn("[blue]Version[/]").Width(35)
                    );

                    foreach (var item in components)
                    {
                        string componentName = !string.IsNullOrEmpty(item.Name) ? item.Name : "N/A";
                        string componentVersion = !string.IsNullOrEmpty(item.Version) ? item.Version : "N/A";

                        table.AddRow(
                            $"[white]{Markup.Escape(componentName)}[/]",
                            $"[cyan]{Markup.Escape(componentVersion)}[/]"
                        );
                    }

                    ConsoleInstance.Write(table);
                    WriteLine();

                    EnvironmentHelper environmentHelper = new EnvironmentHelper();
                    environmentHelper.CallEnvironmentExit(2);

                }, "Components Not Linked", "Alert");
            }
        }
        public static void WriteComponentsNotLinkedListInConsole(List<Components> components)
        {
            if (LoggerFactory.UseSpectreConsole)
            {
                WriteComponentsNotLinkedListTableWithSpectre(components);
                return;
            }
            const string Name = "Name";
            const string Version = "Version";

            if (components.Count > 0)
            {
                EnvironmentHelper environmentHelper = new EnvironmentHelper();
                environmentHelper.CallEnvironmentExit(2);
                Logger.Logger.Log(null, Level.Alert, "* Components Not linked to project :", null);
                Logger.Logger.Log(null, Level.Alert, " Can be linked manually OR Check the Logs AND RE-Run", null);
                Logger.Logger.Log(null, Level.Alert, $"{"=",5}{string.Join("", Enumerable.Repeat("=", 98)),5}", null);
                Logger.Logger.Log(null, Level.Alert, $"{"|",5}{Name,-45} {"|",5} {Version,35} {"|",10}", null);
                Logger.Logger.Log(null, Level.Alert, $"{"=",5}{string.Join("", Enumerable.Repeat("=", 98)),5}", null);

                foreach (var item in components)
                {
                    Logger.Logger.Log(null, Level.Alert, $"{"|",5}{item.Name,-45} {"|",5} {item.Version,35} {"|",10}", null);
                    Logger.Logger.Log(null, Level.Alert, $"{"-",5}{string.Join("", Enumerable.Repeat("-", 98)),5}", null);
                }
                Logger.Info("\n");
            }
        }

        public static void WriteComponentsWithoutDownloadURLToKpi(List<ComparisonBomData> componentInfo, List<Components> lstReleaseNotCreated, string sw360URL, List<Components> DuplicateComponentsByPurlId)
        {
            if (LoggerFactory.UseSpectreConsole)
            {
                WriteComponentsWithoutDownloadURLByUseingSpectreToKpi(componentInfo, lstReleaseNotCreated, sw360URL, DuplicateComponentsByPurlId);
                return;
            }

            const string Name = "Name";
            const string Version = "Version";
            const string URL = "SW360 Release URL";
            if (componentInfo.Count > 0 || lstReleaseNotCreated.Count > 0 || DuplicateComponentsByPurlId.Count > 0)
            {
                Logger.Logger.Log(null, Level.Alert, "Action Item required by the user:\n", null);
                EnvironmentHelper environmentHelper = new EnvironmentHelper();
                environmentHelper.CallEnvironmentExit(2);
            }

            if (componentInfo.Count > 0)
            {
                Logger.Logger.Log(null, Level.Alert, "* List of components without source download URL :", null);
                Logger.Logger.Log(null, Level.Alert, " Update the source download URL & Upload the source code manually if the SRC attachment is missing for the component", null);

                Logger.Logger.Log(null, Level.Alert, $"{"=",5}{string.Join("", Enumerable.Repeat("=", 206)),5}", null);
                Logger.Logger.Log(null, Level.Alert, $"{"|",5}{Name,-45} {"|",5} {Version,25} {"|",5}  {URL,-120}  {"|",-4}", null);
                Logger.Logger.Log(null, Level.Alert, $"{"=",5}{string.Join("", Enumerable.Repeat("=", 206)),5}", null);

                foreach (var item in componentInfo)
                {
                    string Link = CommonHelper.Sw360URL(sw360URL, item.ReleaseID);
                    Logger.Logger.Log(null, Level.Alert, $"{"|",5}{item.Name,-45} {"|",5} {item.Version,25} {"|",5} {Link,-120} {"|",-5}", null);
                    Logger.Logger.Log(null, Level.Alert, $"{"-",5}{string.Join("", Enumerable.Repeat("-", 206)),5}", null);
                }

                Logger.Info("\n");
            }

            if (lstReleaseNotCreated.Count > 0)
            {
                Logger.Logger.Log(null, Level.Alert, "* List of components or releases not created in SW360 :", null);
                Logger.Logger.Log(null, Level.Alert, "  There could be network/SW360/FOSSology server problem. Check and Re-Run the pipeline.Check the logs for more details", null);
                Logger.Logger.Log(null, Level.Alert, $"{"=",5}{string.Join("", Enumerable.Repeat("=", 86)),5}", null);
                Logger.Logger.Log(null, Level.Alert, $"{"|",5}{Name,45} {"|",5} {Version,25} {"|",8}", null);
                Logger.Logger.Log(null, Level.Alert, $"{"=",5}{string.Join("", Enumerable.Repeat("=", 86)),5}", null);

                foreach (var item in lstReleaseNotCreated)
                {
                    Logger.Logger.Log(null, Level.Alert, $"{"|",5}{item.Name,-45} {"|",5} {item.Version,25} {"|",8}", null);
                    Logger.Logger.Log(null, Level.Alert, $"{"-",5}{string.Join("", Enumerable.Repeat("-", 86)),5}", null);
                }
                Logger.Info("\n");
            }
            LogDuplicateComponentsByPurlId(DuplicateComponentsByPurlId, sw360URL);
        }
        public static void DisplayAllSettings(List<Component> componentsInBOM, CommonAppSettings appSettings)
        {
            var projectTypes = GetProjectTypes(componentsInBOM);
            var projectConfigMap = CreateProjectConfigMap(appSettings);

            if (LoggerFactory.UseSpectreConsole)
            {
                DisplaySettingsWithSpectre(projectTypes, projectConfigMap);
            }
            else
            {
                DisplaySettingsWithLogger(projectTypes, projectConfigMap);
            }
        }

        private static List<string> GetProjectTypes(List<Component> componentsInBOM)
        {
            if (componentsInBOM == null || componentsInBOM.Count == 0)
                return new List<string>();

            return [.. componentsInBOM
                .Select(c => c.Properties?
                    .FirstOrDefault(p => p.Name == Dataconstant.Cdx_ProjectType)?.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(CommonHelper.CanonicalizeProjectType)
                .Distinct(StringComparer.OrdinalIgnoreCase)];
        }

        private static Dictionary<string, Config> CreateProjectConfigMap(CommonAppSettings appSettings)
        {
            return new Dictionary<string, Config>(StringComparer.OrdinalIgnoreCase)
    {
        { "npm", appSettings.Npm },
        { "NuGet", appSettings.Nuget },
        { "Maven", appSettings.Maven },
        { "Debian", appSettings.Debian },
        { "Poetry", appSettings.Poetry },
        { "Cargo",appSettings.Cargo },
        { "Conan", appSettings.Conan }
    };
        }

        private static void DisplaySettingsWithSpectre(List<string> projectTypes, Dictionary<string, Config> projectConfigMap)
        {
            SafeSpectreAction(() =>
            {
                var content = new StringBuilder()
                    .AppendLine("[green]Current Application Settings[/]\n");

                foreach (var projectType in projectTypes)
                {
                    AppendProjectTypeSettings(content, projectType, projectConfigMap);
                }

                WriteStyledPanel(content.ToString().TrimEnd(), "", "white");
            }, "Current Application Settings", "Info");
        }

        private static void AppendProjectTypeSettings(StringBuilder content, string projectType, Dictionary<string, Config> projectConfigMap)
        {
            content.AppendLine($"[green]-[/] [green]{projectType}[/]\n");

            if (projectConfigMap.TryGetValue(projectType, out var config))
            {
                if (config != null)
                {
                    AppendConfigSettings(content, config);
                }
            }
            else
            {
                content.AppendLine($"  └──[red]Invalid ProjectType[/]\n");
            }
        }

        private static void AppendConfigSettings(StringBuilder content, Config config)
        {
            AppendRepoSettings(content, config);
            AppendIncludeExcludeSettings(content, config);
        }

        private static void AppendRepoSettings(StringBuilder content, Config config)
        {
            content
                .AppendLine($"  [cyan]DEVDEP_REPO_NAME[/]")
                .AppendLine($"  └──> {(!string.IsNullOrEmpty(config.DevDepRepo) ? config.DevDepRepo : Dataconstant.NotConfigured)}\n")
                .AppendLine($"  [cyan]THIRD_PARTY_REPO_NAME[/]")
                .AppendLine($"  └──> {GetThirdPartyRepoName(config)}\n")
                .AppendLine($"  [cyan]RELEASE_REPO_NAME[/]")
                .AppendLine($"  └──> {(!string.IsNullOrEmpty(config.ReleaseRepo) ? config.ReleaseRepo : Dataconstant.NotConfigured)}\n")
                .AppendLine($"  [cyan]Config[/]");
        }

        private static string GetThirdPartyRepoName(Config config)
        {
            return config.Artifactory?.ThirdPartyRepos?
                .FirstOrDefault(repo => repo.Upload)?.Name ?? Dataconstant.NotConfigured;
        }

        private static void AppendIncludeExcludeSettings(StringBuilder content, Config config)
        {
            var excludeList = GetFormattedList(config.Exclude);
            var includeList = GetFormattedList(config.Include);

            content
                .AppendLine($"  ├──[white]Include[/]")
                .AppendLine($"  │   └──> {includeList}")
                .AppendLine($"  └──[white]Exclude[/]")
                .AppendLine($"      └──> {excludeList}\n");
        }

        private static string GetFormattedList(string[] items)
        {
            return !string.IsNullOrEmpty(items?.FirstOrDefault())
                ? string.Join(", ", items)
                : Dataconstant.NotConfigured;
        }

        private static void DisplaySettingsWithLogger(List<string> projectTypes, Dictionary<string, Config> projectConfigMap)
        {
            Logger.Info("Current Application Settings:");

            foreach (var projectType in projectTypes)
            {
                Logger.Info($"{projectType}:\n\t");

                if (projectConfigMap.TryGetValue(projectType, out var config))
                {
                    DisplayPackageSettings(config);
                }
                else
                {
                    Logger.ErrorFormat("DisplayAllSettings(): Invalid ProjectType - {0}", projectType);
                }
            }
        }
        private static void DisplayPackageSettings(Config project)
        {
            if (project == null)
            {
                Logger.Warn("DisplayPackageSettings(): Config is null.");
                return;
            }

            // Build Include, Exclude, and ThirdPartyRepoName strings safely
            string includeList = !string.IsNullOrEmpty(project.Include?.FirstOrDefault())
                ? string.Join(", ", project.Include)
                : Dataconstant.NotConfigured;

            string excludeList = !string.IsNullOrEmpty(project.Exclude?.FirstOrDefault())
                ? string.Join(", ", project.Exclude)
                : Dataconstant.NotConfigured;

            string devDepRepoName = !string.IsNullOrEmpty(project.DevDepRepo)
                ? project.DevDepRepo
                : Dataconstant.NotConfigured;

            string releaseRepoName = !string.IsNullOrEmpty(project.ReleaseRepo)
                ? project.ReleaseRepo
                : Dataconstant.NotConfigured;

            string thirdPartyRepoName = project.Artifactory?.ThirdPartyRepos?
                .FirstOrDefault(repo => repo.Upload)?.Name ?? Dataconstant.NotConfigured;

            // Log the settings for the project
            Logger.Logger.Log(null, Level.Notice,
                $"\tDEVDEP_REPO_NAME:\t{devDepRepoName}\n" +
                $"\tTHIRD_PARTY_REPO_NAME:\t{thirdPartyRepoName}\n" +
                $"\tRELEASE_REPO_NAME:\t{releaseRepoName}\n" +
                $"\tConfig:\n" +
                $"\t\tExclude:\t\t{excludeList}\n" +
                $"\t\tInclude:\t\t{includeList}\n", null);
        }
        public static void DisplayInputParametersWithSpectreConsole(CatoolInfo caToolInformation, CommonAppSettings appSettings,
    ListofPerametersForCli listofPerameters, string exeType, string bomFilePath)
        {
            string content = GenerateContentByExeType(caToolInformation, appSettings, listofPerameters, exeType, bomFilePath);
            WriteStyledPanel(content);
        }

        private static string GenerateContentByExeType(CatoolInfo caToolInformation, CommonAppSettings appSettings,
            ListofPerametersForCli listofPerameters, string exeType, string bomFilePath)
        {
            return exeType switch
            {
                Dataconstant.Identifier => GenerateIdentifierContent(caToolInformation, appSettings, listofPerameters),
                Dataconstant.Creator => GenerateCreatorContent(caToolInformation, appSettings, bomFilePath),
                Dataconstant.Uploader => GenerateUploaderContent(caToolInformation, appSettings, bomFilePath),
                _ => string.Empty
            };
        }

        private static void AppendDirectoryInfo(StringBuilder content, CommonAppSettings appSettings, int maxPathLength)
        {
            content
                .Append($"[green]-[/] [cyan]PackageFilePath[/]\n")
                .Append($"  └──> {WrapPath(appSettings.Directory.InputFolder, maxPathLength)}\n\n")
                .Append($"[green]-[/] [cyan]BomFolderPath[/]\n")
                .Append($"  └──> {WrapPath(appSettings.Directory.OutputFolder, maxPathLength)}\n\n");
        }

        private static void AppendBasicInfo(StringBuilder content, CatoolInfo caToolInformation, int maxPathLength)
        {
            content
                .Append($"[green]-[/] [cyan]CaToolVersion[/]\n")
                .Append($"  └──> {caToolInformation.CatoolVersion}\n\n")
                .Append($"[green]-[/] [cyan]CaToolRunningPath[/]\n")
                .Append($"  └──> {WrapPath(caToolInformation.CatoolRunningLocation, maxPathLength)}\n\n");
        }

        private static void AppendSw360Info(StringBuilder content, CommonAppSettings appSettings,
            ListofPerametersForCli listofPerameters, int maxPathLength)
        {
            content
                .Append($"[green]-[/] [cyan]SW360Url[/]\n")
                .Append($"  └──> {appSettings.SW360.URL}\n\n")
                .Append($"[green]-[/] [cyan]SW360ProjectName[/]\n")
                .Append($"  └──> {appSettings.SW360.ProjectName}\n\n")
                .Append($"[green]-[/] [cyan]SW360ProjectID[/]\n")
                .Append($"  └──> {appSettings.SW360.ProjectID}\n\n")
                .Append($"[green]-[/] [cyan]ExcludeComponents[/]\n")
                .Append($"  └──> {WrapPath(string.IsNullOrEmpty(listofPerameters.ExcludeComponents) ? "None" : listofPerameters.ExcludeComponents, maxPathLength)}\n\n");
        }

        private static void AppendCommonInfo(StringBuilder content, CommonAppSettings appSettings,
            ListofPerametersForCli listofPerameters, int maxPathLength)
        {
            content
                .Append($"[green]-[/] [cyan]ProjectType[/]\n")
                .Append($"  └──> {appSettings.ProjectType}\n\n")
                .Append($"[green]-[/] [cyan]LogFolderPath[/]\n")
                .Append($"  └──> {WrapPath(Log4Net.CatoolLogPath, maxPathLength)}\n\n")
                .Append($"[green]-[/] [cyan]Include[/]\n")
                .Append($"  └──> {WrapPath(string.IsNullOrEmpty(listofPerameters.Include) ? "None" : listofPerameters.Include, maxPathLength)}\n\n")
                .Append($"[green]-[/] [cyan]Exclude[/]\n")
                .Append($"  └──> {WrapPath(string.IsNullOrEmpty(listofPerameters.Exclude) ? "None" : listofPerameters.Exclude, maxPathLength)}");
        }

        private static string GenerateIdentifierContent(CatoolInfo caToolInformation, CommonAppSettings appSettings,
            ListofPerametersForCli listofPerameters)
        {
            int consoleWidth = GetAutoConsoleWidth();
            int maxPathLength = Math.Max(60, consoleWidth - 20);
            var content = new StringBuilder();

            content
                .Append($"Start of Package Identifier execution: [green]{DateTime.Now}[/]\n\n")
                .Append($"[green]-[/] [green]Input Parameters used in Package Identifier[/]\n\n");

            AppendBasicInfo(content, caToolInformation, maxPathLength);
            AppendDirectoryInfo(content, appSettings, maxPathLength);

            if (appSettings.SW360 != null)
            {
                AppendSw360Info(content, appSettings, listofPerameters, maxPathLength);
            }

            if (appSettings.Jfrog != null)
            {
                content
                    .Append($"[green]-[/] [cyan]InternalRepoList[/]\n")
                    .Append($"  └──> {WrapPath(string.IsNullOrEmpty(listofPerameters.InternalRepoList) ? "None" : listofPerameters.InternalRepoList, maxPathLength)}\n\n");
            }
            if (appSettings.IsTestMode)
            {
                content
                    .Append($"[green]-[/] [cyan]Mode[/]\n")
                    .Append($"  └──> {appSettings.Mode}\n\n");
            }

            AppendCommonInfo(content, appSettings, listofPerameters, maxPathLength);

            return content.ToString();
        }

        private static string GenerateCreatorContent(CatoolInfo caToolInformation, CommonAppSettings appSettings, string bomFilePath)
        {
            int consoleWidth = GetAutoConsoleWidth();
            int maxPathLength = Math.Max(60, consoleWidth - 20);
            var content = new StringBuilder();

            content
                .Append($"Start of Package Creater execution: [green]{DateTime.Now}[/]\n\n")
                .Append($"[green]-[/] [green]Input parameters used in Package Creater[/]\n\n");

            AppendBasicInfo(content, caToolInformation, maxPathLength);
            AppendCreatorSpecificInfo(content, appSettings, bomFilePath, maxPathLength);

            return content.ToString();
        }

        private static string GenerateUploaderContent(CatoolInfo caToolInformation, CommonAppSettings appSettings, string bomFilePath)
        {
            int consoleWidth = GetAutoConsoleWidth();
            int maxPathLength = Math.Max(60, consoleWidth - 20);
            var content = new StringBuilder();

            content
                .Append($"Start of Uploader execution: [green]{DateTime.Now}[/]\n\n")
                .Append($"[green]-[/] [green]Input Parameters used in Artifactory Uploader[/]\n\n");

            AppendBasicInfo(content, caToolInformation, maxPathLength);
            AppendUploaderSpecificInfo(content, appSettings, bomFilePath, maxPathLength);

            return content.ToString();
        }

        private static void AppendCreatorSpecificInfo(StringBuilder content, CommonAppSettings appSettings,
    string bomFilePath, int maxPathLength)
        {
            content
                .Append($"[green]-[/] [cyan]BomFilePath[/]\n")
                .Append($"  └──> {WrapPath(bomFilePath, maxPathLength)}\n\n")
                .Append($"[green]-[/] [cyan]SW360Url[/]\n")
                .Append($"  └──> {appSettings.SW360.URL}\n\n")
                .Append($"[green]-[/] [cyan]SW360ProjectName[/]\n")
                .Append($"  └──> {appSettings.SW360.ProjectName}\n\n")
                .Append($"[green]-[/] [cyan]SW360ProjectID[/]\n")
                .Append($"  └──> {appSettings.SW360.ProjectID}\n\n")
                .Append($"[green]-[/] [cyan]FossologyURL[/]\n")
                .Append($"  └──> {appSettings.SW360.Fossology.URL}\n\n")
                .Append($"[green]-[/] [cyan]EnableFossTrigger[/]\n")
                .Append($"  └──> {appSettings.SW360.Fossology.EnableTrigger}\n\n")
                .Append($"[green]-[/] [cyan]IgnoreDevDependency[/]\n")
                .Append($"  └──> {appSettings.SW360.IgnoreDevDependency}\n\n")
                .Append($"[green]-[/] [cyan]LogFolderPath[/]\n")
                .Append($"  └──> {WrapPath(Log4Net.CatoolLogPath, maxPathLength)}\n\n");
        }


        private static void AppendUploaderSpecificInfo(StringBuilder content, CommonAppSettings appSettings,
    string bomFilePath, int maxPathLength)
        {
            content
                .Append($"[green]-[/] [cyan]BomFilePath[/]\n")
                .Append($"  └──> {WrapPath(bomFilePath, maxPathLength)}\n\n")
                .Append($"[green]-[/] [cyan]JFrogUrl[/]\n")
                .Append($"  └──> {appSettings.Jfrog.URL}\n\n")
                .Append($"[green]-[/] [cyan]Dry-run[/]\n")
                .Append($"  └──> {appSettings.Jfrog.DryRun}\n\n")
                .Append($"[green]-[/] [cyan]LogFolderPath[/]\n")
                .Append($"  └──> {WrapPath(Log4Net.CatoolLogPath, maxPathLength)}\n\n");
        }
        public static void LogInputParameters(CatoolInfo caToolInformation, CommonAppSettings appSettings, ListofPerametersForCli listofPerameters, string exeType = null, string bomFilePath = null)
        {

            if (LoggerFactory.UseSpectreConsole)
            {
                DisplayInputParametersWithSpectreConsole(caToolInformation, appSettings, listofPerameters, exeType, bomFilePath);
            }
            else
            {
                LogInputParametersWithLog4net(caToolInformation, appSettings, listofPerameters, exeType, bomFilePath);
            }
        }
        private static void LogInputParametersWithLog4net(CatoolInfo caToolInformation, CommonAppSettings appSettings, ListofPerametersForCli listofPerameters, string exeType, string bomFilePath)
        {
            if (exeType == Dataconstant.Identifier)
            {
                var logMessage = $"Input Parameters used in Package Identifier:\n\t" +
                $"CaToolVersion\t\t --> {caToolInformation.CatoolVersion}\n\t" +
                $"CaToolRunningPath\t --> {caToolInformation.CatoolRunningLocation}\n\t" +
                $"PackageFilePath\t\t --> {appSettings.Directory.InputFolder}\n\t" +
                $"BomFolderPath\t\t --> {appSettings.Directory.OutputFolder}\n\t";

                if (appSettings.SW360 != null)
                {
                    logMessage += $"SW360Url\t\t --> {appSettings.SW360.URL}\n\t" +
                              $"SW360ProjectName\t --> {appSettings.SW360.ProjectName}\n\t" +
                              $"SW360ProjectID\t\t --> {appSettings.SW360.ProjectID}\n\t" +
                              $"ExcludeComponents\t --> {listofPerameters.ExcludeComponents}\n\t";
                }
                if (appSettings.Jfrog != null)
                {
                    logMessage += $"InternalRepoList\t --> {listofPerameters.InternalRepoList}\n\t";
                }
                if (appSettings.IsTestMode)
                {
                    logMessage += $"Mode\t --> {appSettings.Mode}\n\t";
                }
                logMessage += $"ProjectType\t\t --> {appSettings.ProjectType}\n\t" +
                              $"LogFolderPath\t\t --> {Log4Net.CatoolLogPath}\n\t" +
                              $"Include\t\t\t --> {listofPerameters.Include}\n\t" +
                              $"Exclude\t\t\t --> {listofPerameters.Exclude}\n";

                Logger.Logger.Log(null, Level.Notice, logMessage, null);
            }
            else if (exeType == Dataconstant.Creator)
            {
                Logger.Logger.Log(null, Level.Notice, $"Input parameters used in Package Creator:\n\t" +
                              $"CaToolVersion\t\t --> {caToolInformation.CatoolVersion}\n\t" +
                              $"CaToolRunningPath\t --> {caToolInformation.CatoolRunningLocation}\n\t" +
                              $"BomFilePath\t\t --> {bomFilePath}\n\t" +
                              $"SW360Url\t\t --> {appSettings.SW360.URL}\n\t" +
                              $"SW360ProjectName\t --> {appSettings.SW360.ProjectName}\n\t" +
                              $"SW360ProjectID\t\t --> {appSettings.SW360.ProjectID}\n\t" +
                              $"FossologyURL\t\t --> {appSettings.SW360.Fossology.URL}\n\t" +
                              $"EnableFossTrigger\t --> {appSettings.SW360.Fossology.EnableTrigger}\n\t" +
                              $"IgnoreDevDependency\t --> {appSettings.SW360.IgnoreDevDependency}\n\t" +
                              $"LogFolderPath\t\t --> {Log4Net.CatoolLogPath}\n\t", null);
            }
            else if (exeType == Dataconstant.Uploader)
            {
                Logger.Logger.Log(null, Level.Info, $"Input Parameters used in Artifactory Uploader:\n\t", null);
                Logger.Logger.Log(null, Level.Notice, $"\tBomFilePath:\t\t {bomFilePath}\n\t" +
                    $"CaToolVersion\t\t {caToolInformation.CatoolVersion}\n\t" +
                    $"CaToolRunningPath\t {caToolInformation.CatoolRunningLocation}\n\t" +
                    $"JFrogUrl:\t\t {appSettings.Jfrog.URL}\n\t" +
                    $"Dry-run:\t\t {appSettings.Jfrog.DryRun}\n\t" +
                    $"LogFolderPath:\t\t {Log4Net.CatoolLogPath}\n", null);

            }
        }
        public static string WrapPath(string path, int maxLength = 80, string prefix = "        ")
        {
            if (string.IsNullOrEmpty(path) || path.Length <= maxLength)
                return path;

            var lines = new List<string>();
            var currentPos = 0;

            while (currentPos < path.Length)
            {
                var remainingLength = path.Length - currentPos;
                var lineLength = Math.Min(maxLength, remainingLength);

                if (lineLength < remainingLength)
                {
                    var breakPoint = path.LastIndexOf('\\', currentPos + lineLength);
                    if (breakPoint > currentPos)
                    {
                        lineLength = breakPoint - currentPos + 1;
                    }
                }

                lines.Add(path.Substring(currentPos, lineLength));
                currentPos += lineLength;
            }

            return string.Join($"\n{prefix}", lines);
        }

        public static void WriteStyledPanel(string content, string title = null, string borderStyle = "white", string headerStyle = "yellow")
        {
            SafeSpectreAction(() =>
            {
                int consoleWidth = GetAutoConsoleWidth();
                int panelWidth = Math.Min(consoleWidth, 150);

                var panel = new Panel(content)
                {
                    Border = BoxBorder.Rounded,
                    BorderStyle = Style.Parse(borderStyle),
                    Padding = new Padding(1, 0, 1, 0),
                    Width = panelWidth,
                    Expand = true
                };

                if (!string.IsNullOrEmpty(title))
                {
                    panel.Header = new PanelHeader($"[{headerStyle}]{Markup.Escape(title)}[/]");
                }

                ConsoleWrite(c => c.Write(panel));
            }, content, title ?? "Panel");
        }
        public static void WriteSummaryHeader(string title)
        {
            SafeSpectreAction(() =>
            {
                WriteLine(); // Add a new line before the header
                var consoleWidth = GetAutoConsoleWidth();
                var padding = (consoleWidth - title.Length) / 2;
                var centeredText = title.PadLeft(padding + title.Length).PadRight(consoleWidth);
                var bottomBorder = new string('═', consoleWidth); // Double line border character

                // Write centered header and bottom border

                ConsoleWrite(c =>
                {
                    c.MarkupLine($"[bold white]{Markup.Escape(centeredText)}[/]");
                    c.MarkupLine($"[bold white]{bottomBorder}[/]");
                });
            }, title, "Header");
        }
        public static void WriteHeader(string title)
        {
            SafeSpectreAction(() =>
            {
                WriteLine(); // Add a new line before the header
                var consoleWidth = GetAutoConsoleWidth();
                var padding = (consoleWidth - title.Length) / 2;
                var centeredText = title.PadLeft(padding + title.Length).PadRight(consoleWidth);
                ConsoleInstance.MarkupLine($"[bold white]{Markup.Escape(centeredText)}[/]");
            }, title, "Header");
        }

        public static void WriteFallback(string message, string messageType = "Info")
        {
            string prefix = messageType.ToLower() switch
            {
                "warning" => "WARNING: ",
                "error" => "ERROR: ",
                "success" => "SUCCESS: ",
                "notice" => "NOTICE: ",
                "header" => "",
                "panel" => "",
                "alert" => "",
                _ => ""
            };

            string logMessage = $"{prefix}{message}";

            switch (messageType.ToLower())
            {
                case "warning":
                    Logger.Warn(logMessage);
                    break;
                case "error":
                    Logger.Error(logMessage);
                    break;
                default:
                    Logger.Info(logMessage);
                    break;
            }
        }
        public static void WriteToConsoleTable(Dictionary<string, int> printData, Dictionary<string, double> printTimingData, string ProjectSummaryLink, string exeType, KpiNames KpiNames)
        {
            if (LoggerFactory.UseSpectreConsole)
            {
                WriteToSpectreConsoleTable(printData, printTimingData, ProjectSummaryLink, exeType, KpiNames);
                return;
            }

            const string Count = "Count";
            const string Feature = "Feature";
            const string TimeTakenBy = "Time Taken By";
            Logger.Info("\n");
            Logger.Info("Summary :\n");
            if (!string.IsNullOrWhiteSpace(ProjectSummaryLink))
            {
                Logger.Info($"{ProjectSummaryLink}");
            }
            string separator = $"{"=",5}{string.Join("", Enumerable.Repeat("=", 88)),5}";
            Logger.Info(separator);
            Logger.Info($"{"|",5}{Feature,-70} {"|",5} {Count,5} {"|",5}");
            Logger.Info(separator);

            foreach (var item in printData)
            {
                LogTableRow(item.Key, item.Value);
            }

            foreach (var item in printTimingData)
            {
                Logger.Info($"\n{TimeTakenBy,8} {item.Key,-5} {":",1} {item.Value,8} s\n");
            }
        }

        private static void LogTableRow(string key, int value)
        {
            string row = $"{"|",5}{key,-70} {"|",5} {value,5} {"|",5}";
            string separator = $"{"-",5}{string.Join("", Enumerable.Repeat("-", 88)),5}";

            if ((key == "Packages Not Uploaded Due To Error" || key == "Packages Not Existing in Remote Cache") && value > 0)
            {
                Logger.Error(row);
                Logger.Error(separator);
            }
            else
            {
                Logger.Info(row);
                Logger.Info(separator);
            }
        }

        public static void WriteToSpectreConsoleTable(
    Dictionary<string, int> printData,
    Dictionary<string, double> printTimingData,
    string ProjectSummaryLink,
    string exeType, KpiNames KpiNames)
        {
            SafeSpectreAction(() =>
            {
                WriteSummaryHeader("SUMMARY");
                WriteProjectSummary(ProjectSummaryLink);

                int consoleWidth = GetAutoConsoleWidth();
                int barMaxWidth = Math.Max(20, 40);
                int maxValue = printData.Values.Count != 0 ? printData.Values.Max() : 0;

                var table = CreateSummaryTable(consoleWidth);
                AddSummaryRows(table, printData, maxValue, barMaxWidth, KpiNames);

                table.AddEmptyRow();
                ConsoleInstance.Write(table);

                WriteLine();

                WriteTimingData(printTimingData);
            }, "Package Summary Table", "Panel");
        }

        private static void WriteProjectSummary(string projectSummaryLink)
        {
            if (!string.IsNullOrWhiteSpace(projectSummaryLink))
            {
                WriteLine();
                WriteInfoWithMarkup($"[blue]Project Summary: [/][white]{projectSummaryLink}[/]");
            }
        }

        private static Table CreateSummaryTable(int consoleWidth)
        {
            var table = new Table()
                .BorderColor(Color.White)
                .Border(TableBorder.Rounded)
                .Width(Math.Min(consoleWidth, 200))
                .Expand();

            table.AddColumn(new TableColumn("[green bold]Feature[/]"));
            table.AddColumn(new TableColumn("[blue bold]Count[/]"));
            table.AddRow("", "");
            return table;
        }

        private static void AddSummaryRows(
            Table table,
            Dictionary<string, int> printData,
            int maxValue,
            int barMaxWidth, KpiNames KpiNames)
        {
            foreach (var item in printData)
            {
                string color = GetColorForItem(item.Key, item.Value, KpiNames);
                int barWidth = maxValue > 0 ? (int)((double)item.Value / maxValue * barMaxWidth) : 0;
                string visualBar = barWidth > 0 ? new string('█', barWidth) : "";

                table.AddRow(
                    $"[white]{item.Key}[/]",
                    $"[{color}]{visualBar} {item.Value}[/]"
                );
                table.AddRow("", "");
            }
        }



        private static void WriteTimingData(Dictionary<string, double> printTimingData)
        {
            if (printTimingData.Count == 0)
                return;

            WriteLine();
            foreach (var item in printTimingData)
            {
                string timeFormatted = item.Value.ToString("F3");
                WriteInfoWithMarkup($"[white]Time Taken By {item.Key} : [/][green]{timeFormatted}[/][white] s[/]");
            }
            WriteLine();
        }

        private static string GetColorForItem(string key, int value, KpiNames kpiNames)
        {
            if (string.IsNullOrWhiteSpace(key) || kpiNames == null)
                return "green";

            var errorGroup = new[]
            {
                kpiNames.ComponentsInInputFile,
                kpiNames.ComponentsInBOM,
                kpiNames.ComponentsFromBOM
            };
            var warningGroup = new[]
            {
                kpiNames.PackagesNotPresentInOfficialRepo,
                kpiNames.ComponentsNotUploadedInFOSSology,
                kpiNames.PackagesInNotApprovedState,
                kpiNames.ReleasesWithoutSourceDownloadURL
            };

            var infoGroup = new[]
            {
                kpiNames.ReleasesNotCreatedInSW360,
                kpiNames.ComponentsWithoutPackageURL,
                kpiNames.ComponentsWithoutSourceAndPackageURL,
                kpiNames.PackagesNotCopiedToSipartyRepo,
                kpiNames.PackagesNotCopiedToSipartyDevDepRepo,
                kpiNames.PackagesNotActionedDueToError,
                kpiNames.PackagesNotExistingInRepository,
                kpiNames.PackagesNotMovedToRepo
            };

            var alwaysGreen = new[]
            {
                kpiNames.ReleasesCreatedInSW360,
                kpiNames.ComponentsUploadedInFOSSology,
                kpiNames.ReleasesExistsInSW360,
                kpiNames.ReleasesWithSourceDownloadURL,
                kpiNames.ComponentsAddedFromSBOMTemplate,
                kpiNames.ComponentsOverWrittenFromSBOMTemplate,
                kpiNames.PackagesInApprovedState,
                kpiNames.PackagesCopiedToSipartyRepo,
                kpiNames.PackagesCopiedToSipartyDevDepRepo,
                kpiNames.PackagesMovedToRepo,
                kpiNames.ComponentsFromTheSPDXImportedAsBaselineEntries,
                kpiNames.TotalDuplicateAndInValidComponents,
                kpiNames.PackagesPresentIn3rdPartyRepo,
                kpiNames.PackagesPresentInDevDepRepo,
                kpiNames.PackagesPresentInReleaseRepo,
                kpiNames.DevelopmentComponents,
                kpiNames.BundledComponents,
                kpiNames.InvalidComponentsExcluded,
                kpiNames.DuplicateComponents,
                kpiNames.ManuallyExcludedSw360,
                kpiNames.InternalComponents
            };

            bool Is(string candidate) => !string.IsNullOrEmpty(candidate) && key.Equals(candidate, StringComparison.Ordinal);

            if (errorGroup.Any(Is))
                return value == 0 ? "red" : "green";

            if (warningGroup.Any(Is))
                return value == 0 ? "green" : "yellow";

            if (infoGroup.Any(Is))
                return value == 0 ? "green" : "red";

            if (alwaysGreen.Any(Is))
                return "green";

            if (_colorCache.TryGetValue(key, out var cached))
                return cached;

            var colors = new[] { "green" };
            var assigned = colors[_colorIndex % colors.Length];
            _colorCache[key] = assigned;
            _colorIndex++;
            return assigned;
        }
        public static void WriteInternalComponentsTableInCli(List<Component> internalComponents)
        {
            if (LoggerFactory.UseSpectreConsole)
            {
                WriteInternalComponentsListTableToKpi(internalComponents);
            }
            else
            {
                //Writes internal component ist to kpi
                WriteInternalComponentsListToKpi(internalComponents);
            }
        }
        public static void WriteInternalComponentsListTableToKpi(List<Component> internalComponents)
        {
            if (internalComponents?.Count > 0)
            {
                SafeSpectreAction(() =>
                {
                    WriteLine();
                    WriteInfoWithMarkup("[yellow bold]* Internal Components Identified which will not be sent for clearing:[/]");
                    WriteLine();

                    int consoleWidth = GetAutoConsoleWidth();

                    var table = new Table()
                        .BorderColor(Color.Yellow)
                        .Border(TableBorder.Rounded)
                        .Title("[yellow bold]Internal Components[/]")
                        .Width(Math.Min(consoleWidth, 120));

                    table.AddColumn(new TableColumn("[green bold]Name[/]").Width(60));
                    table.AddColumn(new TableColumn("[blue bold]Version[/]").Width(40));

                    foreach (var item in internalComponents)
                    {
                        string componentName = !string.IsNullOrEmpty(item.Name) ? item.Name : "N/A";
                        string componentVersion = !string.IsNullOrEmpty(item.Version) ? item.Version : "N/A";

                        table.AddRow(
                            $"[white]{Markup.Escape(componentName)}[/]",
                            $"[cyan]{Markup.Escape(componentVersion)}[/]"
                        );
                    }

                    ConsoleInstance.Write(table);
                    WriteLine();
                }, "* Internal Components Identified which will not be sent for clearing:", "Alert");
            }
        }
        public static void WriteInternalComponentsListToKpi(List<Component> internalComponents)
        {
            const string Name = "Name";
            const string Version = "Version";

            if (internalComponents?.Count > 0)
            {
                Logger.Logger.Log(null, Level.Alert, "* Internal Components Identified which will not be sent for clearing:", null);
                Logger.Logger.Log(null, Level.Alert, $"{"=",5}{string.Join("", Enumerable.Repeat("=", 98)),5}", null);
                Logger.Logger.Log(null, Level.Alert, $"{"|",5}{Name,-45} {"|",5} {Version,35} {"|",10}", null);
                Logger.Logger.Log(null, Level.Alert, $"{"=",5}{string.Join("", Enumerable.Repeat("=", 98)),5}", null);

                foreach (var item in internalComponents)
                {
                    Logger.Logger.Log(null, Level.Alert, $"{"|",5}{item.Name,-45} {"|",5} {item.Version,35} {"|",10}", null);
                    Logger.Logger.Log(null, Level.Alert, $"{"-",5}{string.Join("", Enumerable.Repeat("-", 98)),5}", null);
                }
                Logger.Info("\n");
            }
        }
        public static void WriteTelemetryMessage(string message)
        {
            if (LoggerFactory.UseSpectreConsole)
            {
                Logger.Debug($"{message}");
                WriteLine();
                var content = new StringBuilder()
                    .Append($"[yellow]{message}[/]");

                WriteStyledPanel(content.ToString(), "", "yellow", "yellow");
                WriteLine();
            }
            else
            {
                Logger.Warn(message);
            }
        }
        public static void SpectreConsoleInitialMessage(string message)
        {
            if (LoggerFactory.UseSpectreConsole)
            {
                WriteHeader($"{message}");
            }
            else
            {
                Logger.Logger.Log(null, Level.Notice, $"\n====================<<<<< {message} >>>>>====================", null);
                Logger.Logger.Log(null, Level.Notice, $"\nStart of {message} execution: {DateTime.Now}", null);
            }
        }

        public static void ValidFilesInfoDisplayForCli(string configFile)
        {
            if (LoggerFactory.UseSpectreConsole)
            {
                WriteInfoWithMarkup($"[white]    Input file FOUND :[/][green]{configFile}[/]");
            }
            else
            {
                Logger.Info($"    Input file FOUND :{configFile}");
            }
        }

        public static void JfrogConnectionInfoDisplayForCli()
        {
            if (LoggerFactory.UseSpectreConsole)
            {
                WriteInfoWithMarkup($"[white]JFrog Connection was [/][green]successfull...[/]");
            }
            else
            {
                Logger.Info($"JFrog Connection was successfull!!");
            }
        }

        public static void WriteLine()
        {
            SafeSpectreAction(() => ConsoleWrite(c => c.WriteLine()), "", "Info");
        }

        public static void WriteInfoWithMarkup(string message)
        {
            SafeSpectreAction(() => ConsoleWrite(c => c.MarkupLine(message)), message, "Info");
        }
        public static void WriteFossologyProcessInitializeMessage(string formattedName, ComparisonBomData item)
        {
            if (LoggerFactory.UseSpectreConsole)
            {
                Logger.Logger.Log(null, Level.Notice, $"  ├── Initiating FOSSology process for: Release : Name - [cyan]{formattedName}[/] , version - [cyan]{item.Version}[/]", null);
            }
            else
            {
                Logger.Logger.Log(null, Level.Notice, $"\tInitiating FOSSology process for: Release : Name - {formattedName} , version - {item.Version}", null);
            }
        }
        public static void WriteFossologyStatusMessage(string message)
        {
            if (LoggerFactory.UseSpectreConsole)
            {
                WriteInfoWithMarkup($"   [white]└──[/][yellow]{message}[/]");
                Logger.Debug($"   └── {message}");
            }
            else
            {
                Logger.Warn($"\t{message}");
            }
        }
        public static void WriteFossologyExceptionMessage(string message)
        {
            if (LoggerFactory.UseSpectreConsole)
            {
                WriteInfoWithMarkup($"   [white]└──[/][red]{message}[/]");
                Logger.Debug($"   └── {message}");
            }
            else
            {
                Logger.Error($"\t{message}");
            }
        }
        public static void WriteComponentStatusMessage(string message, ComparisonBomData item)
        {
            if (LoggerFactory.UseSpectreConsole)
            {
                WriteInfoWithMarkup($" {message}: Name - [cyan]{item.Name}[/] , version - [cyan]{item.Version}[/]");
            }
            else
            {
                Logger.Logger.Log(null, Level.Notice, $"{message}: Name - {item.Name} , version - {item.Version}", null);
            }
        }

        public static void WriteFossologySucessStatusMessage(string message, string formattedName, ComparisonBomData item)
        {
            if (LoggerFactory.UseSpectreConsole)
            {
                WriteInfoWithMarkup($"   [white]└──[/][green]{message}[/]: Name - [cyan]{formattedName}[/] , version - [cyan]{item.Version}[/]");
            }
            else
            {
                Logger.Logger.Log(null, Level.Info, $"\n{message} : Name - {formattedName}, version - {item.Version}", null);
            }
        }
        public static void MSBuildVersionDisplay(string message, string version)
        {
            if (LoggerFactory.UseSpectreConsole)
            {
                WriteInfoWithMarkup($"[white]{message} [/][green]{version}[/]");
            }
            else
            {
                Logger.Info($"{message}{version}");
            }
        }
        private static void LogDuplicateComponentsByPurlId(List<Components> duplicateComponents, string sw360URL)
        {
            if (duplicateComponents.Count > 0)
            {
                Logger.Logger.Log(null, Level.Alert, "* List of components or releases not created in SW360 due to Invalid Purl ids found in Components ExternalID field in sw360", null);
                Logger.Logger.Log(null, Level.Alert, "  Component Name already exists in SW360 with a different package type PurlId. Manually update the component details.", null);

                const int nameWidth = 45;
                const int versionWidth = 25;
                const int urlWidth = 120;
                const int totalWidth = nameWidth + versionWidth + urlWidth + 10;

                string border = new string('=', totalWidth);
                string separator = new string('-', totalWidth);

                Logger.Logger.Log(null, Level.Alert, border, null);
                Logger.Logger.Log(null, Level.Alert, string.Format("| {0,-45} | {1,-25} | {2,-120} |", "Name", "Version", "SW360 Component URL"), null);
                Logger.Logger.Log(null, Level.Alert, border, null);

                foreach (var item in duplicateComponents)
                {
                    string link = CommonHelper.Sw360ComponentURL(sw360URL, item.ComponentId);
                    Logger.Logger.Log(null, Level.Alert, string.Format("| {0,-45} | {1,-25} | {2,-120} |", item.Name, item.Version, link), null);
                    Logger.Logger.Log(null, Level.Alert, separator, null);
                }

                Logger.Info("\n");
            }
        }

    }
}