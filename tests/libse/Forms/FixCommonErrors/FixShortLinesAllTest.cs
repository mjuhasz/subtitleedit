using Nikse.SubtitleEdit.Core.Common;
using Nikse.SubtitleEdit.Core.Forms.FixCommonErrors;

namespace LibSETests.Forms.FixCommonErrors;

public class FixShortLinesAllTest
{
    private static string Fix(string input)
    {
        var subtitle = new Subtitle();
        subtitle.Paragraphs.Add(new Paragraph(input, 0, 2000));
        subtitle.Renumber();
        new FixShortLinesAll().Fix(subtitle, new EmptyFixCallback());
        return subtitle.Paragraphs[0].Text;
    }

    [Theory]
    [InlineData("♪ Bad boys, bad boys\r\nWhat you gonna do? ♪")]
    [InlineData("<i>♪ Take it low\r\nGirl, go and take it low ♪</i>")]
    [InlineData("♫ Take it low\r\nGirl, go and take it low ♫")]
    [InlineData("♪ Bad boys, bad boys\r\n♪ What you gonna do?")]
    public void SongLines_AreNotMerged(string input)
    {
        var text = input.Replace("\r\n", Environment.NewLine);
        Assert.Equal(text, Fix(text));
    }

    [Fact]
    public void ShortNonSongLines_AreMerged()
    {
        Assert.Equal("Hello there.", Fix("Hello" + Environment.NewLine + "there."));
    }
}
