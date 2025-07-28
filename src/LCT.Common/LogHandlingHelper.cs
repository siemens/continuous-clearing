using LCT.Common.Constants;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using CycloneDX.Models;
using LCT.Common.Model;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace LCT.Common
{
    public static partial class LogHandlingHelper
    {
#pragma warning disable CA2211 
        private static ILog _logger = LogManager.GetLogger(typeof(LogHandlingHelper));
#pragma warning restore CA2211 
        private static readonly char[] NewLineSeparator = new[] { '\n' };
        public const string LogSeparator = "============================================================================================================================================";
        public const string LogHeaderSeparator = "--------------------------------------------------------------------------------------------------------------------------------------------";
        public const string MaskedValue = "*****";
        private static readonly JsonSerializerOptions CachedJsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true 
        };
        public static ILog Logger
        {
            get => _logger;
            set => _logger = value ?? throw new ArgumentNullException(nameof(value));
        }
        public static void ExceptionErrorHandling(string context, string details, Exception ex, string additionalDetails = null)
        {
            var logBuilder = new StringBuilder();
            BuildErrorLog(logBuilder, context, details, null, additionalDetails, ex);
            Logger.Debug("\n" + logBuilder.ToString());
        }
        public static void BasicErrorHandling(string context, string details, string message, string additional)
        {
            var logBuilder = new StringBuilder();
            BuildErrorLog(logBuilder, context, details, message, additional);
            Logger.Debug("\n" + logBuilder.ToString());
        }
        public static async Task HttpResponseHandling(string context, string details, HttpResponseMessage response, string additionalDetails = null)
        {
            // Extract request details
            string requestMethod = response?.RequestMessage?.Method.ToString() ?? string.Empty;
            string requestUrl = response?.RequestMessage?.RequestUri?.ToString() ?? string.Empty;
            string requestHeaders = ExtractRequestHeaders(response);

            // Extract response details
            string reasonPhrase = GetReasonPhrase(response);
            string content = response?.Content != null ? await response.Content.ReadAsStringAsync() : string.Empty;
            string responseContent = GetResponseContentAsync(content);

            // Build the log message
            var logBuilder = new System.Text.StringBuilder();
            AppendLogHeaders(logBuilder, context, details);
            AppendRequestDetails(logBuilder, requestMethod, requestUrl, requestHeaders);
            AppendResponseDetails(logBuilder, response, reasonPhrase, responseContent);
            AppendAdditionalDetails(logBuilder, additionalDetails);

            logBuilder.AppendLine(LogSeparator);

            // Log the constructed message
            Logger.Debug(logBuilder.ToString());
        }

        private static string ExtractRequestHeaders(HttpResponseMessage response)
        {
            if (response?.RequestMessage?.Headers == null) return string.Empty;

            return string.Join("\n", response.RequestMessage.Headers
                .Where(h => !h.Key.Equals("LogWarnings", StringComparison.CurrentCultureIgnoreCase) &&
                            !h.Key.Equals("urlInfo", StringComparison.CurrentCultureIgnoreCase))
                .Select(h =>
                {
                    string headerValue = string.Join(", ", h.Value);
                    if (h.Key.Contains("authorization", StringComparison.CurrentCultureIgnoreCase) ||
                        h.Key.Contains("token", StringComparison.CurrentCultureIgnoreCase) ||
                        headerValue.Contains("bearer", StringComparison.CurrentCultureIgnoreCase))
                    {
                        headerValue = "*****"; // Mask sensitive values
                    }
                    return $"{h.Key}: {headerValue}";
                }));
        }

        private static string GetReasonPhrase(HttpResponseMessage response)
        {
            return !string.IsNullOrEmpty(response?.ReasonPhrase) ? response.ReasonPhrase : "No Reason Phrase";
        }

        private static string GetResponseContentAsync(string response)
        {

            if (!string.IsNullOrEmpty(response))
            {
                response = MaskSensitiveData(response);
                var lines = response.Split(NewLineSeparator, StringSplitOptions.None);
                if (lines.Length <= 100)
                {
                    return response;
                }
                if (Log4Net.Verbose)
                {
                    try
                    {
                        using var jsonDoc = JsonDocument.Parse(response);
                        string formattedJson = JsonSerializer.Serialize(jsonDoc.RootElement, CachedJsonSerializerOptions);

                        var formattedLines = formattedJson.Split(Environment.NewLine, StringSplitOptions.None);

                        if (formattedLines.Length <= 1000)
                        {
                            return formattedJson;
                        }

                        int targetLines = Math.Min(1000, lines.Length);
                        int chunkSize = (int)Math.Ceiling((double)formattedLines.Length / targetLines);
                        string[] compressedLines = new string[targetLines];

                        for (int i = 0; i < targetLines; i++)
                        {
                            int start = i * chunkSize;
                            int end = Math.Min(start + chunkSize, formattedLines.Length);

                            compressedLines[i] = string.Join(" ", formattedLines.Skip(start).Take(end - start));
                        }

                        return string.Join(Environment.NewLine, compressedLines.Where(l => !string.IsNullOrEmpty(l)));
                    }
                    catch (JsonException ex)
                    {
                        ExceptionErrorHandling("Processing json response", $"Methodname:GetResponseContentAsync()", ex, "Api response processing");
                        return response;
                    }

                }
            }

            return "";
        }        
        private static void BuildErrorLog(StringBuilder logBuilder, string context, string details, string message, string additionalDetails, Exception ex = null)
        {
            logBuilder.AppendLine(LogSeparator);
            logBuilder.AppendLine(" ERROR DETECTED");
            logBuilder.AppendLine(LogSeparator);
            logBuilder.AppendLine($"| {"Field",-20} | {"Value",-100} |");
            logBuilder.AppendLine(LogHeaderSeparator);
            logBuilder.AppendLine($"| {"Time",-20} | {GetISTTime(),-100} |");
            logBuilder.AppendLine($"| {"Details",-20} | {details,-100} |");
            logBuilder.AppendLine($"| {"Context",-20} | {context,-100} |");

            // Add additional details if available
            if (!string.IsNullOrEmpty(additionalDetails))
            {
                logBuilder.AppendLine(LogHeaderSeparator);
                logBuilder.AppendLine(" ADDITIONAL DETAILS");
                logBuilder.AppendLine(LogHeaderSeparator);
                logBuilder.AppendLine($"| {"Details",-20} | {additionalDetails,-100} |");
            }

            logBuilder.AppendLine(LogHeaderSeparator);
            logBuilder.AppendLine(" ERROR DETAILS");
            logBuilder.AppendLine(LogHeaderSeparator);

            if (!string.IsNullOrEmpty(message))
            {
                logBuilder.AppendLine($"| {"Message",-20} | {message,-100} |");
            }

            // Add exception details if available
            if (ex != null)
            {
                logBuilder.AppendLine($"| {"Exception Type",-20} | {ex.GetType().Name,-100} |");
                logBuilder.AppendLine($"| {"Message",-20} | {ex.Message,-100} |");
                logBuilder.AppendLine($"| {"Stack Trace",-20} | {ex.StackTrace,-100} |");

                // Add inner exception details if available
                if (ex.InnerException != null)
                {
                    logBuilder.AppendLine(LogHeaderSeparator);
                    logBuilder.AppendLine(" INNER EXCEPTION DETAILS");
                    logBuilder.AppendLine(LogHeaderSeparator);
                    logBuilder.AppendLine($"| {"Exception Type",-20} | {ex.InnerException.GetType().Name,-100} |");
                    logBuilder.AppendLine($"| {"Message",-20} | {ex.InnerException.Message,-100} |");
                    logBuilder.AppendLine($"| {"Stack Trace",-20} | {ex.InnerException.StackTrace,-100} |");
                }
            }

            logBuilder.AppendLine(LogSeparator);
        }
        private static void AppendLogHeaders(StringBuilder logBuilder, string context, string details)
        {
            logBuilder.AppendLine($"\n{LogSeparator}");
            logBuilder.AppendLine(" HTTP API RESPONSE DETAILS");
            logBuilder.AppendLine(LogHeaderSeparator);
            logBuilder.AppendLine($"Details: {details}");
            logBuilder.AppendLine($"Description: {context}");
            logBuilder.AppendLine(LogHeaderSeparator);
        }

        private static void AppendRequestDetails(StringBuilder logBuilder, string method, string url, string headers)
        {
            if (string.IsNullOrEmpty(method) && string.IsNullOrEmpty(url) && string.IsNullOrEmpty(headers)) return;

            logBuilder.AppendLine("REQUEST DETAILS");
            logBuilder.AppendLine(LogHeaderSeparator);
            if (!string.IsNullOrEmpty(method)) logBuilder.AppendLine($"Method: {method}");
            if (!string.IsNullOrEmpty(url)) logBuilder.AppendLine($"URL: {url}");
            if (!string.IsNullOrEmpty(headers)) logBuilder.AppendLine($"Headers:\n{headers}");
            logBuilder.AppendLine(LogHeaderSeparator);
        }

        private static void AppendResponseDetails(StringBuilder logBuilder, HttpResponseMessage response, string reasonPhrase, string content)
        {
            logBuilder.AppendLine("RESPONSE DETAILS");
            logBuilder.AppendLine(LogHeaderSeparator);
            logBuilder.AppendLine($"Status Code: {response?.StatusCode}");
            logBuilder.AppendLine($"Reason Phrase: {reasonPhrase}");

            if (!string.IsNullOrEmpty(content))
            {
                logBuilder.AppendLine(LogHeaderSeparator);
                logBuilder.AppendLine("CONTENT:");
                logBuilder.AppendLine(LogHeaderSeparator);
                logBuilder.AppendLine(content);
            }
        }
        private static void AppendHeaders(StringBuilder logBuilder, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers)
        {
            logBuilder.AppendLine(LogHeaderSeparator);
            logBuilder.AppendLine(" HEADERS");
            logBuilder.AppendLine(LogHeaderSeparator);

            if (headers != null && headers.Any())
            {
                foreach (var header in headers)
                {
                    if (header.Key.Equals("LogWarnings", StringComparison.CurrentCultureIgnoreCase) ||
                        header.Key.Equals("urlInfo", StringComparison.CurrentCultureIgnoreCase))
                    {
                        continue;
                    }

                    string headerValue = string.Join(", ", header.Value);
                    if (header.Key.Contains("authorization", StringComparison.CurrentCultureIgnoreCase) ||
                        header.Key.Contains("token", StringComparison.CurrentCultureIgnoreCase) ||
                        headerValue.Contains("bearer", StringComparison.CurrentCultureIgnoreCase))
                    {
                        headerValue = MaskedValue;
                    }
                    logBuilder.AppendLine($"| {header.Key,-20} | {headerValue,-100} |");
                }
            }
            else
            {
                logBuilder.AppendLine($"| {"No Headers",-20} | {"-",100} |");
            }
        }
        private static void AppendContent(StringBuilder logBuilder, string content, string contentLabel = "CONTENT")
        {
            logBuilder.AppendLine(LogHeaderSeparator);
            logBuilder.AppendLine($" {contentLabel}");
            logBuilder.AppendLine(LogHeaderSeparator);

            if (!string.IsNullOrEmpty(content))
            {
                logBuilder.AppendLine($"| {"Body",-20} | {content,-100} |");
            }
            else
            {
                logBuilder.AppendLine($"| {"No Content",-20} | {"-",100} |");
            }
        }

        private static void AppendLogHeader(StringBuilder logBuilder, string context, string details, string title)
        {
            logBuilder.AppendLine($"\n{LogSeparator}");
            logBuilder.AppendLine($" {title}");
            logBuilder.AppendLine($"{LogSeparator}");
            logBuilder.AppendLine($"| {"Field",-20} | {"Value",-100} |");
            logBuilder.AppendLine(LogHeaderSeparator);
            logBuilder.AppendLine($"| {"Time",-20} | {GetISTTime(),-100} |");
            logBuilder.AppendLine($"| {"Details",-20} | {details,-100} |");
            logBuilder.AppendLine($"| {"Description",-20} | {context,-100} |");
        }
        private static void AppendAdditionalDetails(StringBuilder logBuilder, string additionalDetails)
        {
            if (string.IsNullOrEmpty(additionalDetails)) return;

            logBuilder.AppendLine(LogHeaderSeparator);
            logBuilder.AppendLine("ADDITIONAL DETAILS:");
            logBuilder.AppendLine(LogHeaderSeparator);
            logBuilder.AppendLine(additionalDetails);
        }
        public static async Task HttpResponseErrorHandling(string context, string details, HttpResponseMessage response, string additionalDetails = null)
        {
            // Read the response content asynchronously
            string responseContent = response.Content != null ? await response.Content.ReadAsStringAsync() : string.Empty;

            // Build the log message
            var logBuilder = new StringBuilder();
            AppendLogHeader(logBuilder, context, details, "HTTP API RESPONSE ERROR DETECTED");

            // Add response details
            logBuilder.AppendLine(LogHeaderSeparator);
            logBuilder.AppendLine(" RESPONSE DETAILS");
            logBuilder.AppendLine(LogHeaderSeparator);
            logBuilder.AppendLine($"| {"Status Code",-20} | {response.StatusCode,-100} |");
            logBuilder.AppendLine($"| {"Reason Phrase",-20} | {response.ReasonPhrase ?? "No Reason Phrase provided by the server",-100} |");

            // Add headers
            AppendHeaders(logBuilder, response.Headers);

            // Add content
            AppendContent(logBuilder, responseContent);

            // Add additional details if available
            if (!string.IsNullOrEmpty(additionalDetails))
            {
                logBuilder.AppendLine(LogHeaderSeparator);
                logBuilder.AppendLine(" ADDITIONAL DETAILS");
                logBuilder.AppendLine(LogHeaderSeparator);
                logBuilder.AppendLine($"| {"Details",-20} | {additionalDetails,-100} |");
            }

            logBuilder.AppendLine($"{LogSeparator}");

            // Log the constructed message
            Logger.Debug(logBuilder.ToString());
        }
        public static async Task HttpRequestHandling(string context, string details, HttpClient httpClient, string url, HttpContent httpContent = null)
        {
            // Read the content of the HttpContent asynchronously
            string content = httpContent != null ? await httpContent.ReadAsStringAsync() : string.Empty;

            // Build the log message
            var logBuilder = new StringBuilder();
            AppendLogHeader(logBuilder, context, details, "HTTP API REQUEST DETAILS");

            // Add request details
            logBuilder.AppendLine($"| {"URL",-20} | {url,-100} |");

            // Add headers
            AppendHeaders(logBuilder, httpClient.DefaultRequestHeaders);

            // Add content
            AppendContent(logBuilder, content);

            logBuilder.AppendLine($"{LogSeparator}");

            // Log the constructed message
            Logger.Debug(logBuilder.ToString());
        }
        public static void ListOfBomFileComponents(string bomFilePath, List<Component> components)
        {
            if (components == null || components.Count == 0)
            {
                Logger.Warn($"No components found in the BOM file: {bomFilePath}");
                return;
            }

            // Map actual property names to simplified names
            var propertyMapping = new Dictionary<string, string>
    {
        { Dataconstant.Cdx_IsDevelopment, "development" },
        { Dataconstant.Cdx_SiemensDirect, "siemens:direct" },
        { Dataconstant.Cdx_IdentifierType, "identifier-type" },
        { Dataconstant.Cdx_IsInternal, "is-internal" },
        { Dataconstant.Cdx_ArtifactoryRepoName, "jfrog-repo-name" },
        { Dataconstant.Cdx_ProjectType, "project-type" },
        { Dataconstant.Cdx_Siemensfilename, "filename" },
        { Dataconstant.Cdx_JfrogRepoPath, "jfrog-repo-path" }
    };

            var logBuilder = new System.Text.StringBuilder();
            logBuilder.AppendLine($"\n{LogSeparator}");
            logBuilder.AppendLine($"Components from BOM File: {bomFilePath}");
            logBuilder.AppendLine($"{LogSeparator}");

            // Create the table header
            logBuilder.Append($"|{"Component Name",-60} |{"Version",-15} | {"PURL",-80} |");
            foreach (var simplifiedName in propertyMapping.Values)
            {
                logBuilder.Append($" {simplifiedName,-45}|");
            }
            logBuilder.AppendLine();
            logBuilder.AppendLine(new string('-', 70 + propertyMapping.Count * 20));

            // Create the table rows
            foreach (var component in components)
            {
                string componentName = component.Name ?? "N/A";
                string version = component.Version ?? "N/A";
                string purl = component.Purl ?? "N/A";

                logBuilder.Append($"|{componentName,-60} | {version,-15} | {purl,-80} |");

                foreach (var actualName in propertyMapping.Keys)
                {
                    var propertyValue = component.Properties?.FirstOrDefault(p => p.Name == actualName)?.Value ?? "N/A";
                    logBuilder.Append($" {propertyValue,-45}|");
                }

                logBuilder.AppendLine();
            }

            logBuilder.AppendLine($"{LogSeparator}");
            Logger.Debug(logBuilder.ToString());
        }
        public static void HttpResponseOfStringContent(string context, string details, string response, string additionalDetails = null)
        {
            string responseBody = GetResponseContentAsync(response);

            // Build the log message
            var logBuilder = new System.Text.StringBuilder();
            logBuilder.AppendLine($"\n{LogSeparator}");
            logBuilder.AppendLine(" HTTP API RESPONSE DETAILS");
            logBuilder.AppendLine($"{LogHeaderSeparator}");
            logBuilder.AppendLine($"Details: {details}");
            logBuilder.AppendLine($"Description: {context}");
            logBuilder.AppendLine($"{LogHeaderSeparator}");

            // Add response details
            logBuilder.AppendLine("RESPONSE DETAILS");
            logBuilder.AppendLine($"{LogHeaderSeparator}");

            // Add content if available
            if (!string.IsNullOrEmpty(responseBody))
            {
                logBuilder.AppendLine($"{LogHeaderSeparator}");
                logBuilder.AppendLine("CONTENT:");
                logBuilder.AppendLine($"{LogHeaderSeparator}");
                logBuilder.AppendLine(responseBody);
            }

            // Append additional details if available
            if (!string.IsNullOrEmpty(additionalDetails))
            {
                logBuilder.AppendLine($"{LogHeaderSeparator}");
                logBuilder.AppendLine("ADDITIONAL DETAILS:");
                logBuilder.AppendLine($"{LogHeaderSeparator}");
                logBuilder.AppendLine(additionalDetails);
            }

            logBuilder.AppendLine($"{LogSeparator}");

            // Log the constructed message
            Logger.Debug(logBuilder.ToString());
        }
        public static void ComponentDataForLogTable(string methodName, ComparisonBomData initialItem, ComparisonBomData updatedItem)
        {
            var table = new StringBuilder();

            // Header
            table.AppendLine($"Method: {methodName}");
            table.AppendLine($"{LogHeaderSeparator}");
            table.AppendLine($"| {"Property",-45} | {"Initial Value",-100} | {"Updated Value",-100} |");
            table.AppendLine($"{LogHeaderSeparator}");

            // Rows
            table.AppendLine($"| {"Name",-45} | {initialItem.Name,-100} | {updatedItem.Name,-100} |");
            table.AppendLine($"| {"Group",-45} | {initialItem.Group,-100} | {updatedItem.Group,-100} |");
            table.AppendLine($"| {"Version",-45} | {initialItem.Version,-100} | {updatedItem.Version,-100} |");
            table.AppendLine($"| {"ComponentExternalId",-45} | {initialItem.ComponentExternalId,-100} | {updatedItem.ComponentExternalId,-100} |");
            table.AppendLine($"| {"ReleaseExternalId",-45} | {initialItem.ReleaseExternalId,-100} | {updatedItem.ReleaseExternalId,-100} |");
            table.AppendLine($"| {"PackageUrl",-45} | {initialItem.PackageUrl,-100} | {updatedItem.PackageUrl,-100} |");
            table.AppendLine($"| {"SourceUrl",-45} | {initialItem.SourceUrl,-100} | {updatedItem.SourceUrl,-100} |");
            table.AppendLine($"| {"DownloadUrl",-45} | {initialItem.DownloadUrl,-100} | {updatedItem.DownloadUrl,-100} |");
            table.AppendLine($"| {"PatchURls",-45} | {string.Join(",", initialItem.PatchURls ?? Array.Empty<string>()),-100} | {string.Join(",", updatedItem.PatchURls ?? Array.Empty<string>()),-100} |");
            table.AppendLine($"| {"ComponentStatus",-45} | {initialItem.ComponentStatus,-100} | {updatedItem.ComponentStatus,-100} |");
            table.AppendLine($"| {"ReleaseStatus",-45} | {initialItem.ReleaseStatus,-100} | {updatedItem.ReleaseStatus,-100} |");
            table.AppendLine($"| {"ApprovedStatus",-45} | {initialItem.ApprovedStatus,-100} | {updatedItem.ApprovedStatus,-100} |");
            table.AppendLine($"| {"IsComponentCreated",-45} | {initialItem.IsComponentCreated,-100} | {updatedItem.IsComponentCreated,-100} |");
            table.AppendLine($"| {"IsReleaseCreated",-45} | {initialItem.IsReleaseCreated,-100} | {updatedItem.IsReleaseCreated,-100} |");
            table.AppendLine($"| {"FossologyUploadStatus",-45} | {initialItem.FossologyUploadStatus,-100} | {updatedItem.FossologyUploadStatus,-100} |");
            table.AppendLine($"| {"ReleaseAttachmentLink",-45} | {initialItem.ReleaseAttachmentLink,-100} | {updatedItem.ReleaseAttachmentLink,-100} |");
            table.AppendLine($"| {"ReleaseLink",-45} | {initialItem.ReleaseLink,-100} | {updatedItem.ReleaseLink,-100} |");
            table.AppendLine($"| {"FossologyLink",-45} | {initialItem.FossologyLink,-100} | {updatedItem.FossologyLink,-100} |");
            table.AppendLine($"| {"ReleaseID",-45} | {initialItem.ReleaseID,-100} | {updatedItem.ReleaseID,-100} |");
            table.AppendLine($"| {"AlpineSource",-45} | {initialItem.AlpineSource,-100} | {updatedItem.AlpineSource,-100} |");
            table.AppendLine($"| {"ParentReleaseName",-45} | {initialItem.ParentReleaseName,-100} | {updatedItem.ParentReleaseName,-100} |");
            table.AppendLine($"| {"FossologyUploadId",-45} | {initialItem.FossologyUploadId,-100} | {updatedItem.FossologyUploadId,-100} |");
            table.AppendLine($"| {"ClearingState",-45} | {initialItem.ClearingState,-100} | {updatedItem.ClearingState,-100} |");

            // Footer
            table.AppendLine($"{LogHeaderSeparator}");

            // Log the table to the log file
            Logger.Debug(table.ToString());
        }
        public static void SW360AvailableComponentsList(List<Components> components)
        {
            if (components == null || components.Count == 0)
            {
                // Log a message indicating no components were found
                Logger.Debug($"No components were found in the list");
                return;
            }
            // Build the table
            var logBuilder = new System.Text.StringBuilder();
            logBuilder.AppendLine($"{LogSeparator}");
            logBuilder.AppendLine($" Available SW360 releases data");
            logBuilder.AppendLine($"{LogSeparator}");
            logBuilder.AppendLine($"| {"Name",-40} | {"Version",-20} | {"ReleaseLink",-100} | {"ReleaseExternalId",-100} | {"ComponentExternalId",-100} |");
            logBuilder.AppendLine($"{LogHeaderSeparator}");

            foreach (var component in components)
            {
                logBuilder.AppendLine($"| {component.Name,-40} | {component.Version,-20} | {component.ReleaseLink,-100} |{component.ReleaseExternalId,-100} |{component.ComponentExternalId,-100} |");
            }

            logBuilder.AppendLine($"{LogSeparator}");

            // Log the table
            Logger.Debug(logBuilder.ToString());
        }
        public static void SW360AvailableComponentsData(List<ComparisonBomData> components)
        {
            if (components == null || components.Count == 0)
            {
                // Log a message indicating no components were found
                Logger.Debug($"No components were found in the list");
                return;
            }

            // Build the table
            var logBuilder = new System.Text.StringBuilder();
            logBuilder.AppendLine($"{LogSeparator}");
            logBuilder.AppendLine($" Identified component releases data");
            logBuilder.AppendLine($"{LogSeparator}");

            // Create the table header
            logBuilder.AppendLine($"| {"Name",-20} | {"Group",-15} | {"Version",-10} | {"ComponentExternalId",-50} | {"ReleaseExternalId",-70} | {"SourceUrl",-100} | {"DownloadUrl",-100} | {"ComponentStatus",-20} | {"ReleaseStatus",-20} | {"ApprovedStatus",-25} | {"IsComponentCreated",-20} | {"IsReleaseCreated",-20} | {"FossologyUploadStatus",-25} | {"ReleaseLink",-150} | {"ReleaseID",-15} | {"AlpineSource",-20} | {"PatchUrls",-100} |");
            logBuilder.AppendLine(new string('-', 400));

            // Add rows for each component
            foreach (var component in components)
            {
                logBuilder.AppendLine($"| {component.Name,-20} | {component.Group,-15} | {component.Version,-10} | {component.ComponentExternalId,-50} | {component.ReleaseExternalId,-70} | {component.SourceUrl,-100} | {component.DownloadUrl,-100} | {component.ComponentStatus,-20} | {component.ReleaseStatus,-20} | {component.ApprovedStatus,-25} | {component.IsComponentCreated,-20} | {component.IsReleaseCreated,-20} | {component.FossologyUploadStatus,-25} | {component.ReleaseLink,-150} | {component.ReleaseID,-15} | {component.AlpineSource,-20} | {string.Join(", ", component.PatchURls ?? []),-50} |");
            }

            logBuilder.AppendLine($"{LogSeparator}");

            // Log the table
            Logger.Debug(logBuilder.ToString());
        }
        public static void IdentifierComponentsData(List<Component> allComponents, List<Component> internalComponents)
        {
            // Build the table as a single string
            var tableBuilder = new System.Text.StringBuilder();

            tableBuilder.AppendLine($"\n{LogSeparator}");
            tableBuilder.AppendLine(" Consolidated Component Table");
            tableBuilder.AppendLine($"{LogSeparator}");
            tableBuilder.AppendLine($"| {"Component Name",-30} | {"Version",-15} | {"Repo Name",-35} | {"Internal Repo",-15} |");
            tableBuilder.AppendLine($"{LogHeaderSeparator}");

            foreach (var component in allComponents)
            {
                // Fetch the repository name from the component properties
                string repoName = component.Properties?.FirstOrDefault(p => p.Name == Dataconstant.Cdx_ArtifactoryRepoName)?.Value ?? "-";

                // Determine if the component is in the internal repository
                string isInternalRepo = internalComponents.Contains(component) ? "Yes" : "No";

                // Add the row to the table
                tableBuilder.AppendLine($"| {component.Name,-30} | {component.Version,-15} | {repoName,-35} | {isInternalRepo,-15} |");
            }

            tableBuilder.AppendLine($"{LogSeparator}");

            // Log the entire table as a single log entry
            Logger.Debug(tableBuilder.ToString());
        }
        public static void IdentifierInputFileComponents(string filePath, List<Component> components)
        {
            if (components == null || components.Count == 0)
            {
                // Log a message indicating no components were found
                Logger.Debug($"No components were found in the file: {filePath}");
                return;
            }
            // Build the table
            var logBuilder = new System.Text.StringBuilder();
            logBuilder.AppendLine("\n" + $"{LogSeparator}");
            logBuilder.AppendLine($" COMPONENTS FOUND IN FILE: {filePath}");
            logBuilder.AppendLine($"{LogSeparator}");
            logBuilder.AppendLine($"| {"Name",-40} | {"Version",-40} | {"PURL",-100} | {"DevDependent",-15} |");
            logBuilder.AppendLine($"{LogHeaderSeparator}");

            foreach (var component in components)
            {
                string devDependent = component.Properties?.FirstOrDefault(p => p.Name == Dataconstant.Cdx_IsDevelopment)?.Value ?? "false";
                logBuilder.AppendLine($"| {component.Name,-40} | {component.Version,-40} | {component.Purl,-100} | {devDependent,-15} |");
            }

            logBuilder.AppendLine($"{LogSeparator}");

            // Log the table
            Logger.Debug("\n" + logBuilder.ToString());
        }
        public static void ComponentsList(string filePath, List<Component> components)
        {
            if (components == null || components.Count == 0)
            {
                // Log a message indicating no components were found
                Logger.Debug($"No components were found in the file: {filePath}");
                return;
            }
            // Build the table
            var logBuilder = new System.Text.StringBuilder();
            logBuilder.AppendLine($"{LogSeparator}");
            logBuilder.AppendLine($" Components Foung In File: {filePath}");
            logBuilder.AppendLine($"{LogSeparator}");
            logBuilder.AppendLine($"| {"Name",-40} | {"Version",-20} | {"PURL",-60} |");
            logBuilder.AppendLine($"{LogHeaderSeparator}");

            foreach (var component in components)
            {
                logBuilder.AppendLine($"| {component.Name,-40} | {component.Version,-20} | {component.Purl,-60} |");
            }

            logBuilder.AppendLine($"{LogSeparator}");

            // Log the table
            Logger.Debug(logBuilder.ToString());
        }
        public static DateTime GetISTTime()
        {
            TimeZoneInfo istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istTimeZone);
        }
        private static string MaskSensitiveData(string content)
        {
            // Mask API keys
            content = MyRegex().Replace(content, @"""apiKey"":""*****""");

            // Add more patterns to mask other sensitive data if needed
            return content;
        }

        [System.Text.RegularExpressions.GeneratedRegex(@"""apiKey"":""[^""]+""")]
        private static partial System.Text.RegularExpressions.Regex MyRegex();
    }
}