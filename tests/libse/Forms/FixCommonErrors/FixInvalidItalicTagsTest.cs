using Nikse.SubtitleEdit.Core.Common;
using Nikse.SubtitleEdit.Core.Forms.FixCommonErrors;

namespace LibSETests.Forms.FixCommonErrors;

public class FixInvalidItalicTagsTest
{
    private static string Fix(string input)
    {
        var subtitle = new Subtitle();
        subtitle.Paragraphs.Add(new Paragraph(input.Replace("\r\n", Environment.NewLine), 0, 2000));
        subtitle.Renumber();
        new FixInvalidItalicTags().Fix(subtitle, new EmptyFixCallback());
        return subtitle.Paragraphs[0].Text;
    }

    private static string Nl(string s) => s.Replace("\r\n", Environment.NewLine);

    [Theory]
    [InlineData("♪ It's your birthday\r\nIt's your birthday ♪")]
    [InlineData("♪ <i>It's your birthday\r\nIt's your birthday</i> ♪")]
    [InlineData("♪<i> It's your birthday\r\nIt's your birthday </i>♪")]
    [InlineData("<i>♪ It's your birthday</i>\r\n<i>It's your birthday ♪</i>")]
    public void SongLyrics_AreItalicizedWithNotesInside(string input)
    {
        Assert.Equal(Nl("<i>♪ It's your birthday\r\nIt's your birthday ♪</i>"), Fix(input));
    }

    [Theory]
    [InlineData("♪ Happy birthday ♪", "<i>♪ Happy birthday ♪</i>")]
    [InlineData("♫ Take it low\r\nGirl, go and take it low ♫", "<i>♫ Take it low\r\nGirl, go and take it low ♫</i>")]
    [InlineData("♪ Bad boys, bad boys\r\n♪ What you gonna do?", "<i>♪ Bad boys, bad boys\r\n♪ What you gonna do?</i>")]
    [InlineData("♪ It's your birthday", "<i>♪ It's your birthday</i>")]
    [InlineData("It's your birthday ♪", "<i>It's your birthday ♪</i>")]
    [InlineData("{\\an8}♪ Happy birthday ♪", "{\\an8}<i>♪ Happy birthday ♪</i>")]
    [InlineData("<i>♪ Bad boys, bad boys</i>\r\n<i>♪ What you gonna do?</i>", "<i>♪ Bad boys, bad boys\r\n♪ What you gonna do?</i>")]
    public void SongLyrics_Variants_AreItalicized(string input, string expected)
    {
        Assert.Equal(Nl(expected), Fix(input));
    }

    [Theory]
    [InlineData("[singing] ♪ It's your birthday\r\nIt's your birthday ♪", "[singing] <i>♪ It's your birthday\r\nIt's your birthday ♪</i>")]
    [InlineData("<i>[singing] ♪ It's your birthday ♪</i>", "[singing] <i>♪ It's your birthday ♪</i>")]
    [InlineData("(singing) ♪ It's your birthday ♪", "(singing) <i>♪ It's your birthday ♪</i>")]
    [InlineData("[singing]\r\n♪ It's your birthday ♪", "[singing]\r\n<i>♪ It's your birthday ♪</i>")]
    public void SongLyrics_WithHiLabel_KeepLabelOutsideItalics(string input, string expected)
    {
        Assert.Equal(Nl(expected), Fix(input));
    }

    [Theory]
    [InlineData("- ♪ It's your birthday ♪\r\n- ♪ Yes, it is ♪", "- <i>♪ It's your birthday ♪</i>\r\n- <i>♪ Yes, it is ♪</i>")]
    [InlineData("<i>- ♪ It's your birthday ♪\r\n- ♪ Yes, it is ♪</i>", "- <i>♪ It's your birthday ♪</i>\r\n- <i>♪ Yes, it is ♪</i>")]
    [InlineData("- ♪ Happy birthday ♪\r\n- Thanks!", "- <i>♪ Happy birthday ♪</i>\r\n- Thanks!")]
    [InlineData("- [singing] ♪ Happy birthday ♪\r\n- ♪ Yes, it is ♪", "- [singing] <i>♪ Happy birthday ♪</i>\r\n- <i>♪ Yes, it is ♪</i>")]
    [InlineData("-♪ Happy birthday ♪\r\n-Thanks!", "-<i>♪ Happy birthday ♪</i>\r\n-Thanks!")]
    public void SongLyrics_InDialog_AreItalicizedPerTurn(string input, string expected)
    {
        Assert.Equal(Nl(expected), Fix(input));
    }

    [Theory]
    [InlineData("<i>♪ It's your birthday\r\nIt's your birthday ♪</i>")]
    [InlineData("[singing] <i>♪ It's your birthday ♪</i>")]
    [InlineData("- <i>♪ It's your birthday ♪</i>\r\n- <i>♪ Yes, it is ♪</i>")]
    [InlineData("It's your birthday")]
    [InlineData("♪")]
    [InlineData("♪ ♪")]
    [InlineData("♪ [softly] la la la ♪")]
    [InlineData("♪ I said <i>hello</i> ♪")]
    [InlineData("I love this ♪ song a lot")]
    public void Unchanged(string input)
    {
        Assert.Equal(Nl(input), Fix(input));
    }
}
