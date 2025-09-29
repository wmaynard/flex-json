namespace Maynard.Json.Utilities;

public class LogEventArgs : EventArgs
{
    public string Message { get; set; }
    public FlexJson Data { get; set; }
    public Exception Exception { get; set; }
}