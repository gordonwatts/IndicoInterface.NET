using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndicoInterface.NET.IndicoDataModel
{
    static class Utils
    {
        /// <summary>
        /// Check to see if the conference format has been depreciated. Throw if it has.
        /// </summary>
        /// <param name="conf"></param>
        /// <returns></returns>
        public static iconf CheckNotDepreciated (this iconf conf)
        {
            if (conf._deprecated == "True")
            {
                throw new AgendaFormatDepreciatedException();
            }
            return conf;
        }
    }
}
