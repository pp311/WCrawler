using Microsoft.VisualBasic.CompilerServices;

namespace WCrawler;

public class Source
{
    public string Address { get; set; }
    public string BaseUrl { get; set; }
    public string ChapterUrl { get; set; }
    public string GetChapterUrl => $"{Address}{ChapterUrl}";
    public GetChapterType GetChapterType { get; set; }
    public int ChapterStartPageIndex { get; set; }
    public BookType BookType { get; set; }
    public string TitleSelector { get; set; }
    public string CoverSelector { get; set; }
    public string AuthorNameSelector { get; set; }
//    public string StatusSelector { get; set; }
    public string DescriptionSelector { get; set; }
    public string ChapterListSelector { get; set; }
    public string ChapterTitleSelector { get; set; }
    public string ChapterUrlSelector { get; set; }
    public string ChapterContentSelector { get; set; }
}

public class Book
{
    public string Title { get; set; } = null!;
    public string? Cover { get; set; }
    public string? AuthorName { get; set; }
//    public string? Status { get; set; }
    public string? Description { get; set; }
    public BookType BookType { get; set; }

    public List<Chapter> Chapters { get; set; } = [];
}

public class Chapter
{
    public string? Title { get; set; }
    public int Index { get; set; }
    public string? Content { get; set; }
    public int WordCount { get; set; }
    public string? Url { get; set; }
}

public class Setting
{
    public string ExportDir { get; set; }
}
