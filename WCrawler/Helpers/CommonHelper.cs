namespace WCrawler.Helpers;

public static class CommonHelper
{
    public static void CreateDir(string dir)
    {
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }
}