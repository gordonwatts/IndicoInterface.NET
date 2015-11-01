using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
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
        public void OnlineXMLGet()
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
        public async Task OnlineGetFullMeetingInfoWhiteList()
        {
            var a = new AgendaInfo("https://indico.cern.ch/event/340656/");
            var al = new AgendaLoader(new WebGetter());
            var data = await al.GetNormalizedConferenceData(a);

            Assert.AreEqual("340656", data.ID);
            Assert.AreEqual("7th SYMPOSIUM ON LARGE TPCs FOR LOW-ENERGY RARE EVENT DETECTION", data.Title);
        }

        /// <summary>
        /// Make sure that older conferences, etc., still give us info in a form we can deal with (e.g. a good talk URL).
        /// CERN keeps updating, and they are usually the first thing to cause trouble...
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// This test is to indico, and so requires the new format of request (json rather than xml).
        /// </remarks>
        [TestMethod]
        public async Task OnlineCheckForMaterialOnTalks()
        {
            var a = new AgendaInfo("https://indico.cern.ch/event/340656/");
            var al = new AgendaLoader(new WebGetter());
            var data = await al.GetNormalizedConferenceData(a);

            var ses1 = data.Sessions[0];

            var talk = ses1.Talks.Where(t => t.Title.Contains("Dark Matter-Baryogenesis connection: status after LHC run 1 and future tests")).FirstOrDefault();
            Assert.IsNotNull(talk);

            Assert.IsNotNull(talk.SlideURL);
        }

        [TestMethod]
        public async Task OnlineGetFullMeetingInfoNonWhiteList()
        {
            var a = new AgendaInfo("https://indico.fnal.gov/conferenceDisplay.py?confId=9318");
            var al = new AgendaLoader(new WebGetter());
            var data = await al.GetNormalizedConferenceData(a);

            Assert.AreEqual("9318", data.ID);
            Assert.AreEqual("Dark Energy Survey Chicagoland Meeting", data.Title);
        }

        [TestMethod]
        public async Task OnlineGetWhitelistSiteURL()
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
        public async Task OnlineGetCategoryWithSecret()
        {
            var a = new AgendaCategory("https://indico.cern.ch/category/2636/");
            var info = utils.GetApiAndSecret("indicoapi.key");
            var uri = a.GetCagetoryUri(7, info.Item1, info.Item2);

            var req = WebRequest.Create(uri);
            var response = await req.GetResponseAsync();
            using (var strm = response.GetResponseStream())
            {
                using (var txtrdr = new StreamReader(strm))
                {
                    var all = await txtrdr.ReadToEndAsync();
                    Console.WriteLine(all);
                    Assert.IsTrue(all.StartsWith("BEGIN:VCALENDAR"));
                }
            }
        }

        [TestMethod]
        [DeploymentItem("indicoapi.key")]
        public async Task OnlineGetCategoryWithSecretNoTimestamp()
        {
            var a = new AgendaCategory("https://indico.cern.ch/category/3286/");
            var info = utils.GetApiAndSecret("indicoapi.key");
            var uri = a.GetCagetoryUri(7, info.Item1, info.Item2, false);

            var req = WebRequest.Create(uri);
            var response = await req.GetResponseAsync();
            using (var strm = response.GetResponseStream())
            {
                using (var txtrdr = new StreamReader(strm))
                {
                    var all = await txtrdr.ReadToEndAsync();
                    Console.WriteLine(all);
                    Assert.IsTrue(all.StartsWith("BEGIN:VCALENDAR"));
                }
            }
        }

        [TestMethod]
        [DeploymentItem("indicofermiapikey.key")]
        public async Task OnlineGetCategoryWithNoScret()
        {
            var a = new AgendaCategory("https://indico.fnal.gov/categoryDisplay.py?categId=334");
            var info = utils.GetApiAndSecret("indicofermiapikey.key");
            var uri = a.GetCagetoryUri(7, info.Item1, info.Item2, false);

            var req = WebRequest.Create(uri);
            var response = await req.GetResponseAsync();
            using (var strm = response.GetResponseStream())
            {
                using (var txtrdr = new StreamReader(strm))
                {
                    var all = await txtrdr.ReadToEndAsync();
                    Console.WriteLine(all);
                    Assert.IsTrue(all.StartsWith("BEGIN:VCALENDAR"));
                }
            }
        }

#if false
        [TestMethod]
        [DeploymentItem("indicofermiapikey.key")]
        public async Task GetFermiProtectedMeeting()
        {
            var a = new AgendaInfo("https://indico.fnal.gov/conferenceDisplay.py?confId=9227");
            var al = new AgendaLoader(null);
            var info = utils.GetApiAndSecret("indicofermiapikey.key");
            var uri = al.GetAgendaFullXMLURL(a, true, apiKey: info.Item1, secretKey: info.Item2);
            Console.WriteLine(uri.OriginalString);
            var req = WebRequest.Create(uri);
            (req as HttpWebRequest).UserAgent = "DeepTalk AgentaInfo Test";
            using (var res = await req.GetResponseAsync())
            {
                var r = new StreamReader(res.GetResponseStream());
                var data = await r.ReadToEndAsync();
                Console.Write(data);
                Assert.IsTrue(data.IndexOf("<?xml") >= 0, "The return URL does not have XML in it!");
                res.Close();
            }
        }

        [TestMethod]
        [DeploymentItem("indicoapi.key")]
        public async Task GetCERNProtectedMeeting()
        {
            var a = new AgendaInfo("https://indico.cern.ch/event/384406/");
            var al = new AgendaLoader(null);
            var info = utils.GetApiAndSecret("indicoapi.key");
            var uri = al.GetAgendaFullXMLURL(a, true, apiKey: info.Item1, secretKey: info.Item2);
            Console.WriteLine(uri.OriginalString);
            var req = WebRequest.Create(uri);
            (req as HttpWebRequest).UserAgent = "DeepTalk AgentaInfo Test";
            using (var res = await req.GetResponseAsync())
            {
                var r = new StreamReader(res.GetResponseStream());
                var data = await r.ReadToEndAsync();
                Console.Write(data);
                Assert.IsTrue(data.IndexOf("<?xml") >= 0, "The return URL does not have XML in it!");
                res.Close();
            }
        }
#endif
        [TestMethod]
        public async Task OnlineGetIndicoCategoryWhiteList()
        {
            var a = new AgendaCategory("https://indico.cern.ch/export/categ/1l12.ics?from=-120d");
            var uri = a.GetCagetoryUri(240);
            var req = WebRequest.Create(uri);
            var response = await req.GetResponseAsync();
            using (var strm = response.GetResponseStream())
            {
                using (var txtrdr = new StreamReader(strm))
                {
                    var all = await txtrdr.ReadToEndAsync();
                    Console.Write(all);
                    var regex = new Regex("DESCRIPTION");
                    var matches = regex.Matches(all);
                    Assert.IsTrue(matches.Count > 10);
                }
            }
        }

        [TestMethod]
        public async Task OnlineGetNonWhitelistSiteURL()
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
