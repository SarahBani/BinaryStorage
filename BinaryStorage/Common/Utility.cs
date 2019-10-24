using System;
using System.Configuration;

namespace BinStorage.Common
{
    public static class Utility
    {

        #region Properties

        private const int ConsoleLineLength = 75;

        private const int ConsolePadLeftLength = 15;

        #endregion /Properties

        #region Methods

        public static string GetAppSetting(string key)
        {
            if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings[key]))
            {
                return ConfigurationManager.AppSettings[key].ToString();
            }
            else
            {
                return string.Empty;
            }
        }

        public static void ConsoleWriteLine(string text)
        {
            Console.WriteLine((new String('-', ConsolePadLeftLength) + text).PadRight(ConsoleLineLength, '-'));
        }

        #endregion /Methods

    }
}
