using WCrawler.Extensions;
using Spectre.Console;
using LanguageExt;
using LanguageExt.Common;
using Spectre.Console.Rendering;
using Spectre.Console.Json;
using static LanguageExt.Prelude;

namespace WCrawler.Helpers;

public static class ConsoleHelper
{
    public static Eff<MainMenuAction> GetMainMenuAction()
        => Eff(() => AnsiConsole.Prompt(
            new SelectionPrompt<MainMenuAction>()
                .Title("\nChoose an action:")
                .PageSize(10)
                .UseConverter(action => action.ToValue())
                .AddChoices(Enum.GetValues<MainMenuAction>())
        ));
    
    public static Eff<SourceType> GetCrawlSourceType()
        => Eff(() => AnsiConsole.Prompt(
            new SelectionPrompt<SourceType>()
                .PageSize(10)
                .UseConverter(sourceType => sourceType.ToValue())
                .AddChoices(Enum.GetValues<SourceType>())
        ));
    
    public static Aff<Unit> PrintViewSourceLayout(Source source)
        => CrawlHelper.GetBookAsync(source)
            .Map(book => {
                if (book.Chapters.Count > 0 && book.BookType == BookType.Novel && book.Chapters[0].Content?.Length > 250)
                {
                    book.Chapters[0].Content = book.Chapters[0].Content?.Substring(0, 250);
                    book.Chapters.RemoveAll(c => c != book.Chapters[0]);
                }

                var layout = new Layout("Root")
                    .SplitColumns(
                        new Layout("Left")
                            .Update(
                                new Panel(new JsonText(source.Serialize()))
                                    .Collapse()
                                    .Header("Source")
                                    .RoundedBorder()
                                    .BorderColor(Color.Yellow)
                            ),
                        new Layout("Right")
                            .Update(
                                new Panel(new JsonText(book.Serialize()))
                                    .Collapse()
                                    .Header("Demo book content")
                                    .RoundedBorder()
                                    .BorderColor(Color.Yellow)
                            ));

            AnsiConsole.Write(layout);
            return Unit.Default;
        });

    public static Eff<(SourceType SourceType, string Url)> PromptCrawlUrl(SourceType sourceType)
        => Eff(() => AnsiConsole.Ask<string>($"Enter the URL of {sourceType.ToValue()}:"))
            .Map(url =>
            {
                if (!url.Contains(sourceType.ToValue()))
                    raise<Error>(new ArgumentException("Invalid URL"));

                return (sourceType, url.TrimEnd('/'));
            })
            .IfFail(e => (sourceType, WriteError(e).ToString()[..0]));

    public static Eff<string> PromptCrawlResult()
        => Eff(() => AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .PageSize(10)
                .Title("\nChoose a file:")
                .AddChoices(
                    Directory.GetFiles(AppSetting.CrawlResultDir)
                        .Select(fileName => fileName.Split('/')
                            .Last()
                            .Replace(".json", string.Empty)))
                .EnableSearch()
        ));
    
    public static Eff<Unit> PrintSettingTable()
        => Eff(() =>
        {
            AnsiConsole.Write(
                new Table()
                    .Title("Setting")
                    .Border(new SquareTableBorder())
                    .AddColumns("Name", "Value")
                    .AddRow("Export directory", AppSetting.ExportDir)
            );
            return Unit.Default;
        });
    
    public static SettingAction PromptSettingAction()
    {
        return AnsiConsole.Prompt(
            new SelectionPrompt<SettingAction>()
                .Title("\nChoose an action:")
                .PageSize(10)
                .UseConverter(action => action.ToValue())
                .AddChoices(Enum.GetValues<SettingAction>())
        );
    }
    
    public static Aff<Unit> HandleSettingAction(SettingAction action)
        => action switch
        {
            SettingAction.EditExportDir => Eff(() =>  AnsiConsole.Ask<string>("Enter the new export directory:"))
                .Map(Path.GetFullPath)
                .Bind(AppSetting.UpdateExportDir),
            SettingAction.Back => Eff(() => unit),
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };
    
    public static Unit WriteError(Error error)
    {
        AnsiConsole.WriteException(error, ExceptionFormats.NoStackTrace);
        return unit;
    }
    
    public static Unit WriteLine(string value, Color color = default)
        => act(() => AnsiConsole.MarkupLine($"[{color}] {value} [/]")).ReturnUnit();

    public static Unit Exit()
    {
        Environment.Exit(0);
        return unit;
    }
}