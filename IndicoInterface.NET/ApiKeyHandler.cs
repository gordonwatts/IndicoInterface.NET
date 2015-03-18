
using System;
using System.Collections.Generic;
namespace IndicoInterface.NET
{
    /// <summary>
    /// The indico API uses keys and secret signatures (sometimes) for dealing with
    /// its HTTP api. The code to implement that properly can be found in this class.
    /// </summary>
    public class ApiKeyHandler
    {
        /// <summary>
        /// Encode a URI request. If secret key is blank, then this won't be signed. Otherwise, it will be signed
        /// according to the proceedure found here:
        ///     http://indico.readthedocs.org/en/latest/http_api/access/
        ///     
        /// Note: if the signature is non-null, the uri will be timesamped, and must be used fairly quickly.
        /// </summary>
        /// <param name="site">The site (indico.cern.ch)</param>
        /// <param name="requestedPath">The root path (/export/category/2.ics)</param>
        /// <param name="parameters">Dictionary of parameters in the request (label=10)</param>
        /// <param name="apiKey">The ApiKey from the website</param>
        /// <param name="secretKey">The secret ApiKey from the website</param>
        /// <returns>The request part of the URI for this resource</returns>
        public static string IndicoEncode(string requestedPath, Dictionary<string, string> parameters, string apiKey, string secretKey, DateTime? when = null)
        {
            throw new NotImplementedException();
        }
    }
}
