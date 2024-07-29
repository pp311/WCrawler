using LanguageExt;
using LanguageExt.Common;
using Newtonsoft.Json;
using static LanguageExt.Prelude;

public static class JsonAff 
{
    public static Aff<Unit> WriteToJsonFile<T>(string filePath, T objectToWrite, bool append = false)
    {
        return Aff(async () => await use(new StreamWriter(filePath, append), 
            async writer  =>
            {
                await writer.WriteAsync(JsonConvert.SerializeObject(objectToWrite, Formatting.Indented));
                return unit;
            }));
    }
    
    public static Aff<T> ReadFromJsonFile<T>(string filePath)
    {
        return AffMaybe<T>(async () => 
            await use(new StreamReader(filePath),
                async reader =>
                {
                    var fileContents = await reader.ReadToEndAsync();
                    return JsonConvert.DeserializeObject<T>(fileContents);
                }) switch
            {
                null => Error.New("Cannot deserialize file"),
                var obj => obj
            }
        );
    }
}