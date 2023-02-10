// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model.Foss
{
  /// <summary>
  /// The SW360DownloadLinks model
  /// </summary>
  public class SW360DownloadLinks
  {
    [JsonProperty("sw360:downloadLink")]
    public SW360DownloadHref Sw360DownloadLink { get; set; }
  }
}
