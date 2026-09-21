
using FancyWM.Tests.TestUtilities;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FancyWM.Utilities.Tests
{
    [TestClass]
    public class WindowMatcherTest
    {
        private readonly WindowMockFactory m_mockFactory = new();

        [TestMethod]
        public void TestByProcessNameExact()
        {
            var matcher = new ByProcessNameMatcher("explorer");
            Assert.IsTrue(matcher.Matches(m_mockFactory.CreateExplorerWindow()));
        }

        [TestMethod]
        public void TestByProcessNameInExact()
        {
            var matcher = new ByProcessNameMatcher("ExPlOrEr");
            Assert.IsTrue(matcher.Matches(m_mockFactory.CreateExplorerWindow()));
        }


        [TestMethod]
        public void TestByProcessNameFails()
        {
            var matcher = new ByProcessNameMatcher("explorer.something");
            Assert.IsFalse(matcher.Matches(m_mockFactory.CreateExplorerWindow()));
        }

        [TestMethod]
        public void TestCompositeRequiresAllConditions()
        {
            var window = m_mockFactory.CreateExplorerWindow();

            Assert.IsTrue(CompositeWindowMatcher.TryParse("process=explorer && title=^This PC$")!.Matches(window));
            Assert.IsFalse(CompositeWindowMatcher.TryParse("process=explorer && title=^Downloads$")!.Matches(window));
            Assert.IsFalse(CompositeWindowMatcher.TryParse("process=notepad && title=^This PC$")!.Matches(window));
        }

        [TestMethod]
        public void TestCompositeMalformedRuleIsRejected()
        {
            Assert.IsNull(CompositeWindowMatcher.TryParse(""));
            Assert.IsNull(CompositeWindowMatcher.TryParse("explorer"));
            Assert.IsNull(CompositeWindowMatcher.TryParse("process="));
            Assert.IsNull(CompositeWindowMatcher.TryParse("pid=42"));
            Assert.IsNull(CompositeWindowMatcher.TryParse("process=explorer &&"));
        }

        [TestMethod]
        public void TestCompositeCreatedRuleMatchesOnlyThatTitle()
        {
            // Regex metacharacters in the title must be matched literally.
            var rule = CompositeWindowMatcher.CreateRule("explorer", "This PC");
            Assert.AreEqual("process=^explorer$ && title=^This PC$", rule);
            Assert.IsTrue(CompositeWindowMatcher.TryParse(rule)!.Matches(m_mockFactory.CreateExplorerWindow()));

            var trickyRule = CompositeWindowMatcher.CreateRule("explorer", "C++ (draft)");
            Assert.IsNotNull(CompositeWindowMatcher.TryParse(trickyRule));
            Assert.IsFalse(CompositeWindowMatcher.TryParse(trickyRule)!.Matches(m_mockFactory.CreateExplorerWindow()));
        }
    }
}
