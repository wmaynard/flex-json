namespace Maynard.Json.Exceptions;

public class ModelValidationException(Model model, IEnumerable<string> errors) : Exception(message: $"{model.GetType().Name} failed validation")
{
    public string[] Errors { get; init; } = errors.ToArray();
}