using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Updater
{
    public class DownloadException : Exception
    {
        public DownloadException(string message) : base(message) { }
        public DownloadException(string message, Exception inner) : base(message, inner) { }
    }
}
