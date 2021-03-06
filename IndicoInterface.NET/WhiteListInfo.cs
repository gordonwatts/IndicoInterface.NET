﻿using System.Collections.Generic;
using System.Linq;

namespace IndicoInterface.NET
{
    /// <summary>
    /// Hold onto info about site white lists... These are sites
    /// that have a particular format for their URL.
    /// </summary>
    public class WhiteListInfo
    {
        /// <summary>
        /// Uses the "new" /event/xxxx REST format.
        /// </summary>
        /// <remarks>Start with CERN in here - as it is the one we know about as we write this.</remarks>
        private static HashSet<string> _useEventFormat = null;

        /// <summary>
        /// List of sites that use JSON to get the agenda queries.
        /// </summary>
        private static HashSet<string> _useJSONAgendaRequests = null;

        /// <summary>
        /// Clear out the white list (perhaps only for testing?).
        /// </summary>
        public static void ClearWhiteLists()
        {
            _useEventFormat.Clear();
            _useJSONAgendaRequests.Clear();
        }

        /// <summary>
        /// Add a site to the list of ones that can use the white list.
        /// </summary>
        /// <param name="site"></param>
        public static void AddSiteThatUsesEventFormat(string site)
        {
            _useEventFormat.Add(site);
        }

        /// <summary>
        /// Add a site to the list of ones that can use JSON queries to fetch the agenda.
        /// </summary>
        /// <param name="site"></param>
        public static void AddSiteThatUsesJSONAgendaQueries (string site)
        {
            _useJSONAgendaRequests.Add(site);
        }

        /// <summary>
        /// Return the list of sites on the white list. Usually only used for testing.
        /// </summary>
        /// <returns></returns>
        public static string[] GetUseEventWhitelist()
        {
            return _useEventFormat.ToArray();
        }

        /// <summary>
        /// Return a list of sites that can use JSON for agenda queries that we know about
        /// </summary>
        /// <returns></returns>
        public static string[] GetUseJSONAgendaQueries()
        {
            return _useJSONAgendaRequests.ToArray();
        }

        /// <summary>
        /// Static ctor to setup list properly.
        /// </summary>
        static WhiteListInfo()
        {
            Reset();
        }

        /// <summary>
        /// Reset to when we are first created. Primarily used for testing.
        /// </summary>
        public static void Reset()
        {
            _useEventFormat = new HashSet<string>() { "indico.cern.ch" };
            _useJSONAgendaRequests = new HashSet<string>() { "indico.cern.ch" };
        }

        /// <summary>
        /// Return true if this agenda is at a site that can use the new event format.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public static bool CanUseEventFormat(AgendaInfo info)
        {
            return _useEventFormat.Contains(info.AgendaSite);
        }

        /// <summary>
        /// Return true if we know about JSON queries for this site.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public static bool UseJSONAgendaLoaderRequests(AgendaInfo info)
        {
            return _useJSONAgendaRequests.Contains(info.AgendaSite);
        }
    }
}
