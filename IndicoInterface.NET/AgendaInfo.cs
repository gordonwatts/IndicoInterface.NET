using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace IndicoInterface.NET
{
    /// <summary>
    /// Given a standard conference agenda URL will give and cache information about that agenda - the full
    /// time table.
    /// </summary>
    public class AgendaInfo
    {
        /// <summary>
        /// The main URL of the conference.
        /// </summary>
        public string ConferenceID { get; set; }

        /// <summary>
        /// The web site where the agenda is located.
        /// </summary>
        public string AgendaSite { get; set; }

        /// <summary>
        /// Returns the agenda sub directory
        /// </summary>
        public string AgendaSubDirectory { get; set; }

        /// <summary>
        /// Do not use this - useful only for sereialization.
        /// </summary>
        public AgendaInfo()
        {
            AgendaSubDirectory = "";
        }

        /// <summary>
        /// Find the agenda information from the initial indico URL format.
        /// </summary>
        static Regex _gConfIdFinderStyle1 = new Regex("(?<protocal>http|https)://(?<site>[^/]+)/(?<subdir>.+/)?.*(?i:confId)=(?<conf>\\w+)");

        /// <summary>
        /// Find the agenda information from the event indico URL format
        /// </summary>
        static Regex _gConfIdFinderStyle2 = new Regex("(?<protocal>http|https)://(?<site>[^/]+)/event/(?<conf>\\w+)");

        /// <summary>
        /// Given a standard conference URL like "http://indico.cern.ch/conferenceDisplay.py?confId=14475",
        /// this will be ready to load up the required information.
        /// </summary>
        /// <param name="agendaUrl"></param>
        public AgendaInfo(string agendaUrl)
        {
            ///
            /// Get the conference ID out of the URL
            /// 

            var match = _gConfIdFinderStyle1.Match(agendaUrl);
            if (!match.Success)
                match = _gConfIdFinderStyle2.Match(agendaUrl);

            if (!match.Success)
                throw new AgendaException("URL does not have confId parameter or does not start with http or https!");

            ConferenceID = match.Groups["conf"].Value;
            AgendaSite = match.Groups["site"].Value;
            if (match.Groups["subdir"].Success)
            {
                AgendaSubDirectory = match.Groups["subdir"].Value.Substring(0, match.Groups["subdir"].Value.Length - 1);
            }
            else
            {
                AgendaSubDirectory = "";
            }
        }

        /// <summary>
        /// Use if you already know the conference ID. Defaults to CERN agenda site.
        /// </summary>
        /// <param name="confID"></param>
        public AgendaInfo(int confID)
        {
            ConferenceID = confID.ToString();
            AgendaSite = "indico.cern.ch";
        }

        /// <summary>
        /// Agenda from a different agenda site.
        /// </summary>
        /// <param name="confID"></param>
        /// <param name="agendaSite"></param>
        public AgendaInfo(int confID, string agendaSite)
        {
            ConferenceID = confID.ToString();
            AgendaSite = agendaSite;
        }

        /// <summary>
        /// Returns the URL of the full XML of the conference. Not often used by
        /// someone outside of this guy, but very helpful for testing.
        /// </summary>
        public string AgendaFullXML
        {
            get
            {
                StringBuilder bld = new StringBuilder();
                bld.AppendFormat("http://{0}/", AgendaSite);
                if (AgendaSubDirectory != "")
                {
                    bld.AppendFormat("{0}/", AgendaSubDirectory);
                }
                bld.AppendFormat("conferenceOtherViews.py?confId={0}&view=xml&showDate=all&showSession=all&detailLevel=contribution&fr=no", ConferenceID);
                return bld.ToString();
            }
        }

        /// <summary>
        /// Track the loader for this conference.
        /// </summary>
        [XmlIgnore]
        XmlSerializer _loader = null;

        /// <summary>
        /// Set if you want messages from this guy passed back to some logging mechanism.
        /// </summary>
        [XmlIgnore]
        public Action<string, string> LogMessageCallback;

        /// <summary>
        /// The agent string we should be using for web requests
        /// </summary>
        [XmlIgnore]
        public string HttpAgentString;

        /// <summary>
        /// Returns the full conference agenda data.
        /// </summary>
        /// <returns></returns>
        public IndicoDataModel.iconf GetFullConferenceData(StreamReader externalReader = null)
        {
            if (externalReader == null)
            {
                if (LogMessageCallback != null)
                {
                    LogMessageCallback("AgendaDownload", "Downloading " + ToString() + " - " + AgendaFullXML);
                }
                ServicePointManager.ServerCertificateValidationCallback = (s, certificate, chain, errs) => true;
                WebRequest req = WebRequest.Create(AgendaFullXML);
                if (HttpAgentString != null)
                {
                    (req as HttpWebRequest).UserAgent = HttpAgentString;
                }
                using (WebResponse res = req.GetResponse())
                {
                    using (var r = new StreamReader(res.GetResponseStream()))
                    {
                        var result = GetFullConferenceDataFromStream(r);
                        res.Close();
                        return result;
                    }
                }
            }
            else
            {
                if (LogMessageCallback != null)
                {
                    LogMessageCallback("AgendaDownload", "Reading the agenda from an externally provided data source");
                }
                return GetFullConferenceDataFromStream(externalReader);
            }
        }

        /// <summary>
        /// Gets the full conference data, reading from a stream. Mostly used for
        /// testing.
        /// </summary>
        /// <returns></returns>
        private IndicoDataModel.iconf GetFullConferenceDataFromStream(StreamReader rdr)
        {
            ///
            /// Create the deserializer if need be
            ///

            if (_loader == null)
            {
                _loader = new XmlSerializer(typeof(IndicoDataModel.iconf));
            }

            ///
            /// Loading is complicated by the fact that the web sites don't always give us back complete XML, unfortunately.
            /// So we have to muck around a little bit. If we had a simple filter stream, this might be simpler, but... :-)
            /// 

            /// 

            using (var reader = new MemoryStream())
            {
                TextWriter wtr = new StreamWriter(reader);
                wtr.Write(CleanAgendaXML(rdr.ReadToEnd()));
                wtr.WriteLine();
                wtr.Flush();
                reader.Seek(0, SeekOrigin.Begin);
                IndicoDataModel.iconf data = _loader.Deserialize(reader) as IndicoDataModel.iconf;
                return data;
            }
        }

        /// <summary>
        /// Sometimes these agenda guys send out bad XML. Do our best to clean it up!
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        private string CleanAgendaXML(string lines)
        {
            int startOfXML = lines.IndexOf("<?xml");
            if (startOfXML < 0)
            {
                throw new ArgumentException("XML returned from meeting didn't contains starting XML tag (" + this.AgendaFullXML + ")");
            }
            lines = lines.Substring(startOfXML);
            int endOfXML = lines.IndexOf("</iconf");
            if (endOfXML < 0)
            {
                throw new ArgumentException("XML returned from meeting ddn't contain ending </iconf> tag (" + AgendaFullXML + ")");
            }
            lines = lines.Substring(0, endOfXML + 8);
            return lines;
        }

        /// <summary>
        /// Return the conference data - only do it in the simplified and normalized
        /// format.
        /// </summary>
        /// <returns></returns>
        public SimpleAgendaDataModel.Meeting GetNormalizedConferenceData(StreamReader rdr = null)
        {
            ///
            /// Grab all the details
            ///

            var data = GetFullConferenceData(rdr);

            ///
            /// Create the stuff we will be sending back.
            /// 

            SimpleAgendaDataModel.Meeting m = new IndicoInterface.SimpleAgendaDataModel.Meeting();
            m.ID = data.ID;
            m.Title = data.title.Trim();
            m.Site = AgendaSite;
            m.StartDate = AgendaStringToDate(data.startDate);
            m.EndDate = AgendaStringToDate(data.endDate);

            ///
            /// If there is any material associated with the agenda, then put that together too.
            /// 

            m.MeetingTalks = ParseConferenceExtraMaterial(data.material);

            ///
            /// Convert all the session and talks into our simple data structure.
            /// We can tell the difference between a meeting and a conference by
            /// looking to see if there are any sessions or contributions. One of
            /// them will be null.
            /// 

            if (data.session != null)
            {
                ///
                /// We have a conference.
                /// 

                m.Sessions = ParseConference(data.session);
            }
            else
            {
                ///
                /// We have a single session meeting.
                /// 

                IndicoInterface.SimpleAgendaDataModel.Session s = ParseSingleMeeting(data.material, data.contribution, "0", data.title, data.startDate, data.endDate);
                m.Sessions = new IndicoInterface.SimpleAgendaDataModel.Session[1] { s };
            }

            return m;
        }

        /// <summary>
        /// Given the material attached with this session, turn it into talks for later processing. These are
        /// funny talks (no time, etc.).
        /// </summary>
        /// <param name="material"></param>
        /// <returns></returns>
        private SimpleAgendaDataModel.Talk[] ParseConferenceExtraMaterial(IndicoDataModel.material[] material)
        {
            ///
            /// Simple cases
            /// 

            if (material == null || material.Length == 0)
            {
                return new SimpleAgendaDataModel.Talk[0];
            }

            ///
            /// Build talks from each of the guys. Since this is extra material, we need to package everything up
            /// into sub-talks. Nothing exists at the upper level - hence the nested structure of the below LINQ
            /// query!
            /// 

            var sessionMaterialTalks = from m in material
                                       where m != null
                                       let talk = new IndicoInterface.SimpleAgendaDataModel.Talk()
                                       {
                                           Title = m.title,
                                           SlideURL = null,
                                           StartDate = DateTime.Now,
                                           EndDate = DateTime.Now,
                                           ID = m.ID,
                                           TalkType = SimpleAgendaDataModel.TypeOfTalk.ExtraMaterial,
                                           SubTalks = (from turl in FindAllUniqueMaterial(m)
                                                       where turl != null
                                                       select new IndicoInterface.SimpleAgendaDataModel.Talk()
                                                       {
                                                           Title = m.title,
                                                           SlideURL = turl,
                                                           StartDate = DateTime.Now,
                                                           EndDate = DateTime.Now,
                                                           ID = m.ID,
                                                           TalkType = SimpleAgendaDataModel.TypeOfTalk.ExtraMaterial
                                                       }).ToArray()
                                       }
                                       where talk.SubTalks != null && talk.SubTalks.Length != 0
                                       select talk;

            return sessionMaterialTalks.ToArray();
        }

        /// <summary>
        /// We have a list of files - see if we can figure out what should come back.
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        private IEnumerable<string> FindAllUniqueMaterial(IndicoDataModel.material m)
        {
            ///
            /// First, we check to see if a particular up-load has been fingered as "the one" by indico:
            /// 

            if (m.pptx != null)
            {
                yield return m.pptx;
            }
            else if (m.ppt != null)
            {
                yield return m.ppt;
            }
            else if (m.pdf != null)
            {
                yield return m.pdf;
            }
            else if (m.ps != null)
            {
                yield return m.ps;
            }
            else
            {
                if (m.files != null && m.files.file != null)
                {

                    ///
                    /// If we are here, then we need to look through the list of files for everything attached. There is one
                    /// problem: two files with the same name. So we need to group everything up by the stub name.
                    /// 

                    var groupedFiles = from f in m.files.file
                                       group f by Path.GetFileNameWithoutExtension(f.name);
                    var goodFiles = from g in groupedFiles
                                    let bestFile = (from f in g let ord = CalcTypeIndex(f.type) where ord >= 0 orderby ord descending select f.url).FirstOrDefault()
                                    where bestFile != null
                                    select bestFile;

                    foreach (var f in goodFiles)
                    {
                        yield return f;
                    }
                }
            }
        }

        /// <summary>
        /// A list in reverse order of file types we will let move further into the system.
        /// </summary>
        private string[] fileTypeOrdered = new string[] { "ps", "pdf", "ppt", "pptx" };

        /// <summary>
        /// Give an ordering of the file type. Lowest number is worse. If it isn't on the list, it gets a -1.
        /// </summary>
        /// <param name="fileType"></param>
        /// <returns></returns>
        private int CalcTypeIndex(string fileType)
        {
            for (int i = 0; i < fileTypeOrdered.Length; i++)
            {
                if (fileTypeOrdered[i] == fileType)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Look through the contribution list and extract the talk info.
        /// </summary>
        /// <param name="contribution"></param>
        /// <returns></returns>
        private IndicoInterface.SimpleAgendaDataModel.Session ParseSingleMeeting(IndicoInterface.IndicoDataModel.material[] attachedMaterial, IndicoInterface.IndicoDataModel.contribution[] contribution, string ID, string title, string startTime, string endTime)
        {
            IndicoInterface.SimpleAgendaDataModel.Session result = new IndicoInterface.SimpleAgendaDataModel.Session();
            result.ID = ID;
            result.Title = title;
            result.StartDate = AgendaStringToDate(startTime);
            result.EndDate = AgendaStringToDate(endTime);

            ///
            /// See if any material was put in the Session itself
            /// 

            result.SessionMaterial = ParseConferenceExtraMaterial(attachedMaterial);

            ///
            /// Next, grab all the material that is in the talks proper
            /// 

            if (contribution == null)
            {
                result.Talks = new IndicoInterface.SimpleAgendaDataModel.Talk[0];
            }
            else
            {
                /// Transform each contribution into a talk item.
                var alltalks = from t in contribution
                               select ExtractTalkInfo(t);
                result.Talks = alltalks.ToArray();
            }
            return result;
        }

        /// <summary>
        /// Convert a string time from the agenda server to the proper time.
        /// </summary>
        /// <param name="endTime"></param>
        /// <returns></returns>
        private DateTime AgendaStringToDate(string datetime)
        {
            return DateTime.Parse(datetime);
        }

        /// <summary>
        /// Given a contribution, return a talk.
        /// </summary>
        /// <param name="contrib"></param>
        /// <returns></returns>
        private IndicoInterface.SimpleAgendaDataModel.Talk ExtractTalkInfo(IndicoInterface.IndicoDataModel.contribution contrib)
        {
            IndicoInterface.SimpleAgendaDataModel.Talk result = new IndicoInterface.SimpleAgendaDataModel.Talk();
            result.ID = contrib.ID;
            result.Title = contrib.title;
            if (contrib.startDate != null)
            {
                result.StartDate = AgendaStringToDate(contrib.startDate);
            }
            if (contrib.endDate != null)
            {
                result.EndDate = AgendaStringToDate(contrib.endDate);
            }

            if (contrib.speakers != null)
            {
                var speakers = from s in contrib.speakers
                               from u in s.users
                               select (u.name.first + " " + (u.name.middle + " " + u.name.last).Trim()).Trim();
                result.Speakers = speakers.ToArray();
            }
            else
            {
                result.Speakers = new string[0];
            }

            foreach (var materialType in new string[] { "slides", "transparencies", "poster", "0", null })
            {
                result.SlideURL = FindMaterial(contrib.material, materialType);
                if (result.SlideURL != null)
                {
                    break;
                }
            }
            if (result.SlideURL == null)
            {
                if (LogMessageCallback != null)
                {
                    LogMessageCallback("NoTalkData", "No usable talk slides or poster found for " + result.Title);
                }
            }

            if (contrib.subcontributions != null)
            {
                var subtalks = from c in contrib.subcontributions
                               select ExtractTalkInfo(c);
                result.SubTalks = subtalks.ToArray();
                if (result.SubTalks.Length == 0)
                {
                    result.SubTalks = null;
                }
            }

            return result;
        }

        /// <summary>
        /// Given material for a particular contribution, find the appropriate type, and fetch out
        /// a URL for the actual data.
        /// </summary>
        /// <param name="material"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        private string FindMaterial(IndicoInterface.IndicoDataModel.material[] materiallist, string material_type)
        {
            ///
            /// If no one uploaded anything, then we won't be sending anything back. :(
            ///

            if (materiallist == null)
            {
                return null;
            }

            ///
            /// See if we can find material of the correct type. It seems there is not total database normalization
            /// in indico here. :-)
            /// 

            var good = (from m in materiallist
                        where (material_type == null || m.ID == material_type || m.title == material_type)
                        select m).ToArray();
            if (good.Length == 0)
            {
                return null;
            }

            /// First good is returned!
            foreach (var materialType in new string[] { "pptx", "ppt", "ps", "pdf" })
            {
                string result = SearchMaterialListForType(good, materialType);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        /// <summary>
        /// Scan for a particular material type the list of items in teh indigo model.
        /// </summary>
        /// <param name="good"></param>
        /// <returns></returns>
        private static string SearchMaterialListForType(IEnumerable<IndicoInterface.IndicoDataModel.material> good, string materialType)
        {
            foreach (var item in good)
            {
                /// Anything good uploaded to the agenda server?
                if (item.files != null && item.files.file != null)
                {
                    var filename = item.files.file.FirstOrDefault(f => IsOfType(f, materialType));
                    if (filename != null
                        && filename.url != null)
                    {
                        return filename.url;
                    }
                }

                /// An external link?
                if (item.link != null && item.link.ToLower().EndsWith(materialType))
                {
                    return item.link;
                }

                /// An explicit link - ugly!!?
                if (materialType == "pdf" && item.pdf != null)
                {
                    return item.pdf;
                }
                if (materialType == "pptx" && item.pptx != null)
                {
                    return item.pptx;
                }
                if (materialType == "ps" && item.ps != null)
                {
                    return item.ps;
                }
                if (materialType == "ppt" && item.ppt != null)
                {
                    return item.ppt;
                }
            }

            /// Found nothing!
            return null;
        }

        /// <summary>
        /// Small helper function to sort through some of the mis-labeling that goes on in indico.
        /// </summary>
        /// <param name="f"></param>
        /// <param name="materialType"></param>
        /// <returns></returns>
        private static bool IsOfType(IndicoDataModel.materialFile f, string materialType)
        {
            if (f.type == materialType)
            {
                return true;
            }
            string extension = Path.GetExtension(SantizeURL(f.name));
            if (extension.Length > 1)
            {
                if (extension.Substring(1) == materialType)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// SOme URL's come to us with some funny formatting. Do our best to fix them up!
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static string SantizeURL(string url)
        {
            var r = url.Replace("\\", "/");
            r = r.Replace("\n", ""); // Seriously!

            return r;
        }

        /// <summary>
        /// Given a list of sessions, return a list of normalized sessions.
        /// </summary>
        /// <param name="session">The list of sessiosn -- can't be null, but could be empty.</param>
        /// <returns></returns>
        private IndicoInterface.SimpleAgendaDataModel.Session[] ParseConference(IndicoInterface.IndicoDataModel.session[] session)
        {
            /// LINQ makes this transformation so simple. Much worse in C++!!!
            var allsessions = from s in session
                              select ParseSingleMeeting(s.material, s.contribution, s.ID, s.title, s.startDate, s.endDate);
            return allsessions.ToArray();
        }

        /// <summary>
        /// Returns a printable string about what this thing is.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Agenda at " + AgendaSite + " with conference ID " + ConferenceID.ToString();
        }

        /// <summary>
        /// Code to seralize this guy to an xml dude.
        /// </summary>
        /// <param name="sw"></param>
        public void Seralize(TextWriter sw)
        {
            XmlSerializer ser = new XmlSerializer(typeof(AgendaInfo));
            ser.Serialize(sw, this);
        }

        /// <summary>
        /// Create an agenda info from XML.
        /// </summary>
        /// <param name="rdr"></param>
        /// <returns></returns>
        public static AgendaInfo Deseralize(TextReader rdr)
        {
            XmlSerializer ser = new XmlSerializer(typeof(AgendaInfo));
            return ser.Deserialize(rdr) as AgendaInfo;
        }

        /// <summary>
        /// Desearlize from a string
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static AgendaInfo Deseralize(string p)
        {
            using (StringReader rdr = new StringReader(p))
            {
                return Deseralize(rdr);
            }
        }

        /// <summary>
        /// Gets the Agenda URL - where to go to get the opening page of the
        /// conference.
        /// </summary>
        public string ConferenceUrl
        {
            get
            {
                StringBuilder bld = new StringBuilder();
                bld.AppendFormat("http://{0}/", AgendaSite);
                if (AgendaSubDirectory != "")
                {
                    bld.AppendFormat("{0}/", AgendaSubDirectory);
                }
                bld.AppendFormat("conferenceDisplay.py?confId={0}", ConferenceID);
                return bld.ToString();
            }
        }
    }
}
