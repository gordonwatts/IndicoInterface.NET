
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

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
        public static string IndicoEncode(string requestedPath, IEnumerable<KeyValuePair<string, string>> parameters, string apiKey, string secretKey, DateTime? when = null, bool useTimeStamp = true)
        {
            // Add all parameters
            var allParameters = new Dictionary<string, string>();
            if (parameters != null)
            {
                foreach (var p in parameters)
                {
                    allParameters.Add(p.Key, p.Value);
                }
            }

            // Add the api key and the timestamp
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                allParameters.Add("apikey", apiKey);
            }

            // We only need to add date/time if we are also going to sign this.
            if (!string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(secretKey) && useTimeStamp)
            {
                // Use "Now" if we don't have a specific time.
                var t = when.HasValue ? when.Value : DateTime.Now;
                allParameters.Add("timestamp", t.AsSecondsFromUnixEpoch().ToString());
            }

            // Generate the full request
            var bld = new StringBuilder();
            bld.Append(requestedPath);
            bool first = true;
            foreach (var param in allParameters.OrderBy(x => x.Key))
            {
                var seperator = first ? "?" : "&";
                first = false;
                bld.AppendFormat("{2}{0}={1}", param.Key, param.Value, seperator);
            }

            // Now, generate the signature, and append it, if needed
            if (!string.IsNullOrWhiteSpace(secretKey))
            {
                var s = first ? "?" : "&";
                first = false;
                bld.AppendFormat("{1}signature={0}", GenerateSignature(secretKey, bld.ToString()), s);
            }

            return bld.ToString();
        }

        /// <summary>
        /// Generate the signature.
        /// Pulled from here: http://stackoverflow.com/questions/10254369/generate-sha1-hash-in-portable-class-library
        /// </summary>
        /// <param name="key"></param>
        /// <param name="signatureBase"></param>
        /// <returns></returns>
        private static string GenerateSignature(string key, string signatureBase)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var hashAlgorithm = new HMACSHA1(keyBytes);
            byte[] dataBuffer = Encoding.UTF8.GetBytes(signatureBase);
            byte[] hashBytes = hashAlgorithm.ComputeHash(dataBuffer);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }
}
