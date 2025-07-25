using SiteContentGenerator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SiteContentGenerator.Configuration;

namespace SiteContentGenerator.Services;

public class ReadingLogMarkdownGenerator(CategoryConfiguration categoryConfiguration)
{
    private readonly CategoryConfiguration _categoryConfiguration = categoryConfiguration;
    private readonly StringBuilder _markdownBuilder = new StringBuilder();

    public string GetMarkdownString(List<Article> articles, int logNumber, string youtubeId = "", string artist = "" , string songTitle = "")
    {
        var utcDateTime = string.Format("{0:yyyy-MM-ddTHH:mm:ss.FFFZ}", DateTime.UtcNow);

        _markdownBuilder.AppendLine("---");
        _markdownBuilder.AppendLine($"title: 'Reading Log - {DateTime.Now.ToString("MMMM d, yyyy")} (#{logNumber})'");
        _markdownBuilder.AppendLine($"date: '{utcDateTime}'");
        _markdownBuilder.AppendLine($"permalink: /reading-log/{logNumber}/index.html");
        _markdownBuilder.AppendLine("tags:");
        _markdownBuilder.AppendLine("  - Reading Log");
        _markdownBuilder.AppendLine("---");

        _markdownBuilder.AppendLine("");
        _markdownBuilder.AppendLine("Introduction Text");
        _markdownBuilder.AppendLine("<!-- excerpt -->");
        _markdownBuilder.AppendLine("");
        _markdownBuilder.AppendLine("---");
        _markdownBuilder.AppendLine("");

        foreach (var category in _categoryConfiguration.Categories.OrderBy(c => c.SortOrder))
        {
            if (category.Name == "Podcasts")
            {
                var podcasts = articles.Where(a => a.Category == "Podcasts").ToList();

                if (podcasts.Count > 0)
                {
                    _markdownBuilder.AppendLine($"## {category.Name}");
                    _markdownBuilder.AppendLine("");

                    foreach (var article in podcasts)
                    {
                        _markdownBuilder.AppendLine($"- [{article.Author}: {article.Title}]({article.Url})");
                    }

                    _markdownBuilder.AppendLine("");
                    _markdownBuilder.AppendLine("---");
                    _markdownBuilder.AppendLine("");
                }
            }
            else
            {
                AddSection(articles, category);
            }
        }

        _markdownBuilder.AppendLine("## A Song to Leave You With");
        _markdownBuilder.AppendLine("");

        if (string.IsNullOrWhiteSpace(youtubeId) || string.IsNullOrWhiteSpace(artist) || string.IsNullOrWhiteSpace(songTitle))
        {
            _markdownBuilder.AppendLine("### Artist - Song");
            _markdownBuilder.AppendLine("");
            _markdownBuilder.AppendLine($"{{% youTubeEmbed \"\" \"\" %}}");
            _markdownBuilder.AppendLine("");
        }
        else
        {
            _markdownBuilder.AppendLine($"### {artist} - {songTitle}");
            _markdownBuilder.AppendLine("");
            _markdownBuilder.AppendLine($"{{% youTubeEmbed \"{youtubeId}\" \"{artist} - {songTitle}\" %}}");
            _markdownBuilder.AppendLine("");
        }

        return _markdownBuilder.ToString();
    }

    private void AddSection(List<Article> articles, Category category)
    {
        var categoryArticles = articles.Where(a => a.Category == category.Name).ToList();

        if (categoryArticles.Count > 0)
        {
            _markdownBuilder.AppendLine($"## {category.Name}");
            _markdownBuilder.AppendLine("");

            AddLinks(categoryArticles);

            _markdownBuilder.AppendLine("");
            _markdownBuilder.AppendLine("---");
            _markdownBuilder.AppendLine("");
        }
    }

    private void AddLinks(List<Article> articles)
    {
        foreach (var article in articles)
        {
            _markdownBuilder.AppendLine($"- [{article.Title}]({article.Url}) - *{article.Author}*");
        }
    }
}