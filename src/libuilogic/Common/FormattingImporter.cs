using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Nikse.SubtitleEdit.UiLogic.Common
{
    /// <summary>
    /// "Import formatting": copies the formatting tags (italic, bold, underline, font and
    /// ASSA override blocks) of a line from another subtitle onto the text of the current line.
    /// Tags are placed by aligning the two texts character by character, so partial formatting
    /// like "- &lt;i&gt;What?&lt;/i&gt;" or "[Al] &lt;i&gt;Hi.&lt;/i&gt;" carries over, and small text
    /// differences (fixed typos, other line breaks) between the two files are tolerated.
    /// </summary>
    public static class FormattingImporter
    {
        private static readonly Regex HtmlTagRegex = new Regex(@"</?(?:i|b|u|font)(?:\s[^>]*)?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex AssaTagRegex = new Regex(@"\{\\[^}]*\}", RegexOptions.Compiled);
        private static readonly Regex TagRegex = new Regex(HtmlTagRegex + "|" + AssaTagRegex, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex EmptyTagPairRegex = new Regex(@"<(i|b|u)>(\s*)</\1>|<font(?:\s[^>]*)?>(\s*)</font>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Returns <paramref name="currentText"/> with the formatting of <paramref name="importText"/>.
        /// If the import text has no formatting, the current text is returned unchanged. Existing
        /// html formatting (or ASSA override blocks) in the current text is replaced when the
        /// import text has formatting of that kind.
        /// </summary>
        public static string Apply(string importText, string currentText)
        {
            if (string.IsNullOrEmpty(importText) || string.IsNullOrEmpty(currentText))
            {
                return currentText ?? string.Empty;
            }

            var tags = new List<(int Position, string Tag)>();
            var importPlain = new StringBuilder();
            var last = 0;
            foreach (Match m in TagRegex.Matches(importText))
            {
                importPlain.Append(importText, last, m.Index - last);
                tags.Add((importPlain.Length, m.Value));
                last = m.Index + m.Length;
            }

            if (tags.Count == 0)
            {
                return currentText ?? string.Empty;
            }

            importPlain.Append(importText, last, importText.Length - last);

            var current = currentText;
            if (tags.Any(t => t.Tag.StartsWith("<", StringComparison.Ordinal)))
            {
                current = HtmlTagRegex.Replace(current, string.Empty);
            }

            if (tags.Any(t => t.Tag.StartsWith("{", StringComparison.Ordinal)))
            {
                current = AssaTagRegex.Replace(current, string.Empty);
            }

            var map = Align(importPlain.ToString(), current);
            var result = new StringBuilder(current.Length + importText.Length);
            var previousPosition = 0;
            foreach (var (position, tag) in tags)
            {
                var target = IsClosingTag(tag)
                    ? PositionAfterPreviousMatch(map, position)
                    : PositionBeforeNextMatch(map, position, current.Length);

                // Keep the tag order of the import text, even where the texts differ.
                target = Math.Max(target, previousPosition);
                result.Append(current, previousPosition, target - previousPosition);
                result.Append(tag);
                previousPosition = target;
            }

            result.Append(current, previousPosition, current.Length - previousPosition);

            // Tags around text that only exists in the import file end up empty.
            return EmptyTagPairRegex.Replace(result.ToString(), "$2$3");
        }

        private static bool IsClosingTag(string tag)
        {
            return tag.StartsWith("</", StringComparison.Ordinal);
        }

        /// <summary>
        /// Opening tags stick to the first character they format.
        /// </summary>
        private static int PositionBeforeNextMatch(int[] map, int position, int currentLength)
        {
            for (var i = position; i < map.Length; i++)
            {
                if (map[i] >= 0)
                {
                    return map[i];
                }
            }

            return currentLength;
        }

        /// <summary>
        /// Closing tags stick to the last character they format.
        /// </summary>
        private static int PositionAfterPreviousMatch(int[] map, int position)
        {
            for (var i = position - 1; i >= 0; i--)
            {
                if (map[i] >= 0)
                {
                    return map[i] + 1;
                }
            }

            return 0;
        }

        /// <summary>
        /// Longest common subsequence alignment (case-insensitive, all whitespace equal).
        /// Returns, for each character of <paramref name="a"/>, the index of the matching
        /// character in <paramref name="b"/>, or -1.
        /// </summary>
        private static int[] Align(string a, string b)
        {
            var lcs = new int[a.Length + 1, b.Length + 1];
            for (var i = a.Length - 1; i >= 0; i--)
            {
                for (var j = b.Length - 1; j >= 0; j--)
                {
                    lcs[i, j] = CharEquals(a[i], b[j])
                        ? lcs[i + 1, j + 1] + 1
                        : Math.Max(lcs[i + 1, j], lcs[i, j + 1]);
                }
            }

            var map = new int[a.Length];
            for (var i = 0; i < map.Length; i++)
            {
                map[i] = -1;
            }

            var x = 0;
            var y = 0;
            while (x < a.Length && y < b.Length)
            {
                if (CharEquals(a[x], b[y]))
                {
                    map[x++] = y++;
                }
                else if (lcs[x + 1, y] >= lcs[x, y + 1])
                {
                    x++;
                }
                else
                {
                    y++;
                }
            }

            return map;
        }

        private static bool CharEquals(char a, char b)
        {
            return char.IsWhiteSpace(a)
                ? char.IsWhiteSpace(b)
                : char.ToUpperInvariant(a) == char.ToUpperInvariant(b);
        }
    }
}
