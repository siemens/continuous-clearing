// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model.Foss
{
  /// <summary>
  /// The SW360DownloadHref model
  /// </summary>
  public class SW360DownloadHref
  {

    [JsonProperty("href")]
    public string DownloadUrl { get; set; }
  }
}
