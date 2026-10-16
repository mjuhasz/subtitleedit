using Nikse.SubtitleEdit.UiLogic.Common;
using Xunit;

namespace LibUiLogicTests.Common;

public class FormattingImporterTests
{
    [Theory]
    [InlineData("<i>Hello.</i>", "Hello.", "<i>Hello.</i>")]
    [InlineData("- What?\n- <i>That's right. \"Sergeant Oswald Cochrane.</i>", "- What?\n- That's right. \"Sergeant Oswald Cochrane.", "- What?\n- <i>That's right. \"Sergeant Oswald Cochrane.</i>")]
    [InlineData("- <i>What?</i>\n- That's right. \"Sergeant Oswald Cochrane.", "- What?\n- That's right. \"Sergeant Oswald Cochrane.", "- <i>What?</i>\n- That's right. \"Sergeant Oswald Cochrane.")]
    [InlineData("[man on radio] <i>Dulles, what's going on?\nI'm on approach.</i>", "[man on radio] Dulles, what's going on?\nI'm on approach.", "[man on radio] <i>Dulles, what's going on?\nI'm on approach.</i>")]
    [InlineData("[Al] <i>You and faxes? This is a first.</i>", "[Al] You and faxes? This is a first.", "[Al] <i>You and faxes? This is a first.</i>")]
    [InlineData("- <i>What?</i>\n- <i>That's right. \"Sergeant Oswald Cochrane.</i>", "- What?\n- That's right. \"Sergeant Oswald Cochrane.", "- <i>What?</i>\n- <i>That's right. \"Sergeant Oswald Cochrane.</i>")]
    public void Apply_PartialItalics(string importText, string currentText, string expected)
    {
        Assert.Equal(expected, FormattingImporter.Apply(importText, currentText));
    }

    [Fact]
    public void Apply_NoFormattingInImport_KeepsCurrent()
    {
        Assert.Equal("<i>Hi.</i>", FormattingImporter.Apply("Hi.", "<i>Hi.</i>"));
    }

    [Fact]
    public void Apply_ReplacesExistingFormatting()
    {
        Assert.Equal("- <i>A.</i>\n- B.", FormattingImporter.Apply("- <i>A.</i>\n- B.", "<i>- A.\n- B.</i>"));
    }

    [Fact]
    public void Apply_DifferentLineBreaksAndTypo()
    {
        Assert.Equal("[Al] <i>You and faxes?\nThis is a first.</i>",
            FormattingImporter.Apply("[Al] <i>You and faxes? This is a frist.</i>", "[Al] You and faxes?\nThis is a first."));
    }

    [Fact]
    public void Apply_NestedTagsAndFont()
    {
        Assert.Equal("<font color=\"#ff0000\"><b>Stop</b> it</font>",
            FormattingImporter.Apply("<font color=\"#ff0000\"><b>Stop</b> it</font>", "Stop it"));
    }

    [Fact]
    public void Apply_AssaOverrideBlock()
    {
        Assert.Equal("{\\an8}<i>Top</i>", FormattingImporter.Apply("{\\an8}<i>Top</i>", "Top"));
    }

    [Fact]
    public void Apply_ItalicTextMissingInCurrent_DropsEmptyTags()
    {
        Assert.Equal("Hello.", FormattingImporter.Apply("Hello. <i>Extra</i>", "Hello."));
    }
}
