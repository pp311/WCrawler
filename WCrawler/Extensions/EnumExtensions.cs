namespace WCrawler.Extensions;

public static class EnumExtensions
{
    public static string ToValue(this Enum value)
    {
        var fieldInfo = value.GetType().GetField(value.ToString());

        if (fieldInfo?.GetCustomAttributes(typeof(StringValueAttribute), false) is not StringValueAttribute[] attributes)
            return value.ToString();

        return attributes.Length > 0 ? attributes[0].StringValue : value.ToString();
    }

    public static T ToEnum<T>(this string value) where T : Enum
    {
        var type = typeof(T);
        if (!type.IsEnum) throw new InvalidOperationException();

        foreach (var field in type.GetFields())
        {
            if (field.GetCustomAttributes(typeof(StringValueAttribute), false) is not StringValueAttribute[] attributes)
                continue;

            if (attributes.Length > 0 && attributes[0].StringValue == value)
                return (T)field.GetValue(null)!;
        }

        throw new KeyNotFoundException();
    }
}

public class StringValueAttribute(string value) : Attribute
{
    public string StringValue { get; protected set; } = value;
}
