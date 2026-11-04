namespace WeighingMS.Api.Common;

/// <summary>Enum values on the wire are camelCase strings (api-conventions → Response Format).</summary>
public static class WireNames
{
    /// <summary>The camelCase wire name of an enum value.</summary>
    public static string Of<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        var name = value.ToString();
        return string.Concat(char.ToLowerInvariant(name[0]).ToString(), name[1..]);
    }
}
