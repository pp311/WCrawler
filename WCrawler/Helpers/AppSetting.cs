using LanguageExt;

namespace WCrawler.Helpers;

public static class AppSetting
{
    public static string ExportDir { get; set; }
    public const string SourceDir = "./Sources";
    public const string TemplateDir = "./Templates";
    public static string CrawlResultDir => $"{ExportDir}/CrawlResult";
    public static string BookDir => $"{ExportDir}/Books";
    
    public static Unit InitSetting(string exportDir)
    {
        ExportDir = exportDir;
        CommonHelper.CreateDir(CrawlResultDir);
        CommonHelper.CreateDir(BookDir);
        return Unit.Default;
    }

    public static Aff<Unit> UpdateExportDir(string val)
    {
        ExportDir = val;
        return JsonAff.WriteToJsonFile("appsettings.json", new Setting { ExportDir = val });
    }
}