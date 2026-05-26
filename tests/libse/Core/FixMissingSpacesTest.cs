using Nikse.SubtitleEdit.Core.Common;
using Nikse.SubtitleEdit.Core.Forms.FixCommonErrors;
using Nikse.SubtitleEdit.Core.Interfaces;
using Nikse.SubtitleEdit.Core.SubtitleFormats;
using System.Collections.Generic;
using System.Text;

namespace LibSETests.Core;

public class FixMissingSpacesTest
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

    private static string Fix(string text, string lang = "en")
    {
        var subtitle = new Subtitle();
        subtitle.Paragraphs.Add(new Paragraph { Text = text });
        var fixer = new FixMissingSpaces();
        fixer.Fix(subtitle, new FakeCallbacks { Language = lang });
        return subtitle.Paragraphs[0].Text;
    }

    // Cluster 4: No space inserted between closing quote and Unicode ellipsis
    [Fact]
    public void Quote_UnicodeEllipsis_NoSpaceInserted()
    {
        Assert.Equal("He said \"go…\"", Fix("He said \"go…\""));
    }

    // Cluster 4: Space added after ] before letter
    [Fact]
    public void SdhBracket_MissingSpaceBeforeLetter()
    {
        Assert.Equal("[Tower] text", Fix("[Tower]text"));
    }

    // Cluster 4: Space added after ] before opening HTML tag (not closing)
    [Fact]
    public void SdhBracket_MissingSpaceBeforeOpenTag()
    {
        Assert.Equal("[Tower] <i>text</i>", Fix("[Tower]<i>text</i>"));
    }

    // Cluster 4: No space added after ] before closing HTML tag
    [Fact]
    public void SdhBracket_NoSpaceBeforeCloseTag()
    {
        Assert.Equal("<i>[whispers]</i>", Fix("<i>[whispers]</i>"));
    }
}
