using Nikse.SubtitleEdit.Core.Common;
using System;

namespace Tests.Core
{
    [TestClass]
    public class HtmlUtilTest
    {
        [TestMethod]
        public void TestRemoveOpenCloseTagCyrillicI()
        {
            const string source = "<\u0456>SubtitleEdit</\u0456>";
            Assert.AreEqual("SubtitleEdit", HtmlUtil.RemoveOpenCloseTags(source, HtmlUtil.TagCyrillicI));
        }

        [TestMethod]
        public void TestRemoveOpenCloseTagFont()
        {
            const string source = "<font color=\"#000\">SubtitleEdit</font>";
            Assert.AreEqual("SubtitleEdit", HtmlUtil.RemoveOpenCloseTags(source, HtmlUtil.TagFont));
        }

        [TestMethod]
        public void RemoveHtmlTags1()
        {
            const string source = "<font color=\"#000\"><i>SubtitleEdit</i></font>";
            Assert.AreEqual("SubtitleEdit", HtmlUtil.RemoveHtmlTags(source));
        }

        [TestMethod]
        public void RemoveHtmlTags2()
        {
            const string source = "<font size=\"12\" color=\"#000\">Hi <font color=\"#000\"><i>SubtitleEdit</i></font></font>";
            Assert.AreEqual("Hi SubtitleEdit", HtmlUtil.RemoveHtmlTags(source));
        }

        [TestMethod]
        public void RemoveHtmlTags3()
        {
            const string source = "{\\an9}<i>How are you? Are you <font='arial'>happy</font> and 1 < 2</i>";
            Assert.AreEqual("{\\an9}How are you? Are you happy and 1 < 2", HtmlUtil.RemoveHtmlTags(source));
        }

        [TestMethod]
        public void RemoveHtmlTagsKeepAss()
        {
            const string source = "{\\an2}<i>SubtitleEdit</i>";
            Assert.AreEqual("{\\an2}SubtitleEdit", HtmlUtil.RemoveHtmlTags(source));
        }

        [TestMethod]
        public void RemoveHtmlTagsRemoveAss()
        {
            const string source = "{\\an2}<i>SubtitleEdit</i>";
            Assert.AreEqual("SubtitleEdit", HtmlUtil.RemoveHtmlTags(source, true));
        }

        [TestMethod]
        public void RemoveHtmlTagsBadItalic()
        {
            const string source = "<i>SubtitleEdit<i/>";
            Assert.AreEqual("SubtitleEdit", HtmlUtil.RemoveHtmlTags(source));
        }

        [TestMethod]
        public void FixInvalidItalicTags1()
        {
            const string s = "<i>foobar<i>?";
            Assert.AreEqual("<i>foobar</i>?", HtmlUtil.FixInvalidItalicTags(s));
        }

        [TestMethod]
        public void FixInvalidItalicTags2()
        {
            string s = "<i>foobar?" + Environment.NewLine + "<i>foobar?";
            Assert.AreEqual("<i>foobar?</i>" + Environment.NewLine + "<i>foobar?</i>", HtmlUtil.FixInvalidItalicTags(s));
        }

        [TestMethod]
        public void FixInvalidItalicTags_DashOutsideItalicSpanningLines()
        {
            // - <i>Line one
            // - Line two</i>
            // should become:
            // - <i>Line one</i>
            // - <i>Line two</i>
            string s = "- <i>Excuse me?" + Environment.NewLine + "- Why?</i>";
            Assert.AreEqual("- <i>Excuse me?</i>" + Environment.NewLine + "- <i>Why?</i>", HtmlUtil.FixInvalidItalicTags(s));
        }

        [TestMethod]
        public void FixInvalidItalicTags_DashOutsideItalicSpanningLinesWithPreTags()
        {
            // {\an8}- <i>Line one
            // - Line two</i>
            // should become:
            // {\an8}- <i>Line one</i>
            // - <i>Line two</i>
            string s = "{\\an8}- <i>Excuse me?" + Environment.NewLine + "- Why?</i>";
            Assert.AreEqual("{\\an8}- <i>Excuse me?</i>" + Environment.NewLine + "- <i>Why?</i>", HtmlUtil.FixInvalidItalicTags(s));
        }

        [TestMethod]
        public void FixInvalidItalicTags_DashEmbeddedInClosingItalic()
        {
            // - [woman 1] <i>One herbal medicine, dear.
            // -</i> [woman 2] <i>Yes, ma'am.</i>
            // should become:
            // - [woman 1] <i>One herbal medicine, dear.</i>
            // - [woman 2] <i>Yes, ma'am.</i>
            string s = "- [woman 1] <i>One herbal medicine, dear." + Environment.NewLine + "-</i> [woman 2] <i>Yes, ma'am.</i>";
            Assert.AreEqual("- [woman 1] <i>One herbal medicine, dear.</i>" + Environment.NewLine + "- [woman 2] <i>Yes, ma'am.</i>", HtmlUtil.FixInvalidItalicTags(s));
        }

        [TestMethod]
        public void FixInvalidItalicTags_SpeakerBeforeItalicSpanningLines()
        {
            // - [speaker] <i>Content one
            // - Content two</i>
            // should become:
            // - [speaker] <i>Content one</i>
            // - <i>Content two</i>
            string s = "- [John] <i>Mm-hmm." + Environment.NewLine + "- ...how difficult is it to keep your life</i>";
            Assert.AreEqual("- [John] <i>Mm-hmm.</i>" + Environment.NewLine + "- <i>...how difficult is it to keep your life</i>", HtmlUtil.FixInvalidItalicTags(s));
        }

        [TestMethod]
        public void FixInvalidItalicTags_MergePerLineItalics()
        {
            // Non-dialog per-line italics should be merged into single spanning tags
            // <i>First line</i>
            // <i>Second line</i>
            // should become:
            // <i>First line
            // Second line</i>
            string s = "<i>First line</i>" + Environment.NewLine + "<i>Second line</i>";
            Assert.AreEqual("<i>First line" + Environment.NewLine + "Second line</i>", HtmlUtil.FixInvalidItalicTags(s));
        }

        [TestMethod]
        public void FixInvalidItalicTags_KeepDialogPerLineItalics()
        {
            // Dialog lines should stay as per-line italics (not merged)
            // - <i>First line</i>
            // - <i>Second line</i>
            // should stay unchanged
            string s = "- <i>First line</i>" + Environment.NewLine + "- <i>Second line</i>";
            Assert.AreEqual("- <i>First line</i>" + Environment.NewLine + "- <i>Second line</i>", HtmlUtil.FixInvalidItalicTags(s));
        }

        [TestMethod]
        public void FixInvalidItalicTags_ClosingTagAfterDashSecondLineNotItalic()
        {
            // - <i>Line one
            // - </i> Line two (not italic)
            // should become:
            // - <i>Line one</i>
            // - Line two
            string s = "- <i>I have you under \"probably wants money.\"" + Environment.NewLine +
                       "- </i> I haven't asked you for money in years.";
            Assert.AreEqual("- <i>I have you under \"probably wants money.\"</i>" + Environment.NewLine +
                            "- I haven't asked you for money in years.",
                            HtmlUtil.FixInvalidItalicTags(s));
        }

        [TestMethod]
        public void FixInvalidItalicTags_DialogDashOutsideItalicSpanningLines()
        {
            // - <i>text1</i>… <i> text2
            // - text3</i>
            // should become:
            // - <i>text1</i>… <i> text2</i>
            // - <i>text3</i>
            string s = "- <i>♪ Santa, that's my only wish this year</i>… <i> ♪" + Environment.NewLine +
                       "- ♪ This year ♪</i>";
            Assert.AreEqual("- <i>♪ Santa, that's my only wish this year</i>… <i> ♪</i>" + Environment.NewLine +
                            "- <i>♪ This year ♪</i>",
                            HtmlUtil.FixInvalidItalicTags(s));
        }

        [TestMethod]
        public void FixInvalidItalicTags_ItalicOnlyAroundDash_SingleLine()
        {
            // <i>-</i> Text -> - Text
            string s = "<i>-</i> Is that Carl?";
            Assert.AreEqual("- Is that Carl?", HtmlUtil.FixInvalidItalicTags(s));
        }

        [TestMethod]
        public void FixInvalidItalicTags_ItalicOnlyAroundDash_TwoLines()
        {
            // <i>-</i> Text1
            // - Text2
            // becomes:
            // - Text1
            // - Text2
            string s = "<i>-</i> Is that Carl?" + Environment.NewLine +
                       "- No. He's with TaskRabbit. Carl died.";
            Assert.AreEqual("- Is that Carl?" + Environment.NewLine +
                            "- No. He's with TaskRabbit. Carl died.",
                            HtmlUtil.FixInvalidItalicTags(s));
        }

        [TestMethod]
        public void FixInvalidItalicTags_ItalicSpanningWithClosingAfterSecondDash()
        {
            // <i>- Content1
            // - </i> Content2, <i>Content3</i>
            // becomes:
            // - <i>Content1</i>
            // - Content2, <i>Content3</i>
            string s = "<i>- Saving Emmett?" + Environment.NewLine +
                       "- </i> Yes, <i>Saving Emmett!</i>";
            Assert.AreEqual("- <i>Saving Emmett?</i>" + Environment.NewLine +
                            "- Yes, <i>Saving Emmett!</i>",
                            HtmlUtil.FixInvalidItalicTags(s));
        }

        [TestMethod]
        public void FixInvalidItalicTags_ItalicOnlyAroundDash_SecondLine()
        {
            // - Text1
            // <i>-</i> Text2
            // becomes:
            // - Text1
            // - Text2
            string s = "- Yes, he is." + Environment.NewLine +
                       "<i>-</i> So where is he?";
            Assert.AreEqual("- Yes, he is." + Environment.NewLine +
                            "- So where is he?",
                            HtmlUtil.FixInvalidItalicTags(s));
        }

        [TestMethod]
        public void FixInvalidItalicTags_ItalicSpanningWithDashCloseSecondLine()
        {
            // <i>- Content1
            // -</i> Content2
            // becomes:
            // - <i>Content1</i>
            // - Content2
            string s = "<i>- ♪ Seems like everyone but me is in love ♪" + Environment.NewLine +
                       "-</i> Looking. Looking.";
            Assert.AreEqual("- <i>♪ Seems like everyone but me is in love ♪</i>" + Environment.NewLine +
                            "- Looking. Looking.",
                            HtmlUtil.FixInvalidItalicTags(s));
        }

    }
}
