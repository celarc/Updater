namespace Updater.Models
{
    public class UpdateResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public System.Exception Exception { get; set; }
        public int FilesUpdated { get; set; }

        public static UpdateResult CreateSuccess(string message = null, int filesUpdated = 0)
        {
            return new UpdateResult
            {
                Success = true,
                Message = message ?? "Update completed successfully",
                FilesUpdated = filesUpdated
            };
        }

        public static UpdateResult CreateFailure(string message, System.Exception exception = null)
        {
            return new UpdateResult
            {
                Success = false,
                Message = message,
                Exception = exception
            };
        }
    }
}