// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using NuGet.ProjectModel;
using NuGet.Versioning;
using System.Collections.Generic;

namespace LCT.PackageIdentifier.Interface
{
    public interface IFrameworkPackages
    {
        Dictionary<string, Dictionary<string, NuGetVersion>> GetFrameworkPackages(List<string> lockFilePaths);

        string[] GetFrameworkReferences(LockFile lockFile, LockFileTarget target);
    }
}