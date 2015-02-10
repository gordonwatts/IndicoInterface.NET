using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace IndicoInterface.NET
{
    /// <summary>
    /// Class that will load and fetch things from the agenda server.
    /// </summary>
    public class AgendaLoader
    {
        /// <summary>
        /// Setup the agenda info access
        /// </summary>
        /// <param name="fetcher">Interface to use to fetch URL data form the web or other source</param>
        public AgendaLoader(IUrlFetcher fetcher)
        {
            _fetcher = fetcher;
        }

        /// <summary>
        /// Return the URL that will get the full XML.
        /// </summary>
        /// <param name="info">Agenda the URL is desired for</param>
        /// <returns>A URI that should return the XML from the agenda server</returns>
        public Uri GetAgendaFullXMLURL(AgendaInfo info)
        {
            StringBuilder bld = new StringBuilder();
            bld.AppendFormat("http://{0}/", info.AgendaSite);
            if (!string.IsNullOrWhiteSpace(info.AgendaSubDirectory))
            {
                bld.AppendFormat("{0}/", info.AgendaSubDirectory);
            }
            if (WhileListInfo.CanUseEventFormat(info))
            {
                bld.AppendFormat("event/{0}/other-view?view=xml", info.ConferenceID);
            }
            else
            {
                bld.AppendFormat("conferenceOtherViews.py?confId={0}&view=xml&showDate=all&showSession=all&detailLevel=contribution&fr=no", info.ConferenceID);
            }
            return new Uri(bld.ToString());
        }

        /// <summary>
        /// The serializer to decode XML agenda info
        /// </summary>
        private Lazy<XmlSerializer> _loader = new Lazy<XmlSerializer>(() => new XmlSerializer(typeof(IndicoDataModel.iconf)));

        /// <summary>
        /// Get the full conference XML desterilized info a data model that matches the raw XML.
        /// </summary>
        /// <param name="info">The agenda we should get the data for</param>
        /// <returns>The parsed XML data. Throws an exception if it can't find what it needs</returns>
        public async Task<IndicoDataModel.iconf> GetFullConferenceData(AgendaInfo info)
        {
            using (var data = await _fetcher.GetDataFromURL(GetAgendaFullXMLURL(info)))
            {
                return _loader.Value.Deserialize(data) as IndicoDataModel.iconf;
            }
        }

        /// <summary>
        /// Sometimes these agenda guys send out bad XML. Do our best to clean it up!
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        //private string CleanAgendaXML(string lines)
        //{
        //    int startOfXML = lines.IndexOf("<?xml");
        //    if (startOfXML < 0)
        //    {
        //        throw new ArgumentException("XML returned from meeting didn't contains starting XML tag (" + this.AgendaFullXML + ")");
        //    }
        //    lines = lines.Substring(startOfXML);
        //    int endOfXML = lines.IndexOf("</iconf");
        //    if (endOfXML < 0)
        //    {
        //        throw new ArgumentException("XML returned from meeting didn't contain ending </iconf> tag (" + AgendaFullXML + ")");
        //    }
        //    lines = lines.Substring(0, endOfXML + 8);
        //    return lines;
        //}

        /// <summary>
        /// Get the conference data in a normalized format
        /// </summary>
        /// <returns></returns>
        public async Task<SimpleAgendaDataModel.Meeting> GetNormalizedConferenceData(AgendaInfo meeting)
        {
            ///
            /// Grab all the details
            ///

            var data = await GetFullConferenceData(meeting);

            ///
            /// Create the stuff we will be sending back.
            /// 

            SimpleAgendaDataModel.Meeting m = new IndicoInterface.NET.SimpleAgendaDataModel.Meeting();
            m.ID = data.ID;
            m.Title = data.title.Trim();
            m.Site = meeting.AgendaSite;
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

                var s = ParseSingleMeeting(data.material, data.contribution, "0", data.title, data.startDate, data.endDate);
                m.Sessions = new IndicoInterface.NET.SimpleAgendaDataModel.Session[1] { s };
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
                                       let talk = new IndicoInterface.NET.SimpleAgendaDataModel.Talk()
                                       {
                                           Title = m.title,
                                           SlideURL = null,
                                           StartDate = DateTime.Now,
                                           EndDate = DateTime.Now,
                                           ID = m.ID,
                                           TalkType = SimpleAgendaDataModel.TypeOfTalk.ExtraMaterial,
                                           SubTalks = (from turl in FindAllUniqueMaterial(m)
                                                       where turl != null
                                                       select new IndicoInterface.NET.SimpleAgendaDataModel.Talk()
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
            /// First, we check to see if a particular up-load has been fingered as "the one" by Indico:
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
        private IUrlFetcher _fetcher;

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
        private IndicoInterface.NET.SimpleAgendaDataModel.Session ParseSingleMeeting(IndicoInterface.NET.IndicoDataModel.material[] attachedMaterial, IndicoInterface.NET.IndicoDataModel.contribution[] contribution, string ID, string title, string startTime, string endTime)
        {
            IndicoInterface.NET.SimpleAgendaDataModel.Session result = new IndicoInterface.NET.SimpleAgendaDataModel.Session();
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
                result.Talks = new IndicoInterface.NET.SimpleAgendaDataModel.Talk[0];
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
        private IndicoInterface.NET.SimpleAgendaDataModel.Talk ExtractTalkInfo(IndicoInterface.NET.IndicoDataModel.contribution contrib)
        {
            IndicoInterface.NET.SimpleAgendaDataModel.Talk result = new IndicoInterface.NET.SimpleAgendaDataModel.Talk();
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
        private string FindMaterial(IndicoInterface.NET.IndicoDataModel.material[] materiallist, string material_type)
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
            /// in Indico here. :-)
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
        /// Scan for a particular material type the list of items in the indigo model.
        /// </summary>
        /// <param name="good"></param>
        /// <returns></returns>
        private static string SearchMaterialListForType(IEnumerable<IndicoInterface.NET.IndicoDataModel.material> good, string materialType)
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
        /// Small helper function to sort through some of the miss-labeling that goes on in Indico.
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
        /// <param name="session">The list of session -- can't be null, but could be empty.</param>
        /// <returns></returns>
        private IndicoInterface.NET.SimpleAgendaDataModel.Session[] ParseConference(IndicoInterface.NET.IndicoDataModel.session[] session)
        {
            /// LINQ makes this transformation so simple. Much worse in C++!!!
            var allsessions = from s in session
                              select ParseSingleMeeting(s.material, s.contribution, s.ID, s.title, s.startDate, s.endDate);
            return allsessions.ToArray();
        }

        /// <summary>
        /// Code to serialize this guy to an xml dude.
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
        /// Deserialize from a string
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

    }
}
