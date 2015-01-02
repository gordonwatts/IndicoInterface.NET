using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndicoInterface.NET
{
    public interface IUrlFetcher
    {
        // Fetch the data from the web
        Task<StreamReader> GetDataFromURL(Uri uri);
    }
}
