namespace LinkBlog.Web.Logging;

public static partial class Log
{
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Warning,
        Message = "No file was uploaded.")]
    public static partial void NoFileUploaded(this ILogger logger);
}