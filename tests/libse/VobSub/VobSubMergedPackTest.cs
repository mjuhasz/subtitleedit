using Nikse.SubtitleEdit.Core.VobSub;

namespace LibSETests.VobSub;

public class VobSubMergedPackTest
{
    private static VobSubMergedPack CreatePack()
    {
        var data = new byte[]
        {
            0x00, 0x08, // sub picture data size
            0x00, 0x04, // display control sequence table address
            0x00, 0x00, // delay
            0x00, 0x00, // next sequence address
        };

        return new VobSubMergedPack(data, TimeSpan.Zero, 32, null);
    }

    [Fact]
    public void GetScreenSizeDefaultsToNtsc()
    {
        var size = CreatePack().GetScreenSize();

        Assert.Equal(720, size.Width);
        Assert.Equal(480, size.Height);
    }

    // A PAL idx declares "size: 720x576"; positions below row 480 were cut off on the
    // hardcoded NTSC canvas and exported as {\an7}.
    [Fact]
    public void GetScreenSizeUsesIdxSize()
    {
        var pack = CreatePack();
        pack.ScreenWidth = 720;
        pack.ScreenHeight = 576;

        var size = pack.GetScreenSize();

        Assert.Equal(720, size.Width);
        Assert.Equal(576, size.Height);
    }

    [Fact]
    public void GetScreenSizeIgnoresPartialSize()
    {
        var pack = CreatePack();
        pack.ScreenWidth = 720;

        var size = pack.GetScreenSize();

        Assert.Equal(720, size.Width);
        Assert.Equal(480, size.Height);
    }
}
