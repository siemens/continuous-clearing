using LCT.Common.Interface;
using LCT.Common.Model;
using LCT.Common;
using LCT.Services.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCT.SW360PackageCreator.Interfaces
{
    public interface IPackageCreater
    {
        Task CreatePackageInSw360(CommonAppSettings appSettings, ISw360CreatorService sw360CreatorService, List<ComparisonBomData> parsedBomData,ISW360Service sW360Service);
    }
}
