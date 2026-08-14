// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using NUnit.Framework;
using SIT.Common.Model;
using SIT.Create.Model;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SIT.Create.UTest
{
    /// <summary>
    /// The test class for AptRepositoryService
    /// </summary>
    [TestFixture]
    public class AptRepositoryServiceTest
    {
        private const string VendorArchive = "http://vendor.test/deb/debian/main";
        private const string DistributionArchive = "http://mirror.test/debian";

        private const string VendorSources = @"Package: busybox
Version: 1:1.37.0-6+dhi1
Directory: pool/trixie/main/b/bu/busybox_1.37.0-6+dhi1
Files:
 aaa 1560 busybox_1.37.0-6+dhi1.dsc
 bbb 2565764 busybox_1.37.0.orig.tar.bz2
 ccc 70672 busybox_1.37.0-6+dhi1.debian.tar.xz

Package: gcc-14
Version: 14.2.0-19+dhi3
Directory: pool/trixie/main/g/gc/gcc-14_14.2.0-19+dhi3
Files:
 ddd 100 gcc-14_14.2.0-19+dhi3.dsc
 eee 200 gcc-14_14.2.0.orig.tar.gz
 fff 300 gcc-14_14.2.0-19+dhi3.debian.tar.xz
";

        private const string VendorPackages = @"Package: busybox
Version: 1:1.37.0-6+dhi1
Filename: pool/trixie/main/b/bu/busybox_1.37.0-6+dhi1/busybox_1.37.0-6+dhi1_amd64.deb

Package: libgcc-s1
Version: 14.2.0-19+dhi3
Source: gcc-14
Filename: pool/trixie/main/l/li/libgcc-s1_14.2.0-19+dhi3/libgcc-s1_14.2.0-19+dhi3_amd64.deb

Package: tzdata
Version: 2026b-0+deb13u1+dhi2
Filename: pool/trixie/main/t/tz/tzdata_2026b-0+deb13u1+dhi2/tzdata_2026b-0+deb13u1+dhi2_all.deb
";

        private const string TzdataDsc = @"Format: 3.0 (quilt)
Source: tzdata
Version: 2026b-0+deb13u1+dhi2
Files:
 111 473703 tzdata_2026b.orig.tar.gz
 222 127236 tzdata_2026b-0+deb13u1+dhi2.debian.tar.xz
";

        private const string DistributionSources = @"Package: tzdata
Version: 2026b-0+deb13u1
Directory: pool/main/t/tzdata
Files:
 111 473703 tzdata_2026b.orig.tar.gz
 333 120000 tzdata_2026b-0+deb13u1.debian.tar.xz
";

        private static StubHttpMessageHandler CreateArchives()
        {
            StubHttpMessageHandler handler = new();
            handler.Content[$"{VendorArchive}/dists/trixie/Release"] = "Components: main\nArchitectures: amd64 source\n";
            handler.Content[$"{VendorArchive}/dists/trixie/main/source/Sources"] = VendorSources;
            handler.Content[$"{VendorArchive}/dists/trixie/main/binary-amd64/Packages"] = VendorPackages;
            handler.Content[$"{DistributionArchive}/dists/trixie/Release"] = "Components: main\nArchitectures: amd64 source\n";
            handler.Content[$"{DistributionArchive}/dists/trixie/main/source/Sources"] = DistributionSources;
            return handler;
        }

        private static List<AptRepository> Repositories(params string[] uris)
        {
            return uris.Select(uri => new AptRepository { Name = uri, Uri = uri }).ToList();
        }

        [Test]
        public async Task ResolveSourcePackageAsync_SourcePackageWithSameName_ReturnsAllSourceFiles()
        {
            using AptRepositoryService service = new(new HttpClient(CreateArchives()));

            // the installed version is reported without the epoch of the index
            AptSourceResolution resolution = await service.ResolveSourcePackageAsync(
                Repositories(VendorArchive), "busybox", "1.37.0-6+dhi1", new[] { "trixie" });

            Assert.That(resolution, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(resolution.Name, Is.EqualTo("busybox"));
                Assert.That(resolution.Version, Is.EqualTo("1:1.37.0-6+dhi1"));
                Assert.That(resolution.FileUrls, Is.EquivalentTo(new[]
                {
                    $"{VendorArchive}/pool/trixie/main/b/bu/busybox_1.37.0-6+dhi1/busybox_1.37.0-6+dhi1.dsc",
                    $"{VendorArchive}/pool/trixie/main/b/bu/busybox_1.37.0-6+dhi1/busybox_1.37.0.orig.tar.bz2",
                    $"{VendorArchive}/pool/trixie/main/b/bu/busybox_1.37.0-6+dhi1/busybox_1.37.0-6+dhi1.debian.tar.xz"
                }));
            });
        }

        [Test]
        public async Task ResolveSourcePackageAsync_BinaryPackageWithOtherSourceName_ResolvesThroughBinaryIndex()
        {
            using AptRepositoryService service = new(new HttpClient(CreateArchives()));

            AptSourceResolution resolution = await service.ResolveSourcePackageAsync(
                Repositories(VendorArchive), "libgcc-s1", "14.2.0-19+dhi3", new[] { "trixie" });

            Assert.That(resolution, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(resolution.Name, Is.EqualTo("gcc-14"));
                Assert.That(resolution.Version, Is.EqualTo("14.2.0-19+dhi3"));
                Assert.That(resolution.FileUrls, Has.Count.EqualTo(3));
            });
        }

        [Test]
        public async Task ResolveSourcePackageAsync_PackageMissingInSourceIndex_FallsBackToDscInThePool()
        {
            StubHttpMessageHandler handler = CreateArchives();
            handler.Content[$"{VendorArchive}/pool/trixie/main/t/tz/tzdata_2026b-0+deb13u1+dhi2/tzdata_2026b-0+deb13u1+dhi2.dsc"] = TzdataDsc;
            handler.Content[$"{VendorArchive}/pool/trixie/main/t/tz/tzdata_2026b-0+deb13u1+dhi2/tzdata_2026b-0+deb13u1+dhi2.debian.tar.xz"] = "delta";
            handler.Content[$"{DistributionArchive}/pool/main/t/tzdata/tzdata_2026b.orig.tar.gz"] = "upstream";
            using AptRepositoryService service = new(new HttpClient(handler));

            AptSourceResolution resolution = await service.ResolveSourcePackageAsync(
                Repositories(VendorArchive, DistributionArchive), "tzdata", "2026b-0+deb13u1+dhi2", new[] { "trixie" });

            Assert.That(resolution, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(resolution.Name, Is.EqualTo("tzdata"));
                Assert.That(resolution.Version, Is.EqualTo("2026b-0+deb13u1+dhi2"));

                // the upstream tarball is not served by the vendor archive, it is taken from the distribution
                Assert.That(resolution.FileUrls, Is.EquivalentTo(new[]
                {
                    $"{VendorArchive}/pool/trixie/main/t/tz/tzdata_2026b-0+deb13u1+dhi2/tzdata_2026b-0+deb13u1+dhi2.dsc",
                    $"{DistributionArchive}/pool/main/t/tzdata/tzdata_2026b.orig.tar.gz",
                    $"{VendorArchive}/pool/trixie/main/t/tz/tzdata_2026b-0+deb13u1+dhi2/tzdata_2026b-0+deb13u1+dhi2.debian.tar.xz"
                }));
            });
        }

        [Test]
        public async Task ResolveSourcePackageAsync_UnknownPackage_ReturnsNull()
        {
            using AptRepositoryService service = new(new HttpClient(CreateArchives()));

            AptSourceResolution resolution = await service.ResolveSourcePackageAsync(
                Repositories(VendorArchive), "openssl", "3.5.0-1", new[] { "trixie" });

            Assert.That(resolution, Is.Null);
        }

        [Test]
        public async Task ResolveSourcePackageAsync_WithoutRepositories_ReturnsNull()
        {
            using AptRepositoryService service = new(new HttpClient(CreateArchives()));

            AptSourceResolution resolution = await service.ResolveSourcePackageAsync(
                new List<AptRepository>(), "busybox", "1.37.0-6+dhi1", new[] { "trixie" });

            Assert.That(resolution, Is.Null);
        }

        [Test]
        public async Task ResolveSourcePackageAsync_UnknownSuite_ReturnsNull()
        {
            using AptRepositoryService service = new(new HttpClient(CreateArchives()));

            AptSourceResolution resolution = await service.ResolveSourcePackageAsync(
                Repositories(VendorArchive), "busybox", "1.37.0-6+dhi1", new[] { "bookworm" });

            Assert.That(resolution, Is.Null);
        }

        [Test]
        public void GetDistributionCandidates_PurlWithDistributionQualifiers_PrefersTheCodename()
        {
            List<string> candidates = UrlHelper.GetDistributionCandidates(
                "pkg:deb/debian/busybox@1.37.0-6%2Bdhi1?os_distro=trixie&os_name=debian&os_version=13");

            Assert.That(candidates, Is.EqualTo(new[] { "trixie" }));
        }

        [Test]
        public void GetDistributionCandidates_SyftPurl_ReturnsDistributionAndVersion()
        {
            List<string> candidates = UrlHelper.GetDistributionCandidates(
                "pkg:deb/debian/adduser@3.118?arch=all&distro=debian-10");

            Assert.That(candidates, Is.EqualTo(new[] { "debian-10", "10" }));
        }

        [Test]
        public void GetDistributionCandidates_PurlWithoutQualifiers_ReturnsEmptyList()
        {
            Assert.That(UrlHelper.GetDistributionCandidates("pkg:deb/debian/adduser@3.118"), Is.Empty);
        }

        /// <summary>
        /// Serves the configured URLs and answers with "404 Not Found" for everything else.
        /// </summary>
        private sealed class StubHttpMessageHandler : HttpMessageHandler
        {
            public Dictionary<string, string> Content { get; } = new();

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (!Content.TryGetValue(request.RequestUri.AbsoluteUri, out string content))
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(request.Method == HttpMethod.Head ? string.Empty : content)
                });
            }
        }
    }
}
