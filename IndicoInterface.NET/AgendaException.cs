using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IndicoInterface.NET
{
    /// <summary>
    /// For exceptions thrown by the Agenda interface
    /// </summary>
    public class AgendaException : Exception
    {
        public AgendaException(string message)
            : base(message)
        {
        }

        public AgendaException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// When the format we request is depreciated
    /// </summary>
    public class AgendaFormatDepreciatedException : Exception
    { 
        public AgendaFormatDepreciatedException(string message = "")
            : base (message)
        { }
    }
}
