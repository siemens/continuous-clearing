// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace LCT.PackageIdentifier.Model.NugetModel
{
    public enum ComponentScope
    {
        Required = 0,
        Optional = 1,
        DevDependency = 2
    }

    [ExcludeFromCodeCoverage]
    public abstract class BuildInfoComponent : IEquatable<BuildInfoComponent>
    {
        protected static readonly HashAlgorithm HashAlgorithm = SHA512.Create();

        public string Name { get; set; }

        public string Version { get; set; }

        public ComponentScope Scope { get; set; }

        public string Repository { get; set; }

        public string RepositoryPath { get; set; }

        public HashSet<BuildInfoComponent> Dependencies { get; }

        public HashSet<BuildInfoComponent> Ancestors { get; }

        public string TypeName { get; }

        public abstract string PackageUrl { get; }

        public string Md5 { get; set; }
        public string Sha1 { get; set; }
        public string Sha256 { get; set; }

        protected BuildInfoComponent(string id, string version)
        {
            Name = id;
            Version = version;
            Dependencies = new HashSet<BuildInfoComponent>();
            Ancestors = new HashSet<BuildInfoComponent>();
            TypeName = GetType().Name;
        }

        public virtual string Id
        {
            get
            {
                string input = $"{Name}{Version}{Scope}{GetType()}";
                return GetHashFromString(input);
            }
        }


        public static string GetHashFromString(string input)
        {
            byte[] byteHash = HashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(byteHash);
        }

        public IList<IList<BuildInfoComponent>> GetAncestors(IList<BuildInfoComponent> children)
        {
            List<IList<BuildInfoComponent>> ancestorsPathList = new();
            if (Ancestors.Count == 0)
            {
                if (children == null)
                {
                    return Array.Empty<IList<BuildInfoComponent>>();
                }

                return new List<IList<BuildInfoComponent>>() { new List<BuildInfoComponent>(children) { this } };
            }


            foreach (BuildInfoComponent ancestor in Ancestors)
            {
                List<BuildInfoComponent> myPath;
                if (children == null)
                {
                    myPath = new List<BuildInfoComponent>();
                }
                else
                {
                    myPath = new List<BuildInfoComponent>(children)
                    {
                            this
                    };
                }

                ancestorsPathList.AddRange(ancestor.GetAncestors(myPath));
            }

            return ancestorsPathList;
        }

        public virtual bool Equals(BuildInfoComponent other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Equals(Name,
                           other.Name,
                           StringComparison.InvariantCultureIgnoreCase) &&
                   string.Equals(Version,
                           other.Version,
                           StringComparison.InvariantCultureIgnoreCase) &&
                   Scope == other.Scope &&
                   string.Equals(Repository,
                           other.Repository,
                           StringComparison.InvariantCultureIgnoreCase) &&
                   string.Equals(RepositoryPath,
                           other.RepositoryPath,
                           StringComparison.InvariantCultureIgnoreCase) &&
                   Dependencies.Equals(other.Dependencies) &&
                   Ancestors.Equals(other.Ancestors) &&
                   string.Equals(TypeName,
                           other.TypeName,
                           StringComparison.InvariantCultureIgnoreCase) &&
                   string.Equals(PackageUrl,
                           other.PackageUrl,
                           StringComparison.InvariantCultureIgnoreCase) &&
                   string.Equals(Md5,
                           other.Md5,
                           StringComparison.InvariantCultureIgnoreCase) &&
                   string.Equals(Sha1,
                           other.Sha1,
                           StringComparison.InvariantCultureIgnoreCase) &&
                   string.Equals(Sha256,
                           other.Sha256,
                           StringComparison.InvariantCultureIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as BuildInfoComponent);
        }

        public override int GetHashCode()
        {
            return 0;
        }
    }
}
