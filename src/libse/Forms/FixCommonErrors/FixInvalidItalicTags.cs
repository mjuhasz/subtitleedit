using Nikse.SubtitleEdit.Core.Common;
using Nikse.SubtitleEdit.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nikse.SubtitleEdit.Core.Forms.FixCommonErrors
{
    public class FixInvalidItalicTags : IFixCommonError
    {
        public static class Language
        {
            public static string FixInvalidItalicTag { get; set; } = "Fix invalid italic tag";
            public static string FixInvalidItalicTags { get; set; } = "Fix invalid italic tags";
        }

        private const string BeginTag = "<i>";
        private const string EndTag = "</i>";

        public void Fix(Subtitle subtitle, IFixCallbacks callbacks)
        {
            string fixAction = Language.FixInvalidItalicTag;
            int noOfInvalidHtmlTags = 0;
            for (int i = 0; i < subtitle.Paragraphs.Count; i++)
            {
                if (callbacks.AllowFix(subtitle.Paragraphs[i], fixAction))
                {
                    var oldText = subtitle.Paragraphs[i].Text;
                    var text = ItalicizeSongLyrics(HtmlUtil.FixInvalidItalicTags(oldText));
                    if (text != oldText)
                    {
                        subtitle.Paragraphs[i].Text = text;
                        noOfInvalidHtmlTags++;
                        callbacks.AddFixToListView(subtitle.Paragraphs[i], fixAction, oldText, text);
                    }
                }
            }

            callbacks.UpdateFixStatus(noOfInvalidHtmlTags, Language.FixInvalidItalicTags);
        }

        /// <summary>
        /// Puts song lyrics (text starting or ending with ♪/♫) in italics with the music symbols inside the italics,
        /// e.g. "♪ <i>It's your birthday</i> ♪" becomes "<i>♪ It's your birthday ♪</i>".
        /// Dialog dashes and leading [HI]/(HI) labels are kept outside the italics, and each dialog turn is handled separately.
        /// Lyrics with partial italics (emphasis) are left untouched.
        /// </summary>
        public static string ItalicizeSongLyrics(string input)
        {
            if (string.IsNullOrEmpty(input) || input.IndexOfAny(MusicSymbols) < 0)
            {
                return input;
            }

            var text = input;
            var preTags = string.Empty;
            if (text.StartsWith("{\\", StringComparison.Ordinal))
            {
                var endIdx = text.IndexOf('}', 2);
                if (endIdx > 2)
                {
                    preTags = text.Substring(0, endIdx + 1);
                    text = text.Remove(0, endIdx + 1);
                }
            }

            if (!IsItalicBalanced(text))
            {
                return input;
            }

            var lines = text.SplitToLines();
            var isDialog = lines.Exists(l => HtmlUtil.RemoveHtmlTags(l, true).TrimStart().StartsWith('-'));
            var segments = new List<string>();
            if (isDialog)
            {
                var sb = new StringBuilder();
                foreach (var line in lines)
                {
                    if (sb.Length > 0 && HtmlUtil.RemoveHtmlTags(line, true).TrimStart().StartsWith('-'))
                    {
                        segments.Add(sb.ToString());
                        sb.Clear();
                    }

                    if (sb.Length > 0)
                    {
                        sb.Append(Environment.NewLine);
                    }

                    sb.Append(line);
                }

                segments.Add(sb.ToString());
            }
            else
            {
                segments.Add(string.Join(Environment.NewLine, lines));
            }

            var italicOpen = false;
            var changed = false;
            for (var i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                var fixedSegment = ItalicizeSegment(segment, italicOpen);
                italicOpen = ItalicStateAfter(segment, italicOpen);
                if (fixedSegment != segment)
                {
                    segments[i] = fixedSegment;
                    changed = true;
                }
            }

            if (!changed)
            {
                return input;
            }

            var result = string.Join(Environment.NewLine, segments);
            if (!IsItalicBalanced(result))
            {
                return input;
            }

            return preTags + result;
        }

        private static readonly char[] MusicSymbols = { '♪', '♫' };

        private static string ItalicizeSegment(string segment, bool italicOpen)
        {
            var pos = 0;
            var prefix = new StringBuilder();

            SkipItalicTags(segment, ref pos);
            if (pos < segment.Length && segment[pos] == '-')
            {
                var dashStart = pos;
                pos++;
                SkipSpaces(segment, ref pos);
                prefix.Append(segment, dashStart, pos - dashStart);
            }

            // leading HI labels, e.g. "[singing] " or "(singing) "
            while (true)
            {
                var labelStart = pos;
                SkipItalicTags(segment, ref labelStart);
                if (labelStart >= segment.Length || (segment[labelStart] != '[' && segment[labelStart] != '('))
                {
                    break;
                }

                var close = segment.IndexOf(segment[labelStart] == '[' ? ']' : ')', labelStart + 1);
                if (close < 0)
                {
                    break;
                }

                prefix.Append(RemoveItalicTags(segment.Substring(labelStart, close - labelStart + 1)));
                pos = close + 1;
                SkipItalicTags(segment, ref pos);
                var whitespaceStart = pos;
                while (pos < segment.Length && char.IsWhiteSpace(segment[pos]))
                {
                    pos++;
                }

                prefix.Append(segment.IndexOf('\n', whitespaceStart, pos - whitespaceStart) >= 0 ? Environment.NewLine : " ");
            }

            var body = segment.Substring(pos);
            var plain = HtmlUtil.RemoveHtmlTags(body, true).Trim();
            if (plain.Length == 0 ||
                (plain.IndexOfAny(MusicSymbols) != 0 && plain.LastIndexOfAny(MusicSymbols) != plain.Length - 1) ||
                plain.Contains('[') ||
                !HasLetterOrDigit(plain))
            {
                return segment;
            }

            // italic state at the start of the body (italic tags in the prefix count too)
            var state = ItalicStateAfter(segment.Substring(0, pos), italicOpen);
            CountLettersByItalic(body, state, out var italicCount, out var nonItalicCount);
            if (italicCount > 0 && nonItalicCount > 0)
            {
                return segment; // partial italics - keep emphasis as is
            }

            var bodyLines = RemoveItalicTags(body).SplitToLines();
            for (var i = 0; i < bodyLines.Count; i++)
            {
                bodyLines[i] = bodyLines[i].Trim(' ');
            }

            var newBody = string.Join(Environment.NewLine, bodyLines).Trim();
            return prefix + BeginTag + newBody + EndTag;
        }

        private static void SkipItalicTags(string s, ref int pos)
        {
            while (true)
            {
                if (string.CompareOrdinal(s, pos, BeginTag, 0, BeginTag.Length) == 0)
                {
                    pos += BeginTag.Length;
                }
                else if (string.CompareOrdinal(s, pos, EndTag, 0, EndTag.Length) == 0)
                {
                    pos += EndTag.Length;
                }
                else
                {
                    return;
                }
            }
        }

        private static void SkipSpaces(string s, ref int pos)
        {
            while (pos < s.Length && s[pos] == ' ')
            {
                pos++;
            }
        }

        private static string RemoveItalicTags(string s) => s.Replace(BeginTag, string.Empty).Replace(EndTag, string.Empty);

        private static bool HasLetterOrDigit(string s)
        {
            foreach (var ch in s)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ItalicStateAfter(string s, bool italicOpen)
        {
            CountLettersByItalic(s, italicOpen, out _, out _, out var state);
            return state;
        }

        private static void CountLettersByItalic(string s, bool italicOpen, out int italicCount, out int nonItalicCount)
        {
            CountLettersByItalic(s, italicOpen, out italicCount, out nonItalicCount, out _);
        }

        private static void CountLettersByItalic(string s, bool italicOpen, out int italicCount, out int nonItalicCount, out bool endState)
        {
            italicCount = 0;
            nonItalicCount = 0;
            var state = italicOpen;
            var i = 0;
            while (i < s.Length)
            {
                if (string.CompareOrdinal(s, i, BeginTag, 0, BeginTag.Length) == 0)
                {
                    state = true;
                    i += BeginTag.Length;
                    continue;
                }

                if (string.CompareOrdinal(s, i, EndTag, 0, EndTag.Length) == 0)
                {
                    state = false;
                    i += EndTag.Length;
                    continue;
                }

                var ch = s[i];
                if (ch == '<')
                {
                    var close = s.IndexOf('>', i + 1);
                    if (close > i)
                    {
                        i = close + 1;
                        continue;
                    }
                }

                if (char.IsLetterOrDigit(ch))
                {
                    if (state)
                    {
                        italicCount++;
                    }
                    else
                    {
                        nonItalicCount++;
                    }
                }

                i++;
            }

            endState = state;
        }

        private static bool IsItalicBalanced(string s)
        {
            var depth = 0;
            var i = 0;
            while (i < s.Length)
            {
                if (string.CompareOrdinal(s, i, BeginTag, 0, BeginTag.Length) == 0)
                {
                    depth++;
                    if (depth > 1)
                    {
                        return false;
                    }

                    i += BeginTag.Length;
                }
                else if (string.CompareOrdinal(s, i, EndTag, 0, EndTag.Length) == 0)
                {
                    depth--;
                    if (depth < 0)
                    {
                        return false;
                    }

                    i += EndTag.Length;
                }
                else
                {
                    i++;
                }
            }

            return depth == 0;
        }
    }
}
