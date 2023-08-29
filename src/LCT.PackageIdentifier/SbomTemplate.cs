// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using CycloneDX.Models;
using LCT.Common;
using LCT.Common.Constants;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LCT.PackageIdentifier
{
    internal static class SbomTemplate
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void AddComponentDetails(List<Component> bom, Bom sbomdDetails)
        {
            if (sbomdDetails.Components == null)
            {
                return;
            }

            foreach (var sbomcomp in sbomdDetails.Components)
            {
                try
                {
                    Property cdxIdentifierType = new() { Name = Dataconstant.Cdx_IdentifierType, Value = Dataconstant.TemplateAdded };
                    Property cdxIsDev = new() { Name = Dataconstant.Cdx_IsDevelopment, Value = "false" };

                    Component bomComp = bom.Find(x => x.Name == sbomcomp.Name && x.Version == sbomcomp.Version);
                    if (bomComp == null)
                    {
                        if (sbomcomp.Properties == null)
                        {
                            sbomcomp.Properties = new List<Property>();
                            sbomcomp.Properties.Add(cdxIdentifierType);
                            sbomcomp.Properties.Add(cdxIsDev);
                            bom.Add(sbomcomp);
                            BomCreator.bomKpiData.ComponentsinSBOMTemplateFile++;
                        }
                        else
                        {
                            sbomcomp.Properties.Add(cdxIdentifierType);
                            sbomcomp.Properties.Add(cdxIsDev);
                            bom.Add(sbomcomp);
                            BomCreator.bomKpiData.ComponentsinSBOMTemplateFile++;
                        }
                    }
                    else
                    {
                        bool isLicenseUpdated = false;
                        bool isPropertiesUpdated = false;

                        isLicenseUpdated = UpdateLicenseDetails(bomComp, sbomcomp);
                        isPropertiesUpdated = UpdatePropertiesDetails(bomComp, sbomcomp);

                        if (isLicenseUpdated || isPropertiesUpdated)
                        {

                            BomCreator.bomKpiData.ComponentsUpdatedFromSBOMTemplateFile++;
                        }
                        else
                        {
                            Logger.Debug($"AddComponentDetails():No Details updated for SBOM Template component " + sbomcomp.Name + " : " + sbomcomp.Version);
                        }
                    }
                }
                catch (ArgumentException ex)
                {
                    Logger.Error($"AddComponentDetails():ArgumentException:Error from " + sbomcomp.Name + " : " + sbomcomp.Version, ex);
                }
                catch (InvalidOperationException ex)
                {
                    Logger.Error($"AddComponentDetails():InvalidOperationException:Error from " + sbomcomp.Name + " : " + sbomcomp.Version, ex);
                }
            }
        }

        private static bool UpdateLicenseDetails(Component bomComp, Component sbomcomp)
        {
            //Adding Licenses if mainatined
            if (sbomcomp.Licenses?.Count > 0)
            {
                if (bomComp.Licenses != null)
                {
                    bomComp.Licenses.AddRange(sbomcomp.Licenses);
                    return true;
                }
                else
                {
                    bomComp.Licenses = sbomcomp.Licenses;
                    return true;
                }
            }
            return false;
        }

        private static bool UpdatePropertiesDetails(Component bomComp, Component sbomcomp)
        {
            //Adding properties if mainatined
            if (sbomcomp.Properties?.Count > 0)
            {
                if (CommonHelper.ComponentPropertyCheck(bomComp, Dataconstant.Cdx_IdentifierType))
                {
                    var val = bomComp.Properties.Single(x => x.Name == Dataconstant.Cdx_IdentifierType);
                    val.Value = Dataconstant.TemplateUpdated;
                }
                else
                {
                    bomComp.Properties.Add(new Property()
                    {
                        Name = Dataconstant.Cdx_IdentifierType,
                        Value = Dataconstant.TemplateUpdated
                    });
                }

                bomComp.Properties?.AddRange(sbomcomp.Properties);
                return true;
            }
            return false;
        }
    }
}
