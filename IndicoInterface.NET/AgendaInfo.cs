using System;
using System.Text;
using System.Text.RegularExpressions;

namespace IndicoInterface.NET
{
    /// <summary>
    /// Token that contains a reference to an agenda
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
        /// Find the agenda information from the initial Indico URL format.
        /// </summary>
        static Regex _gConfIdFinderStyle1 = new Regex("(?<protocal>http|https)://(?<site>[^/]+)/(?<subdir>.+/)?.*(?i:confId)=(?<conf>\\w+)");

        /// <summary>
        /// Find the agenda information from the event Indico URL format
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
        /// Parameterless ctor to be used for serialization.
        /// </summary>
        private AgendaInfo()
        {
        }

        /// <summary>
        /// Returns a printable string describing this conference.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Agenda at " + AgendaSite + " with conference ID " + ConferenceID.ToString();
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
                if (WhiteListInfo.CanUseEventFormat(this))
                {
                    bld.AppendFormat("event/{0}", ConferenceID);
                }
                else
                {
                    bld.AppendFormat("conferenceDisplay.py?confId={0}", ConferenceID);
                }
                return bld.ToString();
            }
        }

        /// <summary>
        /// Convert to a short string - meant to be compact and can be written out. Unique as well.
        /// </summary>
        /// <returns>Short string which is unique and can be used to re-generate</returns>
        public static AgendaInfo FromShortString(string ss)
        {
            var s = ss.Split(new string[] { "/^/" }, System.StringSplitOptions.None);
            if (s.Length != 3)
            {
                throw new ArgumentException(string.Format("Indico Agenda short string '{0}' was in bad format.", ss));
            }

            return new AgendaInfo() { AgendaSite = s[0], AgendaSubDirectory = s[1], ConferenceID = s[2] };
        }

        /// <summary>
        /// Return this agenda as a compact unique string. Platform independent.
        /// </summary>
        /// <returns></returns>
        public string AsShortString()
        {
            return string.Format("{0}/^/{1}/^/{2}", AgendaSite, AgendaSubDirectory, ConferenceID);
        }
    }
}
