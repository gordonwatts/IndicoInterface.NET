using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using IndicoInterface;
using IndicoInterface.NET.SimpleAgendaDataModel;
using IndicoInterface.NET;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Xml.Serialization;

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

        [TestMethod]
        public void TestSubDayLinks()
        {
            AgendaInfo ai = new AgendaInfo("http://indico.desy.de/conferenceOtherViews.py?view=standard&confId=1356#2009-01-07");
            Assert.AreEqual("1356", ai.ConferenceID, "The conference ID is not correct!");
        }

        [TestMethod]
        public void TestNoSubDirectory()
        {
            AgendaInfo ai = new AgendaInfo("http://indico.desy.de/conferenceOtherViews.py?view=standard&confId=1356#2009-01-07");
            Assert.AreEqual("", ai.AgendaSubDirectory, "Sub directory isn't blank");
        }

        [TestMethod]
        public void TestIndicoInSubDir()
        {
            AgendaInfo ai = new AgendaInfo("http://indico.ific.uv.es/indico/conferenceDisplay.py?confId=62");
            Assert.AreEqual("62", ai.ConferenceID, "The conference ID is not correct!");
            Assert.AreEqual("indico", ai.AgendaSubDirectory, "The subdirectory isn't right");
            Assert.AreEqual("indico.ific.uv.es", ai.AgendaSite, "The agenda site isn't right");
        }

        [TestMethod]
        public void TestURL()
        {
            AgendaInfo ai = new AgendaInfo("http://indico.cern.ch/conferenceOtherViews.py?view=standard&confId=86819#2010041");
            Assert.AreEqual("http://indico.cern.ch/conferenceDisplay.py?confId=86819", ai.ConferenceUrl, "URL is not correct!");

            ai = new AgendaInfo("http://indico.ific.uv.es/indico/conferenceDisplay.py?confId=62");
            Assert.AreEqual("http://indico.ific.uv.es/indico/conferenceDisplay.py?confId=62", ai.ConferenceUrl, "Subdir URL is not right");
        }

        /// <summary>
        /// Make sure we can make one of these XML and back again.
        /// </summary>
        [TestMethod]
        public void TestAgendaSerialization()
        {
            var ai = new AgendaInfo("http://indico.fnal.gov/conferenceTimeTable.py?confId=1829");

            var ser = new XmlSerializer(typeof(AgendaInfo));
            StringWriter sw = new StringWriter();
            ser.Serialize(sw, ai);

            string xml = sw.ToString();
            Assert.AreNotEqual(null, xml, "the xml translation shouldn't be null!");

            StringReader rdr = new StringReader(xml);
            AgendaInfo aiback = ser.Deserialize(rdr) as AgendaInfo;

            Assert.IsNotNull(aiback, "Null agenda came back!");
            Assert.IsTrue(aiback.ConferenceID == ai.ConferenceID, "Conference ID is not correct");
            Assert.IsTrue(ai.AgendaSite == aiback.AgendaSite, "Conference site is not correct!");
        }

        [TestMethod]
        public void TestAgendaDeseralizationWithNoSub()
        {
            /// We've added the sub directory. Make sure that the XML can be delt with!

            AgendaInfo ai = new AgendaInfo("http://indico.fnal.gov/conferenceTimeTable.py?confId=1829");

            string xml = "<?xml version=\"1.0\" encoding=\"utf-16\"?><AgendaInfo xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><ConferenceID>1829</ConferenceID><AgendaSite>indico.fnal.gov</AgendaSite></AgendaInfo>";

            StringReader rdr = new StringReader(xml);
            var ser = new XmlSerializer(typeof(AgendaInfo));
            AgendaInfo back = ser.Deserialize(rdr) as AgendaInfo;

            var al = new AgendaLoader(null);

            Console.WriteLine(al.GetAgendaFullXMLURL(ai).OriginalString);
            Console.WriteLine(al.GetAgendaFullXMLURL(back).OriginalString);

            Assert.AreEqual(al.GetAgendaFullXMLURL(ai).OriginalString, al.GetAgendaFullXMLURL(back).OriginalString, "The http requests are not the same! Ops!");
        }

        [TestMethod]
        public void TestAgendaDeseralizationWithSub()
        {
            /// We've added the sub directory. Make sure that the XML can be delt with!

            AgendaInfo ai = new AgendaInfo("http://indico.fnal.gov/bogus/conferenceTimeTable.py?confId=1829");

            string xml = "<?xml version=\"1.0\" encoding=\"utf-16\"?><AgendaInfo xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><ConferenceID>1829</ConferenceID><AgendaSite>indico.fnal.gov</AgendaSite><AgendaSubDirectory>bogus</AgendaSubDirectory></AgendaInfo>";

            StringReader rdr = new StringReader(xml);
            var ser = new XmlSerializer(typeof(AgendaInfo));
            AgendaInfo back = ser.Deserialize(rdr) as AgendaInfo;

            var al = new AgendaLoader(null);

            Console.WriteLine(al.GetAgendaFullXMLURL(ai).OriginalString);
            Console.WriteLine(al.GetAgendaFullXMLURL(back).OriginalString);

            Assert.AreEqual(al.GetAgendaFullXMLURL(ai).OriginalString, al.GetAgendaFullXMLURL(back).OriginalString, "The http requests are not the same! Ops!");
        }
    }
}
