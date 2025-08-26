using NUnit.Framework;
using LCT.Common;
using System.Reflection;
using log4net;

namespace LCT.Common.Tests.Logging
{
    [TestFixture]
    public class LoggerFactoryTests
    {
        private class LoggerFactoryDummyClass { }

        [SetUp]
        public void SetUp()
        {
            // Reset to default before each test
            LoggerFactory.UseSpectreConsole = true;
        }

        [Test]
        public void GetLogger_Type_ReturnsSpectreLogAdapter_WhenUseSpectreConsoleIsTrue()
        {
            LoggerFactory.UseSpectreConsole = true;
            var logger = LoggerFactory.GetLogger(typeof(LoggerFactoryDummyClass));
            Assert.IsInstanceOf<SpectreLogAdapter>(logger);
        }

        [Test]
        public void GetLogger_Type_ReturnsLog4NetLogger_WhenUseSpectreConsoleIsFalse()
        {
            LoggerFactory.UseSpectreConsole = false;
            var logger = LoggerFactory.GetLogger(typeof(LoggerFactoryDummyClass));
            Assert.IsNotInstanceOf<SpectreLogAdapter>(logger);
            Assert.IsInstanceOf<ILog>(logger);
        }

        [Test]
        public void GetLogger_MethodBase_ReturnsSpectreLogAdapter_WhenUseSpectreConsoleIsTrue()
        {
            LoggerFactory.UseSpectreConsole = true;
            MethodBase method = typeof(LoggerFactoryDummyClass).GetMethod(nameof(ToString));
            var logger = LoggerFactory.GetLogger(method);
            Assert.IsInstanceOf<SpectreLogAdapter>(logger);
        }

        [Test]
        public void GetLogger_MethodBase_ReturnsLog4NetLogger_WhenUseSpectreConsoleIsFalse()
        {
            LoggerFactory.UseSpectreConsole = false;
            MethodBase method = typeof(LoggerFactoryDummyClass).GetMethod(nameof(ToString));
            var logger = LoggerFactory.GetLogger(method);
            Assert.IsNotInstanceOf<SpectreLogAdapter>(logger);
            Assert.IsInstanceOf<ILog>(logger);
        }
    }
}
