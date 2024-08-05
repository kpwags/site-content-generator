namespace SiteContentGenerator.Models;

public class Article
{
    public string Title { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string Category { get; set; } = "Everything Else";
}