using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace LCT.PackageIdentifier.Model.NugetModel
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum ContainerType
    {
        unknown = 0,
        nuget = 1
    }


    /// <summary>
    /// A container is a logical group which uses packages.
    /// E.g., this could be a <c>nuget.config</c> file or a <c>*.csproj</c> file.
    /// </summary>
    public class Container
    {
        public IDictionary<string, BuildInfoComponent> Components { get; set; } = new Dictionary<string, BuildInfoComponent>();

        public string? Name { get; set; }

        public ContainerType Type { get; set; }

        public ComponentScope Scope { get; set; } = ComponentScope.Required;
    }
}
