// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model;
using LCT.Common;
using LCT.Common.Model;
using log4net;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security;
using Directory = System.IO.Directory;
using Level = log4net.Core.Level;

namespace LCT.APICommunications
{
    /// <summary>
    /// AttachmentHelper class
    /// </summary>
    public class AttachmentHelper
    {
        #region Fields

        /// <summary>
        /// The logger instance for logging messages and errors.
        /// </summary>
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// The full path to the attachment JSON file.
        /// </summary>
        private readonly string fullPathOfAttachmentJSON = $"{Path.GetTempPath()}/ClearingTool/DownloadedFiles/Attachment.json";

        /// <summary>
        /// The SW360 authentication token.
        /// </summary>
        private readonly string sw360AuthToken;

        /// <summary>
        /// The SW360 authentication token type.
        /// </summary>
        private readonly string sw360AuthTokenType;

        /// <summary>
        /// The SW360 release API endpoint URL.
        /// </summary>
        private readonly string sw360ReleaseApi;

        /// <summary>
        /// Lock object for thread-safe access to the attachments JSON file.
        /// </summary>
        private readonly object attachmentsJSONFileLock = new object();

        /// <summary>
        /// The MIME type for compressed files.
        /// </summary>
        private const string fileMimeType = "application /x-compressed";

        /// <summary>
        /// The form key used for file uploads.
        /// </summary>
        private const string fileFormKey = "file";

        /// <summary>
        /// Error message for attachment failures.
        /// </summary>
        private const string AttachmentErrorMessage = "Not able to attachment";

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AttachmentHelper"/> class.
        /// </summary>
        /// <param name="sw360TokenType">The SW360 authentication token type.</param>
        /// <param name="sw360Token">The SW360 authentication token.</param>
        /// <param name="releaseApi">The SW360 release API endpoint URL.</param>
        public AttachmentHelper(string sw360TokenType, string sw360Token, string releaseApi)
        {
            sw360AuthToken = sw360Token;
            sw360AuthTokenType = sw360TokenType;
            sw360ReleaseApi = releaseApi;
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Attaches the component source to SW360 by uploading the attachment file.
        /// </summary>
        /// <param name="attachReport">The attachment report containing release and file information.</param>
        /// <param name="comparisonBomData">The comparison BOM data for the component.</param>
        /// <returns>The release attachment API URL.</returns>
        public string AttachComponentSourceToSW360(AttachReport attachReport, ComparisonBomData comparisonBomData)
        {
            Uri url = new Uri($"{sw360ReleaseApi}/{attachReport.ReleaseId}/{ApiConstant.Attachments}");
            string releaseAttachementApi = url.AbsoluteUri;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url.AbsoluteUri);
                request.Method = ApiConstant.POST;
                request.KeepAlive = true;
                request.Headers.Add(ApiConstant.Authorization, $"{sw360AuthTokenType} {sw360AuthToken}");

                string boundary = CreateFormDataBoundary();
                request.ContentType = "multipart/form-data; boundary=" + boundary;

                Stream requestStream = request.GetRequestStream();

                lock (attachmentsJSONFileLock)
                {
                    FileInfo fileToUpload = new FileInfo(attachReport.AttachmentFile);
                    string localPath = $"{Path.GetTempPath()}/ClearingTool/DownloadedFiles";

                    WriteAttachmentsJSONFile(ApiConstant.AttachmentJsonFileName, localPath, attachReport);
                    FileInfo attachmentToUpload = new FileInfo(fullPathOfAttachmentJSON);

                    if (attachmentToUpload.Exists)
                    {
                        attachmentToUpload.WriteMultipartFormData(requestStream, boundary, ApiConstant.ApplicationJson, "attachment");
                    }

                    if (fileToUpload.Exists)
                    {
                        fileToUpload.WriteMultipartFormData(requestStream, boundary, fileMimeType, fileFormKey);
                    }

                    byte[] endBytes = System.Text.Encoding.UTF8.GetBytes($"--{boundary}--");
                    requestStream.Write(endBytes, 0, endBytes.Length);
                    requestStream.Close();
                    LogHandlingHelper.LogHttpWebRequest("Attach Component Source", $"Uploading component source for ReleaseId: {attachReport.ReleaseId}", request);
                    using WebResponse response = request.GetResponse();
                    HttpWebResponse httpResponse = (HttpWebResponse)response;
                    HandleAcceptedStatus(httpResponse, comparisonBomData);
                    using StreamReader reader = new StreamReader(response.GetResponseStream());
                    reader.ReadToEnd();
                    LogHandlingHelper.LogHttpWebResponse("Attach Component Source", $"Response for attaching component source for ReleaseId: {attachReport.ReleaseId}", httpResponse);
                }
            }
            catch (UriFormatException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling(AttachmentErrorMessage, $"AttachComponentSourceToSW360():Failed to attach component source for ReleaseId: {attachReport.ReleaseId}", ex, $"Invalid URI format: {url.AbsoluteUri}");
                Logger.Error($"   └── AttachComponentSourceToSW360:", ex);
            }
            catch (SecurityException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling(AttachmentErrorMessage, $"AttachComponentSourceToSW360():Failed to attach component source for ReleaseId: {attachReport.ReleaseId}", ex, "A security exception occurred. Ensure the application has the required permissions.");
                Logger.Error($"   └── AttachComponentSourceToSW360:", ex);
            }
            catch (WebException webex)
            {
                WebResponse errResp = webex.Response;
                using (Stream respStream = errResp?.GetResponseStream())
                {
                    if (respStream != null)
                    {
                        StreamReader reader = new StreamReader(respStream);
                        string text = reader.ReadToEnd();
                        LogHandlingHelper.ExceptionErrorHandling(AttachmentErrorMessage, $"Failed to attach component source for ReleaseId: {attachReport.ReleaseId}", webex, $"Web exception occurred. Details: {text}");
                        Logger.Warn($"   └── Web exception: {text}", webex);
                    }
                }
            }
            catch (IOException ex)
            {
                Logger.ErrorFormat("   └── AttachComponentSourceToSW360:Failed attach source for release = {0}", attachReport.ReleaseId);
                LogHandlingHelper.ExceptionErrorHandling(AttachmentErrorMessage, $"AttachComponentSourceToSW360():Failed to attach component source for ReleaseId: {attachReport.ReleaseId}", ex, "An I/O error occurred while processing the attachment.");
            }
            return releaseAttachementApi;
        }


        /// <summary>
        /// Writes the attachments JSON file to the specified folder path.
        /// </summary>
        /// <param name="fileName">The name of the attachment file.</param>
        /// <param name="folderPath">The folder path where the JSON file will be written.</param>
        /// <param name="attachReport">The attachment report containing attachment details.</param>
        public static void WriteAttachmentsJSONFile(string fileName, string folderPath, AttachReport attachReport)
        {
            AttachmentJson attachment = new AttachmentJson
            {
                Filename = fileName,
                AttachmentContentId = Guid.NewGuid().ToString(),
                AttachmentType = string.IsNullOrEmpty(attachReport.AttachmentType) ? ApiConstant.SOURCE : attachReport.AttachmentType,
                CreatedComment = attachReport.AttachmentReleaseComment
            };

            if (!string.IsNullOrEmpty(attachReport.AttachmentCheckStatus))
            {
                attachment.CheckStatus = attachReport.AttachmentCheckStatus;
            }

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string jsonString = JsonConvert.SerializeObject(attachment, Formatting.Indented);
            using var file = new StreamWriter($"{folderPath}/{ApiConstant.AttachmentJsonFileName}");
            file.Write(jsonString);
            file.Flush();
            file.Close();
        }

        /// <summary>
        /// Creates a unique form data boundary string for multipart requests.
        /// </summary>
        /// <returns>A unique boundary string based on the current timestamp.</returns>
        private static string CreateFormDataBoundary()
        {
            return "---------------------------" + DateTime.Now.Ticks.ToString("x");
        }

        /// <summary>
        /// Handles the HTTP accepted status response and logs appropriate messages.
        /// </summary>
        /// <param name="httpResponse">The HTTP web response to check.</param>
        /// <param name="component">The comparison BOM data containing component information.</param>
        private static void HandleAcceptedStatus(HttpWebResponse httpResponse, ComparisonBomData component)
        {
            if (httpResponse.StatusCode == HttpStatusCode.Accepted)
            {
                Logger.Logger.Log(null, Level.Warn, $"   └── Moderation request is created while uploading source code in SW360. Please request {component.ReleaseCreatedBy} or the license clearing team to approve the moderation request.", null);
            }
            else
            {
                Logger.DebugFormat("HTTP Status Code: {0}", httpResponse.StatusCode);
            }
        }

        #endregion Methods
    }
}