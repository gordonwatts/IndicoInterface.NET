﻿using IndicoInterface.NET.SimpleAgendaDataModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace IndicoInterface.NET.Test
{
    [TestClass]
    public class t_AgendaLoader
    {
        [TestMethod]
        [DeploymentItem("ichep2010.xml")]
        public async Task TestMeetingTitle()
        {
            AgendaInfo a = new AgendaInfo("http://indico.cern.ch/conferenceDisplay.py?confId=73513");
            var al = new AgendaLoader(new FileReader("ichep2010.xml"));
            Assert.AreEqual("ICHEP 2010", (await al.GetNormalizedConferenceData(a)).Title, "Title is incorrect");
        }

        class FileReader : IUrlFetcher
        {
            private string _fname;
            public FileReader (string fname)
            {
                _fname = fname;
            }
            public Task<StreamReader> GetDataFromURL(Uri uri)
            {
                var fi = new FileInfo(_fname);
                Assert.IsTrue(fi.Exists);

                return Task<StreamReader>.Factory.StartNew(() =>
                {
                    return fi.OpenText();
                });
            }
        }

        [TestMethod]
        [DeploymentItem("EvtGen-miniworkshop.xml")]
        public async Task TestGetPS()
        {
            /// For a particular agenda there seems to be a missing ps from the talk list. At least,
            /// I'm going to call that a bug!

            string url = "http://indico.cern.ch/conferenceDisplay.py?confId=a042880";
            AgendaInfo info = new AgendaInfo(url);
            var al = new AgendaLoader(new FileReader("EvtGen-miniworkshop.xml"));

            var meeting = await al.GetNormalizedConferenceData(info);

            Assert.IsNotNull(meeting.Sessions, "Should be some sessions!");
            var s0 = meeting.Sessions[0];
            Assert.IsNotNull(s0, "Should be one session");
            Assert.IsNotNull(s0.Talks, "Should be talks");
            Assert.IsTrue(s0.Talks.Length > 1, "Should be at least 2 talks!");
            var t1 = s0.Talks[1];
            Assert.IsTrue(t1.ID == "s1t14", "The second talk doesn't seem to be the correct talk!");
            Assert.IsTrue(t1.Title == "Brief EvtGen Tutorial", "Title is not right either");
            Assert.IsNotNull(t1.SlideURL, "Slide URL should not be null!");
        }

        [TestMethod]
        [DeploymentItem("EvtGen-miniworkshop.xml")]
        public async Task GetNormalSimpleMeeting()
        {
            string url = "http://indico.cern.ch/conferenceDisplay.py?confId=a042880";
            AgendaInfo info = new AgendaInfo(url);
            var al = new AgendaLoader(new FileReader("EvtGen-miniworkshop.xml"));

            var data = await al.GetNormalizedConferenceData(info);

                    Assert.IsTrue(data.ID == "a042880", "Conference ID is incorrect.");
                    Assert.IsTrue(data.Site == "indico.cern.ch", "Conference site is incorrect");
                    Assert.AreEqual("EvtGen miniworkshop", data.Title, "Title is not right");
                    Assert.IsTrue(data.StartDate == new DateTime(2005, 01, 21, 9, 0, 0), "Start date is not right");
                    Assert.IsTrue(data.EndDate == new DateTime(2005, 01, 21, 19, 0, 0), "End date is not right");

                    Assert.IsTrue(data.Sessions.Length == 1, "Should have only a single session");
                    Assert.IsTrue(data.Sessions[0].ID == "0", "Default session ID is not set correctly");

                    var ses = data.Sessions[0];
                    Assert.IsTrue(ses.Title == data.Title, "Session title should be the same as meeting title");
                    Assert.IsTrue(ses.StartDate == data.StartDate, "Session start date should match meeting start date");
                    Assert.IsTrue(ses.EndDate == data.EndDate, "Session end date should match meeting end date");

                    Assert.IsTrue(ses.Talks.Length == 14, "Incorrect number of talks in session!");

                    var talk1 = ses.Talks[0];
                    Assert.IsTrue(talk1.ID == "s1t15", "ID of talk is not correct");
                    Assert.AreEqual(TypeOfTalk.Talk, talk1.TalkType, "Talk type isn't right");
                    Assert.IsTrue(talk1.Title == "Introduction to the EvtGen Mini Workshop", "Talk title is not right");
                    Assert.IsTrue(talk1.StartDate == new DateTime(2005, 01, 21, 9, 0, 0), "Start time of talk is not correct");
                    Assert.IsTrue(talk1.EndDate == new DateTime(2005, 01, 21, 9, 15, 0), "End time of talk is not right");
                    Assert.IsNotNull(talk1.SlideURL, "The URL for the slides should not be null!");
                    Assert.IsTrue(talk1.SlideURL.StartsWith("http://indico.cern.ch"), "Slide URL is not correct");
                    Assert.IsTrue(talk1.Speakers != null, "Speaker list should not be null!");
                    Assert.IsTrue(talk1.Speakers.Length == 1, "Should be only one speaker");
                    Assert.IsTrue(talk1.Speakers[0] == "Bartalini, P.", "Speakers name is not correct");
        }

        [TestMethod]
        [DeploymentItem("EvtGen-miniworkshop.xml")]
        public async Task GetFullSimpleMeeting()
        {
            string url = "http://indico.cern.ch/conferenceDisplay.py?confId=a042880";
            AgendaInfo info = new AgendaInfo(url);
            var al = new AgendaLoader(new FileReader("EvtGen-miniworkshop.xml"));

            var data = await al.GetFullConferenceData(info);

                    /// Just some simple tests to help pin things down.

                    Assert.IsTrue(data != null, "Null data came back - crazy!");
                    Assert.IsTrue(data.session == null, "Should be only one session");
                    Assert.IsTrue(data.contribution.Length > 0, "Should be some contributions at the top level here");

                    var talk1 = data.contribution[0];
                    Assert.IsTrue(talk1.title == "Introduction to the EvtGen Mini Workshop", "Title is incorrect");
                    Assert.IsTrue(talk1.material != null, "Material should not be null!");
                    Assert.IsTrue(talk1.material.Length > 0, "We should have some material for this talk!");
                    Assert.IsTrue(talk1.material[0].files != null, "The file link is null");
                    Assert.IsTrue(talk1.material[0].files.file != null, "The file of files is null");
                    Assert.IsTrue(talk1.material[0].files.file.Length > 0, "Expected at least one file!");
                    Assert.IsTrue(talk1.material[0].files.file[0].url.StartsWith("http://indico.cern.ch"), "The pdf link does not start correctly");
        }

        [TestMethod]
        [DeploymentItem("pheno2008.xml")]
        public async Task GetWisconsonAgenda()
        {
            AgendaInfo a = new AgendaInfo("http://agenda.hep.wisc.edu/conferenceOtherViews.py?view=standard&confId=60");
            Assert.IsTrue(a.ConferenceID == "60", "Conference ID is incorrect!");
            Assert.IsTrue(a.AgendaSite == "agenda.hep.wisc.edu", "Agenda website is not right!");

            var al = new AgendaLoader(new FileReader("pheno2008.xml"));
            var data = await al.GetNormalizedConferenceData(a);
            
            Assert.IsTrue(data.Title == "Pheno 2008 Symposium", "Agenda title is not right");
        }

        [TestMethod]
        [DeploymentItem("pheno2008.xml")]
        public async Task TestPDFLink()
        {
            AgendaInfo a = new AgendaInfo("http://agenda.hep.wisc.edu/conferenceOtherViews.py?view=standard&confId=60");
            var al = new AgendaLoader(new FileReader("pheno2008.xml"));
            var data = await al.GetNormalizedConferenceData(a);
                    var session1 = data.Sessions[0];
                    Assert.IsTrue(session1.Title == "Plenary 1", "The title of the first session is incorrect");
                    var talk1 = session1.Talks[1];
                    Assert.IsTrue(talk1.Title == "SM Tests at the Tevatron Collider", "Title of talk 2 is incorrect");
                    Assert.IsTrue(talk1.SlideURL == "http://agenda.hep.wisc.edu/getFile.py/access?contribId=1&sessionId=10&resId=0&materialId=slides&confId=60", "Slide URL for PDFS for talk 1 is not right");
        }

        [TestMethod]
        [DeploymentItem("pheno2008.xml")]
        public async Task TestPPTLink()
        {
            AgendaInfo a = new AgendaInfo("http://agenda.hep.wisc.edu/conferenceOtherViews.py?view=standard&confId=60");
            var al = new AgendaLoader(new FileReader("pheno2008.xml"));
            var data = await al.GetNormalizedConferenceData(a);
                    var session1 = data.Sessions[2];
                    Assert.IsTrue(session1.Title == "Supersymmetry Breaking", "The title of the first session is incorrect");
                    var talk1 = session1.Talks[2];
                    Assert.IsTrue(talk1.Title == "Discovering Sparticles in the CMSSM and GUT-less SUSY-Breaking Scenarios", "Title of talk 2 is incorrect");
                    Assert.IsTrue(talk1.SlideURL == "http://agenda.hep.wisc.edu/getFile.py/access?contribId=85&sessionId=15&resId=0&materialId=slides&confId=60", "Slide URL for PPT for talk 1 is not right");
        }

        [TestMethod]
        [DeploymentItem("HCP2008.xml")]
        public async Task GetFermiIndicoTalk()
        {
            /// Something goes wrong when we try to get a talk from Fermilab
            AgendaInfo a = new AgendaInfo("http://indico.fnal.gov/conferenceTimeTable.py?confId=1829");

            Assert.IsTrue(a.ConferenceID == "1829", "Conference ID is incorrect");
            Assert.IsTrue(a.AgendaSite == "indico.fnal.gov", "Agenda web site is not right");

            var al = new AgendaLoader(new FileReader("HCP2008.xml"));
            var data = await al.GetNormalizedConferenceData(a);

            Assert.IsTrue(data.Title == "Hadron Collider Physics Symposium 2008 - HCP08", "Conference title is not right");
        }

        [TestMethod]
        [DeploymentItem("HCP2008.xml")]
        public async Task GetPPTOverPDF()
        {
            /// Something goes wrong when we try to get a talk from Fermilab
            AgendaInfo a = new AgendaInfo("http://indico.fnal.gov/conferenceTimeTable.py?confId=1829");
            var al = new AgendaLoader(new FileReader("HCP2008.xml"));
            var data = await al.GetNormalizedConferenceData(a);

                    Session s = data.Sessions[0];
                    Talk t = s.Talks[2];
                    Assert.IsTrue(t.Title == "Top Quark Mass and Cross Section Results from the Tevatron", "Title of talk 3 isn't right");
                    Assert.IsTrue(t.SlideURL == "http://indico.fnal.gov/getFile.py/access?contribId=2&sessionId=0&resId=0&materialId=slides&confId=1829", "URL for PDF is not right");

                    s = data.Sessions[1];
                    t = s.Talks[0];
                    Assert.IsTrue(t.Title == "W/Z Properties at the Tevatron", "title of session 2 talk not right");
                    Assert.IsTrue(t.SlideURL == "http://indico.fnal.gov/getFile.py/access?contribId=6&sessionId=1&resId=2&materialId=slides&confId=1829", "Slide URL of ppt is not right");
        }

        [TestMethod]
        public void TestIndicoInSubDir()
        {
            var ai = new AgendaInfo("http://indico.ific.uv.es/indico/conferenceDisplay.py?confId=62");
            var al = new AgendaLoader(null);
            Assert.IsTrue(al.GetAgendaFullXMLURL(ai).OriginalString.StartsWith("http://indico.ific.uv.es/indico"), "The full XML agenda guy doesn't start correctly!");
        }

        [TestMethod]
        [DeploymentItem("MinBias.xml")]
        public async Task TestTalksAsPapers()
        {
            AgendaInfo ai = new AgendaInfo("http://indico.cern.ch/conferenceOtherViews.py?view=standard&confId=83609");
            var al = new AgendaLoader(new FileReader("MinBias.xml"));
            var agenda = await al.GetNormalizedConferenceData(ai);

            Assert.AreEqual(6, agenda.Sessions.Length, "Wrong number of sessions");
                    var bsession = agenda.Sessions[2];
                    Assert.AreEqual("Modeling of MB and diffraction: Impact of MC modeling on the extraction of luminosity from MB triggers ", bsession.Title, "Session name was not expected!");
                    Assert.AreEqual(1, bsession.Talks.Length, "Expected only one talk in the session");
                    Assert.AreEqual("http://indico.cern.ch/getFile.py/access?contribId=13&sessionId=1&resId=0&materialId=paper&confId=83609", bsession.Talks[0].SlideURL, "Slide URL is not right for the paper talk!");
        }

        [TestMethod]
        [DeploymentItem("ilc.xml")]
        public async Task TestSubTalksRaw()
        {
            AgendaInfo ai = new AgendaInfo("http://ilcagenda.linearcollider.org/conferenceOtherViews.py?view=standard&confId=3154");
            var al = new AgendaLoader(new FileReader("ilc.xml"));
            var agenda = await al.GetFullConferenceData(ai);

                    var ses = (from s in agenda.session where s.title.StartsWith("AAP Review: Exec/SCRF") select s).FirstOrDefault();
                    Assert.IsNotNull(ses, "Could not find the right session!");
                    Assert.AreEqual(2, ses.contribution.Length, "Expected two talks in this session");

                    var talk2 = ses.contribution[1];
                    Assert.AreEqual(3, talk2.subcontributions.Length, "Incorrect number of sub-contributions!");
                    Assert.AreEqual("9:30 - Introduction - A. Yamamoto", talk2.subcontributions[0].title, "Inproper first title!");
        }

        [TestMethod]
        [DeploymentItem("ilc.xml")]
        public async Task TestSubTalks()
        {
            AgendaInfo ai = new AgendaInfo("http://ilcagenda.linearcollider.org/conferenceOtherViews.py?view=standard&confId=3154");
            var al = new AgendaLoader(new FileReader("ilc.xml"));
            var agenda = await al.GetNormalizedConferenceData(ai);

                    var ses = (from s in agenda.Sessions where s.Title.StartsWith("AAP Review: Exec/SCRF") select s).FirstOrDefault();
                    Assert.IsNotNull(ses, "Could not find the proper session!");
                    Assert.AreEqual(2, ses.Talks.Length, "Incorrect # of talks");

                    var talk2 = ses.Talks[1];
                    Assert.IsNotNull(talk2.SlideURL, "Slides were not found for the second talk!");
                    Trace.WriteLine("Talk 2 title is " + talk2.Title);

                    Assert.IsNotNull(talk2.SubTalks, "Expected some subtalks here...");
                    Assert.AreEqual(3, talk2.SubTalks.Length, "Inproper number of sub talks!");
                    Talk subT0 = talk2.SubTalks[0];
                    Assert.AreEqual("9:30 - Introduction - A. Yamamoto", subT0.Title, "Incorrect title for first sub-talk");
                    Assert.IsNotNull(subT0.SlideURL, "slide url should not be zero for the first sub-talk!");
        }

        [TestMethod]
        [DeploymentItem("DarkMatterDirect.xml")]
        public async Task TestAttachedSessionMaterialRaw()
        {
            AgendaInfo ai = new AgendaInfo("http://indico.cern.ch/conferenceDisplay.py?confId=44160");
            var al = new AgendaLoader(new FileReader("DarkMatterDirect.xml"));
            var agenda = await al.GetFullConferenceData(ai);

                    var sesWMat = (from s in agenda.session
                                   where s != null && s.material != null && s.material.Length > 0
                                   select s).FirstOrDefault();

                    Assert.IsNotNull(sesWMat, "Should have found at least one session with some material connected to it!");
                    Assert.AreEqual("13", sesWMat.ID, "Expected the session ID to be something else!");
                    Assert.AreEqual(1, sesWMat.material.Length, "Expected 1 thing associated with the first session");
                    Assert.AreEqual("slides_Michael_Schubnell", sesWMat.material[0].title, "Title was incorrect");
        }

        [TestMethod]
        [DeploymentItem("DarkMatterDirect.xml")]
        public async Task TestAttachedSessionMaterial()
        {
            AgendaInfo ai = new AgendaInfo("http://indico.cern.ch/conferenceDisplay.py?confId=44160");
            var al = new AgendaLoader(new FileReader("DarkMatterDirect.xml"));
            var agenda = await al.GetNormalizedConferenceData(ai);

                    var sesWMat = (from s in agenda.Sessions
                                   where s.SessionMaterial != null && s.SessionMaterial.Length > 0
                                   select s).FirstOrDefault();

                    Assert.IsNotNull(sesWMat, "Should have found at least one session with some material connected to it!");
                    Assert.AreEqual("13", sesWMat.ID, "Expected the session ID to be something else!");
                    Assert.AreEqual(1, sesWMat.SessionMaterial.Length, "Expected 1 thing associated with the first session");
                    Assert.AreEqual("slides_Michael_Schubnell", sesWMat.SessionMaterial[0].Title, "Title was incorrect");
                    Assert.AreEqual(TypeOfTalk.ExtraMaterial, sesWMat.SessionMaterial[0].TalkType, "The talk type isn't correct!");
        }

        [TestMethod]
        [DeploymentItem("DarkMatterDirect.xml")]
        public async Task TestAttachedMeetingMaterialRaw()
        {
            AgendaInfo ai = new AgendaInfo("http://indico.cern.ch/conferenceDisplay.py?confId=44160");
            var al = new AgendaLoader(new FileReader("DarkMatterDirect.xml"));
            var agenda = await al.GetFullConferenceData(ai);

                    Assert.IsNotNull(agenda.material, "Expected non-null material associated with the session!");
                    Assert.AreEqual(3, agenda.material.Length, "Incorrect number of files associated with the agenda");

                    Assert.AreEqual(1, (from m in agenda.material where m.ID == "2" select m).FirstOrDefault().files.file.Count(), "Bad # of files for the schedule");
                    Assert.AreEqual(12, (from m in agenda.material where m.ID == "slides" select m).FirstOrDefault().files.file.Count(), "Bad # of files for the slides of review talks");
                    Assert.AreEqual(10, (from m in agenda.material where m.ID == "3" select m).FirstOrDefault().files.file.Count(), "Bad # of files for the short talk slides");
        }

        [TestMethod]
        [DeploymentItem("DarkMatterDirect.xml")]
        public async Task TestAttachedMeetingMaterial()
        {
            AgendaInfo ai = new AgendaInfo("http://indico.cern.ch/conferenceDisplay.py?confId=44160");
            var al = new AgendaLoader(new FileReader("DarkMatterDirect.xml"));
            var agenda = await al.GetNormalizedConferenceData(ai);

                    Assert.IsNotNull(agenda.MeetingTalks, "Expected non-null list of talks for this meeting at top level!");
                    Assert.AreEqual(3, agenda.MeetingTalks.Length, "Incorrect # of top level meeting talks!");

                    var talk1 = (from t in agenda.MeetingTalks where t.ID == "2" select t).FirstOrDefault();
                    Assert.IsNotNull(talk1, "Missing talk ID 2");
                    Assert.IsNull(talk1.SlideURL, "Talks at meeting level should be null!");
                    Assert.IsNotNull(talk1.SubTalks, "Expected some sub talks!");
                    Assert.AreEqual(1, talk1.SubTalks.Length, "incorrect # of talks for this level");
                    Assert.AreEqual(TypeOfTalk.ExtraMaterial, talk1.SubTalks[0].TalkType, "Incorrect talk type!");

                    var talk2 = (from t in agenda.MeetingTalks where t.ID == "slides" select t).FirstOrDefault();
                    Assert.IsNotNull(talk2, "Missing talk ID slides");
                    Assert.IsNull(talk2.SlideURL, "Talks at meeting level should be null!");
                    Assert.IsNotNull(talk2.SubTalks, "Expected some sub talks!");
                    Assert.AreEqual(12, talk2.SubTalks.Length, "incorrect # of talks for this level");

                    var talk3 = (from t in agenda.MeetingTalks where t.ID == "3" select t).FirstOrDefault();
                    Assert.IsNotNull(talk3, "Missing talk ID 3");
                    Assert.IsNull(talk3.SlideURL, "Talks at meeting level should be null!");
                    Assert.IsNotNull(talk3.SubTalks, "Expected some sub talks!");
                    Assert.AreEqual(10, talk3.SubTalks.Length, "incorrect # of talks for this level");
        }

        [TestMethod]
        [DeploymentItem("lhcxsections.xml")]
        public async Task TestAgendaWithSubContribRaw()
        {
            AgendaInfo ai = new AgendaInfo("http://indico.cern.ch/conferenceOtherViews.py?view=standard&confId=86819#2010041");
            var al = new AgendaLoader(new FileReader("lhcxsections.xml"));
            var agenda = await al.GetFullConferenceData(ai);

                ///
                /// Get the first session with some talks we can't seem to parse
                /// 

                var ses1 = (from s in agenda.session
                            where s.ID == "1"
                            select s.contribution).FirstOrDefault();

                Assert.IsNotNull(ses1, "Didn't find session 1");

                var cont3 = (from c in ses1
                             where c.ID == "3"
                             select c).FirstOrDefault();

                var subcont = (from sc in cont3.subcontributions
                               where sc.ID == "0"
                               select sc).FirstOrDefault();

                Assert.IsNotNull(subcont, "There is no sub-contribution with id 0");
        }

        [TestMethod]
        [DeploymentItem("lhcxsections.xml")]
        public async Task TestAgendaWithSubContrib()
        {
            AgendaInfo ai = new AgendaInfo("http://indico.cern.ch/conferenceOtherViews.py?view=standard&confId=86819#2010041");
            var al = new AgendaLoader(new FileReader("lhcxsections.xml"));
            var agenda = await al.GetNormalizedConferenceData(ai);

                var ses1 = (from s in agenda.Sessions
                            where s.ID == "1"
                            select s.Talks).FirstOrDefault();
                Assert.IsNotNull(ses1, "Failed to find session 1");

                var talk3 = (from t in ses1
                             where t.ID == "3"
                             select t).FirstOrDefault();
                Assert.IsNotNull(talk3, "Talk with ID 3 is not found!");

                Assert.IsNull(talk3.SlideURL, "Unexpected URL for slides for talk 3!");
                Assert.AreEqual(2, talk3.SubTalks.Length, "Incorrect # of sub talks");
        }

        [TestMethod]
        [DeploymentItem("earlyICHEPAgenda.xml")]
        public async Task TestReadFromStream()
        {
            var ai = new AgendaInfo("http://indico.cern.ch/conferenceTimeTable.py?confId=73513");
            var al = new AgendaLoader(new FileReader("earlyICHEPAgenda.xml"));
            var data = await al.GetNormalizedConferenceData(ai);
                    Assert.AreEqual("ICHEP 2010", data.Title, "Title wasn't found!");
        }

        [TestMethod]
        [DeploymentItem("earlyICHEPAgenda.xml")]
        public async Task TestNullDataFromICHEP()
        {
            var ai = new AgendaInfo("http://indico.cern.ch/conferenceTimeTable.py?confId=73513");
            var al = new AgendaLoader(new FileReader("earlyICHEPAgenda.xml"));
            var data = await al.GetNormalizedConferenceData(ai);
                    var bsm = (from s in data.Sessions
                               where s.Title.Contains("Beyond the Standard Model")
                               select s).ToArray();
                    Assert.IsNotNull(bsm, "Expected to find the session!");
                    Assert.AreEqual(8, bsm.Length, "Unexpected number of sessions");

                    var allSessionMaterial = (from s in bsm
                                              from m in s.SessionMaterial
                                              select m).ToArray();

                    Assert.IsTrue(allSessionMaterial.Length == 0, "Expected no section material");

                    var allSessionTalks = from s in bsm
                                          from t in s.Talks
                                          select t;

                    var allTalks = from t in allSessionTalks
                                   where t.SlideURL != null && t.SlideURL != ""
                                   select t;
                    foreach (var item in allTalks)
                    {
                        Console.WriteLine("Talk url is " + item.SlideURL + " for ID=" + item.ID + " - " + item.Title);
                    }
                    Assert.AreEqual(5, allTalks.Count(), "Expected two talks!");

                    var allExtraMaterial = from t in allSessionTalks
                                           where t.SubTalks != null
                                           from m in t.SubTalks
                                           where m != null
                                           where m.SlideURL != null && m.SlideURL != ""
                                           select m;
                    Assert.IsFalse(allExtraMaterial.Any(), "Expected no extra material");
        }

        [TestMethod]
        [DeploymentItem("boost2010.xml")]
        public async Task TestUrlWithNoType()
        {
            // An exception was seen in the field
            var ai = new AgendaInfo("http://indico.cern.ch/conferenceTimeTable.py?confId=74604#20100622");
            var al = new AgendaLoader(new FileReader("boost2010.xml"));
            var data = await al.GetNormalizedConferenceData(ai);
        }

        [TestMethod]
        [DeploymentItem("data2009.xml")]
        public async Task TestAgendaWithBadLink()
        {
            // THis url has a bad link in it
            var ai = new AgendaInfo("https://indico.cern.ch/conferenceDisplay.py?confId=55584");
            var al = new AgendaLoader(new FileReader("data2009.xml"));
            var data = await al.GetNormalizedConferenceData(ai);

                /// Make sure the url is sanitized...

                var talks = from s in data.Sessions
                            from t in s.Talks
                            where t.Title.Contains("Fermi")
                            select t;
                Assert.AreEqual(1, talks.Count(), "# of fermi talks not right");
                Assert.IsNull(talks.First().SubTalks, "# of sub talks");

                var allSlideUrls = from s in data.Sessions
                                   from t in s.Talks
                                   where t.SlideURL != null
                                   select t.SlideURL;

                Assert.IsFalse(allSlideUrls.Any(u => u.Contains("\n")), "A slide url contains a carrage return character!");

                var allSTURLs = from s in data.Sessions
                                from t in s.Talks
                                where t.SubTalks != null
                                from st in t.SubTalks
                                where st.SlideURL != null
                                select st.SlideURL;
                Assert.IsFalse(allSTURLs.Any(u => u.Contains("\n")), "A sub talk url contains a carrage return character!");
        }
    }
}