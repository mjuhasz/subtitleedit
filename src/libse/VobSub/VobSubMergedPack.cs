using System;
using System.Collections.Generic;
using Nikse.SubtitleEdit.Core.Common;
using Nikse.SubtitleEdit.Core.Interfaces;
using SkiaSharp;

namespace Nikse.SubtitleEdit.Core.VobSub
{
    public class VobSubMergedPack : IBinaryParagraphWithPosition
    {
        public SubPicture SubPicture { get; private set; }
        public TimeSpan StartTime { get; private set; }
        public TimeSpan EndTime { get; set; }
        public int StreamId { get; private set; }
        public IdxParagraph IdxLine { get; private set; }
        public List<SKColor> Palette { get; set; }

        /// <summary>
        /// Screen size from the idx "size:" line (e.g. 720x576 for PAL). Zero means unknown,
        /// in which case <see cref="GetScreenSize"/> falls back to NTSC 720x480.
        /// </summary>
        public int ScreenWidth { get; set; }
        public int ScreenHeight { get; set; }

        public VobSubMergedPack(byte[] subPictureData, TimeSpan presentationTimestamp, int streamId, IdxParagraph idxLine)
        {
            SubPicture = new SubPicture(subPictureData);
            StartTime = presentationTimestamp;
            StreamId = streamId;
            IdxLine = idxLine;
        }

        public bool IsForced => SubPicture.Forced;

        public SKBitmap GetBitmap()
        {
            return SubPicture.GetBitmap(Palette, SKColors.Transparent, SKColors.Black, SKColors.White, SKColors.Black, false, true);
        }

        public SKSize GetScreenSize()
        {
            if (ScreenWidth > 0 && ScreenHeight > 0)
            {
                return new SKSize(ScreenWidth, ScreenHeight);
            }

            return new SKSize(720, 480);
        }

        public Position GetPosition()
        {
            return new Position(SubPicture.ImageDisplayArea.Left, SubPicture.ImageDisplayArea.Top);
        }

        public TimeCode StartTimeCode
        {
            get
            {
                //if (IdxLine != null)
                //{
                //    return new TimeCode(IdxLine.StartTime.TotalMilliseconds);
                //}

                return new TimeCode(StartTime.TotalMilliseconds);
            }
        }

        public TimeCode EndTimeCode
        {
            get
            {
                //if (IdxLine != null)
                //{
                //    return new TimeCode(IdxLine.StartTime.TotalMilliseconds + SubPicture.Delay.TotalMilliseconds);
                //}

                return new TimeCode(StartTime.TotalMilliseconds + SubPicture.Delay.TotalMilliseconds);
            }
        }

    }
}
