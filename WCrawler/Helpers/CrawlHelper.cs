using AngleSharp;
using AngleSharp.Dom;
using WCrawler.Extensions;
using Dasync.Collections;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace WCrawler.Helpers;

public static class CrawlHelper
{
    public static Task<IDocument> GetDocumentAsync(string url) => GetContext().OpenAsync(url);
    
    static IBrowsingContext GetContext() => BrowsingContext.New(Configuration.Default.WithDefaultLoader());

    public static Aff<Source> GetSourceAsync(SourceType sourceType, string url)
    {
        return JsonAff.ReadFromJsonFile<Source>($"{AppSetting.SourceDir}/{sourceType.ToValue()}.json")
            .Map(source =>
            {
                source.Address = url;
                return source;
            });
    }

    public static Aff<Book> GetBookAsync(Source source) =>
        GetDocumentAsync(source.Address).ToAff()
            .Map(document => new Book
            {
                Title = document.QuerySelector(source.TitleSelector)?.TextContent ?? string.Empty,
                Cover = document.QuerySelector(source.CoverSelector)?.Attributes.GetNamedItem("src")?.Value,
                AuthorName = document.QuerySelector(source.AuthorNameSelector)?.TextContent,
                Description = document.QuerySelector(source.DescriptionSelector)?.TextContent
            })
            .Bind(book => book.AddChaptersAsync(source).ToAff());
    
    public static async Task<List<Chapter>> GetChaptersInPagedTOCAsync(Source source)
    {
        var chapterPageIndex = source.ChapterStartPageIndex;
        var chapterIndex = 1;

        var allChapters = Seq<Chapter>();

        return (await Aff(async () => await GetDocumentAsync(string.Format(source.GetChapterUrl, chapterPageIndex++)))
            .Map(document => document.QuerySelectorAll(source.ChapterListSelector))
            .Map(chapterElements => chapterElements
                .Select(chapterElement => new Chapter
                {
                    Title = chapterElement.QuerySelector(source.ChapterTitleSelector)?.TextContent,
                    Index = chapterIndex++,
                    Url = chapterElement.QuerySelector(source.ChapterUrlSelector)?.Attributes.GetNamedItem("href")?.Value
                }).ToList())
            .Fold(Schedule.Forever, 
                Seq<Chapter>(),
                (chapters, currentChapters) =>
                {
                    if (currentChapters
                        .Select(_ => _.Title)
                        .ToList()
                        .TrueForAll(title => chapters.Exists(c => c.Title == title)))
                    {
                        allChapters = chapters; 
                        raise<Error>(Error.New("Chapter title already exists"));
                    }

                    return chapters.Concat(currentChapters);
                })
            .IfFail(e =>
            {
                if (e.Message == "Chapter title already exists")
                    return allChapters;
                
                throw e;
            })
            .Run())
            .ThrowIfFail()
            .ToList();
    }

    public static async Task<List<Chapter>> GetChaptersInTOCAsync(Source source) =>
        (await GetDocumentAsync(source.GetChapterUrl))
            .QuerySelectorAll(source.ChapterListSelector)
            .Select((chapterElement, index) =>
                new Chapter
                {
                    Title = chapterElement.QuerySelector(source.ChapterTitleSelector)?.TextContent,
                    Index = index + 1,
                    Url = source.BaseUrl + chapterElement.QuerySelector(source.ChapterUrlSelector)?.Attributes.GetNamedItem("href")?.Value
                })
            .ToList();

    public static async Task<List<Chapter>> GetChaptersInReadingPageAsync(Source source, int chapterPageIndex = 0)
    {
        const int batchSize = 50;
        var chapters = AtomSeq<Chapter>();

        await Enumerable.Range(chapterPageIndex, batchSize).ParallelForEachAsync(async index =>
        {
            var chapter = 
                Some(await GetDocumentAsync(string.Format(source.GetChapterUrl, index)))
                    .Match(
                        Some: document => new Chapter
                        {
                            Index = index,
                            WordCount = document.QuerySelector(source.ChapterContentSelector)?.TextContent.Split(" ").Length ?? 0,
                            Content = document.QuerySelector(source.ChapterContentSelector)?.TextContent,
                            Title = string.IsNullOrEmpty(source.ChapterTitleSelector) 
                                        ? "Chương " + index 
                                        : document.QuerySelector(source.ChapterTitleSelector)?.TextContent
                        },
                        None: () => new Chapter());

            if (!chapter.Content.IsBlank())
                chapters.Add(chapter);
        });

        if (chapters.Length % batchSize != 0)
            chapters.Concat(await GetChaptersInReadingPageAsync(source, chapterPageIndex + batchSize));

        return chapters.OrderBy(c => c.Index).ToList();
    }
}