using System.Collections.Generic;


namespace LCT.PackageIdentifier.Model
{
    public class RuntimeInfo
    {
        public string ProjectPath { get; set; }
        public string ProjectName { get; set; }
        public bool IsSelfContained { get; set; }
        public bool SelfContainedExplicitlySet { get; set; }
        public string SelfContainedEvaluated { get; set; }
        public string SelfContainedReason { get; set; }
        public List<string> RuntimeIdentifiers { get; set; } = new();
        public List<FrameworkReferenceInfo> FrameworkReferences { get; set; } = new();
        public string ErrorMessage { get; set; }
        public string ErrorDetails { get; set; }
    }

    public class FrameworkReferenceInfo
    {
        public string TargetFramework { get; set; }
        public string Name { get; set; }
        public string TargetingPackVersion { get; set; }
    }
}
