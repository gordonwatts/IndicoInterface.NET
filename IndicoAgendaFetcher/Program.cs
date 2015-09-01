using IndicoInterface.NET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace IndicoAgendaFetcher
{
    /// <summary>
    /// Given a string that can be loaded by the AgendaInfo object, fetch the JSON.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            // Extract arguments
            int v;
            bool json = true;
            var baseargs = args
                .Where(a => !ParseArguments(a, ref json));

            var sop = baseargs
                .Where(a => int.TryParse(a, out v))
                .Select(a => int.Parse(a))
                .Select(a => new AgendaInfo(a));

            var snop = baseargs
                .Where(a => !int.TryParse(a, out v))
                .Select(a => new AgendaInfo(a));

            var agendas = sop.Concat(snop).ToArray();

            // See if we can't load up a secret key, etc.
            var info = IndicoInterface.NET.Test.utils.GetApiAndSecret("indicoapi.key");

            // Now, fetch.
            var wg = new WebGetter();
            var loader = new AgendaLoader(wg);
            foreach (var a in agendas)
            {
                var url = json ? loader.GetAgendaFullJSONURL(a, info.Item1, info.Item2, true) : loader.GetAgendaFullXMLURL(a, true, info.Item1, info.Item2, true);
                using (var rdr = wg.GetDataFromURL(url).Result)
                {
                    var txt = rdr.ReadToEnd();
                    Console.WriteLine(txt);
                }
            }

        }

        /// <summary>
        /// parse the arguments
        /// </summary>
        /// <param name="a"></param>
        /// <param name="json"></param>
        /// <returns></returns>
        private static bool ParseArguments(string a, ref bool json)
        {
            if (a == "-xml")
            {
                json = false;
                return true;
            }
            if (a == "-json")
            {
                json = true;
                return true;
            }
            return false;
        }

        class WebGetter : IUrlFetcher
        {
            public async Task<StreamReader> GetDataFromURL(Uri uri)
            {
                var req = WebRequest.Create(uri);
                (req as HttpWebRequest).UserAgent = "DeepTalk AgentaInfo Test";
                var response = await req.GetResponseAsync();
                return new StreamReader(response.GetResponseStream());
            }
        }

    }
}
