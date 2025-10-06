namespace Maynard.Json.Exceptions;

public class ModelValidationException(FlexModel flexModel, IEnumerable<string> errors) : Exception(message: $"{flexModel.GetType().Name} failed validation")
{
    public string[] Errors { get; init; } = errors.ToArray();
}