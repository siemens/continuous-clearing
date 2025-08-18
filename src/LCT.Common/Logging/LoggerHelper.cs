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

                    if (componentInfo.Count > 0)
                    {
                        WriteLine();
                        WriteInfoWithMarkup("[yellow]* List of components without source download URL:[/]");
                        WriteInfoWithMarkup("[yellow] Update the source download URL & Upload the source code manually if the SRC attachment is missing for the component[/]");
                        WriteLine();

                        var table = new Table()
                            .BorderColor(Color.Yellow)
                            .Border(TableBorder.Rounded)
                            .Width(Math.Min(GetConsoleWidth(6, 120), 200))
                            .Expand();

                        table.AddColumns(
                            new TableColumn("[green]Name[/]").Width(45),
                            new TableColumn("[blue]Version[/]").Width(25),
                            new TableColumn("[cyan]SW360 Release URL[/]").Width(120)
                        );

                        foreach (var item in componentInfo)
                        {
                            string link = Sw360URL(sw360URL, item.ReleaseID);
                            table.AddRow(
                                Markup.Escape(item.Name),
                                Markup.Escape(item.Version),
                                Markup.Escape(link)
                            );
                        }

                        AnsiConsole.Write(table);
                        WriteLine();
                    }

                    if (lstReleaseNotCreated.Count > 0)
                    {
                        WriteLine();
                        WriteInfoWithMarkup("[yellow]* List of components or releases not created in SW360:[/]");
                        WriteInfoWithMarkup("[yellow]  There could be network/SW360/FOSSology server problem. Check and Re-Run the pipeline.Check the logs for more details[/]");
                        WriteLine();

                        var table = new Table()
                            .BorderColor(Color.Yellow)
                            .Border(TableBorder.Rounded)
                            .Width(Math.Min(GetConsoleWidth(6, 120), 120));

                        table.AddColumns(
                            new TableColumn("[green]Name[/]").Width(45),
                            new TableColumn("[blue]Version[/]").Width(25)
                        );

                        foreach (var item in lstReleaseNotCreated)
                        {
                            table.AddRow(
                                Markup.Escape(item.Name),
                                Markup.Escape(item.Version)
                            );
                        }

                        AnsiConsole.Write(table);
                        WriteLine();
                    }

                    EnvironmentHelper environmentHelper = new EnvironmentHelper();
                    environmentHelper.CallEnvironmentExit(2);

                }, "Components Without Download URL", "Alert");
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
            if (LoggerFactory.UseSpectreConsole)
            {
                SafeSpectreAction(() =>
                {
                    var projectTypes = componentsInBOM
                        .Select(item => item.Properties.First(x => x.Name == Dataconstant.Cdx_ProjectType).Value)
                        .Distinct()
                        .ToList();

                    string content = $"[green]Current Application Settings[/]\n\n";

                    foreach (var projectType in projectTypes)
                    {
                        content += $"[green]-[/] [green]{projectType}[/]\n\n";

                        var projectConfigMap = new Dictionary<string, Config>(StringComparer.OrdinalIgnoreCase)
                {
                    { "NPM", appSettings.Npm },
                    { "NUGET", appSettings.Nuget },
                    { "MAVEN", appSettings.Maven },
                    { "DEBIAN", appSettings.Debian },
                    { "POETRY", appSettings.Poetry },
                    { "CONAN", appSettings.Conan }
                };

                        if (projectConfigMap.TryGetValue(projectType, out var config))
                        {
                            if (config != null)
                            {
                                content += $"  [cyan]DEVDEP_REPO_NAME[/]\n";
                                content += $"  └──➤ {(!string.IsNullOrEmpty(config.DevDepRepo) ? config.DevDepRepo : Dataconstant.NotConfigured)}\n\n";

                                var thirdPartyRepo = config.Artifactory?.ThirdPartyRepos?.FirstOrDefault(repo => repo.Upload)?.Name ?? Dataconstant.NotConfigured;
                                content += $"  [cyan]THIRD_PARTY_REPO_NAME[/]\n";
                                content += $"  └──➤ {thirdPartyRepo}\n\n";

                                content += $"  [cyan]RELEASE_REPO_NAME[/]\n";
                                content += $"  └──➤ {(!string.IsNullOrEmpty(config.ReleaseRepo) ? config.ReleaseRepo : Dataconstant.NotConfigured)}\n\n";

                                content += $"  [cyan]Config[/]\n";

                                string excludeList = !string.IsNullOrEmpty(config.Exclude?.FirstOrDefault())
                                    ? string.Join(", ", config.Exclude)
                                    : Dataconstant.NotConfigured;
                                string includeList = !string.IsNullOrEmpty(config.Include?.FirstOrDefault())
                                    ? string.Join(", ", config.Include)
                                    : Dataconstant.NotConfigured;

                                content += $"  ├──[white]Include[/]\n";
                                content += $"  │   └──➤ {includeList}\n";
                                content += $"  └──[white]Exclude[/]\n";
                                content += $"      └──➤ {excludeList}\n\n";
                            }
                        }
                        else
                        {
                            content += $"  └──[red]Invalid ProjectType[/]\n\n";
                        }
                    }

                    WriteStyledPanel(content.TrimEnd(), "", "white");

                }, "Current Application Settings", "Info");
            }
            else
            {
                Logger.Info("Current Application Settings:");

                foreach (var projectType in componentsInBOM
                    .Select(item => item.Properties.First(x => x.Name == Dataconstant.Cdx_ProjectType).Value)
                    .Distinct())
                {
                    Logger.Info($"{projectType}:\n\t");

                    var projectConfigMap = new Dictionary<string, Config>(StringComparer.OrdinalIgnoreCase)
            {
                { "NPM", appSettings.Npm },
                { "NUGET", appSettings.Nuget },
                { "MAVEN", appSettings.Maven },
                { "DEBIAN", appSettings.Debian },
                { "POETRY", appSettings.Poetry },
                { "CONAN", appSettings.Conan }
            };

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
        public static void DisplayInputParametersWithSpectreConsole(CatoolInfo caToolInformation, CommonAppSettings appSettings, string listOfInternalRepoList, string listOfInclude, string listOfExclude, string listOfExcludeComponents, string exeType,string bomFilePath)
        {
            string content = string.Empty;
            if (exeType== "Identifier") 
            {
                int consoleWidth = GetConsoleWidth(10, 110);
                int maxPathLength = Math.Max(60, consoleWidth - 20);

                content = $"Start of Package Identifier execution: [green]{DateTime.Now}[/]\n\n";
                content += $"[green]-[/] [yellow]Input Parameters used in Package Identifier[/]\n\n";

                content += $"[green]-[/] [cyan]CaToolVersion[/]\n";
                content += $"  └──✅ {caToolInformation.CatoolVersion}\n\n";
                content += $"[green]-[/] [cyan]CaToolRunningPath[/]\n";
                content += $"  └──➤ {WrapPath(caToolInformation.CatoolRunningLocation, maxPathLength)}\n\n";
                content += $"[green]-[/] [cyan]PackageFilePath[/]\n";
                content += $"  └──➤ {WrapPath(appSettings.Directory.InputFolder, maxPathLength)}\n\n";
                content += $"[green]-[/] [cyan]BomFolderPath[/]\n";
                content += $"  └──➤ {WrapPath(appSettings.Directory.OutputFolder, maxPathLength)}\n\n";

                if (appSettings.SW360 != null)
                {
                    content += $"[green]-[/] [cyan]SW360Url[/]\n";
                    content += $"  └──➤ {appSettings.SW360.URL}\n\n";

                    content += $"[green]-[/] [cyan]SW360AuthTokenType[/]\n";
                    content += $"  └──➤ {appSettings.SW360.AuthTokenType}\n\n";

                    content += $"[green]-[/] [cyan]SW360ProjectName[/]\n";
                    content += $"  └──➤ {appSettings.SW360.ProjectName}\n\n";

                    content += $"[green]-[/] [cyan]SW360ProjectID[/]\n";
                    content += $"  └──➤ {appSettings.SW360.ProjectID}\n\n";

                    content += $"[green]-[/] [cyan]ExcludeComponents[/]\n";
                    content += $"  └──➤ {WrapPath(string.IsNullOrEmpty(listOfExcludeComponents) ? "None" : listOfExcludeComponents, maxPathLength)}\n\n";
                }

                if (appSettings.Jfrog != null)
                {
                    content += $"[green]-[/] [cyan]InternalRepoList[/]\n";
                    content += $"  └──➤ {WrapPath(string.IsNullOrEmpty(listOfInternalRepoList) ? "None" : listOfInternalRepoList, maxPathLength)}\n\n";
                }

                content += $"[green]-[/] [cyan]ProjectType[/]\n";
                content += $"  └──➤ {appSettings.ProjectType}\n\n";

                content += $"[green]-[/] [cyan]LogFolderPath[/]\n";
                content += $"  └──➤ {WrapPath(Log4Net.CatoolLogPath, maxPathLength)}\n\n";

                content += $"[green]-[/] [cyan]Include[/]\n";
                content += $"  └──➤ {WrapPath(string.IsNullOrEmpty(listOfInclude) ? "None" : listOfInclude, maxPathLength)}\n\n";

                content += $"[green]-[/] [cyan]Exclude[/]\n";
                content += $"  └──➤ {WrapPath(string.IsNullOrEmpty(listOfExclude) ? "None" : listOfExclude, maxPathLength)}";

            }else if (exeType == "Creator")
            {
                int consoleWidth = GetConsoleWidth(10, 110);
                int maxPathLength = Math.Max(60, consoleWidth - 20);

                content = $"Start of Package Creater execution: [green]{DateTime.Now}[/]\n\n";
                content += $"[green]-[/] [yellow]Input parameters used in Package Creater[/]\n\n";
                content += $"[green]-[/] [cyan]CaToolVersion[/]\n";
                content += $"  └──✅ {caToolInformation.CatoolVersion}\n\n";
                content += $"[green]-[/] [cyan]CaToolRunningPath[/]\n";
                content += $"  └──➤ {WrapPath(caToolInformation.CatoolRunningLocation, maxPathLength)}\n\n";
                content += $"[green]-[/] [cyan]BomFilePath[/]\n";
                content += $"  └──➤ {WrapPath(bomFilePath, maxPathLength)}\n\n";
                content += $"[green]-[/] [cyan]SW360Url[/]\n";
                content += $"  └──➤ {appSettings.SW360.URL}\n\n";
                content += $"[green]-[/] [cyan]SW360AuthTokenType[/]\n";
                content += $"  └──➤ {appSettings.SW360.AuthTokenType}\n\n";
                content += $"[green]-[/] [cyan]SW360ProjectName[/]\n";
                content += $"  └──➤ {appSettings.SW360.ProjectName}\n\n";
                content += $"[green]-[/] [cyan]SW360ProjectID[/]\n";
                content += $"  └──➤ {appSettings.SW360.ProjectID}\n\n";
                content += $"[green]-[/] [cyan]FossologyURL[/]\n";
                content += $"  └──➤ {appSettings.SW360.Fossology.URL}\n\n";
                content += $"[green]-[/] [cyan]EnableFossTrigger[/]\n";
                content += $"  └──➤ {appSettings.SW360.Fossology.EnableTrigger}\n\n";
                content += $"[green]-[/] [cyan]IgnoreDevDependency[/]\n";
                content += $"  └──➤ {appSettings.SW360.IgnoreDevDependency}\n\n";
                content += $"[green]-[/] [cyan]LogFolderPath[/]\n";
                content += $"  └──➤ {WrapPath(Log4Net.CatoolLogPath, maxPathLength)}\n\n";
            }
            else if (exeType == "Uploader")
            {
                int consoleWidth = GetConsoleWidth(10, 110);
                int maxPathLength = Math.Max(60, consoleWidth - 20);

                content = $"Start of {exeType} execution: [green]{DateTime.Now}[/]\n\n";
                content += $"[green]-[/] [yellow]Input Parameters used in Artifactory Uploader[/]\n\n";
                content += $"[green]-[/] [cyan]CaToolVersion[/]\n";
                content += $"  └──✅ {caToolInformation.CatoolVersion}\n\n";
                content += $"[green]-[/] [cyan]CaToolRunningPath[/]\n";
                content += $"  └──➤ {WrapPath(caToolInformation.CatoolRunningLocation, maxPathLength)}\n\n";
                content += $"[green]-[/] [cyan]BomFilePath[/]\n";
                content += $"  └──➤ {WrapPath(bomFilePath, maxPathLength)}\n\n";
                content += $"[green]-[/] [cyan]JFrogUrl[/]\n";
                content += $"  └──➤ {appSettings.Jfrog.URL}\n\n";
                content += $"[green]-[/] [cyan]Dry-run[/]\n";
                content += $"  └──➤ {appSettings.Jfrog.DryRun}\n\n";
                content += $"[green]-[/] [cyan]LogFolderPath[/]\n";
                content += $"  └──➤ {WrapPath(Log4Net.CatoolLogPath, maxPathLength)}\n\n";
            }
             WriteStyledPanel(content);
        }
        public static void LogInputParameters(CatoolInfo caToolInformation, CommonAppSettings appSettings, string listOfInternalRepoList=null, string listOfInclude = null, string listOfExclude = null, string listOfExcludeComponents = null,string exeType = null,string bomFilePath=null)
        {
            if (LoggerFactory.UseSpectreConsole)
            {
                DisplayInputParametersWithSpectreConsole(caToolInformation, appSettings, listOfInternalRepoList, listOfInclude, listOfExclude, listOfExcludeComponents,exeType,bomFilePath);
            }
            else
            {
                LogInputParametersWithLog4net(caToolInformation, appSettings, listOfInternalRepoList, listOfInclude, listOfExclude, listOfExcludeComponents,exeType,bomFilePath);
            }
        }
        private static void LogInputParametersWithLog4net(CatoolInfo caToolInformation, CommonAppSettings appSettings, string listOfInternalRepoList, string listOfInclude, string listOfExclude, string listOfExcludeComponents,string exeType,string bomFilePath)
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
                              $"ExcludeComponents\t --> {listOfExcludeComponents}\n\t";
                }
                if (appSettings.Jfrog != null)
                {
                    logMessage += $"InternalRepoList\t --> {listOfInternalRepoList}\n\t";
                }

                logMessage += $"ProjectType\t\t --> {appSettings.ProjectType}\n\t" +
                              $"LogFolderPath\t\t --> {Log4Net.CatoolLogPath}\n\t" +
                              $"Include\t\t\t --> {listOfInclude}\n\t" +
                              $"Exclude\t\t\t --> {listOfExclude}\n";

                Logger.Logger.Log(null, Level.Notice, logMessage, null);
            }else if (exeType == "Creator")
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
        public static void WriteFossologystatusMessage(string message)
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
        public static void WriteComponentstatusMessage(string message,ComparisonBomData item)
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
    }
}