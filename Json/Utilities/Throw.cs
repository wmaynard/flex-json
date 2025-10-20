using Maynard.Logging;

namespace Maynard.Json.Utilities;

internal static class Throw
{
    internal static EventHandler<Exception> OnException;

    internal static T Ex<T>(Exception ex)
    {
        if (OnException != null)
        {
            OnException.Invoke(null, ex);
            return default;
        }
        Log.Error(ex.Message, ex);
        throw ex;
    }
}