using Nikse.SubtitleEdit.Core.Common;
using Nikse.SubtitleEdit.Core.Forms.FixCommonErrors;
using Nikse.SubtitleEdit.Core.Interfaces;
using Nikse.SubtitleEdit.Core.SubtitleFormats;
using System.Collections.Generic;
using System.Text;

namespace LibSETests.Core;

public class FixEllipsisStyleTest
{
    private sealed class FakeCallbacks : IFixCallbacks
    {
        public string Language { get; set; } = "en";
        public SubtitleFormat Format => new SubRip();
        public Encoding Encoding => Encoding.UTF8;
        public bool AllowFix(Paragraph p, string action) => true;
        public void AddFixToListView(Paragraph p, string action, string before, string after) { }
        public void AddFixToListView(Paragraph p, string action, string before, string after, bool isChecked) { }
        public void LogStatus(string sender, string message) { }
        public void LogStatus(string sender, string message, bool isImportant) { }
        public void UpdateFixStatus(int count, string description) { }
        public bool IsName(string candidate) => false;
        public HashSet<string> GetAbbreviations() => new HashSet<string>();
        public void AddToTotalErrors(int count) { }
        public void AddToDeleteIndices(int index) { }
    }

    private static string Run(IFixCommonError fixer, string text, string lang = "en")
    {
        var subtitle = new Subtitle();
        subtitle.Paragraphs.Add(new Paragraph { Text = text });
        fixer.Fix(subtitle, new FakeCallbacks { Language = lang });
        return subtitle.Paragraphs[0].Text;
    }

    [Fact]
    public void ThreeDots_BecomeUnicodeEllipsis()
    {
        Assert.Equal("Wait…", Run(new FixEllipsisStyle(), "Wait..."));
    }

    [Fact]
    public void Chinese_IsUntouched()
    {
        Assert.Equal("Wait...", Run(new FixEllipsisStyle(), "Wait...", "zh"));
    }

    [Fact]
    public void UnneededPeriods_NoLongerConvertsEllipsis()
    {
        Assert.Equal("Wait...", Run(new FixUnneededPeriods(), "Wait..."));
    }

    [Fact]
    public void UnneededPeriods_ThenEllipsisStyle_CollapsesFourDots()
    {
        var subtitle = new Subtitle();
        subtitle.Paragraphs.Add(new Paragraph { Text = "Wait...." });
        var callbacks = new FakeCallbacks();
        new FixUnneededPeriods().Fix(subtitle, callbacks);
        new FixEllipsisStyle().Fix(subtitle, callbacks);
        Assert.Equal("Wait…", subtitle.Paragraphs[0].Text);
    }
}
