using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace R3MUS.Devpack.Jabber
{
    public class Censor
    {
        public IList<string> CensoredWords { get; private set; }

        public Censor()
        {
            CensoredWords = Properties.Settings.Default.CensoredWords.Cast<string>().ToList();
        }

        public string CensorText(string text)
        {
            if (text == null)
                throw new ArgumentNullException("text");
            string censoredText = text;

            foreach (string censoredWord in CensoredWords)
            {
                string regularExpression = ToRegexPattern(censoredWord);

                censoredText = Regex.Replace(censoredText, regularExpression, StarCensoredMatch,
                  RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }
            return censoredText;
        }

        private static string StarCensoredMatch(Match m)
        {
            string word = m.Captures[0].Value;

            return new string('*', word.Length);
        }

        private string ToRegexPattern(string wildcardSearch)
        {
            string regexPattern = Regex.Escape(wildcardSearch);
            regexPattern = regexPattern.Replace(@"\*", ".*?");
            regexPattern = regexPattern.Replace(@"\?", ".");

            if (regexPattern.StartsWith(".*?"))
            {
                regexPattern = regexPattern.Substring(3);
                regexPattern = @"(^\b)*?" + regexPattern;
            }
            regexPattern = @"\b" + regexPattern + @"\b";

            return regexPattern;
        }
    }
}
