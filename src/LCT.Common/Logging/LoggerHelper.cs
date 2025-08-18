using CycloneDX.Models;
using LCT.Common.Constants;
using LCT.Common.Model;
using log4net;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Text;
using Level = log4net.Core.Level;

namespace LCT.Common.Logging
{
    public static class LoggerHelper
    {
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly Dictionary<string, string> _colorCache = new Dictionary<string, string>();
        private static int _colorIndex = 0;

        // Helper to get console width with fallback and subtract
        private static int GetConsoleWidth(int subtract = 0, int fallback = 120)
        {
            return Console.WindowWidth > 0 ? Console.WindowWidth - subtract : fallback;
        }

        // Helper to safely execute Spectre.Console actions with fallback
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
            }
        }
        public static void WriteComponentsWithoutDownloadURLByUseingSpectreToKpi(List<ComparisonBomData> componentInfo, List<Components> lstReleaseNotCreated, string sw360URL)
        {
            if (componentInfo.Count > 0 || lstReleaseNotCreated.Count > 0)
            {
                SafeSpectreAction(() =>
                {
                    WriteInfoWithMarkup("[yellow]Action Item required by the user:[/]\n");

                    DisplayComponentsWithoutUrl(componentInfo, sw360URL);
                    DisplayNotCreatedComponents(lstReleaseNotCreated);

                    var environmentHelper = new EnvironmentHelper();
                    environmentHelper.CallEnvironmentExit(2);

                }, "Components Without Download URL", "Alert");
            }
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

            AnsiConsole.Write(table);
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

            AnsiConsole.Write(table);
            WriteLine();
        }

        private static Table CreateComponentTable(bool includeUrl)
        {
            var table = new Table()
                .BorderColor(Color.Yellow)
                .Border(TableBorder.Rounded)
                .Width(Math.Min(GetConsoleWidth(6, 120), includeUrl ? 200 : 120));

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
                string link = Sw360URL(sw360URL, item.ReleaseID);
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

                    int consoleWidth = GetConsoleWidth(6, 120);

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

                    AnsiConsole.Write(table);
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
        private static string Sw360URL(string sw360Env, string releaseId)
        {
            string sw360URL = $"{sw360Env}{"/group/guest/components/-/component/release/detailRelease/"}{releaseId}";
            return sw360URL;
        }
        public static void WriteComponentsWithoutDownloadURLToKpi(List<ComparisonBomData> componentInfo, List<Components> lstReleaseNotCreated, string sw360URL)
        {
            if (LoggerFactory.UseSpectreConsole)
            {
                WriteComponentsWithoutDownloadURLByUseingSpectreToKpi(componentInfo, lstReleaseNotCreated, sw360URL);
                return;
            }

            const string Name = "Name";
            const string Version = "Version";
            const string URL = "SW360 Release URL";
            if (componentInfo.Count > 0 || lstReleaseNotCreated.Count > 0)
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
                    string Link = Sw360URL(sw360URL, item.ReleaseID);
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
            return componentsInBOM
                .Select(item => item.Properties.First(x => x.Name == Dataconstant.Cdx_ProjectType).Value)
                .Distinct()
                .ToList();
        }

        private static Dictionary<string, Config> CreateProjectConfigMap(CommonAppSettings appSettings)
        {
            return new Dictionary<string, Config>(StringComparer.OrdinalIgnoreCase)
    {
        { "NPM", appSettings.Npm },
        { "NUGET", appSettings.Nuget },
        { "MAVEN", appSettings.Maven },
        { "DEBIAN", appSettings.Debian },
        { "POETRY", appSettings.Poetry },
        { "CONAN", appSettings.Conan }
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
                .AppendLine($"  └──➤ {(!string.IsNullOrEmpty(config.DevDepRepo) ? config.DevDepRepo : Dataconstant.NotConfigured)}\n")
                .AppendLine($"  [cyan]THIRD_PARTY_REPO_NAME[/]")
                .AppendLine($"  └──➤ {GetThirdPartyRepoName(config)}\n")
                .AppendLine($"  [cyan]RELEASE_REPO_NAME[/]")
                .AppendLine($"  └──➤ {(!string.IsNullOrEmpty(config.ReleaseRepo) ? config.ReleaseRepo : Dataconstant.NotConfigured)}\n")
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
                .AppendLine($"  │   └──➤ {includeList}")
                .AppendLine($"  └──[white]Exclude[/]")
                .AppendLine($"      └──➤ {excludeList}\n");
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
                "Identifier" => GenerateIdentifierContent(caToolInformation, appSettings, listofPerameters),
                "Creator" => GenerateCreatorContent(caToolInformation, appSettings, bomFilePath),
                "Uploader" => GenerateUploaderContent(caToolInformation, appSettings, bomFilePath),
                _ => string.Empty
            };
        }

        private static void AppendDirectoryInfo(StringBuilder content, CommonAppSettings appSettings, int maxPathLength)
        {
            content
                .Append($"[green]-[/] [cyan]PackageFilePath[/]\n")
                .Append($"  └──➤ {WrapPath(appSettings.Directory.InputFolder, maxPathLength)}\n\n")
                .Append($"[green]-[/] [cyan]BomFolderPath[/]\n")
                .Append($"  └──➤ {WrapPath(appSettings.Directory.OutputFolder, maxPathLength)}\n\n");
        }

        private static void AppendBasicInfo(StringBuilder content, CatoolInfo caToolInformation, int maxPathLength)
        {
            content
                .Append($"[green]-[/] [cyan]CaToolVersion[/]\n")
                .Append($"  └──✅ {caToolInformation.CatoolVersion}\n\n")
                .Append($"[green]-[/] [cyan]CaToolRunningPath[/]\n")
                .Append($"  └──➤ {WrapPath(caToolInformation.CatoolRunningLocation, maxPathLength)}\n\n");
        }

        private static void AppendSw360Info(StringBuilder content, CommonAppSettings appSettings,
            ListofPerametersForCli listofPerameters, int maxPathLength)
        {
            content
                .Append($"[green]-[/] [cyan]SW360Url[/]\n")
                .Append($"  └──➤ {appSettings.SW360.URL}\n\n")
                .Append($"[green]-[/] [cyan]SW360AuthTokenType[/]\n")
                .Append($"  └──➤ {appSettings.SW360.AuthTokenType}\n\n")
                .Append($"[green]-[/] [cyan]SW360ProjectName[/]\n")
                .Append($"  └──➤ {appSettings.SW360.ProjectName}\n\n")
                .Append($"[green]-[/] [cyan]SW360ProjectID[/]\n")
                .Append($"  └──➤ {appSettings.SW360.ProjectID}\n\n")
                .Append($"[green]-[/] [cyan]ExcludeComponents[/]\n")
                .Append($"  └──➤ {WrapPath(string.IsNullOrEmpty(listofPerameters.ExcludeComponents) ? "None" : listofPerameters.ExcludeComponents, maxPathLength)}\n\n");
        }

        private static void AppendCommonInfo(StringBuilder content, CommonAppSettings appSettings,
            ListofPerametersForCli listofPerameters, int maxPathLength)
        {
            content
                .Append($"[green]-[/] [cyan]ProjectType[/]\n")
                .Append($"  └──➤ {appSettings.ProjectType}\n\n")
                .Append($"[green]-[/] [cyan]LogFolderPath[/]\n")
                .Append($"  └──➤ {WrapPath(Log4Net.CatoolLogPath, maxPathLength)}\n\n")
                .Append($"[green]-[/] [cyan]Include[/]\n")
                .Append($"  └──➤ {WrapPath(string.IsNullOrEmpty(listofPerameters.Include) ? "None" : listofPerameters.Include, maxPathLength)}\n\n")
                .Append($"[green]-[/] [cyan]Exclude[/]\n")
                .Append($"  └──➤ {WrapPath(string.IsNullOrEmpty(listofPerameters.Exclude) ? "None" : listofPerameters.Exclude, maxPathLength)}");
        }

        private static string GenerateIdentifierContent(CatoolInfo caToolInformation, CommonAppSettings appSettings,
            ListofPerametersForCli listofPerameters)
        {
            int consoleWidth = GetConsoleWidth(10, 110);
            int maxPathLength = Math.Max(60, consoleWidth - 20);
            var content = new StringBuilder();

            content
                .Append($"Start of Package Identifier execution: [green]{DateTime.Now}[/]\n\n")
                .Append($"[green]-[/] [yellow]Input Parameters used in Package Identifier[/]\n\n");

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
                    .Append($"  └──➤ {WrapPath(string.IsNullOrEmpty(listofPerameters.InternalRepoList) ? "None" : listofPerameters.InternalRepoList, maxPathLength)}\n\n");
            }

            AppendCommonInfo(content, appSettings, listofPerameters, maxPathLength);

            return content.ToString();
        }

        private static string GenerateCreatorContent(CatoolInfo caToolInformation, CommonAppSettings appSettings, string bomFilePath)
        {
            int consoleWidth = GetConsoleWidth(10, 110);
            int maxPathLength = Math.Max(60, consoleWidth - 20);
            var content = new StringBuilder();

            content
                .Append($"Start of Package Creater execution: [green]{DateTime.Now}[/]\n\n")
                .Append($"[green]-[/] [yellow]Input parameters used in Package Creater[/]\n\n");

            AppendBasicInfo(content, caToolInformation, maxPathLength);
            AppendCreatorSpecificInfo(content, appSettings, bomFilePath, maxPathLength);

            return content.ToString();
        }

        private static string GenerateUploaderContent(CatoolInfo caToolInformation, CommonAppSettings appSettings, string bomFilePath)
        {
            int consoleWidth = GetConsoleWidth(10, 110);
            int maxPathLength = Math.Max(60, consoleWidth - 20);
            var content = new StringBuilder();

            content
                .Append($"Start of Uploader execution: [green]{DateTime.Now}[/]\n\n")
                .Append($"[green]-[/] [yellow]Input Parameters used in Artifactory Uploader[/]\n\n");

            AppendBasicInfo(content, caToolInformation, maxPathLength);
            AppendUploaderSpecificInfo(content, appSettings, bomFilePath, maxPathLength);

            return content.ToString();
        }

        private static void AppendCreatorSpecificInfo(StringBuilder content, CommonAppSettings appSettings,
    string bomFilePath, int maxPathLength)
        {
            content
                .Append($"[green]-[/] [cyan]BomFilePath[/]\n")
                .Append($"  └──➤ {WrapPath(bomFilePath, maxPathLength)}\n\n")
                .Append($"[green]-[/] [cyan]SW360Url[/]\n")
                .Append($"  └──➤ {appSettings.SW360.URL}\n\n")
                .Append($"[green]-[/] [cyan]SW360AuthTokenType[/]\n")
                .Append($"  └──➤ {appSettings.SW360.AuthTokenType}\n\n")
                .Append($"[green]-[/] [cyan]SW360ProjectName[/]\n")
                .Append($"  └──➤ {appSettings.SW360.ProjectName}\n\n")
                .Append($"[green]-[/] [cyan]SW360ProjectID[/]\n")
                .Append($"  └──➤ {appSettings.SW360.ProjectID}\n\n")
                .Append($"[green]-[/] [cyan]FossologyURL[/]\n")
                .Append($"  └──➤ {appSettings.SW360.Fossology.URL}\n\n")
                .Append($"[green]-[/] [cyan]EnableFossTrigger[/]\n")
                .Append($"  └──➤ {appSettings.SW360.Fossology.EnableTrigger}\n\n")
                .Append($"[green]-[/] [cyan]IgnoreDevDependency[/]\n")
                .Append($"  └──➤ {appSettings.SW360.IgnoreDevDependency}\n\n")
                .Append($"[green]-[/] [cyan]LogFolderPath[/]\n")
                .Append($"  └──➤ {WrapPath(Log4Net.CatoolLogPath, maxPathLength)}\n\n");
        }


        private static void AppendUploaderSpecificInfo(StringBuilder content, CommonAppSettings appSettings,
    string bomFilePath, int maxPathLength)
        {
            content
                .Append($"[green]-[/] [cyan]BomFilePath[/]\n")
                .Append($"  └──➤ {WrapPath(bomFilePath, maxPathLength)}\n\n")
                .Append($"[green]-[/] [cyan]JFrogUrl[/]\n")
                .Append($"  └──➤ {appSettings.Jfrog.URL}\n\n")
                .Append($"[green]-[/] [cyan]Dry-run[/]\n")
                .Append($"  └──➤ {appSettings.Jfrog.DryRun}\n\n")
                .Append($"[green]-[/] [cyan]LogFolderPath[/]\n")
                .Append($"  └──➤ {WrapPath(Log4Net.CatoolLogPath, maxPathLength)}\n\n");
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
            if (exeType == "Identifier")
            {
                var logMessage = $"Input Parameters used in Package Identifier:\n\t" +
                $"CaToolVersion\t\t --> {caToolInformation.CatoolVersion}\n\t" +
                $"CaToolRunningPath\t --> {caToolInformation.CatoolRunningLocation}\n\t" +
                $"PackageFilePath\t\t --> {appSettings.Directory.InputFolder}\n\t" +
                $"BomFolderPath\t\t --> {appSettings.Directory.OutputFolder}\n\t";

                if (appSettings.SW360 != null)
                {
                    logMessage += $"SW360Url\t\t --> {appSettings.SW360.URL}\n\t" +
                              $"SW360AuthTokenType\t --> {appSettings.SW360.AuthTokenType}\n\t" +
                              $"SW360ProjectName\t --> {appSettings.SW360.ProjectName}\n\t" +
                              $"SW360ProjectID\t\t --> {appSettings.SW360.ProjectID}\n\t" +
                              $"ExcludeComponents\t --> {listofPerameters.ExcludeComponents}\n\t";
                }
                if (appSettings.Jfrog != null)
                {
                    logMessage += $"InternalRepoList\t --> {listofPerameters.InternalRepoList}\n\t";
                }

                logMessage += $"ProjectType\t\t --> {appSettings.ProjectType}\n\t" +
                              $"LogFolderPath\t\t --> {Log4Net.CatoolLogPath}\n\t" +
                              $"Include\t\t\t --> {listofPerameters.Include}\n\t" +
                              $"Exclude\t\t\t --> {listofPerameters.Exclude}\n";

                Logger.Logger.Log(null, Level.Notice, logMessage, null);
            }
            else if (exeType == "Creator")
            {
                Logger.Logger.Log(null, Level.Notice, $"Input parameters used in Package Creator:\n\t" +
                              $"CaToolVersion\t\t --> {caToolInformation.CatoolVersion}\n\t" +
                              $"CaToolRunningPath\t --> {caToolInformation.CatoolRunningLocation}\n\t" +
                              $"BomFilePath\t\t --> {bomFilePath}\n\t" +
                              $"SW360Url\t\t --> {appSettings.SW360.URL}\n\t" +
                              $"SW360AuthTokenType\t --> {appSettings.SW360.AuthTokenType}\n\t" +
                              $"SW360ProjectName\t --> {appSettings.SW360.ProjectName}\n\t" +
                              $"SW360ProjectID\t\t --> {appSettings.SW360.ProjectID}\n\t" +
                              $"FossologyURL\t\t --> {appSettings.SW360.Fossology.URL}\n\t" +
                              $"EnableFossTrigger\t --> {appSettings.SW360.Fossology.EnableTrigger}\n\t" +
                              $"IgnoreDevDependency\t --> {appSettings.SW360.IgnoreDevDependency}\n\t" +
                              $"LogFolderPath\t\t --> {Log4Net.CatoolLogPath}\n\t", null);
            }
            else if (exeType == "Uploader")
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
                int consoleWidth = GetConsoleWidth(4, 120);
                int panelWidth = Math.Min(consoleWidth, 150);

                var panel = new Panel(content)
                {
                    Border = BoxBorder.Rounded,
                    BorderStyle = Style.Parse(borderStyle),
                    Padding = new Padding(1, 0, 1, 0),
                    Width = panelWidth,
                    Expand = false
                };

                if (!string.IsNullOrEmpty(title))
                {
                    panel.Header = new PanelHeader($"[{headerStyle}]{Markup.Escape(title)}[/]");
                }

                AnsiConsole.Write(panel);
            }, content, title ?? "Panel");
        }

        public static void WriteHeader(string title)
        {
            SafeSpectreAction(() =>
            {
                var centeredText = title.PadLeft((Console.WindowWidth + title.Length) / 2);
                AnsiConsole.MarkupLine($"[bold white]{Markup.Escape(centeredText)}[/]");
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
        public static void WriteToConsoleTable(Dictionary<string, int> printData, Dictionary<string, double> printTimingData, string ProjectSummaryLink)
        {
            if (LoggerFactory.UseSpectreConsole)
            {
                WriteToSpectreConsoleTable(printData, printTimingData, ProjectSummaryLink);
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
        public static void WriteToSpectreConsoleTable(Dictionary<string, int> printData, Dictionary<string, double> printTimingData, string ProjectSummaryLink)
        {
            SafeSpectreAction(() =>
            {
                WriteHeader("SUMMARY");
                int consoleWidth = GetConsoleWidth(6, 120);

                int maxValue = printData.Values.Max();
                int barMaxWidth = Math.Max(20, 40);

                var table = new Table()
                    .BorderColor(Color.White)
                    .Border(TableBorder.Rounded)
                    .Width(Math.Min(consoleWidth, 200))
                    .Expand();

                table.AddColumn(new TableColumn("[green bold]Feature[/]"));
                table.AddColumn(new TableColumn("[blue bold]Count[/]"));

                foreach (var item in printData)
                {
                    string color = GetColorForItem(item.Key, item.Value);
                    string formattedValue = item.Value.ToString().PadLeft(6);

                    string formattedKey = item.Key;

                    int barWidth = maxValue > 0 ? (int)((double)item.Value / maxValue * barMaxWidth) : 0;
                    string visualBar = new('█', barWidth);

                    table.AddRow(
                        $"[white]{formattedKey}[/]",
                        $"[{color}]{visualBar.PadRight(barMaxWidth + 2)}{formattedValue}[/]"
                    );
                }

                AnsiConsole.Write(table);

                if (printTimingData.Count != 0)
                {
                    WriteLine();
                    DisplayTimingTable(printTimingData);
                }

                if (!string.IsNullOrWhiteSpace(ProjectSummaryLink))
                {
                    WriteLine();
                    WriteInfoWithMarkup($"[blue]Project Summary: [/][white]{ProjectSummaryLink}[/]");
                }
            }, "Package Summary Table", "Panel");
        }

        private static void DisplayTimingTable(Dictionary<string, double> printTimingData)
        {
            int consoleWidth = GetConsoleWidth(6, 120);

            var table = new Table()
                .BorderColor(Color.Grey)
                .Border(TableBorder.Rounded)
                .Title("[yellow]Execution Timing[/]")
                .Width(Math.Min(consoleWidth, 100));

            table.AddColumn("[green]Operation[/]");
            table.AddColumn("[blue]Time (seconds)[/]");

            foreach (var item in printTimingData)
            {
                string timeFormatted = item.Value.ToString("F2");

                Color timeColor;
                if (item.Value > 60)
                {
                    timeColor = Color.Cyan1;
                }
                else if (item.Value > 30)
                {
                    timeColor = Color.Yellow;
                }
                else
                {
                    timeColor = Color.Green;
                }

                string operationName = item.Key.Length > 50 ? string.Concat(item.Key.AsSpan(0, 47), "...") : item.Key;
                table.AddRow(operationName, $"[{timeColor}]{timeFormatted}[/]");
            }

            AnsiConsole.Write(table);
        }

        private static string GetColorForItem(string key, int value)
        {
            if (key == "Packages Not Uploaded Due To Error" ||
                key == "Packages Not Existing in Remote Cache")
            {
                return value > 0 ? "red" : "green";
            }

            if (_colorCache.TryGetValue(key, out string cachedColor))
            {
                return cachedColor;
            }

            var colors = new[] { "blue", "purple", "magenta", "cyan", "yellow", "green", "teal", "lime", "aqua", "grey", "darkred", "darkcyan" };

            string assignedColor = colors[_colorIndex % colors.Length];
            _colorCache[key] = assignedColor;
            _colorIndex++;

            return assignedColor;
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

                    int consoleWidth = GetConsoleWidth(6, 120);

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

                    AnsiConsole.Write(table);
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
            SafeSpectreAction(() => AnsiConsole.WriteLine(), "", "Info");
        }

        public static void WriteInfoWithMarkup(string message)
        {
            SafeSpectreAction(() => AnsiConsole.MarkupLine(message), message, "Info");
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

        public static void WriteFossologySucessStatusMessage(string message, string formattedName,ComparisonBomData item)
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
    }
}