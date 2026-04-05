using System.ComponentModel;
using System.Globalization;
using System.Linq.Expressions;

namespace Warehouse.GenericFiltering;

/// <summary>
/// Converts raw string filter values into typed <see cref="ConstantExpression"/> instances.
/// <para>Handles null, array literals, collection element types, enums, and scalar parsing.</para>
/// </summary>
internal static class ValueConverter
{
    /// <summary>
    /// Converts a raw string value into a <see cref="ConstantExpression"/> matching the selector return type.
    /// </summary>
    internal static ConstantExpression ConvertToExpression(LambdaExpression selectorExpression, string rawValue)
    {
        Type selectorReturnType = selectorExpression.ReturnType;

        if (string.Equals(rawValue, "null", StringComparison.OrdinalIgnoreCase))
            return BuildNullConstant(selectorReturnType);

        Type underlyingType = Nullable.GetUnderlyingType(selectorReturnType)
                              ?? selectorReturnType;

        if (rawValue.StartsWith("[") && rawValue.EndsWith("]"))
            return BuildArrayConstant(rawValue, underlyingType);

        Type? seqInterface = PropertyPathResolver.FindGenericEnumerableInterface(selectorReturnType);
        if (seqInterface != null && selectorReturnType != typeof(string))
        {
            Type elementType = seqInterface.GetGenericArguments()[0];
            object singleValue = ParseSingleToken(rawValue.Trim(), elementType);
            return Expression.Constant(singleValue, elementType);
        }

        object finalValue = ParseSingleToken(rawValue, underlyingType);
        return Expression.Constant(finalValue, selectorReturnType);
    }

    /// <summary>
    /// Builds a null <see cref="ConstantExpression"/>, wrapping value types in <see cref="Nullable{T}"/>.
    /// </summary>
    private static ConstantExpression BuildNullConstant(Type selectorReturnType)
    {
        if (selectorReturnType.IsValueType && Nullable.GetUnderlyingType(selectorReturnType) == null)
            selectorReturnType = typeof(Nullable<>).MakeGenericType(selectorReturnType);

        return Expression.Constant(null, selectorReturnType);
    }

    /// <summary>
    /// Builds a typed array <see cref="ConstantExpression"/> from an "[a,b,c]" literal.
    /// </summary>
    private static ConstantExpression BuildArrayConstant(string rawValue, Type elementType)
    {
        string inner = rawValue.Substring(1, rawValue.Length - 2).Trim();
        string[] tokens = string.IsNullOrWhiteSpace(inner)
            ? []
            : inner
                .Split([','], StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => t.Length > 0)
                .ToArray();

        Array array = Array.CreateInstance(elementType, tokens.Length);

        for (int i = 0; i < tokens.Length; i++)
            array.SetValue(ParseSingleToken(tokens[i], elementType), i);

        Type ienumerableT = typeof(IEnumerable<>).MakeGenericType(elementType);
        return Expression.Constant(array, ienumerableT);
    }

    /// <summary>
    /// Parses a single string token into an object of the target type.
    /// </summary>
    private static object ParseSingleToken(string token, Type targetType)
    {
        if (token.Equals("null"))
            return null!;

        if (targetType == typeof(string))
            return StripQuotes(token);

        if (targetType.IsEnum)
            return Enum.Parse(targetType, token, ignoreCase: true);

        TypeConverter converter = TypeDescriptor.GetConverter(targetType);
        if (converter.CanConvertFrom(typeof(string)) && !string.IsNullOrEmpty(token))
            return converter.ConvertFromInvariantString(token)!;

        return Convert.ChangeType(token, targetType, CultureInfo.InvariantCulture)!;
    }

    /// <summary>
    /// Strips surrounding single or double quotes from a token.
    /// </summary>
    private static string StripQuotes(string token)
    {
        if ((token.StartsWith("'") && token.EndsWith("'")) ||
            (token.StartsWith("\"") && token.EndsWith("\"")))
            return token.Substring(1, token.Length - 2);

        return token;
    }
}
