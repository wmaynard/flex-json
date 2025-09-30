using Maynard.Json;

namespace Maynard.Logging;

public class FlexJsonLogEventArgs : EventArgs
{
    internal const int VERBOSE = 0;
    internal const int INFO = 1;
    internal const int WARN = 2;
    internal const int ERROR = 3;
    internal const int CRITICAL = 5;
    internal const int GOOD = 6;
    
    public string Message { get; set; }
    public int Severity { get; set; }
    
    public object Data { get; set; }
}