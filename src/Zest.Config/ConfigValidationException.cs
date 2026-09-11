namespace Zest.Config;

public sealed class ConfigValidationException : Exception
{
    public ConfigValidationException(IReadOnlyList<string> errors)
        : base("Configuration validation failed:" + Environment.NewLine + string.Join(Environment.NewLine, errors.Select(error => $"- {error}")))
    {
        Errors = errors;
    }

    public IReadOnlyList<string> Errors { get; }
}
