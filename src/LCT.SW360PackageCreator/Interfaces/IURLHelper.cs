// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 


using LCT.Common.Model;
using System.Threading.Tasks;

namespace LCT.SW360PackageCreator.Interfaces
{
    /// <summary>
    /// IURLHelper interface
    /// </summary>
    public interface IUrlHelper
    {
        /// <summary>
        /// Gets the SourceUrl For Debian Package
        /// </summary>
        /// <param name="componentName"></param>
        /// <param name="componenVersion"></param>
        /// <param name="componentsData"></param>
        /// <returns>string</returns>
         Task<Components> GetSourceUrlForDebianPackage(string componentName, string componenVersion);

        /// <summary>
        /// Gets the Source Url For Nuget Package
        /// </summary>
        /// <param name="componentName"></param>
        /// <param name="componenVersion"></param>
        /// <returns>string</returns>
        Task<string> GetSourceUrlForNugetPackage(string componentName, string componenVersion);

        /// <summary>
        /// Gets the Source URL for NPM Package
        /// </summary>
        /// <param name="componentName"></param>
        /// <param name="version"></param>
        /// <returns>string</returns>
        string GetSourceUrlForNpmPackage(string componentName, string version);

        /// <summary>
        /// Gets the Source URL for PYTHON Package
        /// </summary>
        /// <param name="componentName"></param>
        /// <param name="componenVersion"></param>
        /// <returns>string</returns>
        Task<string> GetSourceUrlForPythonPackage(string componentName, string componenVersion);


        /// <summary>
        /// Gets the Source URL for Conan Package
        /// </summary>
        /// <param name="componentName"></param>
        /// <param name="componenVersion"></param>
        /// <returns>string</returns>
        Task<string> GetSourceUrlForConanPackage(string componentName, string componenVersion);

        /// <summary>
        /// Gets the SourceUrl For Alpine Package
        /// </summary>
        /// <param name="componentName"></param>
        /// <param name="componenVersion"></param>
        /// <param name="bomRef"></param>
        /// <returns>string</returns>
        Task<Components> GetSourceUrlForAlpinePackage(string componentName, string componenVersion, string bomRef);
    }
}
