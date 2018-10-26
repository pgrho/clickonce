using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

[assembly: InternalsVisibleTo("Shipwreck.ClickOnce.Manifest.Tests")]

namespace Shipwreck.ClickOnce.Manifest
{
    internal struct Minimatch
    {
        internal readonly bool Result;
        internal readonly Regex Regex;

        private Minimatch(bool result, Regex regex)
        {
            Result = result;
            Regex = regex;
        }

        public bool? IsMatch(string s)
            => s != null && Regex?.IsMatch(s) == true ? Result : (bool?)null;

        internal static Minimatch Create(string pattern)
        {
            if (string.IsNullOrEmpty(pattern) || pattern[0] == '#')
            {
                // Never match
                return new Minimatch();
            }

            var result = true;
            if (pattern[0] == '!')
            {
                result = false;
                pattern = pattern.Substring(1);
            }
            else if (pattern.StartsWith("\\#"))
            {
                pattern = pattern.Substring(1);
            }

            // +(|)
            // *(|)
            // ?(|)
            // @(|)
            // !(|)

            // {foo|bar}
            // {m..n}

            // ** -> [^/]+(/[^/]+)*
            // * -> [^/]+
            // ? -> [^/]
            var sb = new StringBuilder(pattern.Length * 2);
            sb.Append("^");

            var last = 0;
            foreach (Match m in Regex.Matches(pattern, @"(\*\*\/\*|\*\*?|\?)"))
            {
                if (last < m.Index)
                {
                    sb.Append(Regex.Escape(pattern.Substring(last, m.Index - last)));
                }

                switch (m.Value)
                {
                    case "**/*":
                        sb.Append(".*");
                        break;

                    case "**":
                        sb.Append("([^/]+(/[^/]+)*)?");
                        break;

                    case "*":
                        sb.Append("[^/]+");
                        break;

                    default:
                        sb.Append("[^/]");
                        break;
                }

                last = m.Index + m.Length;
            }

            if (last < pattern.Length)
            {
                sb.Append(Regex.Escape(pattern.Substring(last)));
            }

            sb.Append('$');

            return new Minimatch(result, new Regex(sb.ToString(), RegexOptions.IgnoreCase));
        }

        internal static Func<string, bool> Compile(IEnumerable<string> patterns)
        {
            var mms = patterns.Select(Create).ToArray();

            return s =>
            {
                bool? r = null;
                foreach (var mm in mms)
                {
                    r = mm.IsMatch(s) ?? r;
                }
                return r ?? false;
            };
        }

        public static Func<string, bool?> Compile(string pattern)
            => Create(pattern).IsMatch;
    }
}