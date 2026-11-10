using Nikse.SubtitleEdit.Core.SubtitleFormats;
using Nikse.SubtitleEdit.Logic;

namespace UITests.Logic;

/// <summary>
/// "Snap all times to frames" used 1000 / 23.976 ms per frame instead of the exact 1001/24 that
/// libse and Beautify time codes use. The two grids drift apart by a few ms over a feature film, so
/// a file already on frames had almost every time code "snapped". Values are from a real 23.976
/// SDH file where Beautify found only four time codes off the frame grid.
/// </summary>
public class FrameGridSnapTests
{
    private const double Ntsc24 = 23.976;

    [Theory]
    [InlineData(3578897, 3578909)] // 0.28 frame early, almost an hour in
    [InlineData(3578980, 3578992)]
    [InlineData(3077354, 3077366)]
    public void Snap_OffGridTime_MovesToTheFrameBeautifyUses(double input, double expected)
    {
        Assert.False(FrameGridSnap.IsOnFrame(input, Ntsc24));
        Assert.Equal(expected, FrameGridSnap.Snap(input, Ntsc24));
    }

    [Theory]
    [InlineData(3577365)] // an hour in: the nominal 23.976 grid is ~3.6 ms away from it here
    [InlineData(3581161)]
    [InlineData(66566)] // frame 1596 is exactly 66566.5 ms - either whole ms is on the frame
    [InlineData(66567)]
    [InlineData(0)]
    public void Snap_TimeAlreadyOnFrame_IsLeftAlone(double input)
    {
        Assert.True(FrameGridSnap.IsOnFrame(input, Ntsc24));
        Assert.Equal(input, FrameGridSnap.Snap(input, Ntsc24));
    }

    [Fact]
    public void Snap_25Fps_RoundsToTheNearest40Ms()
    {
        Assert.Equal(1000, FrameGridSnap.Snap(1013, 25));
        Assert.Equal(6480, FrameGridSnap.Snap(6499, 25));
        Assert.Equal(4000, FrameGridSnap.Snap(4000, 25));
    }

    [Fact]
    public void SnapCue_SubFrameCue_StaysOneFrameLong()
    {
        var (start, end) = FrameGridSnap.SnapCue(10001, 10009, 25);

        Assert.Equal(10000, start);
        Assert.Equal(10040, end);
    }

    [Fact]
    public void SnapCue_SubFrameCueAt23976_EndsOnTheNextFrame()
    {
        var startFrame = SubtitleFormat.MillisecondsToFrames(3578909, Ntsc24);

        var (start, end) = FrameGridSnap.SnapCue(3578905, 3578912, Ntsc24);

        Assert.Equal(3578909, start);
        Assert.Equal(SubtitleFormat.FramesToMilliseconds(startFrame + 1, Ntsc24), end);
    }
}
