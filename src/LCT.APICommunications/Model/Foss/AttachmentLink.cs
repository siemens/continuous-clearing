// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;
using System.Collections.Generic;

namespace LCT.APICommunications.Model.Foss
{
  /// <summary>
  /// AttachmentLink class
  /// </summary>
  public class AttachmentLink
  {
    [JsonProperty("filename")]
    public string Filename { get; set; }

    [JsonProperty("sha1")]
    public string Sha1 { get; set; }

    [JsonProperty("attachmentType")]
    public string AttachmentType { get; set; }

    [JsonProperty("createdBy")]
    public string CreatedBy { get; set; }

    [JsonProperty("createdTeam")]
    public string CreatedTeam { get; set; }

    [JsonProperty("createdComment")]
    public string CreatedComment { get; set; }

    [JsonProperty("createdOn")]
    public string CreatedOn { get; set; }

    [JsonProperty("checkedComment")]
    public string CheckedComment { get; set; }

    [JsonProperty("checkStatus")]
    public string CheckStatus { get; set; }

    [JsonProperty("_links")]
    public SW360DownloadLinks Links { get; set; }

    [JsonProperty("self")]
    public Self Self { get; set; }

    [JsonProperty("curies")]
    public IList<Cury> Curies { get; set; }
  }
}
