using System.IO.Compression;
using WCrawler.Extensions;
using LanguageExt;
using static LanguageExt.Prelude;

namespace WCrawler.Helpers;

public static class EpubHelper
{
    public static Aff<Unit> CreateEpubAsync(Book book)
    {
        // make new dir
        var bookDir = $"./{book.Title}";
        CommonHelper.CreateDir(bookDir);
        CommonHelper.CreateDir($"{bookDir}/fonts");
        CommonHelper.CreateDir($"{bookDir}/META-INF");
        CommonHelper.CreateDir($"{bookDir}/OEBPS");

        return Aff(async () =>
        {
            await Task.WhenAll(
                CreateMimeTypeFileAsync(bookDir),
                SaveCoverAsync(book.Cover, bookDir),
                new Task(() => CopyContainerXml(bookDir)),
                new Task(() => CopyFonts(bookDir)),
                new Task(() => CopyStyleSheets(bookDir)),
                new Task(() => CopyTitlePage(bookDir)),
                CreateContentOpfAsync(book, bookDir),
                CreateTocNcxAsync(book, bookDir),
                CreateTableOfContentHtmlAsync(book, bookDir),
                CreateTitlePageAsync(book, bookDir),
                CreateChapterPagesAsync(book, bookDir));

            ZipToEpubAsync(bookDir, book.Title);
            RemoveDir(bookDir);

            return Unit.Default;
        });
    }

    private static Task SaveCoverAsync(string? coverUrl, string bookDir)
        => bookDir.IsBlank()
            ? Task.CompletedTask
            : use(File.Create($"{bookDir}/cover.jpeg"), async file => await (await new HttpClient().GetStreamAsync(coverUrl)).CopyToAsync(file));

    private static async Task CreateMimeTypeFileAsync(string bookDir)
        => await File.WriteAllTextAsync($"{bookDir}/mimetype", "application/epub+zip");

    private static void CopyContainerXml(string bookDir)
        => File.Copy($"{AppSetting.TemplateDir}/container.xml", $"{bookDir}/META-INF/container.xml", true);

    private static void CopyStyleSheets(string bookDir)
    {
        File.Copy($"{AppSetting.TemplateDir}/stylesheet.css", $"{bookDir}/stylesheet.css", true);
        File.Copy($"{AppSetting.TemplateDir}/page_styles.css", $"{bookDir}/page_styles.css", true);
    }

    private static void CopyFonts(string bookDir)
    {
        File.Copy($"{AppSetting.TemplateDir}/fonts/Bookerly.ttf", $"{bookDir}/fonts/Bookerly.ttf", true);
    }

    private static Task CreateContentOpfAsync(Book book, string bookDir)
        => Aff(() => File.ReadAllTextAsync($"{AppSetting.TemplateDir}/content.opf").ToValue())
            .Map(contentOpf => contentOpf
                .Replace("{{Title}}", book.Title)
                .Replace("{{Author}}", book.AuthorName)
                .Replace("{{Guid}}", Guid.NewGuid().ToString())
                .Replace("{{Date}}", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fff000+00:00")) // format: 2023-09-19T02:57:02.620000+00:00
                .Replace("{{Lang}}", "vi")
                .Replace("{{Item}}", string.Join("\n\t", book.Chapters // <item id="page-0" href="OEBPS/page-0.html" media-type="application/xhtml+xml"/>
                    .Select(c => 
                        $"<item id=\"page-{c.Index}\" href=\"OEBPS/page-{c.Index}.html\" media-type=\"application/xhtml+xml\"/>")))
                .Replace("{{ItemRef}}", string.Join("\n\t", book.Chapters //<itemref idref="page-0"/>
                    .Select(c => 
                        $"<itemref idref=\"page-{c.Index}\"/>"))))
            .MapAsync(contentOpf => File.WriteAllTextAsync($"{bookDir}/content.opf", contentOpf).ToUnit().ToValue())
            .RunUnit()
            .AsTask();

    private static Task CreateTocNcxAsync(Book book, string bookDir)
    {
        var startPlayOrder = 3;
        
        return Aff(() => File.ReadAllTextAsync($"{AppSetting.TemplateDir}/toc.ncx").ToValue())
            .Map(tocNcx => tocNcx
                .Replace("{{Nav}}", string.Join("\n\t\t", book.Chapters
                    .Select(c => 
                        $"<navPoint id=\"page-{c.Index}\" playOrder=\"{startPlayOrder++}\" class=\"chapter\"><navLabel><text>{c.Title}</text></navLabel><content src=\"OEBPS/page-{c.Index}.html\"/></navPoint>")))
                .Replace("{{Title}}", book.Title)
                .Replace("{{Guid}}", Guid.NewGuid().ToString()))
            .MapAsync(tocNcx => File.WriteAllTextAsync($"{bookDir}/toc.ncx", tocNcx).ToUnit().ToValue())
            .RunUnit()
            .AsTask();
    }

    private static Task CreateTableOfContentHtmlAsync(Book book, string bookDir)
        => Aff(() => File.ReadAllTextAsync($"{AppSetting.TemplateDir}/table-of-contents.html").ToValue())
            .Map(tocHtml => tocHtml.Replace("{{TOC}}", string.Join("\n\t\t\t", book.Chapters
                .Select(c => 
                    $"<li><a href=\"page-{c.Index}.html\"><span>{c.Title}</span></a></li>"))))
            .MapAsync(tocHtml => File.WriteAllTextAsync($"{bookDir}/OEBPS/table-of-contents.html", tocHtml).ToUnit().ToValue())
            .RunUnit()
            .AsTask();

    private static void CopyTitlePage(string bookDir)
        => File.Copy($"{AppSetting.TemplateDir}/titlepage.xhtml", $"{bookDir}/titlepage.xhtml", true);


    private static Task CreateTitlePageAsync(Book book, string bookDir)
        => Aff(() => File.ReadAllTextAsync($"{AppSetting.TemplateDir}/title-page.html").ToValue())
            .Map(titlePage => titlePage
                .Replace("{{Title}}", book.Title)
                .Replace("{{AuthorName}}", book.AuthorName)
                .Replace("{{Description}}", book.Description))
            .MapAsync(titlePage => File.WriteAllTextAsync($"{bookDir}/OEBPS/title-page.html", titlePage).ToUnit().ToValue())
            .RunUnit()
            .AsTask();

    private static Task CreateChapterPagesAsync(Book book, string bookDir)
        => Aff(() => File.ReadAllTextAsync($"{AppSetting.TemplateDir}/page.html").ToValue())
            .Map(template => book.Chapters
                .Select(chapter =>
                {
                    var chapterPage = template
                        .Replace("{{ChapterTitle}}", chapter.Title)
                        .Replace("{{ChapterContent}}",
                            chapter.Content?.Replace("\n\n", "\n").Replace("\n", "<br><br>"));
                    return File.WriteAllTextAsync($"{bookDir}/OEBPS/page-{chapter.Index}.html", chapterPage);
                }))
            .MapAsync(tasks => Task.WhenAll(tasks).ToUnit().ToValue())
            .RunUnit()
            .AsTask();

    private static void ZipToEpubAsync(string bookDir, string bookName)
    {
        var epubPath = $"{AppSetting.BookDir}/{bookName}.epub";
        if (File.Exists(epubPath))
            File.Delete(epubPath);
        
        ZipFile.CreateFromDirectory(bookDir, epubPath, CompressionLevel.NoCompression, false);
    }
    
    private static void RemoveDir(string bookDir) => Directory.Delete(bookDir, true);
}