// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model.Foss
{
  /// <summary>
  /// Multiple upload component model
  /// </summary>
  public class MultipleUploadComponentModel
  {
    [JsonProperty("folderid")]
    public string Folderid { get; set; }

    [JsonProperty("foldername")]
    public string Foldername { get; set; }

    [JsonProperty("id")]
    public string UploadId { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("uploadname")]
    public string Uploadname { get; set; }

    [JsonProperty("uploaddate")]
    public string Uploaddate { get; set; }

    [JsonProperty("filesize")]
    public string Filesize { get; set; }
  }
}
