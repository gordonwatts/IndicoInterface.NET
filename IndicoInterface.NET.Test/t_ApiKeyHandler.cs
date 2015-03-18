using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace IndicoInterface.NET.Test
{
    [TestClass]
    public class t_ApiKeyHandler
    {
        [TestMethod]
        public void SimpleUriNoExtraInfo1()
        {
            var s = ApiKeyHandler.IndicoEncode("/export/categ/2636.ics", null, null, null);
            Assert.AreEqual("/export/categ/2636.ics", s);
        }

        [TestMethod]
        public void SimpleUriNoExtraInfo2()
        {
            var s = ApiKeyHandler.IndicoEncode("/export/categ/2636.ics", null, "", "");
            Assert.AreEqual("/export/categ/2636.ics", s);
        }

        [TestMethod]
        public void SimpleUriNo1Param()
        {
            var s = ApiKeyHandler.IndicoEncode("/export/categ/2636.ics", new Dictionary<string, string> { { "limit", "10" } }, "", "");
            Assert.AreEqual("/export/categ/2636.ics?limit=10", s);
        }

        [TestMethod]
        public void SimpleUriNo2Param()
        {
            var s = ApiKeyHandler.IndicoEncode("/export/categ/2636.ics", new Dictionary<string, string> { { "limit", "10" }, { "fork", "dude" } }, "", "");
            Assert.AreEqual("/export/categ/2636.ics?fork=dude&limit=10", s);
        }

        [TestMethod]
        public void SimpleUriWithApiKey()
        {
            var s = ApiKeyHandler.IndicoEncode("/export/categ/2636.ics", null, "00000000-0000-0000-0000-000000000000", "");
            Assert.AreEqual("/export/categ/2636.ics?apiKey=00000000-0000-0000-0000-000000000000", s);
        }

        [TestMethod]
        public void SimpleUriWithApiKey2Param()
        {
            var s = ApiKeyHandler.IndicoEncode("/export/categ/2636.ics", new Dictionary<string, string> { { "limit", "10" }, { "fork", "dude" } }, "00000000-0000-0000-0000-000000000000", "");
            Assert.AreEqual("/export/categ/2636.ics?apiKey=00000000-0000-0000-0000-000000000000&fork=dude&limit=10", s);
        }

        [TestMethod]
        public void SimpleUriWithApiKeyAndSecret()
        {
            // Test case generated with tool found at http://indico.readthedocs.org/en/latest/http_api/tools/
            var dt = 1426720253.FromUnixTime();
            var s = ApiKeyHandler.IndicoEncode("/export/categ/2636.ics", null, "00000000-0000-0000-0000-000000000000", "00000000-0000-0000-0000-000000000010", dt);
            Assert.AreEqual("/export/categ/2636.ics?timestamp=1426720253&ak=00000000-0000-0000-0000-000000000000&signature=d2d4defc041097be9a29df7f7101cd356621e910", s);
        }

        [TestMethod]
        public void SimpleUriWithApiKey2ParamAndSecret()
        {
            // Test case generated with tool found at http://indico.readthedocs.org/en/latest/http_api/tools/
            var dt = 1426720253.FromUnixTime();
            var s = ApiKeyHandler.IndicoEncode("/export/categ/2636.ics", new Dictionary<string, string> { { "limit", "10" }, { "fork", "dude" } }, "00000000-0000-0000-0000-000000000000", "00000000-0000-0000-0000-000000000010", dt);
            Assert.AreEqual("/export/categ/2636.ics?timestamp=1426720253&ak=00000000-0000-0000-0000-000000000000&signature=d2d4defc041097be9a29df7f7101cd356621e910", s);
        }
    }
}
