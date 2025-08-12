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
using Level = log4net.Core.Level;

namespace LCT.APICommunications
{
    /// <summary>
    /// AttachmentHelper class
    /// </summary>
    public class AttachmentHelper
    {
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly string fullPathOfAttachmentJSON = $"{Path.GetTempPath()}/ClearingTool/DownloadedFiles/Attachment.json";
        private readonly string sw360AuthToken;
        private readonly string sw360AuthTokenType;
        private readonly string sw360ReleaseApi;
        private readonly object attachmentsJSONFileLock = new object();

        private const string fileMimeType = "application /x-compressed";
        private const string fileFormKey = "file";

        public AttachmentHelper(string sw360TokenType, string sw360Token, string releaseApi)
        {
            sw360AuthToken = sw360Token;
            sw360AuthTokenType = sw360TokenType;
            sw360ReleaseApi = releaseApi;
        }

        /// <summary>
        /// Attaches the component Source to Sw360
        /// </summary>
        /// <param name="releaseId">releaseId</param>
        /// <param name="attachmentType">attachmentType</param>
        /// <param name="attachmentFile">attachmentFile</param>
        /// <returns>attached api url</returns>
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
                    using WebResponse response = request.GetResponse();
                    HttpWebResponse httpResponse = (HttpWebResponse)response;
                    HandleAcceptedStatus(httpResponse, comparisonBomData);
                    using StreamReader reader = new StreamReader(response.GetResponseStream());
                    reader.ReadToEnd();                    
                }
            }
            catch (UriFormatException ex)
            {
                Logger.Error($"AttachComponentSourceToSW360:", ex);
            }
            catch (SecurityException ex)
            {
                Logger.Error($"AttachComponentSourceToSW360:", ex);
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
                        Logger.Debug($"Web exception: {text}", webex);
                        Logger.Warn($"Web exception: {text}", webex);
                    }
                }
            }
            catch (IOException ex)
            {
                Logger.Error($"AttachComponentSourceToSW360:Failed attach source for release = {attachReport.ReleaseId}");
                Logger.Debug($"AttachComponentSourceToSW360:", ex);
            }
            return releaseAttachementApi;
        }


        /// <summary>
        /// WriteAttachmentsJSONFile
        /// </summary>
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

        private static string CreateFormDataBoundary()
        {
            return "---------------------------" + DateTime.Now.Ticks.ToString("x");
        }
        private static void HandleAcceptedStatus(HttpWebResponse httpResponse, ComparisonBomData component)
        {
            if (httpResponse.StatusCode == HttpStatusCode.Accepted)
            {
                Logger.Logger.Log(null, Level.Warn, $"Moderation request is created while uploading source code in SW360. Please request {component.ReleaseCreatedBy} or the license clearing team to approve the moderation request.", null);
            }
            else
            {
                Logger.Debug($"HTTP Status Code: {httpResponse.StatusCode}");
            }
        }
    }
}


