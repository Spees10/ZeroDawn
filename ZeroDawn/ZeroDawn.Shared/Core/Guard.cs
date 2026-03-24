#nullable enable

namespace ZeroDawn.Shared.Core;

public static class Guard
{
    public static T AgainstNull<T>(T? value, string paramName) where T : class
        => value ?? throw new ArgumentNullException(paramName);

    public static string AgainstNullOrEmpty(string? value, string paramName)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value cannot be null or empty.", paramName)
            : value;

    public static Guid AgainstEmptyGuid(Guid value, string paramName)
        => value == Guid.Empty
            ? throw new ArgumentException("GUID cannot be empty.", paramName)
            : value;
}
