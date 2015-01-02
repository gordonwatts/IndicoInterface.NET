using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using IndicoInterface;
using IndicoInterface.NET.SimpleAgendaDataModel;
using IndicoInterface.NET;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace t_IndicoInterface
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class t_AgendaInfo
    {
        [TestMethod]
        public void TestCtor()
        {
            string url = "http://indico.cern.ch/conferenceDisplay.py?confId=14475";
            var a = new AgendaInfo(url);
            Assert.IsTrue("14475" == a.ConferenceID);
            AgendaInfo a1 = new AgendaInfo("http://indico.cern.ch/conferenceOtherViews.py?confId=14475");
            Assert.IsTrue("14475" == a.ConferenceID, "Other random URL does not return proper conference ID");
            AgendaInfo a2 = new AgendaInfo(14475);
            Assert.IsTrue("14475" == a.ConferenceID, "Integer constructor does not return the proper conference ID");
            Assert.IsTrue(a.AgendaSite == "indico.cern.ch", "Agenda website is not right!");
            try
            {
                AgendaInfo a3 = new AgendaInfo("hi there");
                Assert.IsTrue(false, "Exception should have been thrown when invalid URL was passed");
            }
            catch (AgendaException)
            {
            }
        }

        [TestMethod]
        public void EventURLFormat()
        {
            string url = "http://indico.cern.ch/event/297657/";
            var a = new AgendaInfo(url);
            Assert.AreEqual("297657", a.ConferenceID);
            Assert.AreEqual("indico.cern.ch", a.AgendaSite);
            Assert.AreEqual("", a.AgendaSubDirectory);
        }

        [TestMethod]
        public void EventURLWithTrailingOptions()
        {
            string url = "http://indico.cern.ch/event/287488/timetable/";
            var a = new AgendaInfo(url);
            Assert.AreEqual("287488", a.ConferenceID);
            Assert.AreEqual("indico.cern.ch", a.AgendaSite);
            Assert.AreEqual("", a.AgendaSubDirectory);
        }

        [TestMethod]
        public void TestCtorWithFunnyString()
        {
            string url = "http://indico.cern.ch/conferenceDisplay.py?confId=14475\n";
            AgendaInfo a = new AgendaInfo(url);
            Assert.AreEqual("14475", a.ConferenceID, "confID not correct");
        }

        [TestMethod]
        public void TestCtorWithLowerCase()
        {
            string url = "http://indico.cern.ch/conferenceDisplay.py?confid=14475\n";
            AgendaInfo a = new AgendaInfo(url);
            Assert.AreEqual("14475", a.ConferenceID, "confID not correct");
        }

        /// <summary>
        /// Make sure we can deal with non-number types of agenda id's. Yuck!!!
        /// </summary>
        [TestMethod]
        public void TestNonIntAgendaId()
        {
            string url = "http://indico.cern.ch/conferenceDisplay.py?confId=a042880";
            AgendaInfo info = new AgendaInfo(url);
            Assert.IsTrue(info.ConferenceID == "a042880");
        }

        [TestMethod]
        public void TestTimeTableURL()
        {
            string url = "http://indico.cern.ch/conferenceTimeTable.py?confId=74604#20100622";
            AgendaInfo info = new AgendaInfo(url);
            Assert.AreEqual("74604", info.ConferenceID, "Timetable .py with a # is not being parsed correctly");
        }

        [TestMethod]
        public void TestHTTPS()
        {
            AgendaInfo ai_s = new AgendaInfo("https://indico.desy.de/conferenceOtherViews.py?view=standard&confId=1356");
            Assert.AreEqual("1356", ai_s.ConferenceID, "Incorrect agenda ID");
        }
    }
}
