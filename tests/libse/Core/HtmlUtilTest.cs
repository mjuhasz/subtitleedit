using Nikse.SubtitleEdit.Core.Common;

namespace LibSETests.Core;

public class HtmlUtilTest
{
    [Fact]
    public void TestRemoveOpenCloseTagCyrillicI()
    {
        const string source = "<\u0456>SubtitleEdit</\u0456>";
        Assert.Equal("SubtitleEdit", HtmlUtil.RemoveOpenCloseTags(source, HtmlUtil.TagCyrillicI));
    }

    [Fact]
    public void TestRemoveOpenCloseTagFont()
    {
        const string source = "<font color=\"#000\">SubtitleEdit</font>";
        Assert.Equal("SubtitleEdit", HtmlUtil.RemoveOpenCloseTags(source, HtmlUtil.TagFont));
    }

    [Fact]
    public void RemoveHtmlTags1()
    {
        const string source = "<font color=\"#000\"><i>SubtitleEdit</i></font>";
        Assert.Equal("SubtitleEdit", HtmlUtil.RemoveHtmlTags(source));
    }

    [Fact]
    public void RemoveHtmlTags2()
    {
        const string source = "<font size=\"12\" color=\"#000\">Hi <font color=\"#000\"><i>SubtitleEdit</i></font></font>";
        Assert.Equal("Hi SubtitleEdit", HtmlUtil.RemoveHtmlTags(source));
    }

    [Fact]
    public void RemoveHtmlTags3()
    {
        const string source = "{\\an9}<i>How are you? Are you <font='arial'>happy</font> and 1 < 2</i>";
        Assert.Equal("{\\an9}How are you? Are you happy and 1 < 2", HtmlUtil.RemoveHtmlTags(source));
    }

    [Fact]
    public void RemoveHtmlTagsKeepAss()
    {
        const string source = "{\\an2}<i>SubtitleEdit</i>";
        Assert.Equal("{\\an2}SubtitleEdit", HtmlUtil.RemoveHtmlTags(source));
    }

    [Fact]
    public void RemoveHtmlTagsRemoveAss()
    {
        const string source = "{\\an2}<i>SubtitleEdit</i>";
        Assert.Equal("SubtitleEdit", HtmlUtil.RemoveHtmlTags(source, true));
    }

    [Fact]
    public void RemoveHtmlTagsBadItalic()
    {
        const string source = "<i>SubtitleEdit<i/>";
        Assert.Equal("SubtitleEdit", HtmlUtil.RemoveHtmlTags(source));
    }

    [Fact]
    public void RemoveHtmlTagsWebVttCMultiline()
    {
        var source = "<c.yellow>-Qu'est-ce qu'on a ?</c>" + Environment.NewLine + "<c.yellow>-Adrien Dorval, 65 ans.</c>";
        var expected = "-Qu'est-ce qu'on a ?" + Environment.NewLine + "-Adrien Dorval, 65 ans.";
        Assert.Equal(expected, HtmlUtil.RemoveHtmlTags(source, true));
    }

    [Fact]
    public void RemoveHtmlTagsWebVttVMultiline()
    {
        var source = "<v Roger>Hi</v>" + Environment.NewLine + "<v.loud Bob>Bye</v>";
        var expected = "Hi" + Environment.NewLine + "Bye";
        Assert.Equal(expected, HtmlUtil.RemoveHtmlTags(source, true));
    }

    [Fact]
    public void FixInvalidItalicTags1()
    {
        const string s = "<i>foobar<i>?";
        Assert.Equal("<i>foobar</i>?", HtmlUtil.FixInvalidItalicTags(s));
    }

    [Fact]
    public void FixInvalidItalicTags2()
    {
        string s = "<i>foobar?" + Environment.NewLine + "<i>foobar?";
        Assert.Equal("<i>foobar?</i>" + Environment.NewLine + "<i>foobar?</i>", HtmlUtil.FixInvalidItalicTags(s));
    }

    // Cluster 1: Dialog dash normalization — single line

    [Fact]
    public void DialogDash_SingleLine_DashInsideItalic()
    {
        Assert.Equal("- <i>Text</i>", HtmlUtil.FixInvalidItalicTags("<i>- Text</i>"));
    }

    [Fact]
    public void DialogDash_SingleLine_DashOnlyInsideItalic()
    {
        Assert.Equal("- ", HtmlUtil.FixInvalidItalicTags("<i>-</i>"));
    }

    [Fact]
    public void DialogDash_SingleLine_DashWithSpaceOnlyInsideItalic()
    {
        Assert.Equal("- ", HtmlUtil.FixInvalidItalicTags("<i>- </i>"));
    }

    [Fact]
    public void DialogDash_SingleLine_DashNoSpaceInsideItalic()
    {
        Assert.Equal("- <i>Text</i>", HtmlUtil.FixInvalidItalicTags("<i>-Text</i>"));
    }

    // Cluster 1: Dialog dash normalization — multi-line

    [Fact]
    public void DialogDash_MultiLine_BothDashesInsideSpanningItalic()
    {
        var s = "<i>- Line one" + Environment.NewLine + "- Line two</i>";
        var expected = "- <i>Line one</i>" + Environment.NewLine + "- <i>Line two</i>";
        Assert.Equal(expected, HtmlUtil.FixInvalidItalicTags(s));
    }

    [Fact]
    public void DialogDash_MultiLine_FirstLineDashInsideSpanningItalic()
    {
        // Dash on the first line is pulled before the italic; spanning italic is preserved
        var s = "<i>- Line one" + Environment.NewLine + "Line two</i>";
        var expected = "- <i>Line one" + Environment.NewLine + "Line two</i>";
        Assert.Equal(expected, HtmlUtil.FixInvalidItalicTags(s));
    }

    // Cluster 3: Per-line italic merging (non-dialog only)

    [Fact]
    public void ItalicMerge_NonDialog_PerLineToSpanning()
    {
        var s = "<i>Line one</i>" + Environment.NewLine + "<i>Line two</i>";
        var expected = "<i>Line one" + Environment.NewLine + "Line two</i>";
        Assert.Equal(expected, HtmlUtil.FixInvalidItalicTags(s));
    }

    [Fact]
    public void ItalicMerge_Dialog_PerLineNotMerged()
    {
        var s = "- <i>Line one</i>" + Environment.NewLine + "- <i>Line two</i>";
        Assert.Equal(s, HtmlUtil.FixInvalidItalicTags(s));
    }

    [Fact]
    public void ItalicMerge_SkipSdhBracketLabel()
    {
        // Bracket-only italic is stripped; since the first line loses its <i>,
        // the two lines are no longer both italic and do not merge
        var s = "<i>[König:]</i>" + Environment.NewLine + "<i>Dialog line</i>";
        var expected = "[König:]" + Environment.NewLine + "<i>Dialog line</i>";
        Assert.Equal(expected, HtmlUtil.FixInvalidItalicTags(s));
    }

    // Cluster 3: Move italic before bracket to after bracket

    [Fact]
    public void ItalicBeforeBracket_MovedAfterBracket()
    {
        Assert.Equal("[echoing] <i>You killed two people.</i>", HtmlUtil.FixInvalidItalicTags("<i>[echoing] You killed two people.</i>"));
    }

    [Fact]
    public void ItalicInsideBracket_MovedAfterBracket()
    {
        Assert.Equal("[operator] <i>Yes, sir</i>", HtmlUtil.FixInvalidItalicTags("[<i>operator] Yes, sir</i>"));
    }

    // Cluster 3: Remove italic wrapping bracket-only content

    [Fact]
    public void ItalicBracketOnly_ItalicRemoved()
    {
        Assert.Equal("[repeating message in French]", HtmlUtil.FixInvalidItalicTags("<i>[repeating message in French]</i>"));
    }

}
