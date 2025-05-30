﻿// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model;
using LCT.Common;
using LCT.Common.Interface;
using LCT.Common.Model;
using LCT.Services.Interface;
using System.Threading.Tasks;

namespace LCT.PackageIdentifier.Interface
{
    /// <summary>
    /// BomCreator interface
    /// </summary>
    public interface IBomCreator
    {
        public IJFrogService JFrogService { get; set; }

        public IBomHelper BomHelper { get; set; }

        public Task GenerateBom(CommonAppSettings appSettings, IBomHelper bomHelper, IFileOperations fileOperations,
                                ProjectReleases projectReleases, CatoolInfo caToolInformation);

        public Task<bool> CheckJFrogConnection(CommonAppSettings appSettings);
    }
}
