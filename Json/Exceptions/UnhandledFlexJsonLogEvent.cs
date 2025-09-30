using Maynard.Logging;

namespace Maynard.Json.Exceptions;

public class UnhandledFlexJsonLogEventException(string message, FlexJsonLogEventArgs args) : Exception(message)
{
    public FlexJsonLogEventArgs Args { get; set; } = args;
}