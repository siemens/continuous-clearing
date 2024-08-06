// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.Common.Model;
using NUnit.Framework;
using System.Collections.Generic;

namespace LCT.Common.UTest
{
    public class CommonHelperTest
    {
        //This test case ignore due to environment exit comes in the method
        //[Test]        
        //public void WriteComponentsNotLinkedListInConsole_PassingList_ReturnSuccess()
        //{
        //    //Arrange
        //    List<Components> ComponentsNotLinked = new List<Components>();
        //    ComponentsNotLinked.Add(new Components());

        //    //Act
        //    CommonHelper.WriteComponentsNotLinkedListInConsole(ComponentsNotLinked);

        //    //Assert
        //    Assert.IsTrue(true);
        //}

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
            int noOfExcludedComponents = 0;

            List<string> list = new List<string>();
            list.Add("Debian:*");
            list.Add("Newton:3.1.3");

            //Act
            CommonHelper.RemoveExcludedComponents(ComponentsForBom, list, ref noOfExcludedComponents);

            //Assert            
            Assert.That(noOfExcludedComponents, Is.EqualTo(4), "Returns the count of excluded components");

        }
    }
}
