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
        
        return Aff(() => CreateMimeTypeFileAsync(bookDir).ToUnit().ToValue())
            .Bind(_ => SaveCoverAsync(book.Cover, bookDir))
            .Map(_ => CopyContainerXml(bookDir))
            .Map(_ => CopyStyleSheets(bookDir))
            .Map(_ => CopyFonts(bookDir))
            .Map(_ => CopyTitlePage(bookDir))
            .Bind(_ => CreateContentOpfAsync(book, bookDir))
            .Bind(_ => CreateTocNcxAsync(book, bookDir))
            .Bind(_ => CreateTableOfContentHtmlAsync(book, bookDir))
            .Bind(_ => CreateTitlePageAsync(book, bookDir))
            .Bind(_ => CreateChapterPagesAsync(book, bookDir))
            .Map(_ => ZipToEpubAsync(bookDir, book.Title))
            .Map(_ => RemoveDir(bookDir));
    }

    private static Aff<Unit> SaveCoverAsync(string? coverUrl, string bookDir)
    {
        if (bookDir.IsBlank())
            return new Aff<Unit>();
            
        return Aff(async () => await use(File.Create($"{bookDir}/cover.jpeg"), 
            async file =>
            {
                await (await new HttpClient().GetStreamAsync(coverUrl)).CopyToAsync(file);
                return unit;
            }));
    }

    private static async Task CreateMimeTypeFileAsync(string bookDir)
        => await File.WriteAllTextAsync($"{bookDir}/mimetype", "application/epub+zip");

    private static Unit CopyContainerXml(string bookDir)
    {
        File.Copy($"{AppSetting.TemplateDir}/container.xml", $"{bookDir}/META-INF/container.xml", true);
        return default;
    }

    private static Unit CopyStyleSheets(string bookDir)
    {
        File.Copy($"{AppSetting.TemplateDir}/stylesheet.css", $"{bookDir}/stylesheet.css", true);
        File.Copy($"{AppSetting.TemplateDir}/page_styles.css", $"{bookDir}/page_styles.css", true);
        return default;
    }

    private static Unit CopyFonts(string bookDir)
    {
        File.Copy($"{AppSetting.TemplateDir}/fonts/Bookerly.ttf", $"{bookDir}/fonts/Bookerly.ttf", true);
        return default;
    }

    private static Aff<Unit> CreateContentOpfAsync(Book book, string bookDir)
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
            .MapAsync(contentOpf => File.WriteAllTextAsync($"{bookDir}/content.opf", contentOpf).ToUnit().ToValue());

    private static Aff<Unit> CreateTocNcxAsync(Book book, string bookDir)
    {
        var startPlayOrder = 3;
        
        return Aff(() => File.ReadAllTextAsync($"{AppSetting.TemplateDir}/toc.ncx").ToValue())
            .Map(tocNcx => tocNcx
                .Replace("{{Nav}}", string.Join("\n\t\t", book.Chapters
                    .Select(c => 
                        $"<navPoint id=\"page-{c.Index}\" playOrder=\"{startPlayOrder++}\" class=\"chapter\"><navLabel><text>{c.Title}</text></navLabel><content src=\"OEBPS/page-{c.Index}.html\"/></navPoint>")))
                .Replace("{{Title}}", book.Title)
                .Replace("{{Guid}}", Guid.NewGuid().ToString()))
            .MapAsync(tocNcx => File.WriteAllTextAsync($"{bookDir}/toc.ncx", tocNcx).ToUnit().ToValue());
    }

    private static Aff<Unit> CreateTableOfContentHtmlAsync(Book book, string bookDir)
        => Aff(() => File.ReadAllTextAsync($"{AppSetting.TemplateDir}/table-of-contents.html").ToValue())
            .Map(tocHtml => tocHtml.Replace("{{TOC}}", string.Join("\n\t\t\t", book.Chapters
                .Select(c => 
                    $"<li><a href=\"page-{c.Index}.html\"><span>{c.Title}</span></a></li>"))))
            .MapAsync(tocHtml => File.WriteAllTextAsync($"{bookDir}/OEBPS/table-of-contents.html", tocHtml).ToUnit().ToValue());

    private static Unit CopyTitlePage(string bookDir)
    {
        File.Copy($"{AppSetting.TemplateDir}/titlepage.xhtml", $"{bookDir}/titlepage.xhtml", true);
        return default;
    }

    private static Aff<Unit> CreateTitlePageAsync(Book book, string bookDir)
        => Aff(() => File.ReadAllTextAsync($"{AppSetting.TemplateDir}/title-page.html").ToValue())
            .Map(titlePage => titlePage
                .Replace("{{Title}}", book.Title)
                .Replace("{{AuthorName}}", book.AuthorName)
                .Replace("{{Description}}", book.Description))
            .MapAsync(titlePage => File.WriteAllTextAsync($"{bookDir}/OEBPS/title-page.html", titlePage).ToUnit().ToValue());

    private static Aff<Unit> CreateChapterPagesAsync(Book book, string bookDir)
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
            .MapAsync(tasks => Task.WhenAll(tasks).ToUnit().ToValue());

    private static Unit ZipToEpubAsync(string bookDir, string bookName)
    {
        var epubPath = $"{AppSetting.BookDir}/{bookName}.epub";
        if (File.Exists(epubPath))
            File.Delete(epubPath);
        
        ZipFile.CreateFromDirectory(bookDir, epubPath, CompressionLevel.NoCompression, false);
        return default;
    }
    
    private static Unit RemoveDir(string bookDir)
    {
        Directory.Delete(bookDir, true);
        return default;
    }
}