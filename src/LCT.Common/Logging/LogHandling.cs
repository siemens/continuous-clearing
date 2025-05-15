using CycloneDX.Models;
using LCT.Common.Constants;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using LCT.Common.Model;
using System.Text;


namespace LCT.Common.Logging
{
    public static class LogHandling
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LogHandling));
        public static void HttpErrorHandelingForLog(string context, string details, Exception ex, string additionalDetails = null)
        {
            // Build the log message in table format
            var logBuilder = new System.Text.StringBuilder();
            logBuilder.AppendLine("============================================================================================================================================");
            logBuilder.AppendLine(" ERROR DETECTED");
            logBuilder.AppendLine("============================================================================================================================================");
            logBuilder.AppendLine($"| {"Field",-20} | {"Value",-100} |");
            logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");
            logBuilder.AppendLine($"| {"Time",-20} | {GetISTTime(),-100} |");
            logBuilder.AppendLine($"| {"Details",-20} | {details,-100} |");
            logBuilder.AppendLine($"| {"Context",-20} | {context,-100} |");

            // Add additional details if available
            if (!string.IsNullOrEmpty(additionalDetails))
            {
                logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");
                logBuilder.AppendLine(" ADDITIONAL DETAILS");
                logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");
                logBuilder.AppendLine($"| {"Details",-20} | {additionalDetails,-100} |");
            }

            logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");
            logBuilder.AppendLine(" ERROR DETAILS");
            logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");
            logBuilder.AppendLine($"| {"Exception Type",-20} | {ex.GetType().Name,-100} |");
            logBuilder.AppendLine($"| {"Message",-20} | {ex.Message,-100} |");
            logBuilder.AppendLine($"| {"Stack Trace",-20} | {ex.StackTrace,-100} |");

            // Add inner exception details if available
            if (ex.InnerException != null)
            {
                logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");
                logBuilder.AppendLine(" INNER EXCEPTION DETAILS");
                logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");
                logBuilder.AppendLine($"| {"Exception Type",-20} | {ex.InnerException.GetType().Name,-100} |");
                logBuilder.AppendLine($"| {"Message",-20} | {ex.InnerException.Message,-100} |");
                logBuilder.AppendLine($"| {"Stack Trace",-20} | {ex.InnerException.StackTrace,-100} |");
            }

            logBuilder.AppendLine("============================================================================================================================================");

            // Log the constructed message
            Logger.Debug("\n" + logBuilder.ToString());
        }
        public static void BasicErrorHandelingForLog(string context, string details, string message, string additional)
        {
            // Build the log message in table format
            var logBuilder = new System.Text.StringBuilder();
            logBuilder.AppendLine("============================================================================================================================================");
            logBuilder.AppendLine(" ERROR DETECTED");
            logBuilder.AppendLine("============================================================================================================================================");
            logBuilder.AppendLine($"| {"Field",-20} | {"Value",-100} |");
            logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");
            logBuilder.AppendLine($"| {"Time",-20} | {GetISTTime(),-100} |");
            logBuilder.AppendLine($"| {"Details",-20} | {details,-100} |");
            logBuilder.AppendLine($"| {"Description",-20} | {context,-100} |");
            logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");
            logBuilder.AppendLine(" ERROR DETAILS");
            logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");
            logBuilder.AppendLine($"| {"Message",-20} | {message,-100} |");

            // Add additional details if available
            if (!string.IsNullOrEmpty(additional))
            {
                logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");
                logBuilder.AppendLine(" ADDITIONAL DETAILS");
                logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");
                logBuilder.AppendLine($"| {"Solution",-20} | {additional,-100} |");
            }

            logBuilder.AppendLine("============================================================================================================================================");

            // Log the constructed message
            Logger.Debug("\n" + logBuilder.ToString());
        }
        public static void HttpResponseErrorHandlingForLog(string context, string details, HttpResponseMessage response, string additionalDetails = null)
        {
            // Read the response content
            string responseContent = response.Content != null ? response.Content.ReadAsStringAsync().Result : string.Empty;

            // Mask sensitive headers
            var headers = response.Headers.Select(h =>
            {
                string headerValue = string.Join(", ", h.Value);
                if (h.Key.ToLower().Contains("authorization") || h.Key.ToLower().Contains("token") || headerValue.ToLower().Contains("bearer"))
                {
                    headerValue = "*****"; // Mask sensitive values
                }
                return new { Key = h.Key, Value = headerValue };
            }).ToList();

            // Build the log message in table format
            var logBuilder = new System.Text.StringBuilder();
            logBuilder.AppendLine("\n============================================================================================================================================");
            logBuilder.AppendLine(" HTTP API RESPONSE ERROR DETECTED");
            logBuilder.AppendLine("============================================================================================================================================");
            logBuilder.AppendLine($"| {"Field",-20} | {"Value",-100} |");
            logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");
            logBuilder.AppendLine($"| {"Time",-20} | {GetISTTime(),-100} |");
            logBuilder.AppendLine($"| {"Details",-20} | {details,-100} |");
            logBuilder.AppendLine($"| {"Description",-20} | {context,-100} |");
            logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");
            logBuilder.AppendLine(" RESPONSE DETAILS");
            logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");
            logBuilder.AppendLine($"| {"Status Code",-20} | {response.StatusCode,-100} |");
            logBuilder.AppendLine($"| {"Reason Phrase",-20} | {response.ReasonPhrase ?? "No Reason Phrase provided by the server",-100} |");

            // Add headers if available
            logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");
            logBuilder.AppendLine(" HEADERS");
            logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");
            if (headers.Any())
            {
                foreach (var header in headers)
                {
                    logBuilder.AppendLine($"| {header.Key,-20} | {header.Value,-100} |");
                }
            }
            else
            {
                logBuilder.AppendLine($"| {"No Headers",-20} | {"-",100} |");
            }

            // Add content if available
            logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");
            logBuilder.AppendLine(" CONTENT");
            logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");
            if (!string.IsNullOrEmpty(responseContent))
            {
                logBuilder.AppendLine($"| {"Body",-20} | {responseContent,-100} |");
            }
            else
            {
                logBuilder.AppendLine($"| {"No Content",-20} | {"-",100} |");
            }

            // Add additional details if available
            if (!string.IsNullOrEmpty(additionalDetails))
            {
                logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");
                logBuilder.AppendLine(" ADDITIONAL DETAILS");
                logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");
                logBuilder.AppendLine($"| {"Details",-20} | {additionalDetails,-100} |");
            }

            logBuilder.AppendLine("============================================================================================================================================");

            // Log the constructed message
            Logger.Debug(logBuilder.ToString());
        }
        public static void LogRequestDetails(string context, string details, HttpClient httpClient, string url, HttpContent httpContent = null)
        {
            // Mask sensitive headers
            var headers = httpClient.DefaultRequestHeaders.Select(h =>
            {
                string headerValue = string.Join(", ", h.Value);
                if (h.Key.ToLower().Contains("authorization") || h.Key.ToLower().Contains("token") || headerValue.ToLower().Contains("bearer"))
                {
                    headerValue = "*****"; // Mask sensitive values
                }
                return new { Key = h.Key, Value = headerValue };
            }).ToList();

            // Read the content of the HttpContent
            string content = httpContent != null ? httpContent.ReadAsStringAsync().Result : string.Empty;

            // Build the log message
            var logBuilder = new System.Text.StringBuilder();
            logBuilder.AppendLine("\n============================================================================================================================================");
            logBuilder.AppendLine(" HTTP API REQUEST DETAILS");
            logBuilder.AppendLine("============================================================================================================================================");
            logBuilder.AppendLine($"| {"Field",-20} | {"Value",-100} |");
            logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");
            logBuilder.AppendLine($"| {"Time",-20} | {GetISTTime(),-100} |");
            logBuilder.AppendLine($"| {"Details",-20} | {details,-100} |");
            logBuilder.AppendLine($"| {"Description",-20} | {context,-100} |");
            logBuilder.AppendLine($"| {"URL",-20} | {url,-100} |");
            logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");
            logBuilder.AppendLine(" HEADERS");
            logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");
            if (headers.Any())
            {
                foreach (var header in headers)
                {
                    logBuilder.AppendLine($"| {header.Key,-20} | {header.Value,-100} |");
                }
            }
            else
            {
                logBuilder.AppendLine($"| {"No Headers",-20} | {"-",100} |");
            }
            logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");
            logBuilder.AppendLine(" CONTENT");
            logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");
            if (!string.IsNullOrEmpty(content))
            {
                logBuilder.AppendLine($"| {"Body",-20} | {content,-100} |");
            }
            else
            {
                logBuilder.AppendLine($"| {"No Content",-20} | {"-",100} |");
            }
            logBuilder.AppendLine("============================================================================================================================================");

            // Log the constructed message
            Logger.Debug(logBuilder.ToString());
        }      
        public static void LogHttpResponseDetails(string context, string details, HttpResponseMessage response, string additionalDetails = null)
        {
            // Extract request details from the response
            string requestMethod = response?.RequestMessage?.Method.ToString() ?? string.Empty;
            string requestUrl = response?.RequestMessage?.RequestUri?.ToString() ?? string.Empty;
            string requestHeaders = response?.RequestMessage?.Headers != null
                ? string.Join("\n", response.RequestMessage.Headers.Select(h =>
                {
                    string headerValue = string.Join(", ", h.Value);
                    if (h.Key.ToLower().Contains("authorization") || h.Key.ToLower().Contains("token") || headerValue.ToLower().Contains("bearer"))
                    {
                        headerValue = "*****"; // Mask sensitive values
                    }
                    return $"{h.Key}: {headerValue}";
                }))
                : string.Empty;

            // Simplified reasonPhrase logic
            string reasonPhrase = !string.IsNullOrEmpty(response?.ReasonPhrase)
                ? response.ReasonPhrase
                : "No Reason Phrase";

            // Parse response content (full content for all responses)
            string responseContent = response?.Content != null
                ? response.Content.ReadAsStringAsync().Result
                : string.Empty;

            // Mask sensitive information in the response content
            if (!string.IsNullOrEmpty(responseContent))
            {
                responseContent = MaskSensitiveData(responseContent);
            }
            // If verbose is false, limit the response content to the first 1000 lines
            if (!Log4Net.verbose && !string.IsNullOrEmpty(responseContent))
            {
                // Define a configurable limit for the number of lines to display
                int maxLinesToShow = 1000; // Change this value to show more lines

                var lines = responseContent.Split(new[] { '\n' }, StringSplitOptions.None);

                // Check if the content has more lines than the limit
                if (lines.Length > maxLinesToShow)
                {
                    responseContent = string.Join("\n", lines.Take(maxLinesToShow));
                    responseContent += $"\n... [Content truncated. Showing first {maxLinesToShow} lines. Enable verbose mode to see full content.]";
                }
                else
                {
                    // If the content has fewer than or equal to 1000 lines, display it as is
                    responseContent = string.Join("\n", lines);
                }
            }

            // Build the log message
            var logBuilder = new System.Text.StringBuilder();
            logBuilder.AppendLine("\n============================================================================================================================================");
            logBuilder.AppendLine(" HTTP API RESPONSE DETAILS");
            logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");
            logBuilder.AppendLine($"Details: {details}");
            logBuilder.AppendLine($"Description: {context}");
            logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");

            // Add request details if available
            if (!string.IsNullOrEmpty(requestMethod) || !string.IsNullOrEmpty(requestUrl) || !string.IsNullOrEmpty(requestHeaders))
            {
                logBuilder.AppendLine("REQUEST DETAILS");
                logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");
                if (!string.IsNullOrEmpty(requestMethod)) logBuilder.AppendLine($"Method: {requestMethod}");
                if (!string.IsNullOrEmpty(requestUrl)) logBuilder.AppendLine($"URL: {requestUrl}");
                if (!string.IsNullOrEmpty(requestHeaders)) logBuilder.AppendLine($"Headers:\n{requestHeaders}");
                logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");
            }

            // Add response details
            logBuilder.AppendLine("RESPONSE DETAILS");
            logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");
            logBuilder.AppendLine($"Status Code: {response?.StatusCode}");
            logBuilder.AppendLine($"Reason Phrase: {reasonPhrase}");

            // Add full content for all responses
            if (!string.IsNullOrEmpty(responseContent))
            {
                logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");
                logBuilder.AppendLine("CONTENT:");
                logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");
                logBuilder.AppendLine(responseContent);
            }

            // Append additional details if available
            if (!string.IsNullOrEmpty(additionalDetails))
            {
                logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");
                logBuilder.AppendLine("ADDITIONAL DETAILS:");
                logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");
                logBuilder.AppendLine(additionalDetails);
            }

            logBuilder.AppendLine("============================================================================================================================================");

            // Log the constructed message
            Logger.Debug(logBuilder.ToString());
        }
        public static void LogConsolidatedComponentTable(List<Component> allComponents, List<Component> internalComponents)
        {
            // Build the table as a single string
            var tableBuilder = new System.Text.StringBuilder();

            tableBuilder.AppendLine("\n================================================================================================================");
            tableBuilder.AppendLine(" Consolidated Component Table");
            tableBuilder.AppendLine("================================================================================================================");
            tableBuilder.AppendLine($"| {"Component Name",-30} | {"Version",-15} | {"Repo Name",-35} | {"Internal Repo",-15} |");
            tableBuilder.AppendLine("----------------------------------------------------------------------------------------------------------------");

            foreach (var component in allComponents)
            {
                // Fetch the repository name from the component properties
                string repoName = component.Properties?.FirstOrDefault(p => p.Name == Dataconstant.Cdx_ArtifactoryRepoName)?.Value ?? "-";

                // Determine if the component is in the internal repository
                string isInternalRepo = internalComponents.Contains(component) ? "Yes" : "No";

                // Add the row to the table
                tableBuilder.AppendLine($"| {component.Name,-30} | {component.Version,-15} | {repoName,-35} | {isInternalRepo,-15} |");
            }

            tableBuilder.AppendLine("================================================================================================================");

            // Log the entire table as a single log entry
            Logger.Debug(tableBuilder.ToString());
        }

        public static void LogComponentsTable(string filepath, List<Component> components)
        {
            if (components == null || !components.Any())
            {
                // Log a message indicating no components were found
                Logger.Debug($"No components were found in the file: {filepath}");
                return;
            }
            // Build the table
            var logBuilder = new System.Text.StringBuilder();
            logBuilder.AppendLine("=====================================================================================================================================================");
            logBuilder.AppendLine($" COMPONENTS FOUND IN FILE: {filepath}");
            logBuilder.AppendLine("=====================================================================================================================================================");
            logBuilder.AppendLine($"| {"Name",-40} | {"Version",-20} | {"PURL",-60} | {"DevDependent",-15} |");
            logBuilder.AppendLine("-----------------------------------------------------------------------------------------------------------------------------------------------------");

            foreach (var component in components)
            {
                string devDependent = component.Properties?.FirstOrDefault(p => p.Name == Dataconstant.Cdx_IsDevelopment)?.Value ?? "false";
                logBuilder.AppendLine($"| {component.Name,-40} | {component.Version,-20} | {component.Purl,-60} | {devDependent,-15} |");
            }

            logBuilder.AppendLine("=====================================================================================================================================================");

            // Log the table
            Logger.Debug("\n" + logBuilder.ToString());
        }

        public static void LogCyclonedxComponentsTable(string filepath, List<Component> components)
        {
            if (components == null || !components.Any())
            {
                // Log a message indicating no components were found
                Logger.Debug($"No packages were found in the file: {filepath}");
                return;
            }
            // Build the table
            var logBuilder = new System.Text.StringBuilder();
            logBuilder.AppendLine("=====================================================================================================================================================");
            logBuilder.AppendLine($" packages FOUND IN FILE: {filepath}");
            logBuilder.AppendLine("=====================================================================================================================================================");
            logBuilder.AppendLine($"| {"Name",-40} | {"Version",-20} | {"PURL",-60} |");
            logBuilder.AppendLine("-----------------------------------------------------------------------------------------------------------------------------------------------------");

            foreach (var component in components)
            {
                logBuilder.AppendLine($"| {component.Name,-40} | {component.Version,-20} | {component.Purl,-60} |");
            }

            logBuilder.AppendLine("=====================================================================================================================================================");

            // Log the table
            Logger.Debug(logBuilder.ToString());
        }
        public static void LogAvailableComponentList(List<Components> components)
        {
            if (components == null || !components.Any())
            {
                // Log a message indicating no components were found
                Logger.Debug($"No components were found in the list");
                return;
            }
            // Build the table
            var logBuilder = new System.Text.StringBuilder();
            logBuilder.AppendLine("=====================================================================================================================================================");
            logBuilder.AppendLine($" Available SW360 releases data");
            logBuilder.AppendLine("=====================================================================================================================================================");
            logBuilder.AppendLine($"| {"Name",-40} | {"Version",-20} | {"ReleaseLink",-100} | {"ReleaseExternalId",-100} | {"ComponentExternalId",-100} |");
            logBuilder.AppendLine("-----------------------------------------------------------------------------------------------------------------------------------------------------");

            foreach (var component in components)
            {
                logBuilder.AppendLine($"| {component.Name,-40} | {component.Version,-20} | {component.ReleaseLink,-100} |{component.ReleaseExternalId,-100} |{component.ComponentExternalId,-100} |");
            }

            logBuilder.AppendLine("=====================================================================================================================================================");

            // Log the table
            Logger.Debug(logBuilder.ToString());
        }

        public static void LogComparitionComponentListData(List<ComparisonBomData> components)
        {
            if (components == null || !components.Any())
            {
                // Log a message indicating no components were found
                Logger.Debug($"No components were found in the list");
                return;
            }

            // Build the table
            var logBuilder = new System.Text.StringBuilder();
            logBuilder.AppendLine("=====================================================================================================================================================");
            logBuilder.AppendLine($" Identified component releases data");
            logBuilder.AppendLine("=====================================================================================================================================================");

            // Create the table header
            logBuilder.AppendLine($"| {"Name",-20} | {"Group",-15} | {"Version",-10} | {"ComponentExternalId",-50} | {"ReleaseExternalId",-70} | {"SourceUrl",-100} | {"DownloadUrl",-100} | {"ComponentStatus",-20} | {"ReleaseStatus",-20} | {"ApprovedStatus",-25} | {"IsComponentCreated",-20} | {"IsReleaseCreated",-20} | {"FossologyUploadStatus",-25} | {"ReleaseLink",-150} | {"ReleaseID",-15} | {"AlpineSource",-20} | {"PatchUrls",-100} |");
            logBuilder.AppendLine(new string('-', 400));

            // Add rows for each component
            foreach (var component in components)
            {
                logBuilder.AppendLine($"| {component.Name,-20} | {component.Group,-15} | {component.Version,-10} | {component.ComponentExternalId,-50} | {component.ReleaseExternalId,-70} | {component.SourceUrl,-100} | {component.DownloadUrl,-100} | {component.ComponentStatus,-20} | {component.ReleaseStatus,-20} | {component.ApprovedStatus,-25} | {component.IsComponentCreated,-20} | {component.IsReleaseCreated,-20} | {component.FossologyUploadStatus,-25} | {component.ReleaseLink,-150} | {component.ReleaseID,-15} | {component.AlpineSource,-20} | {string.Join(", ", component.PatchURls ?? new string[0]),-50} |");
            }

            logBuilder.AppendLine("=====================================================================================================================================================");

            // Log the table
            Logger.Debug(logBuilder.ToString());
        }
        public static void LogCommandLineArguments(string[] args)
        {
            if (args != null && args.Length > 0)
            {
                Logger.Debug($"Command-line arguments: {string.Join(" ", args)}");
            }
            else
            {
                Logger.Debug("No command-line arguments were provided.");
            }
        }

        public static void LogCreaterCyclonedxComponentsTable(string bomFilePath, List<Component> components)
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
            logBuilder.AppendLine("\n================================================================================================================");
            logBuilder.AppendLine($"Components from BOM File: {bomFilePath}");
            logBuilder.AppendLine("================================================================================================================");

            // Create the table header
            logBuilder.Append($"|{"Component Name",-30} |{"Version",-15} | {"PURL",-50} |");
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

                logBuilder.Append($"|{componentName,-30} | {version,-15} | {purl,-50} |");

                foreach (var actualName in propertyMapping.Keys)
                {
                    var propertyValue = component.Properties?.FirstOrDefault(p => p.Name == actualName)?.Value ?? "N/A";
                    logBuilder.Append($" {propertyValue,-45}|");
                }

                logBuilder.AppendLine();
            }

            logBuilder.AppendLine("================================================================================================================");
            Logger.Debug(logBuilder.ToString());
        }
        public static void LogHttpResponseDetailsForStringContent(string context, string details, string responseBody, string additionalDetails = null)
        {
            if (!string.IsNullOrEmpty(responseBody))
            {
                responseBody = MaskSensitiveData(responseBody);
            }
            // If verbose is false, limit the response content to the first 1000 lines
            if (!Log4Net.verbose && !string.IsNullOrEmpty(responseBody))
            {
                // Define a configurable limit for the number of lines to display
                int maxLinesToShow = 1000; // Change this value to show more lines

                var lines = responseBody.Split(new[] { '\n' }, StringSplitOptions.None);

                // Check if the content has more lines than the limit
                if (lines.Length > maxLinesToShow)
                {
                    responseBody = string.Join("\n", lines.Take(maxLinesToShow));
                    responseBody += $"\n... [Content truncated. Showing first {maxLinesToShow} lines. Enable verbose mode to see full content.]";
                }
                else
                {
                    // If the content has fewer than or equal to 1000 lines, display it as is
                    responseBody = string.Join("\n", lines);
                }
            }
            // Build the log message
            var logBuilder = new System.Text.StringBuilder();
            logBuilder.AppendLine("\n============================================================================================================================================");
            logBuilder.AppendLine(" HTTP API RESPONSE DETAILS");
            logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");
            logBuilder.AppendLine($"Details: {details}");
            logBuilder.AppendLine($"Description: {context}");
            logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");

            // Add response details
            logBuilder.AppendLine("RESPONSE DETAILS");
            logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");

            // Add content if available
            if (!string.IsNullOrEmpty(responseBody))
            {
                logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");
                logBuilder.AppendLine("CONTENT:");
                logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");
                logBuilder.AppendLine(responseBody);
            }

            // Append additional details if available
            if (!string.IsNullOrEmpty(additionalDetails))
            {
                logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");
                logBuilder.AppendLine("ADDITIONAL DETAILS:");
                logBuilder.AppendLine("--------------------------------------------------------------------------------------------------------------------------------------------");
                logBuilder.AppendLine(additionalDetails);
            }

            logBuilder.AppendLine("============================================================================================================================================");

            // Log the constructed message
            Logger.Debug(logBuilder.ToString());
        }
        public static void LogItemDetailsInTableFormat(string methodName, ComparisonBomData initialItem, ComparisonBomData updatedItem)
        {
            var table = new StringBuilder();

            table.AppendLine($"Method: {methodName}");
            table.AppendLine("+---------------------------+---------------------------+---------------------------+");
            table.AppendLine($"| {"Property",-25} | {"Initial Value",-150} | {"Updated Value",-150} |");
            table.AppendLine("+---------------------------+---------------------------+---------------------------+");

            table.AppendLine($"| Name                      | {initialItem.Name,-150} | {updatedItem.Name,-150} |");
            table.AppendLine($"| Group                     | {initialItem.Group,-150} | {updatedItem.Group,-150} |");
            table.AppendLine($"| Version                   | {initialItem.Version,-150} | {updatedItem.Version,-150} |");
            table.AppendLine($"| ComponentExternalId       | {initialItem.ComponentExternalId,-150} | {updatedItem.ComponentExternalId,-150} |");
            table.AppendLine($"| ReleaseExternalId         | {initialItem.ReleaseExternalId,-150} | {updatedItem.ReleaseExternalId,-150} |");
            table.AppendLine($"| PackageUrl                | {initialItem.PackageUrl,-150} | {updatedItem.PackageUrl,-150} |");
            table.AppendLine($"| SourceUrl                 | {initialItem.SourceUrl,-150} | {updatedItem.SourceUrl,-150} |");
            table.AppendLine($"| DownloadUrl               | {initialItem.DownloadUrl,-150} | {updatedItem.DownloadUrl,-25} |");
            table.AppendLine($"| PatchURls                 | {string.Join(",", initialItem.PatchURls ?? Array.Empty<string>()),-150} | {string.Join(",", updatedItem.PatchURls ?? Array.Empty<string>()),-150} |");
            table.AppendLine($"| ComponentStatus           | {initialItem.ComponentStatus,-150} | {updatedItem.ComponentStatus,-150} |");
            table.AppendLine($"| ReleaseStatus             | {initialItem.ReleaseStatus,-150} | {updatedItem.ReleaseStatus,-150} |");
            table.AppendLine($"| ApprovedStatus            | {initialItem.ApprovedStatus,-150} | {updatedItem.ApprovedStatus,-150} |");
            table.AppendLine($"| IsComponentCreated        | {initialItem.IsComponentCreated,-150} | {updatedItem.IsComponentCreated,-150} |");
            table.AppendLine($"| IsReleaseCreated          | {initialItem.IsReleaseCreated,-150} | {updatedItem.IsReleaseCreated,-150} |");
            table.AppendLine($"| FossologyUploadStatus     | {initialItem.FossologyUploadStatus,-150} | {updatedItem.FossologyUploadStatus,-150} |");
            table.AppendLine($"| ReleaseAttachmentLink     | {initialItem.ReleaseAttachmentLink,-150} | {updatedItem.ReleaseAttachmentLink,-150} |");
            table.AppendLine($"| ReleaseLink               | {initialItem.ReleaseLink,-150} | {updatedItem.ReleaseLink,-150} |");
            table.AppendLine($"| FossologyLink             | {initialItem.FossologyLink,-150} | {updatedItem.FossologyLink,-150} |");
            table.AppendLine($"| ReleaseID                 | {initialItem.ReleaseID,-150} | {updatedItem.ReleaseID,-150} |");
            table.AppendLine($"| AlpineSource              | {initialItem.AlpineSource,-150} | {updatedItem.AlpineSource,-150} |");
            table.AppendLine($"| ParentReleaseName         | {initialItem.ParentReleaseName,-150} | {updatedItem.ParentReleaseName,-150} |");
            table.AppendLine($"| FossologyUploadId         | {initialItem.FossologyUploadId,-150} | {updatedItem.FossologyUploadId,-150} |");
            table.AppendLine($"| ClearingState             | {initialItem.ClearingState,-150} | {updatedItem.ClearingState,-150} |");

            table.AppendLine("+---------------------------+---------------------------+---------------------------+");

            // Log the table to the log file
            Logger.Debug(table.ToString());
        }
        public static DateTime GetISTTime()
        {
            TimeZoneInfo istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istTimeZone);
        }
        private static string MaskSensitiveData(string content)
        {
            // Mask API keys
            content = System.Text.RegularExpressions.Regex.Replace(content, @"""apiKey"":""[^""]+""", @"""apiKey"":""*****""");

            // Add more patterns to mask other sensitive data if needed
            return content;
        }

    }
}
