using Maynard.Json;

namespace Maynard.Logging;

public class FlexJsonLogEventArgs : EventArgs
{
    public const int GOOD = 0;
    public const int VERBOSE = 1;
    public const int INFO = 2;
    public const int WARN = 3;
    public const int ERROR = 4;
    public const int ALERT = 5;
    
    public string Message { get; set; }
    public int Severity { get; set; }
    
    public object Data { get; set; }
}