using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatneyAstrometry.SolverVizTools.Exceptions
{
    public class DownloadException : Exception
    {
        public DownloadException(string message, Exception inner = null) : base(message, inner)
        {
            
        }
    }
}
