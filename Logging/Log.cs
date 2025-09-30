using Maynard.Json.Exceptions;

namespace Maynard.Logging;

internal static class Log
{
    internal static EventHandler<FlexJsonLogEventArgs> OnLog;
    
    internal static void Verbose(string message, object data = null) => OnLog?.Invoke(null, new ()
    {
        Message = message, 
        Severity = FlexJsonLogEventArgs.VERBOSE,
        Data = data
    });

    internal static void Info(string message, object data = null) => OnLog?.Invoke(null, new()
    {
        Message = message,
        Severity = FlexJsonLogEventArgs.INFO,
        Data = data
    });
    
    internal static void Warn(string message, object data = null) => OnLog?.Invoke(null, new()
    {
        Message = message,
        Severity = FlexJsonLogEventArgs.WARN,
        Data = data
    });
    
    internal static void Error(string message, object data = null) => OnLog?.Invoke(null, new()
    {
        Message = message,
        Severity = FlexJsonLogEventArgs.ERROR,
        Data = data
    });
    
    internal static void Critical(string message, object data = null) => OnLog?.Invoke(null, new()
    {
        Message = message,
        Severity = FlexJsonLogEventArgs.CRITICAL,
        Data = data
    });
    
    internal static void Good(string message, object data = null) => OnLog?.Invoke(null, new()
    {
        Message = message,
        Severity = FlexJsonLogEventArgs.GOOD,
        Data = data
    });
    
    
    private static void Fire(FlexJsonLogEventArgs args)
    {
        bool unhandled = OnLog == null && (args.Severity is FlexJsonLogEventArgs.ERROR or FlexJsonLogEventArgs.CRITICAL);
        if (unhandled)
            throw new UnhandledFlexJsonLogEventException("Unhandled error-level FlexJson log event.", args);
        OnLog?.Invoke(null, args);
    }
}