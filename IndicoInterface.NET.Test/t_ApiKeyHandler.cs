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
            Assert.AreEqual("/export/categ/2636.ics?apikey=00000000-0000-0000-0000-000000000000", s);
        }

        [TestMethod]
        public void SimpleUriWithApiKey2Param()
        {
            var s = ApiKeyHandler.IndicoEncode("/export/categ/2636.ics", new Dictionary<string, string> { { "limit", "10" }, { "fork", "dude" } }, "00000000-0000-0000-0000-000000000000", "");
            Assert.AreEqual("/export/categ/2636.ics?apikey=00000000-0000-0000-0000-000000000000&fork=dude&limit=10", s);
        }

        [TestMethod]
        public void UriWithMixedCaseParam1()
        {
            // Should be sorted ignoring case
            var s = ApiKeyHandler.IndicoEncode("/export/categ/2636.ics", new Dictionary<string, string> { { "limit", "10" }, { "Fork", "dude" } }, "00000000-0000-0000-0000-000000000000", "");
            Assert.AreEqual("/export/categ/2636.ics?apikey=00000000-0000-0000-0000-000000000000&Fork=dude&limit=10", s);
        }

        [TestMethod]
        public void UriWithMixedCaseParam2()
        {
            // Should be sorted ignoring case
            var s = ApiKeyHandler.IndicoEncode("/export/categ/2636.ics", new Dictionary<string, string> { { "Limit", "10" }, { "fork", "dude" } }, "00000000-0000-0000-0000-000000000000", "");
            Assert.AreEqual("/export/categ/2636.ics?apikey=00000000-0000-0000-0000-000000000000&fork=dude&Limit=10", s);
        }

        [TestMethod]
        public void SimpleUriWithApiKeyAndSecret()
        {
            // Test case generated with tool found at http://indico.readthedocs.org/en/latest/http_api/tools/
            var dt = 1426720253.FromUnixTime();
            var s = ApiKeyHandler.IndicoEncode("/export/categ/2636.ics", null, "00000000-0000-0000-0000-000000000000", "00000000-0000-0000-0000-000000000010", dt);
            Assert.AreEqual("/export/categ/2636.ics?apikey=00000000-0000-0000-0000-000000000000&timestamp=1426720253&signature=25295f72e8f7659634eabaa6a2f64d711f20cb1e", s);
        }

        [TestMethod]
        public void SimpleUriWithApiKey2ParamAndSecret()
        {
            // Test case generated with tool found at http://indico.readthedocs.org/en/latest/http_api/tools/
            var dt = 1426720253.FromUnixTime();
            var s = ApiKeyHandler.IndicoEncode("/export/categ/2636.ics", new Dictionary<string, string> { { "limit", "10" }, { "fork", "dude" } }, "00000000-0000-0000-0000-000000000000", "00000000-0000-0000-0000-000000000010", dt);
            Assert.AreEqual("/export/categ/2636.ics?apikey=00000000-0000-0000-0000-000000000000&fork=dude&limit=10&timestamp=1426720253&signature=95ec880ee6256fbd213a09ec39b4fbaa940f5ea4", s);
        }

        [TestMethod]
        [DeploymentItem("indicoapi.key")]
        [DeploymentItem("indico3285answers.key")]
        public void EncodeMySecret()
        {
            // Fill the indicoapi.key file with your api key in first line, and secret in second line.
            // Fill the indico2385answers.key by getting the url from indico for private look-ahead, and extractin the api key into the first line, and the
            // signature into the second line.
            var a = new AgendaCategory("https://indico.cern.ch/category/3286/");
            var info = utils.GetApiAndSecret("indicoapi.key");
            var uri = a.GetCagetoryUri(7, info.Item1, info.Item2, false);
            var answers = utils.GetApiAndSecret("indico3285answers.key");
            Assert.AreEqual(string.Format("https://indico.cern.ch/export/categ/3286.ics?apikey={0}&from=-7d&signature={1}", answers.Item1, answers.Item2), uri.OriginalString);
        }
    }
}
