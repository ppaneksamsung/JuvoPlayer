using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JuvoPlayer.Common.Logging
{
    public class ConfigParser
    {
        public Dictionary<string, LogLevel> LoggingLevels { get; } 

        public ConfigParser(string contents)
        {
            LoggingLevels = new Dictionary<string, LogLevel>();

            using (var reader = new StringReader(contents))
            {
                string line = null;
                while ((line = reader.ReadLine()) != null)
                {
                    var splitLine = line.Split('=');
                    if (splitLine.Length != 2)
                        continue;
                    var channel = splitLine[0];
                    var levelString = splitLine[1];

                    if (Enum.TryParse(levelString, true, out LogLevel level)) LoggingLevels[channel] = level;
                }
            }
        }
    }
}
