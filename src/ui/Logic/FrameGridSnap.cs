using Nikse.SubtitleEdit.Core.Common;
using Nikse.SubtitleEdit.Core.SubtitleFormats;
using System;

namespace Nikse.SubtitleEdit.Logic;

/// <summary>
/// Snaps subtitle times to the frame grid that libse (and Beautify time codes) use: frame number
/// times the exact frame duration - 24000/1001 for 23.976, the whole-number rate in SMPTE timing.
/// "1000 / 23.976" drifts from that grid by a few ms over a feature film, so every time code ended
/// up "snapped". A time within half a millisecond of its frame is already on it (subtitle times are
/// whole ms, and frame 1596 at 23.976 is 66566.5 ms), so it is left as it is.
/// </summary>
internal static class FrameGridSnap
{
    private const double OnFrameToleranceMs = 0.5;

    public static bool IsOnFrame(double milliseconds, double frameRate)
    {
        var frames = SubtitleFormat.MillisecondsToFrames(milliseconds, frameRate);
        return Math.Abs(milliseconds - ExactFrameMilliseconds(frames, frameRate)) <= OnFrameToleranceMs;
    }

    /// <summary>
    /// <paramref name="milliseconds"/> unchanged when it is already on a frame, otherwise the
    /// nearest frame's time in whole milliseconds.
    /// </summary>
    public static double Snap(double milliseconds, double frameRate)
    {
        if (IsOnFrame(milliseconds, frameRate))
        {
            return milliseconds;
        }

        return SubtitleFormat.FramesToMilliseconds(SubtitleFormat.MillisecondsToFrames(milliseconds, frameRate), frameRate);
    }

    /// <summary>
    /// Snaps both ends of a cue. Snapping can collapse start and end to the same frame (or invert
    /// them) for sub-frame durations; the cue is then kept one frame long.
    /// </summary>
    public static (double StartMs, double EndMs) SnapCue(double startMs, double endMs, double frameRate)
    {
        var newStartMs = Snap(startMs, frameRate);
        var newEndMs = Snap(endMs, frameRate);
        if (newEndMs <= newStartMs)
        {
            var startFrame = SubtitleFormat.MillisecondsToFrames(newStartMs, frameRate);
            newEndMs = SubtitleFormat.FramesToMilliseconds(startFrame + 1, frameRate);
        }

        return (newStartMs, newEndMs);
    }

    private static double ExactFrameMilliseconds(int frames, double frameRate) =>
        frames * TimeCode.BaseUnit / SubtitleFormat.GetTimeCodeFrameRate(frameRate);
}
