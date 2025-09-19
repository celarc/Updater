namespace Updater.Models
{
    public class UpdateProgress
    {
        public int PercentComplete { get; set; }
        public string StatusMessage { get; set; }
        public string CurrentFile { get; set; }
        public bool IsError { get; set; }
        public System.Exception Exception { get; set; }

        public long BytesTransferred { get; set; }
        public long TotalBytes { get; set; }
        public double TransferSpeed { get; set; }
        public string OperationType { get; set; }

        public static UpdateProgress Create(int percent, string status = null, string currentFile = null)
        {
            return new UpdateProgress
            {
                PercentComplete = percent,
                StatusMessage = status,
                CurrentFile = currentFile,
                IsError = false
            };
        }

        public static UpdateProgress CreateWithDetails(int percent, string status, string currentFile,
            long bytesTransferred, long totalBytes, double transferSpeed, string operationType)
        {
            return new UpdateProgress
            {
                PercentComplete = percent,
                StatusMessage = status,
                CurrentFile = currentFile,
                IsError = false,
                BytesTransferred = bytesTransferred,
                TotalBytes = totalBytes,
                TransferSpeed = transferSpeed,
                OperationType = operationType
            };
        }

        public static UpdateProgress CreateError(System.Exception exception)
        {
            return new UpdateProgress
            {
                IsError = true,
                Exception = exception,
                StatusMessage = exception.Message
            };
        }

        public string FormattedSpeed
        {
            get
            {
                if (TransferSpeed < 1024)
                    return $"{TransferSpeed:F0} B/s";
                else if (TransferSpeed < 1024 * 1024)
                    return $"{TransferSpeed / 1024:F1} KB/s";
                else
                    return $"{TransferSpeed / (1024 * 1024):F1} MB/s";
            }
        }

        public string FormattedBytes
        {
            get
            {
                if (TotalBytes == 0) return "";

                string formatBytes(long bytes)
                {
                    if (bytes < 1024) return $"{bytes} B";
                    if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
                    return $"{bytes / (1024.0 * 1024):F1} MB";
                }

                return $"{formatBytes(BytesTransferred)} / {formatBytes(TotalBytes)}";
            }
        }
    }
}