using Nikse.SubtitleEdit.Core.Common;
using Nikse.SubtitleEdit.Core.Forms;

namespace LibSETests.Core;

public class RemoveTextForHITest
{
    private static RemoveTextForHI BuildRemover(List<string> removeIfContains)
    {
        var settings = new RemoveTextForHISettings(new Subtitle())
        {
            RemoveWhereContains = true,
            RemoveIfTextContains = removeIfContains,
        };
        return new RemoveTextForHI(settings);
    }

    // Cluster 5: "Remove text if contains" — single-line is always removed
    [Fact]
    public void RemoveIfContains_SingleLine_Removed()
    {
        var r = BuildRemover(new List<string> { "♪" });
        var result = r.RemoveTextFromHearImpaired("♪ Happy days ♪", "en");
        Assert.Equal(string.Empty, result);
    }

    // Cluster 5: Multi-line where ALL lines match — entire entry removed
    [Fact]
    public void RemoveIfContains_MultiLine_AllMatch_Removed()
    {
        var r = BuildRemover(new List<string> { "♪" });
        var text = "♪ Song line one ♪" + Environment.NewLine + "♪ Song line two ♪";
        var result = r.RemoveTextFromHearImpaired(text, "en");
        Assert.Equal(string.Empty, result);
    }

    // Cluster 5: Multi-line with one matching line and one dialog line (dash) — keep dialog line
    [Fact]
    public void RemoveIfContains_MultiLine_DialogLineSurvives()
    {
        var r = BuildRemover(new List<string> { "♪" });
        var text = "♪ Song line ♪" + Environment.NewLine + "- Hello there.";
        var result = r.RemoveTextFromHearImpaired(text, "en");
        Assert.Contains("Hello there", result);
        Assert.DoesNotContain("♪", result);
    }

    [Fact]
    public void DialogHyphen_NotItalicized_AfterHIRemoval()
    {
        var settings = new RemoveTextForHISettings(new Subtitle())
        {
            RemoveTextBetweenSquares = true,
        };
        var r = new RemoveTextForHI(settings);
        var input = "- [Michael] Get lost!" + Environment.NewLine
                  + "- [KITT] <i>I can't do that, Michael.</i>";
        var result = r.RemoveTextFromHearImpaired(input, "en");
        Assert.DoesNotContain("<i>-", result);
        Assert.Contains("- <i>", result);
    }

    // Cluster 5: Multi-line continuation lyrics (no dialog dash) — entire entry removed
    [Fact]
    public void RemoveIfContains_MultiLine_ContinuationLyrics_Removed()
    {
        var r = BuildRemover(new List<string> { "♪" });
        var text = "♪ Song line one ♪" + Environment.NewLine + "More song lyrics.";
        var result = r.RemoveTextFromHearImpaired(text, "en");
        Assert.Equal(string.Empty, result);
    }
}
