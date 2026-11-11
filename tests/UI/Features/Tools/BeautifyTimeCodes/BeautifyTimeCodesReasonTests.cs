using System.Collections.Generic;
using Nikse.SubtitleEdit.Controls.AudioVisualizerControl;
using Nikse.SubtitleEdit.Core.Common;
using Nikse.SubtitleEdit.Core.Settings;
using Nikse.SubtitleEdit.Core.SubtitleFormats;
using Nikse.SubtitleEdit.Features.Main;
using Nikse.SubtitleEdit.Features.Tools.BeautifyTimeCodes;
using Nikse.SubtitleEdit.Logic.Config;

namespace UITests.Features.Tools.BeautifyTimeCodes;

/// <summary>
/// The change notes guessed "min. gap enforced" whenever the new gap happened to equal the general
/// minimum-gap setting, even when the gap was already the profile's 2 frames and the cues were only
/// snapped onto the frame grid. Cues 716/717 are from a real 23.976 file: 0.28 frame off the grid
/// with a 1.99-frame gap (83 ms), moved 12 ms by beautify.
/// </summary>
public class BeautifyTimeCodesReasonTests
{
    private const double Fps = 23.976;

    private static SubtitleLineViewModel Line(int number, double startMs, double endMs) =>
        new(new Paragraph("Line " + number, startMs, endMs), new SubRip()) { Number = number };

    private static double Frame(int frame) => SubtitleFormat.FramesToMilliseconds(frame, Fps);

    private static void WithNetflixProfile(Action<BeautifyTimeCodesViewModel> test, List<double>? shotChanges = null)
    {
        using var _ = new SettingsScope("General.CurrentFrameRate");
        Se.Settings.General.CurrentFrameRate = Fps;
        var savedProfile = Configuration.Settings.BeautifyTimeCodes.Profile;
        Configuration.Settings.BeautifyTimeCodes.Profile =
            new BeautifyTimeCodesSettings.BeautifyTimeCodesProfile(BeautifyTimeCodesSettings.BeautifyTimeCodesProfile.Preset.Netflix);
        try
        {
            var vm = new BeautifyTimeCodesViewModel(new StubWindowService());
            var visualizer = new AudioVisualizer();
            if (shotChanges != null)
            {
                visualizer.ShotChanges = shotChanges;
            }

            vm.Initialize(new List<SubtitleLineViewModel> { Line(1, 1000, 2000) }, visualizer, string.Empty);
            test(vm);
        }
        finally
        {
            Configuration.Settings.BeautifyTimeCodes.Profile = savedProfile;
        }
    }

    [Fact]
    public void OffGridPairWithTwoFrameGap_IsReportedAsSnappedToFrame()
    {
        WithNetflixProfile(vm =>
        {
            var original716 = Line(716, 3577365, 3578897);
            var original717 = Line(717, 3578980, 3581161);
            var beautified716 = Line(716, 3577365, 3578909);
            var beautified717 = Line(717, 3578992, 3581161);
            var snapped = Se.Language.Tools.BeautifyTimeCodes.SnappedToFrame;

            Assert.Equal(snapped, vm.DetectEndReason(original716, beautified716, original717, beautified717));
            Assert.Equal(snapped, vm.DetectStartReason(original717, beautified717, original716, beautified716));
        });
    }

    [Fact]
    public void OneFrameGapWidenedToTwoFrames_IsReportedAsMinGapEnforced()
    {
        WithNetflixProfile(vm =>
        {
            var originalLeft = Line(1, Frame(50), Frame(100));
            var originalRight = Line(2, Frame(101), Frame(150));
            var beautifiedLeft = Line(1, Frame(50), Frame(99));
            var beautifiedRight = Line(2, Frame(101), Frame(150));

            Assert.Equal(
                Se.Language.Tools.BeautifyTimeCodes.MinGapEnforced,
                vm.DetectEndReason(originalLeft, beautifiedLeft, originalRight, beautifiedRight));
        });
    }

    /// <summary>
    /// A connected pair in a shot change's green zone is moved ~4 frames onto the zone edge (12 frames before
    /// the shot change). That is a shot change snap, not a frame snap - a frame snap never moves a time by 178 ms.
    /// </summary>
    [Fact]
    public void ConnectedPairMovedToGreenZoneEdge_IsReportedAsSnappedToShotChange()
    {
        WithNetflixProfile(vm =>
        {
            var original338 = Line(338, 1402234, 1406500);
            var original339 = Line(339, 1406583, 1408198);
            var beautified338 = Line(338, 1402234, 1406322);
            var beautified339 = Line(339, Frame(33720), 1408198);
            var snapped = Se.Language.Tools.BeautifyTimeCodes.SnappedToShotChange;

            Assert.Equal(snapped, vm.DetectStartReason(original339, beautified339, original338, beautified338));
            Assert.Equal(snapped, vm.DetectEndReason(original338, beautified338, original339, beautified339));
        }, new List<double> { Frame(33732) / 1000.0 });
    }

    [Fact]
    public void LargeMoveWithoutShotChange_IsNotReportedAsSnappedToFrame()
    {
        WithNetflixProfile(vm =>
        {
            var original = Line(1, Frame(1000) + 5, Frame(1100));
            var beautified = Line(1, Frame(995), Frame(1100));

            Assert.NotEqual(
                Se.Language.Tools.BeautifyTimeCodes.SnappedToFrame,
                vm.DetectStartReason(original, beautified, null, null));
        });
    }
}
