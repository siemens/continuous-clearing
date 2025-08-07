using log4net;
using System.Reflection;

namespace LCT.Common
{
    public static class LoggerFactory
    {
        // This property determines whether to use Spectre.Console for logging or log4net.
        public static bool UseSpectreConsole { get; set; } = false;

        public static ILog GetLogger(System.Type type)
        {
            if (UseSpectreConsole)
                return new SpectreLogAdapter(type.FullName);
            else
                return LogManager.GetLogger(type);
        }

        public static ILog GetLogger(MethodBase method)
        {
            if (UseSpectreConsole)
                return new SpectreLogAdapter(method.DeclaringType?.FullName);
            else
                return LogManager.GetLogger(method.DeclaringType);
        }
    }
}