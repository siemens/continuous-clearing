// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model;
using LCT.APICommunications.Model.Foss;
using LCT.Common.Model;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace LCT.Services.Interface
{
    /// <summary>
    /// ISW360Service interface
    /// </summary>
    public interface ISW360Service
    {
        /// <summary>
        /// Asynchronously gets the component release identifier by component name and version.
        /// </summary>
        /// <param name="componentName">The component name.</param>
        /// <param name="version">The component version.</param>
        /// <returns>A task representing the asynchronous operation that returns the release identifier.</returns>
        Task<string> GetComponentReleaseID(string componentName, string version);

        /// <summary>
        /// Asynchronously gets the release information by release identifier.
        /// </summary>
        /// <param name="releaseLink">The release link.</param>
        /// <returns>A task representing the asynchronous operation that returns an HTTP response message.</returns>
        Task<HttpResponseMessage> GetReleaseInfoByReleaseId(string releaseLink);

        /// <summary>
        /// Asynchronously gets the release data of a component.
        /// </summary>
        /// <param name="releaseLink">The release link.</param>
        /// <returns>A task representing the asynchronous operation that returns the release information.</returns>
        Task<ReleasesInfo> GetReleaseDataOfComponent(string releaseLink);

        /// <summary>
        /// Asynchronously gets the available releases in SW360 for the specified components.
        /// </summary>
        /// <param name="listOfComponentsToBom">The list of components to query.</param>
        /// <returns>A task representing the asynchronous operation that returns a list of available components.</returns>
        Task<List<Components>> GetAvailableReleasesInSw360(List<Components> listOfComponentsToBom);

        /// <summary>
        /// Asynchronously gets the attachment download link.
        /// </summary>
        /// <param name="releaseAttachmentUrl">The release attachment URL.</param>
        /// <returns>A task representing the asynchronous operation that returns the attachment hash information.</returns>
        Task<Sw360AttachmentHash> GetAttachmentDownloadLink(string releaseAttachmentUrl);

        /// <summary>
        /// Asynchronously gets the upload description from SW360.
        /// </summary>
        /// <param name="componentName">The component name.</param>
        /// <param name="componetVersion">The component version.</param>
        /// <param name="sw360url">The SW360 URL.</param>
        /// <returns>A task representing the asynchronous operation that returns the upload description.</returns>
        Task<string> GetUploadDescriptionfromSW360(string componentName, string componetVersion, string sw360url);
        
        /// <summary>
        /// Gets the duplicate components by PURL identifier.
        /// </summary>
        /// <returns>A list of duplicate components.</returns>
        List<Components> GetDuplicateComponentsByPurlId();
    }
}
