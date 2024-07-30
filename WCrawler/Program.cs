using WCrawler;
using WCrawler.Helpers;
using Spectre.Console;
using LanguageExt;
using WCrawler.Extensions;
using static LanguageExt.Prelude;

#region InitSource
// var source = new Source
// {
//     Address = "https://bachngocsach.com.vn/reader/kiem-dao-de-nhat-tien-convert",
//     ChapterUrl = "https://bachngocsach.com.vn/reader/kiem-dao-de-nhat-tien-convert/muc-luc?page=all",
//     BaseUrl = "https://bachngocsach.com.vn",
//     TitleSelector = "#truyen-title",
//     GetChapterType = GetChapterType.AllChapterPage,
//     ChapterListSelector = ".chuong-item",
//     ChapterTitleSelector = ".chuong-name",
//     ChapterUrlSelector = ".chuong-link",
//     AuthorNameSelector = "#tacgia > a",
//     ChapterContentSelector = "#noi-dung",
//     ChapterStartPageIndex = 1,
//     CoverSelector = "#anhbia > img",
//     StatusSelector = "",
//     DescriptionSelector = "#gioithieu > div",
// };
#endregion

await JsonAff.ReadFromJsonFile<Setting>("appsettings.json")
    .Iter(setting => AppSetting.InitSetting(setting.ExportDir))
    .Run();


AnsiConsole.Write(new FigletText("Whitemage").Centered().Color(Color.Red));


(await ConsoleHelper.GetMainMenuAction()
    .MapAsync(action =>
        action switch
        {
            MainMenuAction.Crawl => ConsoleHelper.GetCrawlSourceType()
                .Bind(ConsoleHelper.PromptCrawlUrl)
                .Bind(result => CrawlHelper.GetSourceAsync(result.SourceType, result.Url))
                .Bind(CrawlHelper.GetBookAsync)
                .Bind(book => JsonAff.WriteToJsonFile($"{AppSetting.CrawlResultDir}/{book.Title}.json", book))
                .Iter(_ => ConsoleHelper.WriteLine("Success", Color.Green))
                .RunUnit(),
            MainMenuAction.TestSource => ConsoleHelper.GetCrawlSourceType()
                .Bind(sourceType => JsonAff.ReadFromJsonFile<Source>($"{AppSetting.SourceDir}/{sourceType.ToValue()}.json"))
                .Bind(ConsoleHelper.PrintViewSourceLayout)
                .RunUnit(),
            MainMenuAction.Export => ConsoleHelper.PromptCrawlResult()
                .Bind(fileName => JsonAff.ReadFromJsonFile<Book>($"{AppSetting.CrawlResultDir}/{fileName}.json")) 
                .Iter(EpubHelper.CreateEpubAsync)
                .RunUnit(),
            MainMenuAction.Setting => ConsoleHelper.PrintSettingTable()
                .Map(_ => ConsoleHelper.PromptSettingAction())
                .Bind(ConsoleHelper.HandleSettingAction)
                .IfFail(ConsoleHelper.WriteError)
                .RunUnit(),
            MainMenuAction.Quit => Eff(ConsoleHelper.Exit).ToAff().RunUnit(),
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        }
    )
    .Repeat(Schedule.RepeatForever)
    .Run())
    .ThrowIfFail();



