using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace IndicoInterface.NET.Test
{
    /// <summary>
    /// All tests here require an active network connection. This is just an easy way to keep them straight.
    /// </summary>
    [TestClass]
    public class t_ActiveNet
    {
        [TestMethod]
        public void TestXMLGet()
        {
            var url = "http://indico.cern.ch/conferenceDisplay.py?confId=a042880";
            var a = new AgendaInfo(url);
            var al = new AgendaLoader(null);
            var req = WebRequest.Create(al.GetAgendaFullXMLURL(a));
            (req as HttpWebRequest).UserAgent = "DeepTalk AgentaInfo Test";
            using (WebResponse res = req.GetResponse())
            {
                var r = new StreamReader(res.GetResponseStream());
                var data = r.ReadToEnd();
                Assert.IsTrue(data.IndexOf("<?xml") >= 0, "The return URL does not have XML in it!");
                res.Close();
            }
        }

        /// <summary>
        /// Simple URL reader that will fetch a URL from the web.
        /// </summary>
        class WebGetter : IUrlFetcher
        {
            public async Task<StreamReader> GetDataFromURL(Uri uri)
            {
                var req = WebRequest.Create(uri);
                (req as HttpWebRequest).UserAgent = "DeepTalk AgentaInfo Test";
                var response = await req.GetResponseAsync();
                return new StreamReader(response.GetResponseStream());
            }
        }

        [TestMethod]
        public async Task GetFullMeetingInfoWhiteList()
        {
            var a = new AgendaInfo("https://indico.cern.ch/event/340656/");
            var al = new AgendaLoader(new WebGetter());
            var data = await al.GetNormalizedConferenceData(a);

            Assert.AreEqual("340656", data.ID);
            Assert.AreEqual("7th SYMPOSIUM ON LARGE TPCs FOR LOW-ENERGY RARE EVENT DETECTION", data.Title);
        }

        [TestMethod]
        public async Task GetFullMeetingInfoNonWhiteList()
        {
            var a = new AgendaInfo("https://indico.fnal.gov/conferenceDisplay.py?confId=9318");
            var al = new AgendaLoader(new WebGetter());
            var data = await al.GetNormalizedConferenceData(a);

            Assert.AreEqual("9318", data.ID);
            Assert.AreEqual("Dark Energy Survey Chicagoland Meeting", data.Title);
        }

        [TestMethod]
        public async Task GetWhitelistSiteURL()
        {
            var a = new AgendaInfo("https://indico.cern.ch/event/340656/");
            var uri = a.ConferenceUrl;
            var req = WebRequest.Create(uri);
            var response = await req.GetResponseAsync();
            using (var strm = response.GetResponseStream())
            {
                using (var txtrdr = new StreamReader(strm))
                {
                    var all = await txtrdr.ReadToEndAsync();
                    Assert.IsTrue(all.Contains("7th SYMPOSIUM ON LARGE TPCs FOR LOW-ENERGY RARE EVENT DETECTION"));
                }
            }
        }

        [TestMethod]
        [DeploymentItem("indicoapi.key")]
        public async Task GetCategoryWithSecret()
        {
            var a = new AgendaCategory("https://indico.cern.ch/category/2636/");
            var info = GetApiAndSecret("indicoapi.key");
            var uri = a.GetCagetoryUri(7, info.Item1, info.Item2);

            var req = WebRequest.Create(uri);
            var response = await req.GetResponseAsync();
            using (var strm = response.GetResponseStream())
            {
                using (var txtrdr = new StreamReader(strm))
                {
                    var all = await txtrdr.ReadToEndAsync();
                    Console.WriteLine(all);
                }
            }
            Assert.Inconclusive();
        }

        /// <summary>
        /// Do a file...
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private Tuple<string, string> GetApiAndSecret(string p)
        {
            var f = new FileInfo(p);
            Assert.IsTrue(f.Exists);
            using (var fs = f.OpenText())
            {
                var l1 = fs.ReadLine();
                var l2 = fs.ReadLine();
                return Tuple.Create(l1, l2);
            }
        }

        [TestMethod]
        public async Task GetIndicoCategoryWhiteList()
        {
            var a = new AgendaCategory("https://indico.cern.ch/export/categ/1l12.ics?from=-60d");
            var uri = a.GetCagetoryUri(120);
            var req = WebRequest.Create(uri);
            var response = await req.GetResponseAsync();
            using (var strm = response.GetResponseStream())
            {
                using (var txtrdr = new StreamReader(strm))
                {
                    var all = await txtrdr.ReadToEndAsync();
                    Console.Write(all);
                    Assert.IsTrue(all.Contains("7th SYMPOSIUM ON LARGE TPCs FOR LOW-ENERGY RARE EVENT DETECTION"));
                }
            }
        }

        [TestMethod]
        public async Task GetNonWhitelistSiteURL()
        {
            var a = new AgendaInfo("https://indico.fnal.gov/conferenceDisplay.py?confId=9318");
            var uri = a.ConferenceUrl;
            var req = WebRequest.Create(uri);
            var response = await req.GetResponseAsync();
            using (var strm = response.GetResponseStream())
            {
                using (var txtrdr = new StreamReader(strm))
                {
                    var all = await txtrdr.ReadToEndAsync();
                    Assert.IsTrue(all.Contains("Dark Energy Survey Chicagoland Meeting"));
                }
            }
        }
    }
}
