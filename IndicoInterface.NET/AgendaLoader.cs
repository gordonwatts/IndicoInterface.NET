using IndicoInterface.NET.IndicoDataModel;
using IndicoInterface.NET.SimpleAgendaDataModel;
using Newtonsoft.Json;
using NodaTime;
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
        /// <param name="useEventFormat">If true it will use the new event format URL, if false, it will not unless site is on white list.</param>
        /// <param name="apiKey">API key to use to access the event</param>
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

            var bld = BuildUriFromPath(info, useNewFormat, stem);

            return new Uri(bld);
        }

        /// <summary>
        /// Build the final URI from the path
        /// </summary>
        /// <param name="info">The agenda we are building against</param>
        /// <param name="useHttps">What http protocal should be used?</param>
        /// <param name="stem">The stem that goes after the absolute base</param>
        /// <returns></returns>
        private static string BuildUriFromPath(AgendaInfo info, bool useHttps, string stem)
        {
            // Build the first part of the URL request now
            StringBuilder bld = new StringBuilder();

            bld.AppendFormat("http{1}://{0}", info.AgendaSite, useHttps ? "s" : "");

            if (!string.IsNullOrWhiteSpace(info.AgendaSubDirectory))
            {
                bld.AppendFormat("/{0}", info.AgendaSubDirectory);
            }
            bld.Append(stem);
            return bld.ToString();
        }

        /// <summary>
        /// Return the URL for a REST query.
        /// </summary>
        /// <param name="info"></param>
        /// <returns>Uri pointing to the resource</returns>
        /// <remarks>
        /// Only "modern" versions of indico can handle this, and in this code base, AgendaLoader is
        /// the one that tries to sort out what version of indico it is dealing with.
        /// </remarks>
        public Uri GetAgendaFullJSONURL(AgendaInfo info, string apiKey = null, string secretKey = null, bool useTimeStamp = true)
        {
            var path = new StringBuilder();
            path.AppendFormat("/export/event/{0}.json", info.ConferenceID);
            var requestParams = new Dictionary<string, string>();
            requestParams["nc"] = "yes";
            requestParams["detail"] = "sessions";

            var stem = ApiKeyHandler.IndicoEncode(path.ToString(), requestParams, apiKey, secretKey, useTimeStamp: useTimeStamp);

            return new Uri(BuildUriFromPath(info, true, stem));
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
        public async Task<IndicoDataModel.iconf> GetFullConferenceDataXML(AgendaInfo info)
        {
            try
            {
                using (var data = await _fetcher.GetDataFromURL(GetAgendaFullXMLURL(info)))
                {
                    return (_loader.Value.Deserialize(data) as iconf).CheckNotDepreciated();
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
                return r.CheckNotDepreciated();
            }
        }

        /// <summary>
        /// Return the full conference JSON deserialized into a data model that matches the raw JSON.
        /// </summary>
        /// <param name="info">The agenda we would like to get the data for</param>
        /// <returns>The parsed XML data. Throws an exception if the data is not returned</returns>
        public async Task<JSON.Result> GetFullConferenceDataJSON(AgendaInfo info)
        {
            using (var data = await _fetcher.GetDataFromURL(GetAgendaFullJSONURL(info)))
            {
                var r = JsonConvert.DeserializeObject<JSON.IndicoGetMeetingInfoReturn>(await data.ReadToEndAsync());
                if (r.results.Count != 1)
                {
                    throw new InvalidOperationException("Don't know how to deal with an odd number of items back");
                }
                return r.results[0];
            }
        }

        /// <summary>
        /// Get the conference data in a normalized format
        /// </summary>
        /// <param name="meeting"></param>
        /// <returns></returns>
        /// <remarks>
        /// Attempts to be smart about what format to grab the data in - JSON or XML.
        /// </remarks>
        public async Task<SimpleAgendaDataModel.Meeting> GetNormalizedConferenceData(AgendaInfo meeting)
        {
            bool xml = false;
            if (!WhiteListInfo.UseJSONAgendaLoaderRequests(meeting))
            {
                try
                {
                    return await GetNormalizedConferenceDataFromXML(meeting);
                }
                catch (AgendaFormatDepreciatedException e)
                {
                    xml = true;
                    // Try JSON in this case.
                }
            }

            var r = await GetNormalizedConferenceDataFromJSON(meeting);

            // If we aren't white listed, and we got here ok, then we should be white listed!
            if (xml)
            {
                WhiteListInfo.AddSiteThatUsesJSONAgendaQueries(meeting.AgendaSite);
            }

            return r;
        }

        /// <summary>
        /// Get the conference data in a normalized format assuming an xml source.
        /// </summary>
        /// <returns></returns>
        private async Task<SimpleAgendaDataModel.Meeting> GetNormalizedConferenceDataFromJSON(AgendaInfo meeting)
        {
            // Get the data from the agenda and load it into our internal data model.
            var data = await GetFullConferenceDataJSON(meeting);

            // Do the basic meeting header
            var m = new IndicoInterface.NET.SimpleAgendaDataModel.Meeting();
            m.ID = data.id;
            m.Title = data.title.Trim();
            m.Site = meeting.AgendaSite;
            m.StartDate = AgendaStringToDate(data.startDate);
            m.EndDate = AgendaStringToDate(data.endDate);

            // Sessions can either be at the top level, not associated,
            // or they can be in sessions. We have to pick up talks from both
            // sources.
            var definedSessions = ExtractContributionsBySession(data.sessions);
            var undefinedSessions = SortNonSessionTalksIntoSessions(definedSessions, data.contributions.Select(t => CreateTalk(t)));
            var sessionsNotAssociated = ExtractContributionsBySession(data.contributions);
            var sessions = definedSessions.Concat(undefinedSessions);

            foreach (var s in sessions.Where(ms => ms.Title == ""))
            {
                s.Title = m.Title;
            }
            
            // Put them into our meeting
            m.Sessions = sessions
                .OrderBy(s => s.StartDate)
                .ToArray();

            // Extra material in the meeting
            m.MeetingTalks = ParseConferenceExtraMaterialJSON(data.folders);

            return m;
        }

        private IList<Session> ExtractContributionsBySession(IList<JSON.Contribution> list)
        {
            var contribBySession = from s in list
                                   group s by NormalizedSessionName(s.session);

            return contribBySession
                .Select(talks => CreateSessionFromContribs("", talks))
                .ToList();
        }

        /// <summary>
        /// Extract session data by contribution
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        private IList<Session> ExtractContributionsBySession(IList<JSON.Session> list)
        {
            return list
                .Select(ConvertToSession)
                .ToList();
        }

        /// <summary>
        /// Given a JSON session, convert it to a real one.
        /// </summary>
        /// <param name="jSession"></param>
        /// <returns></returns>
        private Session ConvertToSession(JSON.Session jSession)
        {
            // Fill in with the default stuff.
            var s = CreateSessionFromContribs(jSession.title, jSession.contributions);

            s.EndDate = AgendaStringToDate(jSession.endDate);
            s.StartDate = AgendaStringToDate(jSession.startDate);
            s.ID = jSession.id;
            s.SessionMaterial = ParseConferenceExtraMaterialJSON(jSession.session.folders);

            return s;
        }

        /// <summary>
        /// Given a list of contributions, create a session object
        /// </summary>
        /// <param name="sessionName"></param>
        /// <param name="contribs"></param>
        /// <returns></returns>
        private Session CreateSessionFromContribs(string sessionName, IEnumerable<JSON.Contribution> contribs)
        {
            var talks = contribs
                .Select(t => CreateTalk(t))
                .Where(t => t != null)
                .OrderBy(t => t.StartDate)
                .ToArray();

            var r = new Session()
            {
                Title = sessionName,
                Talks = talks,
                ID = "0",
                StartDate = talks.Length != 0 ? FindEarliestTime(talks) : new DateTime(),
                EndDate = talks.Length != 0 ? FindLastTime(talks) : new DateTime(),
                SessionMaterial = new Talk[0]
            };

            return r;
        }

        /// <summary>
        /// Return the first start time.
        /// </summary>
        /// <param name="talks">List of talks, expected to have at least on talk</param>
        /// <returns></returns>
        private static DateTime FindEarliestTime(IEnumerable<Talk> talks)
        {
            return talks
                .Select(t => t.StartDate)
                .Min();
        }

        /// <summary>
        /// Return the last end time.
        /// </summary>
        /// <param name="talks">List of talks, expected to have at least one talk</param>
        /// <returns></returns>
        private static DateTime FindLastTime(Talk[] talks)
        {
            return talks
                .Select(t => t.EndDate)
                .Max();
        }

        /// <summary>
        /// Creates a talk from a contribution.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private Talk CreateTalk(JSON.Contribution t)
        {
            // Grab the attached slides.
            var folders = t.folders;
            var title = t.title;
            var id = t.id;
            var start = AgendaStringToDate(t.startDate);
            var end = AgendaStringToDate(t.endDate);
            var speakers = t.speakers.Select(s => ConvertToSpeaker(s)).ToArray();

            var allMaterial = folders
                .SelectMany(f => ConvertToTalkMaterial(f)).ToArray();
            var bestMaterial = FindBestMaterial(allMaterial);

            var subTalks = t.subContributions == null ? new Talk[0] : t.subContributions.Select(st => CreateTalk(st)).ToArray();

            // And build the talk.
            var rt = new Talk()
            {
                Title = title,
                ID = id,
                StartDate = start,
                EndDate = end,
                AllMaterial = allMaterial,
                DisplayFilename = bestMaterial != null ? bestMaterial.DisplayFilename : "",
                FilenameExtension = bestMaterial != null ? bestMaterial.FilenameExtension : "",
                SlideURL = bestMaterial != null ? bestMaterial.URL : "",
                Speakers = speakers,
                SubTalks = subTalks,
                TalkType = TypeOfTalk.Talk
            };
            return rt;
        }

        /// <summary>
        /// Create a talk from a sub-contribution
        /// </summary>
        /// <param name="st"></param>
        /// <returns></returns>
        private Talk CreateTalk(JSON.SubContribution st)
        {
            var allMaterial = st.folders.SelectMany(f => ConvertToTalkMaterial(f)).ToArray();
            var bm = FindBestMaterial(allMaterial);

            var t = new Talk()
            {
                Title = st.title,
                ID = st.id,
                TalkType = TypeOfTalk.Talk,
                Speakers = st.speakers.Select(s => ConvertToSpeaker(s)).ToArray(),
                AllMaterial = allMaterial,
                DisplayFilename = bm != null ? bm.DisplayFilename : "",
                FilenameExtension = bm != null ? bm.FilenameExtension : "",
                SlideURL = bm != null ? bm.URL : ""
            };
            return t;
        }

        /// <summary>
        /// Extract a speaker from a speaker.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private string ConvertToSpeaker(JSON.Person s)
        {
            return s.fullName;
        }

        /// <summary>
        /// Generic list of material types we should search for in order to get the "best" thing to present.
        /// </summary>
        private static string[] gGenericMaterialList = new string[] { "slides", "transparencies", "poster", "0", null };

        /// <summary>
        /// Given a list of all the material associated with a talk, pull out the
        /// "most" interesting.
        /// </summary>
        /// <param name="allMaterial"></param>
        /// <returns></returns>
        private TalkMaterial FindBestMaterial(TalkMaterial[] allMaterial)
        {
            var orderedMaterial = from tm in allMaterial
                                  let mtlow = ExtractMaterialType(tm)
                                  let ord = Array.FindIndex(gGenericMaterialList, s => s == mtlow)
                                  let normOrd = ord < 0 ? gGenericMaterialList.Length : ord
                                  group tm by normOrd;
            var sorted = orderedMaterial.OrderBy(k => k.Key).FirstOrDefault();
            if (sorted == null)
            {
                return null;
            }

            // Next, order by file type
            var byFileType = from tm in sorted
                             group tm by CalcTypeIndex(tm.FilenameExtension);
            return byFileType.OrderByDescending(k => k.Key).First().First();
        }

        /// <summary>
        /// Protected material type return
        /// </summary>
        /// <param name="tm"></param>
        /// <returns></returns>
        private static string ExtractMaterialType(TalkMaterial tm)
        {
            return tm.MaterialType != null ? tm.MaterialType.ToLower() : "slides";
        }

        /// <summary>
        /// Convert a talk's folder list to TalkMaterial.
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        private IEnumerable<TalkMaterial> ConvertToTalkMaterial(JSON.Folder f)
        {
            return f.attachments
                .Select(a => ConvertToTalkMaterial(a, f.title));
        }

        /// <summary>
        /// Convert an attachment to a talk material
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        private TalkMaterial ConvertToTalkMaterial(JSON.Attachment a, string mtype)
        {
            return new TalkMaterial()
            {
                DisplayFilename = a.title,
                URL = a.download_url,
                FilenameExtension = Path.GetExtension(a.download_url),
                MaterialType = mtype
            };
        }

        /// <summary>
        /// Normalize session names for consumption by everyone else.
        /// </summary>
        /// <param name="sname"></param>
        /// <returns></returns>
        /// <remarks>A null is converted to an empty string</remarks>
        private string NormalizedSessionName(string sname)
        {
            if (sname != null)
                return sname;
            return "";
        }

        /// <summary>
        /// Get the conference data in a normalized format assuming an xml source.
        /// </summary>
        /// <returns></returns>
        private async Task<SimpleAgendaDataModel.Meeting> GetNormalizedConferenceDataFromXML(AgendaInfo meeting)
        {
            ///
            /// Grab all the details
            ///

            var data = await GetFullConferenceDataXML(meeting);

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
        /// Given the material attached with this session, turn it into talks for later processing. These are
        /// funny talks (no time, etc.).
        /// </summary>
        /// <param name="material"></param>
        /// <returns></returns>
        private SimpleAgendaDataModel.Talk[] ParseConferenceExtraMaterialJSON(IList<JSON.Folder> material)
        {
            if (material == null)
                return new Talk[0];

            return material
                .Select(CreateExtraMaterialTalk)
                .ToArray();
        }

        /// <summary>
        /// If we don't get a real contribution and just material, here is what we can do...
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private Talk CreateExtraMaterialTalk(JSON.Folder arg)
        {
            var allMaterial = ConvertToTalkMaterial(arg);

            var rt = new Talk()
            {
                Title = arg.title,
                ID = arg.id.ToString(),
                StartDate = new DateTime(),
                EndDate = new DateTime(),
                SubTalks = allMaterial
                    .Select(tm => new Talk()
                    {
                        SlideURL = tm.URL,
                        AllMaterial = new TalkMaterial[] { tm },
                        DisplayFilename = tm.DisplayFilename,
                        FilenameExtension = tm.FilenameExtension,
                        ID = "0",
                        TalkType = TypeOfTalk.ExtraMaterial
                    })
                    .ToArray(),
                TalkType = TypeOfTalk.ExtraMaterial
            };
            return rt;
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
            if (fileType.StartsWith("."))
            {
                fileType = fileType.Substring(1);
            }
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

            return SortNonSessionTalksIntoSessions(sessions, talks)
                .ToArray();
        }

        /// <summary>
        /// Given a list of sessions that were specifically organized, and a list of talks that are
        /// not associated with sessions, generate a series of dummy sessions that contain the talks. The
        /// trick is to split the talks around the sessions (so we may generate multiple sessions).
        /// </summary>
        /// <param name="definedSessions">List of defined sessions in the meeting</param>
        /// <param name="unassociatedTalks">The talks not associated to any session</param>
        /// <returns>List of sessions containing the unassociated talks</returns>
        private static IEnumerable<Session> SortNonSessionTalksIntoSessions(IEnumerable<Session> definedSessions, IEnumerable<Talk> unassociatedTalks)
        {
            // The easy case is there are no sessions, so this becomes just one session.
            if (definedSessions == null || definedSessions.Count() == 0)
            {
                var s1 = new Session()
                {
                    ID = "-1",
                    Title = "<ad-hoc session>",
                    Talks = unassociatedTalks.OrderBy(t => t.StartDate).ToArray()
                };
                s1.StartDate = FindEarliestTime(s1.Talks);
                s1.EndDate = FindLastTime(s1.Talks);
                return new Session[] { s1 };
            }

            // We have to split the contributions up around each session. The algorithm is as follows:
            // 1. Find the time before a session that each talk occurs
            // 2. Find the smallest amount of time, and associate that talk with that session.
            // 3. Each group should be made into a new session.
            // 4. All other talks (which presumably occur after the last session) are made into their own session.

            var deltaTime = from c in unassociatedTalks
                            select new
                            {
                                contrib = c,
                                closestSession = definedSessions.Where(s => s.StartDate > c.StartDate).OrderBy(s => s.StartDate - c.StartDate).FirstOrDefault()
                            };

            var sessionGroups = deltaTime.Where(c => c.closestSession != null).GroupBy(x => x.closestSession);
            var contribSessions = from sg in sessionGroups
                                  select new Session()
                                  {
                                      ID = "-1",
                                      Title = "<ad-hoc session>",
                                      Talks = sg
                                      .Select(c => c.contrib)
                                      .OrderBy(t => t.StartDate)
                                      .ToArray()
                                  };

            // And the sessions that are left over.
            var lastSessionTalks = deltaTime.Where(c => c.closestSession == null).Select(c => c.contrib);
            var lastSession = new Session()
            {
                ID = "-1",
                Title = "<ad-hoc session>",
                Talks = lastSessionTalks
                        .OrderBy(t => t.StartDate)
                        .ToArray()
            };

            // Put it all together
            var result = contribSessions;
            if (lastSession.Talks.Length > 0)
            {
                result = result
                    .Concat(new Session[] { lastSession });
            }

            // Normalize start and end times
            result = result
                .Select(t =>
                {
                    t.StartDate = FindEarliestTime(t.Talks);
                    t.EndDate = FindLastTime(t.Talks);
                    return t;
                });

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
        /// Convert a JSON agenda format from the server into a real date.
        /// </summary>
        /// <param name="jDate"></param>
        /// <returns></returns>
        private DateTime AgendaStringToDate(JSON.JDate jDate)
        {
            // If a meeting has a null date or time, we should
            // pass back something that is so obviously bogus...
            if (jDate == null)
            {
                return new DateTime();
            }

            // Next, extract the time, taking into account the time zone info.
            var dt = DateTime.Parse(jDate.date) + TimeSpan.Parse(jDate.time);
            var lt = new LocalDateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute);
            var tz = DateTimeZoneProviders.Tzdb[jDate.tz];
            var x = tz.AtStrictly(lt);

            return x.ToDateTimeUnspecified();
        }

        /// <summary>
        /// Given a contribution, return a talk.
        /// </summary>
        /// <param name="contrib"></param>
        /// <returns></returns>
        private Talk ExtractTalkInfo(IndicoInterface.NET.IndicoDataModel.contribution contrib)
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

            // Get all material and the best material to show
            result.AllMaterial = ConvertToTalkMaterial(contrib.material).ToArray();
            var bm = FindBestMaterial(result.AllMaterial);
            if (bm != null)
            {
                result.SlideURL = bm.URL;
                result.DisplayFilename = bm.DisplayFilename;
                result.FilenameExtension = bm.FilenameExtension;
            }

            // Next, sub contributions, if there are any!
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
        /// Given the list of material's from the XML parse, return it as actual material
        /// </summary>
        /// <param name="allMaterial"></param>
        /// <returns></returns>
        private static IEnumerable<TalkMaterial> ConvertToTalkMaterial(IndicoDataModel.material[] allMaterial)
        {
            if (allMaterial == null)
            {
                return new TalkMaterial[0];
            }

            // Do the conversion
            return (from m in allMaterial
                    where m.files != null && m.files.file != null
                    from f in m.files.file
                    select new TalkMaterial()
                    {
                        URL = f.url,
                        FilenameExtension = Path.GetExtension(SantizeURL(f.name)),
                        DisplayFilename = Path.GetFileNameWithoutExtension(SantizeURL(f.name)),
                        MaterialType = m.title
                    });
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
