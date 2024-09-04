// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using NUnit.Framework;
using LCT.APICommunications.Model.AQL;
using System.Collections.Generic;
using LCT.PackageIdentifier;

namespace PackageIdentifier.Tests
{
    [TestFixture]
    public class CommonIdentiferHelperTests
    {
        private const string NotFoundInRepo = "Not Found in JFrogRepo";

        [Test]
        public void GetRepodetailsFromPerticularOrder_InputIsNull_ReturnsNotFound()
        {
            var result = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(null);
            Assert.AreEqual(NotFoundInRepo, result);
        }

        [Test]
        public void GetRepodetailsFromPerticularOrder_ContainsRelease_ReturnsReleaseRepo()
        {
            var aqlResults = new List<AqlResult>
            {
                new AqlResult { Repo = "release-repo" }
            };
            var result = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(aqlResults);
            Assert.AreEqual("release-repo", result);
        }

        [Test]
        public void GetRepodetailsFromPerticularOrder_ContainsDevdep_ReturnsDevdepRepo()
        {
            var aqlResults = new List<AqlResult>
            {
                new AqlResult { Repo = "devdep-repo" }
            };
            var result = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(aqlResults);
            Assert.AreEqual("devdep-repo", result);
        }

        [Test]
        public void GetRepodetailsFromPerticularOrder_ContainsDev_ReturnsDevRepo()
        {
            var aqlResults = new List<AqlResult>
            {
                new AqlResult { Repo = "dev-repo" }
            };
            var result = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(aqlResults);
            Assert.AreEqual("dev-repo", result);
        }

        [Test]
        public void GetRepodetailsFromPerticularOrder_NoSpecificRepo_ReturnsFirstRepo()
        {
            var aqlResults = new List<AqlResult>
            {
                new AqlResult { Repo = "generic-repo" }
            };
            var result = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(aqlResults);
            Assert.AreEqual("generic-repo", result);
        }
    }
}
