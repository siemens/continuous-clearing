using LCT.APICommunications;
using LCT.Common.Constants;
using LCT.Common.Model;
using LCT.Common;
using LCT.Services.Interface;
using LCT.SW360PackageCreator.Interfaces;
using log4net.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using System.Reflection;
using CycloneDX.Models;
using LCT.Common.Interface;
using LCT.Services;
using LCT.APICommunications.Model;

namespace LCT.SW360PackageCreator
{
    public class PackageCreater:IPackageCreater
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public async Task CreatePackageInSw360(CommonAppSettings appSettings,ISw360CreatorService sw360CreatorService,List<ComparisonBomData> parsedBomData,ISW360Service sW360Service)
        {
            Logger.Logger.Log(null, Level.Notice, $"No of Unique and Valid components read from Comparison BOM = {parsedBomData.Count} ", null);
            // create package in sw360
            if (!appSettings.IsTestMode)
            {
                foreach (ComparisonBomData item in parsedBomData)
                {
                    await CreatePackage(sw360CreatorService, item, appSettings);
                }                
            }
            
        }        
        private static async Task CreatePackage(ISw360CreatorService sw360CreatorService, ComparisonBomData item, CommonAppSettings appSettings)
        {

            try
            {
                if (item.PackageStatus == Dataconstant.NotAvailable)
                {
                    Logger.Logger.Log(null, Level.Notice, $"Creating the Package : Name - {item.PackageName} , version - {item.Version}", null);
                    PackageCreateStatus createdStatus = await sw360CreatorService.CreatePackageBasesOFswComaprisonBOM(item);
                    item.IsPackageCreated = ComponentCreator.GetCreatedStatus(createdStatus.IsCreated);
                }
                else
                {
                    Logger.Logger.Log(null, Level.Notice, $"Package exists : Name - {item.PackageName} , version - {item.Version}", null);
                }
            }
            catch (AggregateException ex)
            {
                Logger.Debug($"CreatePackage()", ex);
            }
        }
    }
}
