using System.Diagnostics.CodeAnalysis;

namespace LCT.SW360PackageCreator.Model
{
    [ExcludeFromCodeCoverage]
    internal class PythonPackage
    {
        public string Name { get; set; }

        public string Version { get; set; }

        public string PurlID { get; set; }

        public string SourceUrl { get; set; }

        public string WheelUrl { get; set; }
    }
}
