// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.Common.Model;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LCT.Common.UTest
{
    [TestFixture]
    public class CommonHelperTest
    {
       
        [Test]
        public void WriteComponentsNotLinkedListInConsole_PassingList_ReturnSuccess()
        {
            //Arrange
            List<Components> ComponentsNotLinked = new List<Components>();
            ComponentsNotLinked.Add(new Components());

            //Act
            CommonHelper.WriteComponentsNotLinkedListInConsole(ComponentsNotLinked);

            //Assert
            Assert.Pass();
        }

        [Test]
        public void RemoveExcludedComponents_PassingList_ReturnSuccess()
        {
            //Arrange
            List<Component> ComponentsForBom = new List<Component>();
            ComponentsForBom.Add(new Component() { Name = "Name", Version = "12" });
            int noOfExcludedComponents = 0;

            List<string> list = new List<string>();
            list.Add("Debian:Debian");

            //Act
            List<Component> result = CommonHelper.RemoveExcludedComponents(ComponentsForBom, list, ref noOfExcludedComponents);

            //Assert
            Assert.IsTrue(result.Count > 0);
        }

        [Test]
        public void RemoveMultipleExcludedComponents_ReturnSuccess()
        {
            //Arrange
            List<Component> ComponentsForBom = new List<Component>();
            ComponentsForBom.Add(new Component() { Name = "Debian", Version = "3.1.0" });
            ComponentsForBom.Add(new Component() { Name = "Debian", Version = "3.1.1" });
            ComponentsForBom.Add(new Component() { Name = "Debian", Version = "3.1.2" });
            ComponentsForBom.Add(new Component() { Name = "Newton", Version = "3.1.3" });
            ComponentsForBom.Add(new Component() { Name = "Log4t", Version = "3.1.4" });
            ComponentsForBom.Add(new Component() { Name = "Log4t", Version = "3.1.5",Purl= "pkg:npm/foobar@12.3.1" });
            ComponentsForBom.Add(new Component() { Name = "Log4t", Version = "3.1.5", Purl = "pkg:npm/foobar@12.3.2" });
            int noOfExcludedComponents = 0;

            List<string> list = new List<string>();
            list.Add("Debian:*");
            list.Add("Newton:3.1.3");
            list.Add("pkg:npm/foobar@12.3.1");

            //Act
            CommonHelper.RemoveExcludedComponents(ComponentsForBom, list, ref noOfExcludedComponents);

            //Assert            
            Assert.That(noOfExcludedComponents, Is.EqualTo(5), "Returns the count of excluded components");

        }

        [Test]
        public void GetDetailsforManuallyAdded_PassingList_ReturnSuccess()
        {
            //Arrange
            List<Component> componentsForBOM = new List<Component>();
            componentsForBOM.Add(new Component() { Name = "Component1", Version = "1.0" });
            componentsForBOM.Add(new Component() { Name = "Component2", Version = "2.0" });
            List<Component> listComponentForBOM = new List<Component>();

            //Act
            CommonHelper.GetDetailsforManuallyAdded(componentsForBOM, listComponentForBOM);

            //Assert
            Assert.AreEqual(2, listComponentForBOM.Count);
            Assert.AreEqual("Component1", listComponentForBOM[0].Name);
            Assert.AreEqual("1.0", listComponentForBOM[0].Version);
            Assert.AreEqual("Component2", listComponentForBOM[1].Name);
            Assert.AreEqual("2.0", listComponentForBOM[1].Version);
        }

        [Test]
        public void CheckNullOrEmpty_WhenValueIsNull_ThrowsArgumentException()
        {
            // Arrange
            string name = "TestName";
            string value = null;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => CommonHelper.CheckNullOrEmpty(name, value));
        }

        [Test]
        public void CheckNullOrEmpty_WhenValueIsEmpty_ThrowsArgumentException()
        {
            // Arrange
            string name = "TestName";
            string value = "";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => CommonHelper.CheckNullOrEmpty(name, value));
        }

        [Test]
        public void CheckNullOrEmpty_WhenValueIsWhiteSpace_ThrowsArgumentException()
        {
            // Arrange
            string name = "TestName";
            string value = "   ";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => CommonHelper.CheckNullOrEmpty(name, value));
        }

        [Test]
        public void CheckNullOrEmpty_WhenValueIsNotNullOrEmpty_DoesNotThrowException()
        {
            // Arrange
            string name = "TestName";
            string value = "TestValue";

            // Act & Assert
            Assert.DoesNotThrow(() => CommonHelper.CheckNullOrEmpty(name, value));
        }
        [Test]
        public void Convert_WhenAttributeExists_ReturnsDisplayName()
        {
            // Arrange
            var objectValue = new TestObject();
            var nameOfProperty = "Property1";

            // Act
            var result = CommonHelper.Convert(objectValue, nameOfProperty);

            // Assert
            Assert.AreEqual("Display Name 1", result);
        }

        [Test]
        public void Convert_WhenAttributeDoesNotExist_ReturnsEmptyString()
        {
            // Arrange
            var objectValue = new TestObject();
            var nameOfProperty = "Property2";

            // Act
            var result = CommonHelper.Convert(objectValue, nameOfProperty);

            // Assert
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void TrimEndOfString_WhenSuffixToRemoveIsNull_ReturnsInputString()
        {
            // Arrange
            string input = "TestString";
            string suffixToRemove = null;

            // Act
            string result = CommonHelper.TrimEndOfString(input, suffixToRemove);

            // Assert
            Assert.AreEqual(input, result);
        }

        [Test]
        public void TrimEndOfString_WhenSuffixToRemoveIsEmpty_ReturnsInputString()
        {
            // Arrange
            string input = "TestString";
            string suffixToRemove = "";

            // Act
            string result = CommonHelper.TrimEndOfString(input, suffixToRemove);

            // Assert
            Assert.AreEqual(input, result);
        }

        [Test]
        public void TrimEndOfString_WhenSuffixToRemoveDoesNotMatch_ReturnsInputString()
        {
            // Arrange
            string input = "TestString";
            string suffixToRemove = "Suffix";

            // Act
            string result = CommonHelper.TrimEndOfString(input, suffixToRemove);

            // Assert
            Assert.AreEqual(input, result);
        }

        [Test]
        public void TrimEndOfString_WhenSuffixToRemoveMatches_ReturnsTrimmedString()
        {
            // Arrange
            string input = "TestStringSuffix";
            string suffixToRemove = "Suffix";
            string expected = "TestString";

            // Act
            string result = CommonHelper.TrimEndOfString(input, suffixToRemove);

            // Assert
            Assert.AreEqual(expected, result);
        }
        [Test]
        public void IsAzureDevOpsDebugEnabled_WhenSystemDebugEnabledIsTrue_ReturnsTrue()
        {
            // Arrange
            System.Environment.SetEnvironmentVariable("System.Debug", "true");

            // Act
            bool result = CommonHelper.IsAzureDevOpsDebugEnabled();

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IsAzureDevOpsDebugEnabled_WhenSystemDebugEnabledIsFalse_ReturnsFalse()
        {
            // Arrange
            System.Environment.SetEnvironmentVariable("System.Debug", "false");

            // Act
            bool result = CommonHelper.IsAzureDevOpsDebugEnabled();

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsAzureDevOpsDebugEnabled_WhenSystemDebugEnabledIsNotSet_ReturnsFalse()
        {
            // Arrange
            System.Environment.SetEnvironmentVariable("System.Debug", null);

            // Act
            bool result = CommonHelper.IsAzureDevOpsDebugEnabled();

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void RemoveExcludedComponents_WhenExcludedComponentMatches_ReturnsExcludedComponents()
        {
            // Arrange
            List<Component> componentList = new List<Component>
            {
                new Component { Name = "Component1", Version = "1.0" },
                new Component { Name = "Component2", Version = "2.0" },
                new Component { Name = "Component3", Version = "3.0" }
            };
            List<string> excludedComponents = new List<string> { "Component1:*", "Component2:2.0" };
            int noOfExcludedComponents = 0;

            // Act
            List<Component> result = CommonHelper.RemoveExcludedComponents(componentList, excludedComponents, ref noOfExcludedComponents);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.IsFalse(result.Any(c => c.Name == "Component1" && c.Version == "1.0"));
            Assert.IsTrue(result.Any(c => c.Name == "Component3" && c.Version == "3.0"));
            Assert.AreEqual(2, noOfExcludedComponents);
        }

        [Test]
        public void RemoveExcludedComponents_WhenExcludedComponentDoesNotMatch_ReturnsOriginalComponentList()
        {
            // Arrange
            List<Component> componentList = new List<Component>
            {
                new Component { Name = "Component1", Version = "1.0" },
                new Component { Name = "Component2", Version = "2.0" },
                new Component { Name = "Component3", Version = "3.0" }
            };
            List<string> excludedComponents = new List<string> { "Component4:*", "Component2:3.0" };
            int noOfExcludedComponents = 0;

            // Act
            List<Component> result = CommonHelper.RemoveExcludedComponents(componentList, excludedComponents, ref noOfExcludedComponents);

            // Assert
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Any(c => c.Name == "Component1" && c.Version == "1.0"));
            Assert.IsTrue(result.Any(c => c.Name == "Component2" && c.Version == "2.0"));
            Assert.IsTrue(result.Any(c => c.Name == "Component3" && c.Version == "3.0"));
            Assert.AreEqual(0, noOfExcludedComponents);
        }
    }

    public class TestObject
    {
        [System.ComponentModel.DisplayName("Display Name 1")]
        public string Property1 { get; set; }

        public string Property2 { get; set; }
    }
}
