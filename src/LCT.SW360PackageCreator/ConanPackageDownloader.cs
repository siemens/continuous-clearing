using LCT.Common.Model;
using LCT.SW360PackageCreator.Interfaces;
using LCT.SW360PackageCreator.Model;
using log4net;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace LCT.SW360PackageCreator
{
   
    public class ConanPackageDownloader: IPackageDownloader
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly List<DownloadedSourceInfo> m_downloadedSourceInfos = new List<DownloadedSourceInfo>();
        private const string Source = "source";
        public async Task<string> DownloadPackage(ComparisonBomData component, string localPathforDownload)
        {
            string path = Download(component, localPathforDownload);
            await Task.Delay(10);
            return path;
        }
        private string Download(ComparisonBomData component, string downloadPath) 
        {
            return "";
        }
    }
}
