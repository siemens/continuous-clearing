// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using LCT.Common;
using LCT.PackageIdentifier.Model;

namespace LCT.PackageIdentifier.Interface
{
    public interface IRuntimeIdentifier
    {
        RuntimeInfo IdentifyRuntime(CommonAppSettings appSettings);
    }
}
