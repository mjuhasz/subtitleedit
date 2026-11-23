using Nikse.SubtitleEdit.Core.Common;
using Nikse.SubtitleEdit.Core.Forms;

namespace LibSETests.Forms;

public class TimeCodesBeautifierTest
{
    [Theory]
    [InlineData(71571)]
    [InlineData(71572)] // frame 1716 at 23.976 is exactly 71571.5 ms: either whole ms is on the frame
    public void Beautify_TimeWithinHalfMsOfFrame_IsNotMoved(double startMs)
    {
        var subtitle = new Subtitle();
        subtitle.Paragraphs.Add(new Paragraph("Hello", startMs, startMs + 2000));

        new TimeCodesBeautifier(subtitle, 23.976, new List<double>(), new List<double>()).Beautify();

        Assert.Equal(startMs, subtitle.Paragraphs[0].StartTime.TotalMilliseconds);
    }
}
