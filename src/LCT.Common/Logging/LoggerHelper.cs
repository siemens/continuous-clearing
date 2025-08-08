using LCT.Common.Model;
using log4net;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using CycloneDX.Models;
using Level=log4net.Core.Level;

namespace LCT.Common.Logging
{
    public class LoggerHelper
    {
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static Dictionary<string, string> _colorCache = new Dictionary<string, string>();
        private static int _colorIndex = 0;
        public static void DisplayInputParametersWithSpectreConsole(CatoolInfo caToolInformation, CommonAppSettings appSettings, string listOfInternalRepoList, string listOfInclude, string listOfExclude, string listOfExcludeComponents)
        {
            int consoleWidth = Console.WindowWidth > 0 ? Console.WindowWidth - 10 : 110;
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
            try
            {                
                int consoleWidth = Console.WindowWidth > 0 ? Console.WindowWidth - 4 : 120;
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
            }
            catch
            {
                WriteFallback(content, title ?? "Panel");
            }
        }

        public static void WriteHeader(string title)
        {
            try
            {
                var centeredText = title.PadLeft((Console.WindowWidth + title.Length) / 2);
                AnsiConsole.MarkupLine($"[bold white]{Markup.Escape(centeredText)}[/]");
            }
            catch
            {
                WriteFallback(title, "Header");
            }
        }
        public static void WriteFallback(string message, string messageType = "Info")
        {
            switch (messageType.ToLower())
            {
                case "warning":
                    Console.WriteLine($"WARNING: {message}");
                    break;
                case "error":
                    Console.WriteLine($"ERROR: {message}");
                    break;
                case "success":
                    Console.WriteLine($"SUCCESS: {message}");
                    break;
                case "notice":
                    Console.WriteLine($"NOTICE: {message}");
                    break;
                case "header":
                    Console.WriteLine(message);
                    break;
                case "panel":
                    Console.WriteLine(message);
                    break;
                default:
                    Console.WriteLine(message);
                    break;
            }
        }


        public static void WriteToSpectreConsoleTable(Dictionary<string, int> printData, Dictionary<string, double> printTimingData, string ProjectSummaryLink)
        {
            try
            {
                WriteHeader("SUMMARY");
                int consoleWidth = Console.WindowWidth > 0 ? Console.WindowWidth - 6 : 120;

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
            }
            catch
            {
                DisplaySimpleSummary(printData, printTimingData);
            }
        }

        private static void DisplayTimingTable(Dictionary<string, double> printTimingData)
        {
            int consoleWidth = Console.WindowWidth > 0 ? Console.WindowWidth - 6 : 120;

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
                Color timeColor = item.Value > 60 ? Color.Cyan1 :
                                 item.Value > 30 ? Color.Yellow : Color.Green;

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

        private static void DisplaySimpleSummary(Dictionary<string, int> printData, Dictionary<string, double> printTimingData)
        {
            WriteInfoWithMarkup("[green bold]Package Summary:[/]");
            WriteLine();

            foreach (var item in printData)
            {
                string color = GetColorForItem(item.Key, item.Value);
                WriteInfoWithMarkup($"[{color}]{item.Key}: {item.Value}[/]");
            }

            if (printTimingData.Count != 0)
            {
                WriteLine();
                WriteInfoWithMarkup("[yellow bold]Timing Summary:[/]");
                foreach (var item in printTimingData)
                {
                    WriteInfoWithMarkup($"[cyan]{item.Key}: {item.Value:F2} seconds[/]");
                }
            }
        }
        public static void WriteInternalComponentsListTableToKpi(List<Component> internalComponents)
        {
            if (internalComponents?.Count > 0)
            {
                try
                {
                    WriteLine();
                    WriteInfoWithMarkup("[yellow bold]* Internal Components Identified which will not be sent for clearing:[/]");
                    WriteLine();

                    int consoleWidth = Console.WindowWidth > 0 ? Console.WindowWidth - 6 : 120;

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
                }
                catch (Exception)
                {
                    WriteInternalComponentsListToKpiFallback(internalComponents);
                }
            }
        }

        private static void WriteInternalComponentsListToKpiFallback(List<Component> internalComponents)
        {
            const string Name = "Name";
            const string Version = "Version";

            WriteFallback("* Internal Components Identified which will not be sent for clearing:", "Alert");
            WriteFallback($"{"=",5}{string.Join("", Enumerable.Repeat("=", 98)),5}", "Alert");
            WriteFallback($"{"|",5}{Name,-45} {"|",5} {Version,35} {"|",10}", "Alert");
            WriteFallback($"{"=",5}{string.Join("", Enumerable.Repeat("=", 98)),5}", "Alert");

            foreach (var item in internalComponents)
            {
                WriteFallback($"{"|",5}{item.Name,-45} {"|",5} {item.Version,35} {"|",10}", "Alert");
                WriteFallback($"{"-",5}{string.Join("", Enumerable.Repeat("-", 98)),5}", "Alert");
            }

            WriteLine();
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
            try
            {
                AnsiConsole.WriteLine();
            }
            catch
            {
                Console.WriteLine();
            }
        }
        public static void WriteInfoWithMarkup(string message)
        {
            try
            {
                AnsiConsole.MarkupLine(message); 
            }
            catch
            {
                WriteFallback(message, "Info");
            }
        }
    }
}
