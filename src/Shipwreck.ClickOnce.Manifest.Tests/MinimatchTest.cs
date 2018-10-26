using Xunit;

namespace Shipwreck.ClickOnce.Manifest
{
    public class MinimatchTest
    {
        [Theory]
        [InlineData("a/b", "a/b", true)]
        [InlineData("a/b", "a/b/c", null)]
        [InlineData("b/c", "a/b/c", null)]
        [InlineData("!a/b", "a/b", false)]
        [InlineData("!a/b", "a/b/c", null)]
        [InlineData("!b/c", "a/b/c", null)]
        [InlineData("a?c", "abc", true)]
        [InlineData("a?c", "a/c", null)]
        [InlineData("a?c", "abbc", null)]
        [InlineData("a*c", "abc", true)]
        [InlineData("a*c", "a/c", null)]
        [InlineData("a*c", "abbc", true)]
        [InlineData("a*c", "ac", null)]
        [InlineData("a*c", "a/bc", null)]
        [InlineData("a*c", "ab/c", null)]
        [InlineData("a*c", "ab/bc", null)]
        [InlineData("a**c", "ab/bc", true)]
        [InlineData("a**c", "ab/b/bc", true)]
        [InlineData("a**c", "a/bc", null)]
        [InlineData("a**c", "ab/c", null)]
        public void TestAll(string pattern, string path, bool? result)
        {
            var d = Minimatch.Compile(pattern);
            Assert.Equal(result, d(path));
        }
    }
}
