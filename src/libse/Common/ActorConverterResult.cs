namespace Nikse.SubtitleEdit.Core.Common
{
    public class ActorConverterResult
    {
        public Paragraph Paragraph { get; set; }
        public Paragraph NextParagraph { get; set; }
        public bool Selected { get; set; }
        public bool Skip { get; internal set; }

        /// <summary>
        /// No colon in the paragraph reads as a speaker label - the conversion is a guess, to be
        /// confirmed by the user rather than applied.
        /// </summary>
        public bool NotActor { get; set; }
    }
}
