using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.IO;

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
#if false
        [TestMethod]
        public void GetSimpleMeetingAgenda()
        {
            string url = "http://indico.cern.ch/conferenceDisplay.py?confId=33078";
            AgendaInfo a = new AgendaInfo(url);
            var data = a.GetFullConferenceData();
            Assert.IsTrue(data.ID == "33078", "Conference ID was not filled correctly");
            Assert.IsTrue(data.category == "B-tagging", "Catagory was incorrect!");
            Assert.IsTrue(data.title == "CSC Note 10: Dijet Calibration Working Group Meeting", "Title is not correct");
            Assert.IsTrue(data.startDate == "2008-04-29T16:30:00", "Start date is not right");
            Assert.IsTrue(data.endDate == "2008-04-29T19:25:00", "End date for meeting is not right");

            Assert.IsTrue(data.session == null, "The conference should have no sessions.");
            Assert.IsTrue(data.contribution.Length == 4, "Didn't find the expected 4 talks");
            Assert.IsTrue(data.contribution[0].ID == "2", "ID of first contribution talk is incorrect");
            Assert.IsTrue(data.contribution[0].startDate == "2008-04-29T16:30:00", "Start date of first contribution is not right");
            Assert.IsTrue(data.contribution[0].endDate == "2008-04-29T16:45:00", "End date of contribution is not right");
            Assert.IsTrue(data.contribution[0].speakers != null, "No speakers for talk!");
            Assert.IsTrue(data.contribution[0].speakers.Length == 1, "Speakers shoudl have only one entry in here!");
            Assert.IsTrue(data.contribution[0].speakers[0].users != null, "User should not be null!");
            Assert.IsTrue(data.contribution[0].speakers[0].users.Length == 1, "There should be only one user!");
            Assert.IsTrue(data.contribution[0].speakers[0].users[0].name != null, "User's name should be non-szero!");
            Assert.IsTrue(data.contribution[0].speakers[0].users[0].name.first == "", "User's name is wrong!");
            Assert.IsTrue(data.contribution[0].speakers[0].users[0].name.middle == "", "User's name is wrong!");
            Assert.IsTrue(data.contribution[0].speakers[0].users[0].name.last == "Orin Harris (University of Washington)", "User's name is wrong!");

            var firsttalk = data.contribution[0].material;
            Assert.IsTrue(firsttalk != null, "The material for first talk is a null!");
            Assert.IsTrue(firsttalk.Length == 1, "There should be only one type of material in the talk");
            Assert.IsTrue(firsttalk[0].ID == "slides", "The material type should be slides!");
            Assert.IsTrue(firsttalk[0].title == "Slides", "The material title should be slides!");
            Assert.IsTrue(firsttalk[0].files.file.Length == 1, "There should be only one file associated with this talk");
            Assert.IsTrue(firsttalk[0].files.file[0].name == "s8_29_04_08.pdf", "The talk file is named incorrectly");
            Assert.IsTrue(firsttalk[0].files.file[0].type == "pdf", "The talk file type is not pdf");
            Assert.IsTrue(firsttalk[0].files.file[0].url.StartsWith("http://indico.cern.ch"), "Url of talk doesn't look right");

            Assert.IsTrue(data.contribution[1].material[0].link == null, "Link for second talk doesn't seem to be correct.");
        }
#endif

#if false
        [TestMethod]
        public void GetConferenceMeetingAgenda()
        {
            AgendaInfo a = new AgendaInfo(14475);
            var data = a.GetFullConferenceData();

            Assert.IsTrue(data.ID == "14475", "Conference ID is incorrect.");
            Assert.IsTrue(data.category == "B-tagging", "Conference catagory is incorrect.");
            Assert.IsTrue(data.title == "ATLAS b-tagging workshop", "Conference title is incorrect");
            Assert.IsTrue(data.contribution == null, "The conference should have no contributions!");
            Assert.IsTrue(data.session != null, "The conference should have sessions.");
            Assert.IsTrue(data.session.Length == 11, "The meeting does not have the right number of sessions!");

            Assert.IsTrue(data.session[0].ID == "8", "The first session ID is not 8");
            Assert.IsTrue(data.session[1].ID == "11", "The second session ID is not 11");

            Assert.IsTrue(data.session[0].title == "Introduction I", "Session title is not right");
            Assert.IsTrue(data.session[0].startDate == "2007-05-10T09:15:00", "session start date is not right");
            Assert.IsTrue(data.session[0].endDate == "2007-05-10T10:30:00", "session start date is not right");

            Assert.IsTrue(data.session[0].contribution.Length == 5, "First session has incorrect number of contributions");
            Assert.IsTrue(data.session[1].contribution.Length == 4, "Second session has incorrect number of contributions");

            Assert.IsTrue(data.session[0].contribution[0].ID == "21", "First contribution ID is not correct");
            Assert.IsTrue(data.session[0].contribution[4].ID == "23", "First contribution ID is not correct");

            /// Won't test the internals of the contribution as we expect the XML to be the same as in the simple case.
        }
#endif
#if false
        /// Since CERN turned on authenticaion, this particular guy is no longer accessible, so we will skip this test. When I get around to implementing
        /// authentication, then this will be back in...
        [TestMethod]
        public void GetNormalSimpleMeetingAgenda()
        {
            /// Make sure we normalize this guy correctly.
            string url = "http://indico.cern.ch/conferenceDisplay.py?confId=33078";
            AgendaInfo a = new AgendaInfo(url);
            var data = a.GetNormalizedConferenceData();

            Assert.IsTrue(data.ID == 33078, "Conference ID is incorrect.");
            Assert.IsTrue(data.Site == "indico.cern.ch", "Site is incorrect");
            Assert.IsTrue(data.Title == "CSC Note 10: Dijet Calibration Working Group Meeting", "Conference title is incorrect.");
            Assert.IsTrue(data.StartDate == new DateTime(2008, 04, 29, 16, 30, 0), "Meeting start date is not right");
            Assert.IsTrue(data.EndDate == new DateTime(2008, 04, 29, 19, 25, 0), "Meeting end date is not right");

            Assert.IsTrue(data.Sessions.Length == 1, "Should be only a single session in this meeting");
            Assert.IsTrue(data.Sessions[0].ID == 0, "Null session for meeting should have ID 0");

            Assert.IsTrue(data.Sessions[0].Title == data.Title, "Session title is not the same as the meeting title!");
            Assert.IsTrue(data.Sessions[0].StartDate == data.StartDate, "Session start time is not the same as the meeting start time.");
            Assert.IsTrue(data.Sessions[0].EndDate == data.EndDate, "Session end date is not right either");

            Assert.IsTrue(data.Sessions[0].Talks.Length == 4, "Wrong number of talks in the conference");
            Assert.IsTrue(data.Sessions[0].Talks[0].SlideURL.StartsWith("http://indico.cern.ch"), "URL is not correct");
            Assert.IsTrue(data.Sessions[0].Talks[0].ID == 2, "ID of first talk is not right");
            Assert.IsTrue(data.Sessions[0].Talks[0].Title == "Performance of alternative implementations of System 8", "Title of first talk is not correct");
            Assert.IsTrue(data.Sessions[0].Talks[0].StartDate == new DateTime(2008, 4, 29, 16, 30, 0), "start time of talk is not right");
            Assert.IsTrue(data.Sessions[0].Talks[0].EndDate == new DateTime(2008, 4, 29, 16, 45, 0), "start time of talk is not right");
            Assert.IsTrue(data.Sessions[0].Talks[0].Speakers != null, "Speaker array should not be zero!");
            Assert.IsTrue(data.Sessions[0].Talks[0].Speakers.Length == 1, "Speaker array should be of length 1");
            Assert.IsTrue(data.Sessions[0].Talks[0].Speakers[0] == "Orin Harris (University of Washington)", "Speaker's name is not right!");

            Assert.IsTrue(data.Sessions[0].Talks[1].ID == 3, "ID of second talk not right");
            Assert.IsTrue(data.Sessions[0].Talks[1].SlideURL.StartsWith("http://indico.cern.ch"), "URL of second talk not right!");
        }
#endif

#if false
        [TestMethod]
        public void GetNormalConferenceMeetingAgenda()
        {
            AgendaInfo a = new AgendaInfo(14475);
            var data = a.GetNormalizedConferenceData();

            Assert.IsTrue(data.ID == "14475", "Conference ID is incorrect.");
            Assert.IsTrue(data.Title == "ATLAS b-tagging workshop", "Conference title is incorrect");
            Assert.IsTrue(data.StartDate == new DateTime(2007, 05, 10, 9, 00, 00), "Start date of meeting is not right!");
            Assert.IsTrue(data.EndDate == new DateTime(2007, 05, 11, 18, 00, 00), "Start date of meeting is not right!");

            Assert.IsTrue(data.Sessions != null, "The conference should have sessions.");
            Assert.IsTrue(data.Sessions.Length == 11, "The meeting does not have the right number of sessions!");

            Assert.IsTrue(data.Sessions[0].ID == "8", "The first session ID is not 8");
            Assert.IsTrue(data.Sessions[1].ID == "11", "The second session ID is not 11");

            Assert.IsTrue(data.Sessions[0].Title == "Introduction I", "Title of first session is not correct!");
            Assert.IsTrue(data.Sessions[0].StartDate == new DateTime(2007, 05, 10, 9, 15, 0), "Start time is not correct");
            Assert.IsTrue(data.Sessions[0].EndDate == new DateTime(2007, 05, 10, 10, 30, 0), "Start time is not correct");

            Assert.IsTrue(data.Sessions[0].Talks.Length == 5, "First session has incorrect number of contributions");
            Assert.IsTrue(data.Sessions[1].Talks.Length == 4, "Second session has incorrect number of contributions");

            Assert.IsTrue(data.Sessions[0].Talks[0].ID == "21", "First contribution ID is not correct");
            Assert.IsTrue(data.Sessions[0].Talks[4].ID == "23", "First contribution ID is not correct");
        }
#endif
    }
}
