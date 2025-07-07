using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Updater
{
    public interface IDownloadService
    {
        Task DownloadAsync(
            string targetDirectory,
            string version = null,
            IProgress<int> progress = null,
            CancellationToken cancellationToken = default);
    }
}
