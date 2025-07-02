// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System;
using System.IO;
using System.Text;

namespace LCT.APICommunications
{
    /// <summary>
    /// File info extensions class
    /// </summary>
    public static class FileInfoExtensions
    {
        public const string HeaderTemplate = "--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"\r\nContent-Type: {3}\r\n\r\n";


        public static void WriteMultipartFormData(
                                            this FileInfo file,
                                            Stream stream,
                                            string mimeBoundary,
                                            string mimeType,
                                            string formKey)
        {
            ArgumentNullException.ThrowIfNull(file);

            CheckIfFileExists(file);

            ArgumentNullException.ThrowIfNull(stream);

            if (string.IsNullOrEmpty(mimeBoundary))
            {
                throw new ArgumentException("MIME boundary may not be empty.", nameof(mimeBoundary));
            }

            if (string.IsNullOrWhiteSpace(mimeType))
            {
                throw new ArgumentException("MIME type may not be empty.", nameof(mimeType));
            }

            if (string.IsNullOrWhiteSpace(formKey))
            {
                throw new ArgumentException("Form key may not be empty.", nameof(formKey));
            }

            string header = string.Format(HeaderTemplate, mimeBoundary, formKey, file.Name, mimeType);
            byte[] headerbytes = Encoding.UTF8.GetBytes(header);
            stream.Write(headerbytes, 0, headerbytes.Length);
            using (FileStream fileStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[1024];
                int bytesRead = 0;
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    stream.Write(buffer, 0, bytesRead);
                }
                fileStream.Close();
            }
            byte[] newlineBytes = Encoding.UTF8.GetBytes("\r\n");
            stream.Write(newlineBytes, 0, newlineBytes.Length);
        }

        private static void CheckIfFileExists(FileInfo file)
        {
            if (!file.Exists)
            {
                throw new FileNotFoundException("Unable to find file to write to stream.", file.FullName);
            }
        }
    }
}
