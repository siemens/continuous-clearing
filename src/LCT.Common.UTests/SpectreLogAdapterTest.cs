using NUnit.Framework;
using Moq;
using LCT.Common;
using log4net;
using log4net.Core;
using System;
using System.Globalization;

namespace LCT.Common.UTests
{
    [TestFixture]
    public class SpectreLogAdapterTests
    {
        private Mock<ILog> _mockFileLogger;
        private SpectreLogAdapter _adapter;

        [SetUp]
        public void SetUp()
        {
            _mockFileLogger = new Mock<ILog>();
            // Use reflection to inject the mock into the adapter
            _adapter = (SpectreLogAdapter)Activator.CreateInstance(
                typeof(SpectreLogAdapter),
                new object[] { "TestLogger" }
            );
            typeof(SpectreLogAdapter)
                .GetField("_fileLogger", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(_adapter, _mockFileLogger.Object);
        }

        [Test]
        public void Constructor_SetsLoggerName()
        {
            var adapter = new SpectreLogAdapter("LoggerName");
            Assert.IsNotNull(adapter.Logger);
            Assert.AreEqual("LoggerName", adapter.Logger.Name);
        }

        [Test]
        public void IsEnabledProperties_AlwaysTrue()
        {
            Assert.IsTrue(_adapter.IsDebugEnabled);
            Assert.IsTrue(_adapter.IsInfoEnabled);
            Assert.IsTrue(_adapter.IsWarnEnabled);
            Assert.IsTrue(_adapter.IsErrorEnabled);
            Assert.IsTrue(_adapter.IsFatalEnabled);
        }

        [Test]
        public void Debug_CallsFileLoggerDebug()
        {
            var msg = "debug";
            _adapter.Debug(msg);
            _mockFileLogger.Verify(x => x.Debug(msg), Times.Once);
        }

        [Test]
        public void Debug_WithException_CallsFileLoggerDebug()
        {
            var msg = "debug";
            var ex = new Exception("ex");
            _adapter.Debug(msg, ex);
            _mockFileLogger.Verify(x => x.Debug(msg), Times.Once);
        }

        [Test]
        public void Info_CallsFileLoggerInfo()
        {
            var msg = "info";
            _adapter.Info(msg);
            _mockFileLogger.Verify(x => x.Info(msg), Times.Once);
        }

        [Test]
        public void Info_WithException_CallsFileLoggerInfo()
        {
            var msg = "info";
            var ex = new Exception("ex");
            _adapter.Info(msg, ex);
            _mockFileLogger.Verify(x => x.Info(msg), Times.Once);
        }

        [Test]
        public void Warn_CallsFileLoggerWarn()
        {
            var msg = "warn";
            _adapter.Warn(msg);
            _mockFileLogger.Verify(x => x.Warn(msg), Times.Once);
        }

        [Test]
        public void Warn_WithException_CallsFileLoggerWarn()
        {
            var msg = "warn";
            var ex = new Exception("ex");
            _adapter.Warn(msg, ex);
            _mockFileLogger.Verify(x => x.Warn(msg), Times.Once);
        }

        [Test]
        public void Error_CallsFileLoggerError()
        {
            var msg = "error";
            _adapter.Error(msg);
            _mockFileLogger.Verify(x => x.Error(msg), Times.Once);
        }

        [Test]
        public void Error_WithException_CallsFileLoggerError()
        {
            var msg = "error";
            var ex = new Exception("ex");
            _adapter.Error(msg, ex);
            _mockFileLogger.Verify(x => x.Error(msg), Times.Once);
        }

        [Test]
        public void Fatal_CallsFileLoggerFatal()
        {
            var msg = "fatal";
            _adapter.Fatal(msg);
            _mockFileLogger.Verify(x => x.Fatal(msg), Times.Once);
        }

        [Test]
        public void Fatal_WithException_CallsFileLoggerFatal()
        {
            var msg = "fatal";
            var ex = new Exception("ex");
            _adapter.Fatal(msg, ex);
            _mockFileLogger.Verify(x => x.Fatal(msg), Times.Once);
        }

        // Format methods: test that they do not throw and call SpectreLogger.Log
        [Test]
        public void DebugFormat_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _adapter.DebugFormat("Debug {0}", "test"));
        }

        [Test]
        public void InfoFormat_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _adapter.InfoFormat("Info {0}", "test"));
        }

        [Test]
        public void WarnFormat_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _adapter.WarnFormat("Warn {0}", "test"));
        }

        [Test]
        public void ErrorFormat_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _adapter.ErrorFormat("Error {0}", "test"));
        }

        [Test]
        public void FatalFormat_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _adapter.FatalFormat("Fatal {0}", "test"));
        }
        [Test]
        public void DebugFormat_WithTwoArguments_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _adapter.DebugFormat("Debug {0} {1}", "foo", 123));
        }

        [Test]
        public void DebugFormat_WithThreeArguments_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _adapter.DebugFormat("Debug {0} {1} {2}", "foo", 123, "bar"));
        }

        [Test]
        public void DebugFormat_WithFormatProvider_DoesNotThrow()
        {
            var provider = CultureInfo.InvariantCulture;
            Assert.DoesNotThrow(() => _adapter.DebugFormat(provider, "Debug {0} {1}", new object[] { "foo", 123 }));
        }
        [Test]
        public void InfoFormat_WithTwoArguments_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _adapter.InfoFormat("Info {0} {1}", "foo", 123));
        }

        [Test]
        public void InfoFormat_WithThreeArguments_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _adapter.InfoFormat("Info {0} {1} {2}", "foo", 123, "bar"));
        }

        [Test]
        public void InfoFormat_WithFormatProvider_DoesNotThrow()
        {
            var provider = CultureInfo.InvariantCulture;
            Assert.DoesNotThrow(() => _adapter.InfoFormat(provider, "Info {0} {1}", new object[] { "foo", 123 }));
        }
    }
}

