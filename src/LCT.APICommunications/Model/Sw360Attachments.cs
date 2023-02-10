// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model
{
  /// <summary>
  /// Sw360Attachments model
  /// </summary>
  public class Sw360Attachments
  {
    [JsonProperty("filename")]
    public string Filename { get; set; }

    [JsonProperty("sha1")]
    public string Sha1 { get; set; }

    [JsonProperty("attachmentType")]
    public string AttachmentType { get; set; }

    [JsonProperty("_links")]
    public AttachmentLinks Links { get; set; }
  }
}