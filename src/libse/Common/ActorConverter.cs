using Nikse.SubtitleEdit.Core.Dictionaries;
using Nikse.SubtitleEdit.Core.SubtitleFormats;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Nikse.SubtitleEdit.Core.Common
{
    public class ActorConverter
    {
        public const int NormalCase = 0;
        public const int UpperCase = 1;
        public const int LowerCase = 2;
        public const int ProperCase = 3;

        private SubtitleFormat _subtitleFormat;
        private string _languageCode;

        private NameList _namesList;
        private List<string> _nameListInclMulti;

        public bool ToSquare { get; set; }
        public bool ToParentheses { get; set; }
        public bool ToColon { get; set; }
        public bool ToActor { get; set; }


        public ActorConverter(SubtitleFormat subtitleFormat, string languageCode)
        {
            _subtitleFormat = subtitleFormat;
            _languageCode = languageCode;
            _namesList = new NameList(Configuration.DictionariesDirectory, languageCode, Configuration.Settings.WordLists.UseOnlineNames, Configuration.Settings.WordLists.NamesUrl);
            _nameListInclMulti = _namesList.GetAllNames();
        }

        public string FixActorsFromActor(Paragraph p, int? changeCasing, Color? color)
        {
            var actor = p.Actor;
            if (changeCasing.HasValue)
            {
                actor = SetCasing(_subtitleFormat, changeCasing, actor);
            }

            if (ToSquare)
            {
                actor = "[" + actor + "]";
            }
            else if (ToParentheses)
            {
                actor = "(" + actor + ")";
            }
            else if (ToColon)
            {
                actor = actor + ":";
            }
            else if (ToActor)
            {
                return p.Text;
            }

            if (color.HasValue && !ToActor)
            {
                actor = SetColor(_subtitleFormat, color.Value, actor);
            }

            p.Text = actor + " " + p.Text.TrimStart(' ');

            return p.Text;
        }

        public string FixActorsFromBeforeColon(Paragraph p, char ch, int? changeCasing, Color? color)
        {
            var lines = p.Text.SplitToLines();

            // Check if any line has a leading dialog hyphen (after stripping positioning/italic tags)
            var hasDialogHyphen = false;
            if (lines.Count > 1)
            {
                foreach (var line in lines)
                {
                    var stripped = line.Trim();
                    stripped = Regex.Replace(stripped, @"^\{\\an\d+\}", string.Empty);
                    if (stripped.StartsWith("<i>", StringComparison.Ordinal))
                    {
                        stripped = stripped.Substring(3);
                    }

                    if (stripped.StartsWith("-", StringComparison.Ordinal))
                    {
                        hasDialogHyphen = true;
                        break;
                    }
                }
            }

            // Also treat as dialog when multiple lines have actors
            if (!hasDialogHyphen && lines.Count > 1)
            {
                var actorLineCount = 0;
                foreach (var line in lines)
                {
                    var stripped = line.Trim();
                    stripped = Regex.Replace(stripped, @"^\{\\an\d+\}", string.Empty);
                    if (stripped.StartsWith("<i>", StringComparison.Ordinal))
                    {
                        stripped = stripped.Substring(3);
                    }

                    stripped = stripped.TrimStart(' ', '-');
                    if (stripped.IndexOf(ch) > 0)
                    {
                        actorLineCount++;
                    }
                }

                if (actorLineCount > 1)
                {
                    hasDialogHyphen = true;
                }
            }

            var sb = new StringBuilder();
            foreach (var line in lines)
            {
                var s = line.Trim();

                // Extract positioning tag prefix like {\an8}
                var posTag = string.Empty;
                var posMatch = Regex.Match(s, @"^\{\\an\d+\}");
                if (posMatch.Success)
                {
                    posTag = posMatch.Value;
                    s = s.Substring(posTag.Length);
                }

                // Extract italic wrapper
                var hasItalic = s.StartsWith("<i>", StringComparison.Ordinal) &&
                                s.EndsWith("</i>", StringComparison.Ordinal);
                if (hasItalic)
                {
                    s = s.Substring(3, s.Length - 7);
                }

                var startIdx = s.IndexOf(ch);
                if (startIdx > 0)
                {
                    var actor = s.Substring(0, startIdx).Trim(' ', '-', '"');
                    if (changeCasing.HasValue)
                    {
                        actor = SetCasing(_subtitleFormat, changeCasing, actor);
                    }

                    if (ToSquare)
                    {
                        actor = "[" + actor + "]";
                    }
                    else if (ToParentheses)
                    {
                        actor = "(" + actor + ")";
                    }
                    else if (ToColon)
                    {
                        actor = actor + ":";
                    }

                    if (color.HasValue && !ToActor)
                    {
                        actor = SetColor(_subtitleFormat, color.Value, actor);
                    }

                    var rest = s.Substring(startIdx + 1).TrimStart(' ');
                    if (hasItalic)
                    {
                        rest = "<i>" + rest + "</i>";
                    }

                    var dialogHyphen = hasDialogHyphen ? "-" : string.Empty;

                    if (ToActor)
                    {
                        s = posTag + rest;
                    }
                    else
                    {
                        s = posTag + dialogHyphen + actor + " " + rest;
                    }
                }
                else
                {
                    // No actor on this line - reconstruct with positioning tag and italic
                    if (hasItalic)
                    {
                        s = posTag + "<i>" + s + "</i>";
                    }
                    else
                    {
                        s = posTag + s;
                    }
                }

                sb.AppendLine(s);
            }

            return sb.ToString().Trim();
        }

        public ActorConverterResult FixActors(Paragraph paragraph, char start, char end, int? changeCasing, Color? color)
        {
            var p = new Paragraph(paragraph, false);
            Paragraph nextParagraph = null;
            var lines = p.Text.SplitToLines();
            if (lines.Count > 2)
            {
                return new ActorConverterResult { Paragraph = paragraph, Skip = true };
            }

            var lineIdx = 0;
            p.Text = string.Empty;
            var selectFix = true;
            foreach (var line in lines)
            {
                var s = line;
                var startIdx = s.IndexOf(start);
                var endIdx = s.IndexOf(end);
                if (startIdx != -1 && endIdx != -1)
                {
                    if (endIdx < startIdx)
                    {
                        break;
                    }

                    var actor = s.Substring(startIdx + 1, endIdx - startIdx - 1).Trim(' ', '-', '"');
                    selectFix = IsActor(actor);
                    if (changeCasing.HasValue)
                    {
                        actor = SetCasing(_subtitleFormat, changeCasing, actor);
                    }

                    if (ToSquare)
                    {
                        actor = "[" + actor + "]";
                    }
                    else if (ToParentheses)
                    {
                        actor = "(" + actor + ")";
                    }
                    else if (ToColon)
                    {
                        actor = actor + ":";
                    }
                    else if (ToActor)
                    {
                        s = s.Substring(0, startIdx) + s.Substring(endIdx + 1).Trim();
                    }

                    if (color.HasValue && !ToActor)
                    {
                        actor = SetColor(_subtitleFormat, color.Value, actor);
                    }

                    if (ToSquare)
                    {
                        s = s.Substring(0, startIdx) + actor + " " + s.Substring(endIdx + 1).TrimStart(' ');
                    }
                    else if (ToParentheses)
                    {
                        s = s.Substring(0, startIdx) + actor + " " + s.Substring(endIdx + 1).TrimStart(' ');
                    }
                    else if (ToColon)
                    {
                        s = s.Substring(0, startIdx) + actor + " " + s.Substring(endIdx + 1).TrimStart(' ');
                    }

                    if (lineIdx == 0)
                    {
                        if (ToActor)
                        {
                            p.Actor = actor;
                        }

                        p.Text = s;
                    }
                    else if (lineIdx == 1 && ToActor)
                    {
                        nextParagraph = new Paragraph(p);
                        nextParagraph.Text = s.Trim();
                        nextParagraph.Actor = actor;
                    }
                    else if (lineIdx == 1)
                    {
                        p.Text += Environment.NewLine + s.Trim();
                    }

                }
                else
                {
                    p.Text = (p.Text + Environment.NewLine + s).Trim();
                }

                lineIdx++;
            }

            return new ActorConverterResult
            {
                Paragraph = p,
                NextParagraph = nextParagraph,
                Selected = selectFix,
            };
        }

        private static string SetCasing(SubtitleFormat format, int? changeCasing, string actor)
        {
            switch (changeCasing.Value)
            {
                case NormalCase:
                    actor = actor.ToLower().CapitalizeFirstLetter();
                    break;
                case UpperCase:
                    actor = actor.ToUpper();
                    break;
                case LowerCase:
                    actor = actor.ToLower();
                    break;
                case ProperCase:
                    actor = actor.ToProperCase(format);
                    break;
            }

            return actor;
        }

        private static string SetColor(SubtitleFormat format, Color color, string actor)
        {
            if (format.FriendlyName == AdvancedSubStationAlpha.NameOfFormat)
            {
                actor = "{\\" + AdvancedSubStationAlpha.GetSsaColorStringForEvent(color, "c") + "}" + actor + "{\\c}";
            }
            else
            {
                actor = "<font color=\"" + Settings.Settings.ToHtml(color) + "\">" + actor + "</font>";
            }

            return actor;
        }

        private bool IsActor(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return false;
            }

            var words = s.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
            {
                return false;
            }

            if (_nameListInclMulti.Contains(s, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }

            foreach (var word in words)
            {
                if (word.Length < 2)
                {
                    return false;
                }

                if (word.Any(c => char.IsDigit(c) || (!char.IsLetter(c) && c != '-' && c != '\'')))
                {
                    return false;
                }

                var commonTitles = new[] { "Mr.", "Mrs.", "Dr.", };
                if (commonTitles.Contains(word))
                {
                    continue;
                }

                if (!_nameListInclMulti.Contains(word, StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
