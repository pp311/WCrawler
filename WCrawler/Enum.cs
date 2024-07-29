using WCrawler.Extensions;

namespace WCrawler;

public enum GetChapterType
{
    PagedTOC = 1,
    TOC = 2,
    ReadingPage = 3
}

public enum MainMenuAction
{
    [StringValue("Crawl")] Crawl = 1,
    [StringValue("Edit source")] EditSource = 2,
    [StringValue("Export")] Export = 3,
    [StringValue("Setting")] Setting = 4,
    [StringValue("Quit")] Quit = 5
}

public enum SettingAction
{
    [StringValue("Edit export dir")] EditExportDir = 1,
    [StringValue("Back")] Back = 2
}

public enum SourceType
{
    [StringValue("bachngocsach.com.vn")] BachNgocSach = 1,
    [StringValue("truyenfull.vn")] TruyenFull = 2
}