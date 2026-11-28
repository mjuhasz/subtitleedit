using Nikse.SubtitleEdit.Core.Common;
using Nikse.SubtitleEdit.Core.Interfaces;
using System;

namespace Nikse.SubtitleEdit.Core.Forms.FixCommonErrors
{
    /// <summary>
    /// Replaces three dots with a single Unicode ellipsis character.
    /// Kept separate from <see cref="FixUnneededPeriods"/> so it can be toggled on its own.
    /// </summary>
    public class FixEllipsisStyle : IFixCommonError
    {
        public static class Language
        {
            public static string EllipsisStyle { get; set; } = "Ellipsis style";
            public static string UseUnicodeEllipsis { get; set; } = "Use … instead of ...";
        }

        public void Fix(Subtitle subtitle, IFixCallbacks callbacks)
        {
            if (callbacks.Language == "zh")
            {
                callbacks.UpdateFixStatus(0, Language.UseUnicodeEllipsis);
                return;
            }

            var fixAction = Language.EllipsisStyle;
            var fixedCount = 0;
            for (var i = 0; i < subtitle.Paragraphs.Count; i++)
            {
                var p = subtitle.Paragraphs[i];
                if (!p.Text.Contains("...", StringComparison.Ordinal) || !callbacks.AllowFix(p, fixAction))
                {
                    continue;
                }

                var oldText = p.Text;
                p.Text = oldText.Replace("...", "…");
                fixedCount++;
                callbacks.AddFixToListView(p, fixAction, oldText, p.Text);
            }

            callbacks.UpdateFixStatus(fixedCount, Language.UseUnicodeEllipsis);
        }
    }
}
