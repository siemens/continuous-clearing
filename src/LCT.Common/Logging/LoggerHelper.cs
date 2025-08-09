using LCT.Common.Model;
using log4net;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using CycloneDX.Models;
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
        private static void SafeSpectreAction(Action spectreAction, string fallbackMessage, string fallbackType = "Info")
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

        public static void DisplayInputParametersWithSpectreConsole(CatoolInfo caToolInformation, CommonAppSettings appSettings, string listOfInternalRepoList, string listOfInclude, string listOfExclude, string listOfExcludeComponents)
        {
            int consoleWidth = GetConsoleWidth(10, 110);
            int maxPathLength = Math.Max(60, consoleWidth - 20);

            var content = $"Start of Package Identifier execution: [green]{DateTime.Now}[/]\n\n";
            content += $"[green]-[/] [yellow]Input Parameters used in Package Identifier:[/]\n\n";

            content += $"[green]-[/] CaToolVersion\n";
            content += $"  └──✅ {caToolInformation.CatoolVersion}\n\n";
            content += $"[green]-[/] CaToolRunningPath\n";
            content += $"  └──➤ {WrapPath(caToolInformation.CatoolRunningLocation, maxPathLength)}\n\n";
            content += $"[green]-[/] PackageFilePath\n";
            content += $"  └──➤ {WrapPath(appSettings.Directory.InputFolder, maxPathLength)}\n\n";
            content += $"[green]-[/] BomFolderPath\n";
            content += $"  └──➤ {WrapPath(appSettings.Directory.OutputFolder, maxPathLength)}\n\n";

            if (appSettings.SW360 != null)
            {
                content += $"[green]-[/] SW360Url\n";
                content += $"  └──➤ {appSettings.SW360.URL}\n\n";

                content += $"[green]-[/] SW360AuthTokenType\n";
                content += $"  └──➤ {appSettings.SW360.AuthTokenType}\n\n";

                content += $"[green]-[/] SW360ProjectName\n";
                content += $"  └──➤ {appSettings.SW360.ProjectName}\n\n";

                content += $"[green]-[/] SW360ProjectID\n";
                content += $"  └──➤ {appSettings.SW360.ProjectID}\n\n";

                content += $"[green]-[/] ExcludeComponents\n";
                content += $"  └──➤ {WrapPath(string.IsNullOrEmpty(listOfExcludeComponents) ? "None" : listOfExcludeComponents, maxPathLength)}\n\n";
            }

            if (appSettings.Jfrog != null)
            {
                content += $"[green]-[/] InternalRepoList\n";
                content += $"  └──➤ {WrapPath(string.IsNullOrEmpty(listOfInternalRepoList) ? "None" : listOfInternalRepoList, maxPathLength)}\n\n";
            }

            content += $"[green]-[/] ProjectType\n";
            content += $"  └──➤ {appSettings.ProjectType}\n\n";

            content += $"[green]-[/] LogFolderPath\n";
            content += $"  └──➤ {WrapPath(Log4Net.CatoolLogPath, maxPathLength)}\n\n";

            content += $"[green]-[/] Include\n";
            content += $"  └──➤ {WrapPath(string.IsNullOrEmpty(listOfInclude) ? "None" : listOfInclude, maxPathLength)}\n\n";

            content += $"[green]-[/] Exclude\n";
            content += $"  └──➤ {WrapPath(string.IsNullOrEmpty(listOfExclude) ? "None" : listOfExclude, maxPathLength)}";

            WriteStyledPanel(content);
        }
        public static void LogInputParameters(CatoolInfo caToolInformation, CommonAppSettings appSettings, string listOfInternalRepoList, string listOfInclude, string listOfExclude, string listOfExcludeComponents)
        {
            if (LoggerFactory.UseSpectreConsole)
            {
                DisplayInputParametersWithSpectreConsole(caToolInformation, appSettings, listOfInternalRepoList, listOfInclude, listOfExclude, listOfExcludeComponents);
            }
            else
            {
                LogInputParametersWithLog4net(caToolInformation, appSettings, listOfInternalRepoList, listOfInclude, listOfExclude, listOfExcludeComponents);
            }
        }
        private static void LogInputParametersWithLog4net(CatoolInfo caToolInformation, CommonAppSettings appSettings, string listOfInternalRepoList, string listOfInclude, string listOfExclude, string listOfExcludeComponents)
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
        }
        private static string WrapPath(string path, int maxLength = 80, string prefix = "        ")
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
        
        public static void SpectreConsoleInitialMessage()
        {
            if (LoggerFactory.UseSpectreConsole)
            {
                WriteHeader("PACKAGE IDENTIFIER");
            }
            else
            {
                Logger.Logger.Log(null, Level.Notice, $"\n====================<<<<< Package Identifier >>>>>====================", null);
                Logger.Logger.Log(null, Level.Notice, $"\nStart of Package Identifier execution: {DateTime.Now}", null);
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
    }
}