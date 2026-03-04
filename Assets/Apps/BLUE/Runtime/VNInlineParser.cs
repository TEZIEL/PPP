using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PPP.BLUE.VN
{
    public class VNInlineToken
    {
        public bool isCommand;
        public string name;
        public string arg;
        public string text;
    }

    public static class VNInlineParser
    {
        static Regex cmdRegex = new Regex(@"\[(.*?)\]", RegexOptions.Compiled);

        public static List<VNInlineToken> Parse(string input)
        {
            List<VNInlineToken> tokens = new List<VNInlineToken>();

            int lastIndex = 0;

            foreach (Match m in cmdRegex.Matches(input))
            {
                if (m.Index > lastIndex)
                {
                    tokens.Add(new VNInlineToken
                    {
                        isCommand = false,
                        text = input.Substring(lastIndex, m.Index - lastIndex)
                    });
                }

                string raw = m.Groups[1].Value;
                string name = raw;
                string arg = null;

                int colon = raw.IndexOf(':');
                if (colon >= 0)
                {
                    name = raw.Substring(0, colon);
                    arg = raw.Substring(colon + 1);
                }

                tokens.Add(new VNInlineToken
                {
                    isCommand = true,
                    name = name.ToLower(),
                    arg = arg
                });

                lastIndex = m.Index + m.Length;
            }

            if (lastIndex < input.Length)
            {
                tokens.Add(new VNInlineToken
                {
                    isCommand = false,
                    text = input.Substring(lastIndex)
                });
            }

            return tokens;
        }
    }
}