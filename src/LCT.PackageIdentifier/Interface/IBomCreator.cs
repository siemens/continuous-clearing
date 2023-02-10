// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common;
using LCT.Common.Interface;
using System.Threading.Tasks;

namespace LCT.PackageIdentifier.Interface
{
    /// <summary>
    /// BomCreator interface
    /// </summary>
    public interface IBomCreator
    {
       public  Task GenerateBom(CommonAppSettings appSettings, IBomHelper bomHelper, IFileOperations fileOperations);
    }
}
