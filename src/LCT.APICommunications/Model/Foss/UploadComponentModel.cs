// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model.Foss
{
  /// <summary>
  /// The UploadComponentModel
  /// </summary>
  public class UploadComponentModel
  {
    [JsonProperty("code")]
    public string Code { get; set; }

    [JsonProperty("message")]
    public string UploadId { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }
  }
}
