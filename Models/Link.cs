using System;

namespace SiteContentGenerator.Models;

public class Link
{
    public int LinkId { get; set; }
    
    public int LinkTypeId { get; set; }
    
    public int LinkCategoryId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;
    
    public string Author { get; set; } = string.Empty;
    
    public DateTime LinkDate { get; set; }
    
    public int ReadingLogIssueNumber { get; set; }

    public LinkType Type { get; set; } = new();
    
    public LinkCategory Category { get; set; } = new();
}
