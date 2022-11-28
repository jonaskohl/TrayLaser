using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LaserPointer
{
    internal static class AppMeta
    {
        public static string USER_AGENT_STRING
        {
            get
            {
                var asm = Assembly.GetExecutingAssembly();
                var appname = asm?.GetName().Name ?? "<UNKNOWN>";
                var appver = GetCurrentAppVersion()?.ToString() ?? "<UNKNOWN>";

                return $"{appname}/{appver} Windows/{Environment.OSVersion.Version}";
            }
        }

        public static Version? GetCurrentAppVersion()
        {
            return Assembly.GetExecutingAssembly()?.GetName()?.Version;
        }
    }
}
