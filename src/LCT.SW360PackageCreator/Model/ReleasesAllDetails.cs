// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace LCT.SW360PackageCreator.Model
{
    /// <summary>
    /// Represents all release details from SW360 including embedded data and pagination.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ReleasesAllDetails
    {
        #region Properties
        /// <summary>
        /// Gets or sets the embedded release data.
        /// </summary>
        [JsonProperty("_embedded")]
        public AllReleasesEmbedded Embedded { get; set; }

        /// <summary>
        /// Gets or sets the pagination information.
        /// </summary>
        [JsonProperty("page")]
        public Pagination Page { get; set; }
        #endregion

        /// <summary>
        /// Represents embedded data containing releases and attachments.
        /// </summary>
        public class AllReleasesEmbedded
        {
            #region Properties
            /// <summary>
            /// Gets or sets the list of SW360 releases.
            /// </summary>
            [JsonProperty("sw360:releases")]
            public List<Sw360Release> Sw360releases { get; set; }

            /// <summary>
            /// Gets or sets the list of SW360 attachments for each release.
            /// </summary>
            [JsonProperty("sw360:attachments")]
            public List<List<Attachment>> Sw360attachments { get; set; }
            #endregion
        }

        /// <summary>
        /// Represents an attachment with filename and type.
        /// </summary>
        public class Attachment
        {
            #region Properties
            /// <summary>
            /// Gets or sets the filename of the attachment.
            /// </summary>
            [JsonProperty("filename")]
            public string Filename { get; set; }

            /// <summary>
            /// Gets or sets the type of the attachment.
            /// </summary>
            [JsonProperty("attachmentType")]
            public string AttachmentType { get; set; }
            #endregion
        }

        /// <summary>
        /// Represents links associated with a resource.
        /// </summary>
        public class Links
        {
            #region Properties
            /// <summary>
            /// Gets or sets the self link.
            /// </summary>
            [JsonProperty("self")]
            public Self Self { get; set; }
            #endregion
        }

        /// <summary>
        /// Represents a self-referencing link.
        /// </summary>
        public class Self
        {
            #region Properties
            /// <summary>
            /// Gets or sets the href URL.
            /// </summary>
            [JsonProperty("href")]
            public string Href { get; set; }
            #endregion
        }

        /// <summary>
        /// Represents a SW360 release with name, version, clearing state, and embedded data.
        /// </summary>
        public class Sw360Release
        {
            #region Properties
            /// <summary>
            /// Gets or sets the name of the release.
            /// </summary>
            [JsonProperty("name")]
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets the version of the release.
            /// </summary>
            [JsonProperty("version")]
            public string Version { get; set; }

            /// <summary>
            /// Gets or sets the clearing state of the release.
            /// </summary>
            [JsonProperty("clearingState")]
            public string ClearingState { get; set; }

            /// <summary>
            /// Gets or sets the links associated with the release.
            /// </summary>
            [JsonProperty("_links")]
            public Links Links { get; set; }

            /// <summary>
            /// Gets or sets the embedded release data.
            /// </summary>
            [JsonProperty("_embedded")]
            public AllReleasesEmbedded AllReleasesEmbedded { get; set; }
            #endregion
        }

        /// <summary>
        /// Represents pagination information.
        /// </summary>
        public class Pagination
        {
            #region Properties
            /// <summary>
            /// Gets or sets the total number of pages.
            /// </summary>
            [JsonProperty("totalPages")]
            public int TotalPages { get; set; }
            #endregion
        }
    }
}
