using Dasync.Collections;
using static LanguageExt.Prelude;
using static WCrawler.Helpers.CrawlHelper;

namespace WCrawler.Extensions;

public static class CrawlExtensions
{
    public static async Task<Book> AddChaptersAsync(this Book book, Source source)
    {
        var chapters = source.GetChapterType switch
        {
            GetChapterType.ReadingPage => await GetChaptersInReadingPageAsync(source),
            GetChapterType.TOC => await GetChaptersInTOCAsync(source),
            GetChapterType.PagedTOC => await GetChaptersInPagedTOCAsync(source),
            _ => throw new ArgumentOutOfRangeException(source.GetChapterType.ToString())
        };
        
        var failedChapter = AtomSeq<Chapter>();

        await chapters.ParallelForEachAsync(async chapter =>
        {
            if (chapter.Url.IsBlank())
                return;
            
            if (book.BookType == BookType.Novel)
                Optional(await GetDocumentAsync(chapter.Url))
                    .Map(document => document.QuerySelector(source.ChapterContentSelector)?.TextContent)
                    .BiIter(
                        Some: content =>
                        {
                            chapter.Content = content;
                            chapter.WordCount = content?.Split(" ").Length ?? 0;
                        },
                        None: () => failedChapter.Add(chapter));
            else
                Optional(await GetDocumentAsync(chapter.Url))
                    .Map(document => document.QuerySelectorAll(source.ChapterContentSelector)
                        .Select(item => item.Attributes.GetNamedItem("href")?.Value))
                    .BiIter(
                        Some: urls =>
                        {
                            chapter.Content = urls.Serialize();
                            chapter.WordCount = urls.Count();
                        },
                        None: () => failedChapter.Add(chapter));
        });
        
        failedChapter.IfAction(!failedChapter.IsEmpty, _ => $"Failed: {failedChapter.Count}".WriteLine());
        book.Chapters = chapters.Except(failedChapter).ToList();

        return book;
    }
    
   



}