using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using WinMan;

namespace FancyWM.Utilities
{
    internal interface IWindowMatcher
    {
        bool Matches(IWindow window);
    }

    internal class MatchHelpers
    {
        public static bool IsMatch(string input, string pattern)
        {
            if (string.Equals(input, pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (pattern.Length == 0)
            {
                return false;
            }

            try
            {
                return Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }

    internal class ByProcessNameMatcher(string processName) : IWindowMatcher
    {
        public string ProcessName { get; } = processName;

        public bool Matches(IWindow window)
        {
            return MatchHelpers.IsMatch(window.GetCachedProcessName(), ProcessName);
        }
    }

    internal class ByClassNameMatcher(string className) : IWindowMatcher
    {
        public string ClassName { get; } = className;

        public bool Matches(IWindow window)
        {
            return (window is WinMan.Windows.Win32Window w) && MatchHelpers.IsMatch(w.ClassName, ClassName);
        }
    }

    /// <summary>
    /// Matches windows satisfying all conditions of a rule such as
    /// <c>process=vivaldi &amp;&amp; title=^Bitwarden - Vivaldi$</c>.
    /// </summary>
    internal class CompositeWindowMatcher : IWindowMatcher
    {
        private const string ConditionSeparator = "&&";
        private const string ProcessKey = "process";
        private const string ClassKey = "class";
        private const string TitleKey = "title";

        private readonly IReadOnlyList<(string Key, string Pattern)> m_conditions;

        private CompositeWindowMatcher(IReadOnlyList<(string Key, string Pattern)> conditions)
        {
            m_conditions = conditions;
        }

        /// <summary>
        /// Builds a rule matching exactly this process name and window title.
        /// </summary>
        public static string CreateRule(string processName, string title)
        {
            return $"{ProcessKey}=^{EscapeLiteral(processName)}$ {ConditionSeparator} {TitleKey}=^{EscapeLiteral(title)}$";
        }

        private static string EscapeLiteral(string text)
        {
            // Regex.Escape also escapes spaces, which only matters with IgnorePatternWhitespace
            // and makes the generated rule hard to read.
            return Regex.Escape(text).Replace("\\ ", " ");
        }

        /// <summary>
        /// Parses a rule. Returns null if the rule is malformed, so that a typo
        /// disables the rule instead of turning it into one that matches too much.
        /// </summary>
        public static CompositeWindowMatcher? TryParse(string rule)
        {
            // ponytail: a pattern cannot contain "&&" itself; add escaping if anyone needs it.
            var conditions = new List<(string Key, string Pattern)>();
            foreach (var part in rule.Split(ConditionSeparator))
            {
                var separatorIndex = part.IndexOf('=');
                if (separatorIndex < 0)
                {
                    return null;
                }

                var key = part[..separatorIndex].Trim().ToLowerInvariant();
                var pattern = part[(separatorIndex + 1)..].Trim();
                if (pattern.Length == 0 || key is not (ProcessKey or ClassKey or TitleKey))
                {
                    return null;
                }
                conditions.Add((key, pattern));
            }
            return new CompositeWindowMatcher(conditions);
        }

        public bool Matches(IWindow window)
        {
            return m_conditions.All(condition => condition.Key switch
            {
                ProcessKey => MatchHelpers.IsMatch(window.GetCachedProcessName(), condition.Pattern),
                ClassKey => window is WinMan.Windows.Win32Window w && MatchHelpers.IsMatch(w.ClassName, condition.Pattern),
                TitleKey => MatchHelpers.IsMatch(window.Title, condition.Pattern),
                _ => false,
            });
        }
    }
}
