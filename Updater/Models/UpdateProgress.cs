namespace Updater.Models
{
    public class UpdateProgress
    {
        public int PercentComplete { get; set; }
        public string StatusMessage { get; set; }
        public string CurrentFile { get; set; }
        public bool IsError { get; set; }
        public System.Exception Exception { get; set; }

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

        public static UpdateProgress CreateError(System.Exception exception)
        {
            return new UpdateProgress
            {
                IsError = true,
                Exception = exception,
                StatusMessage = exception.Message
            };
        }
    }
}