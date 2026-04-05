using System;
using System.Collections.Generic;
using System.IO;

namespace NostalgiaMenu.Parsers
{
    public static class IniParser
    {
        public static Dictionary<string, Dictionary<string, string>> Parse(string filePath)
        {
            var result = new Dictionary<string, Dictionary<string, string>>(
                StringComparer.OrdinalIgnoreCase);

            if (!File.Exists(filePath))
                return result;

            string currentSection = null;

            foreach (string rawLine in File.ReadLines(filePath))
            {
                string line = rawLine.Trim();

                if (string.IsNullOrEmpty(line) || line[0] == ';' || line[0] == '#')
                    continue;

                if (line[0] == '[')
                {
                    int close = line.IndexOf(']');
                    if (close > 1)
                    {
                        currentSection = line.Substring(1, close - 1).Trim();
                        if (!result.ContainsKey(currentSection))
                            result[currentSection] = new Dictionary<string, string>(
                                StringComparer.OrdinalIgnoreCase);
                    }
                    continue;
                }

                if (currentSection == null) continue;

                int eq = line.IndexOf('=');
                if (eq < 1) continue;

                string key   = line.Substring(0, eq).Trim();
                string value = line.Substring(eq + 1).Trim();

                int commentPos = FindInlineComment(value);
                if (commentPos >= 0)
                    value = value.Substring(0, commentPos).TrimEnd();

                result[currentSection][key] = value;
            }

            return result;
        }

        // Only strip ; or # preceded by whitespace to avoid truncating Windows paths.
        private static int FindInlineComment(string value)
        {
            for (int i = 1; i < value.Length; i++)
            {
                if ((value[i] == ';' || value[i] == '#') && char.IsWhiteSpace(value[i - 1]))
                    return i;
            }
            return -1;
        }
    }
}
