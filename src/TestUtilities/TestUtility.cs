// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications;
using LCT.APICommunications.Model;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace TestUtilities
{
    [ExcludeFromCodeCoverage]
    public static class TestUtility
    {
        /// <summary>
        /// Updating the additonal data section in sw360
        /// </summary>
        public static async Task AdditonalDataUpdator(string name, string version, Dictionary<string, string> additionalDataValue = null)
        {
            string href = null;
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(TestConstant.TestSw360TokenType, TestConstant.TestSw360TokenValue);

            string responseBody = await httpClient.GetStringAsync($"{TestConstant.Sw360ReleaseApi}?name={name}");
            var responseData = JsonConvert.DeserializeObject<ComponentsRelease>(responseBody);
            for (int index = 0; index < responseData?.Embedded?.Sw360Releases?.Count; index++)
            {
                if (responseData.Embedded.Sw360Releases[index].Name.ToUpperInvariant() == name.ToUpperInvariant()
                    && responseData.Embedded.Sw360Releases[index].Version.ToUpperInvariant() == version.ToLowerInvariant())
                {
                    href = responseData.Embedded.Sw360Releases[index].Links.Self.Href;
                    break;
                }
            }
            string releaseid = href?.Replace($"{TestConstant.Sw360ReleaseApi}/", "");
            string response = await httpClient.GetStringAsync($"{TestConstant.Sw360ReleaseApi}/{releaseid}");
            var responsestring = JsonConvert.DeserializeObject<ReleasesInfo>(response);


            if ( additionalDataValue.Count != 0)
            {
                responsestring.AdditionalData = additionalDataValue;
            }

            var content = new StringContent(
                JsonConvert.SerializeObject(responsestring),
                Encoding.UTF8,
                ApiConstant.ApplicationJson);

            string releaseApi = $"{TestConstant.Sw360ReleaseApi}/{releaseid}";
            await httpClient.PatchAsync(releaseApi, content);

        }
    }
}
