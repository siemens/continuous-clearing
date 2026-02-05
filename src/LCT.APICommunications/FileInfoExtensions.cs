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
        #region Fields

        /// <summary>
        /// The template string for multipart form data headers.
        /// </summary>
        public const string HeaderTemplate = "--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"\r\nContent-Type: {3}\r\n\r\n";

        #endregion Fields

        #region Methods

        /// <summary>
        /// Writes the file content as multipart form data to the specified stream.
        /// </summary>
        /// <param name="file">The file to write to the stream.</param>
        /// <param name="stream">The destination stream to write the multipart form data to.</param>
        /// <param name="mimeBoundary">The MIME boundary string used to separate form data parts.</param>
        /// <param name="mimeType">The MIME type of the file content.</param>
        /// <param name="formKey">The form field key name for the file.</param>
        /// <exception cref="ArgumentNullException">Thrown when file or stream is null.</exception>
        /// <exception cref="ArgumentException">Thrown when mimeBoundary, mimeType, or formKey is empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
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

        /// <summary>
        /// Checks if the specified file exists and throws an exception if it does not.
        /// </summary>
        /// <param name="file">The file to check for existence.</param>
        /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
        private static void CheckIfFileExists(FileInfo file)
        {
            if (!file.Exists)
            {
                throw new FileNotFoundException("Unable to find file to write to stream.", file.FullName);
            }
        }

        #endregion Methods
    }
}
