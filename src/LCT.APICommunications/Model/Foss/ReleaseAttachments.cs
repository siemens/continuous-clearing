// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model.Foss
{
  /// <summary>
  /// Release Attachment model
  /// </summary>
  public class ReleaseAttachments
  {
    [JsonProperty("_embedded")]
    public AttachmentEmbedded Embedded { get; set; }
  }
}
