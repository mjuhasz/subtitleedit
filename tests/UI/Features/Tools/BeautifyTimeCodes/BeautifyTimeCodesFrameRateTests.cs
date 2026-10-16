using System.Collections.Generic;
using Nikse.SubtitleEdit.Controls.AudioVisualizerControl;
using Nikse.SubtitleEdit.Core.Common;
using Nikse.SubtitleEdit.Core.SubtitleFormats;
using Nikse.SubtitleEdit.Features.Main;
using Nikse.SubtitleEdit.Features.Tools.BeautifyTimeCodes;
using Nikse.SubtitleEdit.Logic.Config;

namespace UITests.Features.Tools.BeautifyTimeCodes;

/// <summary>
/// Without a video, beautify used a hard-coded 25 fps and ignored the project frame rate set in
/// the toolbar, so 23.976 content was snapped to the wrong frame grid. It must use the project
/// frame rate - the one dragging on the waveform and "Snap all times to frames" use.
/// </summary>
public class BeautifyTimeCodesFrameRateTests
{
    [Fact]
    public void Initialize_WithoutVideo_UsesProjectFrameRate()
    {
        using var _ = new SettingsScope("General.CurrentFrameRate");
        Se.Settings.General.CurrentFrameRate = 23.976;

        var lines = new List<SubtitleLineViewModel>
        {
            new(new Paragraph("First", 1013, 3987), new SubRip()) { Number = 1 },
            new(new Paragraph("Second", 4007, 6499), new SubRip()) { Number = 2 },
        };

        var vm = new BeautifyTimeCodesViewModel(new StubWindowService());
        vm.Initialize(lines, new AudioVisualizer(), string.Empty);

        Assert.Contains("23.976", vm.StatsLine);
        Assert.DoesNotContain(": 25 ", vm.StatsLine);
    }
}
