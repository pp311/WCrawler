using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using LanguageExt;
using Newtonsoft.Json;
using Spectre.Console;

namespace WCrawler.Extensions;

public static class CommonExtensions {
    public static Unit WriteLine(this string value) {
        AnsiConsole.WriteLine(value);
        return Unit.Default;
    }
    
    public static string Serialize<T>(this T obj) {
        return JsonConvert.SerializeObject(obj);
    }
    
    public static T? Deserialize<T>(this string json) {
        var obj = System.Text.Json.JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        return obj;
    }
    
    public static bool IsBlank([NotNullWhen(false)] this string? value) {
        return string.IsNullOrWhiteSpace(value);
    }

    public static void IfAction<T>(this T obj, bool cond, Action<T> action)
    {
        if (cond)
            action(obj);
    }
    
    public static Option<T> ToOption<T>(this T obj) {
        return obj == null ? Option<T>.None : Option<T>.Some(obj);
    }
}