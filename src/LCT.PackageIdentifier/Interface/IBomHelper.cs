// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.APICommunications.Model.AQL;
using LCT.PackageIdentifier.Model;
using LCT.Services.Interface;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LCT.PackageIdentifier.Interface
{
    /// <summary>
    /// BOM helper interface
    /// </summary>
    public interface IBomHelper
    {
        /// <summary>
        /// Writes BOM KPI data to the console.
        /// </summary>
        /// <param name="bomKpiData">The BOM KPI data to write.</param>
        public void WriteBomKpiDataToConsole(BomKpiData bomKpiData);
        
        /// <summary>
        /// Gets the project summary link for the specified project.
        /// </summary>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="sw360Url">The SW360 URL.</param>
        /// <returns>The project summary link.</returns>
        public string GetProjectSummaryLink(string projectId, string sw360Url);
        
        /// <summary>
        /// Gets the full name of a component.
        /// </summary>
        /// <param name="item">The component.</param>
        /// <returns>The full name of the component.</returns>
        public string GetFullNameOfComponent(Component item);
        
        /// <summary>
        /// Asynchronously gets the list of components from the specified repositories.
        /// </summary>
        /// <param name="repoList">The array of repository names.</param>
        /// <param name="jFrogService">The JFrog service instance.</param>
        /// <returns>A task representing the asynchronous operation that returns a list of AQL results.</returns>
        public Task<List<AqlResult>> GetListOfComponentsFromRepo(string[] repoList, IJFrogService jFrogService);
        
        /// <summary>
        /// Asynchronously gets the list of NPM components from the specified repositories.
        /// </summary>
        /// <param name="repoList">The array of repository names.</param>
        /// <param name="jFrogService">The JFrog service instance.</param>
        /// <returns>A task representing the asynchronous operation that returns a list of AQL results.</returns>
        public Task<List<AqlResult>> GetNpmListOfComponentsFromRepo(string[] repoList, IJFrogService jFrogService);
        
        /// <summary>
        /// Asynchronously gets the list of PyPI components from the specified repositories.
        /// </summary>
        /// <param name="repoList">The array of repository names.</param>
        /// <param name="jFrogService">The JFrog service instance.</param>
        /// <returns>A task representing the asynchronous operation that returns a list of AQL results.</returns>
        public Task<List<AqlResult>> GetPypiListOfComponentsFromRepo(string[] repoList, IJFrogService jFrogService);
        
        /// <summary>
        /// Asynchronously gets the list of Cargo components from the specified repositories.
        /// </summary>
        /// <param name="repoList">The array of repository names.</param>
        /// <param name="jFrogService">The JFrog service instance.</param>
        /// <returns>A task representing the asynchronous operation that returns a list of AQL results.</returns>
        public Task<List<AqlResult>> GetCargoListOfComponentsFromRepo(string[] repoList, IJFrogService jFrogService);
    }
}
