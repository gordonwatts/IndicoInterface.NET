using IndicoInterface.NET.SimpleAgendaDataModel;
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
        /// <param name="useEventFormat">If true it will use the new event format url, if false, it will not unless site is on whitelist.</param>
        /// <param name="apiKey">Api key to use to access the event</param>
        /// <param name="secretKey">The secret key to also use to access event. Will be time encoded if this is present.</param>
        /// <returns>A URI that should return the XML from the agenda server</returns>
        public Uri GetAgendaFullXMLURL(AgendaInfo info, bool useEventFormat = false, string apiKey = null, string secretKey = null, bool useTimestamp = true)
        {
            var path = new StringBuilder();
            var requestParams = new Dictionary<string, string>();
            var useNewFormat = WhiteListInfo.CanUseEventFormat(info) || useEventFormat;
            if (useNewFormat)
            {
                path.AppendFormat("/event/{0}/other-view", info.ConferenceID);
            }
            else
            {
                path.AppendFormat("/conferenceOtherViews.py");
                requestParams["confId"] = info.ConferenceID;
            }
            requestParams["view"] = "xml";
            requestParams["showDate"] = "all";
            requestParams["showSession"] = "all";
            requestParams["detailLevel"] = "contribution";
            requestParams["fr"] = "no";

            var stem = ApiKeyHandler.IndicoEncode(path.ToString(), requestParams, apiKey, secretKey, useTimeStamp: useTimestamp);

            // Build the first part of the URL request now
            StringBuilder bld = new StringBuilder();

            bld.AppendFormat("http{1}://{0}", info.AgendaSite, useNewFormat ? "s" : "");

            if (!string.IsNullOrWhiteSpace(info.AgendaSubDirectory))
            {
                bld.AppendFormat("/{0}", info.AgendaSubDirectory);
            }
            bld.Append(stem);

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
        /// <remarks>
        /// If bad XML is returned, it could be because only the new format URL's are being used. In which case
        /// we will re-try with a new format URL. If that is successful, then we will mark the site as white listed.
        /// </remarks>
        public async Task<IndicoDataModel.iconf> GetFullConferenceData(AgendaInfo info)
        {
            try
            {
                using (var data = await _fetcher.GetDataFromURL(GetAgendaFullXMLURL(info)))
                {
                    return _loader.Value.Deserialize(data) as IndicoDataModel.iconf;
                }
            }
            catch (InvalidOperationException)
            {
                // This is the bad XML error...
                if (WhiteListInfo.CanUseEventFormat(info))
                    throw; // We already tried the new format!
            }

            using (var data = await _fetcher.GetDataFromURL(GetAgendaFullXMLURL(info, useEventFormat: true)))
            {
                var r = _loader.Value.Deserialize(data) as IndicoDataModel.iconf;
                WhiteListInfo.AddSiteThatUsesEventFormat(info.AgendaSite);
                return r;
            }
        }

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
                /// We have a conference or a meeting that has been split into sessions.
                /// There can still be extra talks that are put in as if there were no sessions,
                /// so we generate an extra session to handle that.
                /// 

                m.Sessions = ParseConference(data.session);
                var extraSessions = ParseTalksAsSession(data.contribution, m.Sessions);
                if (extraSessions != null)
                {
                    m.Sessions = m.Sessions
                        .Concat(extraSessions)
                        .ToArray();
                }
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
                                           AllMaterial = new TalkMaterial[0],
                                           StartDate = DateTime.Now,
                                           EndDate = DateTime.Now,
                                           ID = m.ID,
                                           TalkType = SimpleAgendaDataModel.TypeOfTalk.ExtraMaterial,
                                           SubTalks = (from tFiles in FindAllUniqueMaterial(m)
                                                       where tFiles != null
                                                       select new IndicoInterface.NET.SimpleAgendaDataModel.Talk()
                                                       {
                                                           Title = m.title,
                                                           SlideURL = tFiles.url,
                                                           DisplayFilename = Path.GetFileNameWithoutExtension(tFiles.name),
                                                           FilenameExtension = Path.GetExtension(tFiles.name),
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
        private IEnumerable<IndicoDataModel.materialFile> FindAllUniqueMaterial(IndicoDataModel.material m)
        {
            // Look at the files grouped by name (there can be a pptx and a pdf of the same file, for example).
            if (m.files != null && m.files.file != null)
            {
                var groupedFiles = from f in m.files.file
                                   group f by Path.GetFileNameWithoutExtension(f.name);
                var goodFiles = from g in groupedFiles
                                let bestFile = (from f in g let ord = CalcTypeIndex(f.type) where ord >= 0 orderby ord descending select f).FirstOrDefault()
                                where bestFile != null
                                select bestFile;

                foreach (var f in goodFiles)
                {
                    yield return f;
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
        /// Given a list of contributions, turn them into an ad-hoc session.
        /// </summary>
        /// <param name="contributions"></param>
        /// <returns></returns>
        private Session[] ParseTalksAsSession(IndicoDataModel.contribution[] contributions, Session[] sessions)
        {
            if (contributions == null || contributions.Length == 0)
                return null;

            var talks = (from t in contributions select ExtractTalkInfo(t)).ToArray();
            Session[] result = null;

            // The easy case is there are no sessions, so this becomes just one session.
            if (sessions == null || sessions.Length == 0)
            {
                var s1 = new Session()
                {
                    ID = "-1",
                    Title = "<ad-hoc session>",
                    Talks = (from t in contributions select ExtractTalkInfo(t)).ToArray()
                };
                result = new Session[] { s1 };
            }
            else
            {
                // We have to split the contributions up around each session. The algorithm is as follows:
                // 1. Find the time before a session that each talk occurs
                // 2. Find the smallest amount of time, and associate that talk with that session.
                // 3. Each group should be made into a new session.
                // 4. All other talks (which presumably occur after the last session) are made into their own session.

                var deltaTime = from c in talks
                                select new
                                {
                                    contrib = c,
                                    closestSession = sessions.Where(s => s.StartDate > c.StartDate).OrderBy(s => s.StartDate - c.StartDate).FirstOrDefault()
                                };

                var sessionGroups = deltaTime.Where(c => c.closestSession != null).GroupBy(x => x.closestSession);
                var contribSessions = from sg in sessionGroups
                                      select new Session()
                                      {
                                          ID = "-1",
                                          Title = "<ad-hoc session>",
                                          Talks = sg.Select(c => c.contrib).ToArray()
                                      };

                // And the sessions that are left over.
                var lastSessionTalks = deltaTime.Where(c => c.closestSession == null).Select(c => c.contrib);
                var lastSession = new Session()
                {
                    ID = "-1",
                    Title = "<ad-hoc session>",
                    Talks = lastSessionTalks.ToArray()
                };

                if (lastSession.Talks.Length > 0)
                {
                    result = contribSessions.Concat(new Session[] { lastSession }).ToArray();
                }
                else
                {
                    result = contribSessions.ToArray();
                }
            }

            // Get the start and end dates right for each session
            foreach (var s in result)
            {
                s.StartDate = s.Talks.Select(t => t.StartDate).Min();
                s.EndDate = s.Talks.Select(t => t.EndDate).Max();
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
            var result = new Talk();
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
                var mainFile = FindMaterial(contrib.material, materialType);
                if (mainFile != null)
                {
                    result.SlideURL = mainFile.url;
                    result.DisplayFilename = Path.GetFileNameWithoutExtension(SantizeURL(mainFile.name));
                    result.FilenameExtension = Path.GetExtension(SantizeURL(mainFile.name));
                }
                if (result.SlideURL != null)
                {
                    break;
                }
            }

            if (contrib.material != null)
            {
                result.AllMaterial = (from m in contrib.material
                                      where m.files != null && m.files.file != null
                                      from f in m.files.file
                                      select new TalkMaterial()
                                      {
                                          URL = f.url,
                                          FilenameExtension = Path.GetExtension(SantizeURL(f.name)),
                                          DisplayFilename = Path.GetFileNameWithoutExtension(SantizeURL(f.name)),
                                          MaterialType = m.title
                                      }).ToArray();
            }
            else
            {
                result.AllMaterial = new TalkMaterial[0];
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
        private IndicoDataModel.materialFile FindMaterial(IndicoInterface.NET.IndicoDataModel.material[] materiallist, string material_type)
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
                var result = SearchMaterialListForType(good, materialType);
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
        private static IndicoDataModel.materialFile SearchMaterialListForType(IEnumerable<IndicoDataModel.material> good, string materialType)
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
                        return filename;
                    }
                }

#if false
                /// An external link?
                if (item.link != null && item.link.ToLower().EndsWith(materialType))
                {
                    return item.link;
                }
#endif
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
            return url
                .Replace("\\", "/")
                .Replace("\n", ""); // Seriously!
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

        /// <summary>
        /// Fetch category information from the website.
        /// </summary>
        /// <param name="cat"></param>
        public async Task<IEnumerable<AgendaInfoExtended>> GetCategory(AgendaCategory cat, int daysBefore, string apiKey = null, string secretKey = null)
        {
            using (var data = await _fetcher.GetDataFromURL(cat.GetCagetoryUri(daysBefore, apiKey, secretKey)))
            {
                var cals = await data.iCalFromStream();
                return cals.Select(evt => new AgendaInfoExtended(evt.URL.OriginalString, evt.Summary, evt.DTStart, evt.DTEnd));
            }
        }

    }
}
