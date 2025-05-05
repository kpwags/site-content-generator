using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SiteContentGenerator.Configuration;
using SiteContentGenerator.Exceptions;
using SiteContentGenerator.Models;
using SiteContentGenerator.Services;
using Link = SiteContentGenerator.Models.Link;

namespace SiteContentGenerator;

internal class Program
{
    private static DirectoryConfiguration? _configuration;
    private static ApiConfiguration? _apiConfiguration;
    private static CategoryConfiguration? _categoryConfiguration;

    static async Task Main()
    {
        var appVersion = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version;

        IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();

        _configuration = config.GetRequiredSection("Directory").Get<DirectoryConfiguration>();
        _apiConfiguration = config.GetRequiredSection("Api").Get<ApiConfiguration>();
        _categoryConfiguration = config.GetRequiredSection("Category").Get<CategoryConfiguration>();

        if (_configuration is null || _apiConfiguration is null || _categoryConfiguration is null)
        {
            Console.WriteLine("Unable to read settings");
            return;
        }

        WriteWithColor($"Site Generator (v{appVersion?.Major}.{appVersion?.Minor}.{appVersion?.Build})", ConsoleColor.Cyan);
        Console.WriteLine("");
        Console.WriteLine("Please Specify Content Template");
        Console.WriteLine("1. Blog Post");
        Console.WriteLine("2. Note");
        Console.WriteLine("3. Book Note");
        Console.WriteLine("4. Reading Log");
        Console.WriteLine("5. Week Notes");
        Console.WriteLine("6. Monthly Check-In");
        Console.WriteLine("");

        try
        {
            var choice = Utilities.GetInteger("Selection");

            Console.WriteLine("");

            switch ((ContentTemplates)choice)
            {
                case ContentTemplates.BlogPost:
                    await BuildBlogTemplate();
                    WriteConsoleSuccess("Blog Post Template Created!");
                    break;
                case ContentTemplates.Note:
                    await BuildNoteTemplate();
                    WriteConsoleSuccess("Note Template Created!");
                    break;
                case ContentTemplates.BookNote:
                    await BuildBookNoteTemplate();
                    WriteConsoleSuccess("Book Note Template Created!");
                    break;
                case ContentTemplates.ReadingLog:
                    await BuildReadingLog();
                    WriteConsoleSuccess("Reading Log Created");
                    break;
                case ContentTemplates.WeekNotes:
                    await BuildWeekNotesTemplate();
                    WriteConsoleSuccess("Week Notes Post Created");
                    break;
                case ContentTemplates.MonthlyCheckIn:
                    await BuildMonthlyCheckInTemplate();
                    WriteConsoleSuccess("Monthly Check-In Post Created");
                    break;
                default:
                    throw new Exception("Invalid selection");
            }
        }
        catch (Exception e)
        {
            WriteConsoleError(e.Message);
        }
    }

    static async Task BuildBlogTemplate()
    {
        var utcDateTime = string.Format("{0:yyyy-MM-ddTHH:mm:ss.FFFZ}", DateTime.UtcNow);

        WriteWithColor("Building Blog Template", ConsoleColor.Magenta);
        Console.WriteLine("");

        var title = Utilities.GetString("Enter Title");

        if (string.IsNullOrWhiteSpace(title))
        {
            WriteConsoleError("Title Not Specified");
            return;
        }

        var description = Utilities.GetString("Enter Description");

        var urlSlug = BuildUrlSlug(title);

        var slug = Utilities.GetString($"Enter Permalink ({urlSlug})");

        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = urlSlug;
        }

        var tags = Utilities.GetTagInput("Enter Tags (Separated by Commas)");

        var rssOnlyResponse = Utilities.GetString("Is the Post RSS only? (yes/no) (no)");

        var isRssOnly = rssOnlyResponse.ToLower(CultureInfo.InvariantCulture) == "yes";

        Console.WriteLine("");

        var stringBuilder = new StringBuilder();

        stringBuilder.AppendLine("---");
        stringBuilder.AppendLine($"title: \"{title}\"");
        stringBuilder.AppendLine($"date: '{utcDateTime}'");
        stringBuilder.AppendLine($"permalink: /posts/{DateTime.UtcNow.ToString("yyyy")}/{DateTime.UtcNow.ToString("MM")}/{DateTime.UtcNow.ToString("dd")}/{slug}/index.html");

        if (!string.IsNullOrWhiteSpace(description))
        {
            stringBuilder.AppendLine($"description: \"{description}\"");
        }

        if (isRssOnly)
        {
            stringBuilder.AppendLine("rss_only: true");
        }

        if (tags.Count > 0)
        {
            stringBuilder.AppendLine("tags:");

            foreach (var tag in tags)
            {
                stringBuilder.AppendLine($"  - {tag}");
            }
        }

        stringBuilder.AppendLine("---");

        var fileName = $"{DateTime.UtcNow.ToString("yyyy")}-{DateTime.UtcNow.ToString("MM")}-{DateTime.UtcNow.ToString("dd")}-{slug}.md";

        var outputDirectory = Path.Join(_configuration?.RootContentDirectory, _configuration?.Blog, DateTime.UtcNow.ToString("yyyy"));

        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var outputFile = Path.Join(outputDirectory, fileName);

        await WriteToFile(stringBuilder.ToString(), outputFile);
    }

    static async Task BuildNoteTemplate()
    {
        var utcDateTime = string.Format("{0:yyyy-MM-ddTHH:mm:ss.FFFZ}", DateTime.UtcNow);

        WriteWithColor("Building Note Template", ConsoleColor.Yellow);
        Console.WriteLine("");

        var title = Utilities.GetString("Enter Title");

        if (string.IsNullOrWhiteSpace(title))
        {
            WriteConsoleError("Title Not Specified");
            return;
        }

        var link = Utilities.GetString("Enter Link");

        if (string.IsNullOrWhiteSpace(link))
        {
            WriteConsoleError("Link Not Specified");
            return;
        }

        var author = Utilities.GetString("Enter Author");

        if (string.IsNullOrWhiteSpace(author))
        {
            WriteConsoleError("Author Not Specified");
            return;
        }

        var urlSlug = BuildUrlSlug(title);

        var slug = Utilities.GetString($"Enter Permalink ({urlSlug})");

        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = urlSlug;
        }

        var tags = Utilities.GetTagInput("Enter Tags (Separated by Commas)");

        Console.WriteLine("");

        var stringBuilder = new StringBuilder();

        stringBuilder.AppendLine("---");
        stringBuilder.AppendLine($"title: \"{title}\"");
        stringBuilder.AppendLine($"date: '{utcDateTime}'");
        stringBuilder.AppendLine($"link: {link}");
        stringBuilder.AppendLine($"author: {author}");
        stringBuilder.AppendLine($"permalink: /notes/{slug}/index.html");

        if (tags.Count > 0)
        {
            stringBuilder.AppendLine("tags:");

            foreach (var tag in tags)
            {
                stringBuilder.AppendLine($"  - {tag}");
            }
        }

        stringBuilder.AppendLine("---");

        var fileName = $"{slug}.md";

        var outputDirectory = Path.Join(_configuration?.RootContentDirectory, _configuration?.Notes, DateTime.UtcNow.ToString("yyyy"));

        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var outputFile = Path.Join(outputDirectory, fileName);

        await WriteToFile(stringBuilder.ToString(), outputFile);
    }

    static async Task BuildBookNoteTemplate()
    {
        WriteWithColor("Building Book Note Template", ConsoleColor.DarkCyan);
        Console.WriteLine("");

        var title = Utilities.GetString("Enter Title");

        if (string.IsNullOrWhiteSpace(title))
        {
            WriteConsoleError("Title Not Specified");
            return;
        }

        var subtitle = Utilities.GetString("Enter Subtitle");

        var author = Utilities.GetString("Enter Author");

        if (string.IsNullOrWhiteSpace(author))
        {
            WriteConsoleError("Author Not Specified");
            return;
        }

        var imageUrl = Utilities.GetString("Enter Image URL");

        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            WriteConsoleError("Image URL Not Specified");
            return;
        }

        var format = Utilities.GetString("Enter Format");

        if (string.IsNullOrWhiteSpace(format))
        {
            WriteConsoleError("Format Not Specified");
            return;
        }

        var dateFinished = Utilities.GetDateTime("Enter Date Read");

        var rating = Utilities.GetInteger("Enter Rating");

        if (rating < 1 || rating > 5)
        {
            WriteConsoleError("Rating Must Be Between 1 and 5");
            return;
        }

        var urlSlug = BuildUrlSlug($"{author} {title}");

        var slug = Utilities.GetString($"Enter Permalink ({urlSlug})");

        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = urlSlug;
        }

        var categories = Utilities.GetTagInput("Enter Categories (Separated by Commas)");

        Console.WriteLine("");

        var stringBuilder = new StringBuilder();

        stringBuilder.AppendLine("---");
        stringBuilder.AppendLine($"title: \"{title}\"");

        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            stringBuilder.AppendLine($"subtitle: \"{subtitle}\"");
            stringBuilder.AppendLine($"fullTitle: \"{title}: {subtitle}\"");
        }
        else
        {
            stringBuilder.AppendLine($"fullTitle: \"{title}\"");
        }

        stringBuilder.AppendLine($"author: \"{author}\"");
        stringBuilder.AppendLine($"format: '{format}'");
        stringBuilder.AppendLine($"coverImage: '{imageUrl}'");
        stringBuilder.AppendLine($"rating: {rating}");
        stringBuilder.AppendLine($"date: '{dateFinished.ToString("yyyy-MM-dd")}'");
        stringBuilder.AppendLine($"permalink: '/books/{slug}/index.html'");

        if (categories.Count > 0)
        {
            stringBuilder.AppendLine("categories:");

            foreach (var category in categories)
            {
                stringBuilder.AppendLine($"  - {category}");
            }
        }

        stringBuilder.AppendLine("purchaseLinks: [");
        stringBuilder.AppendLine("  { title: '', url: '' }");
        stringBuilder.AppendLine("]");

        stringBuilder.AppendLine("---");

        var fileName = $"{slug}.md";

        var outputDirectory = Path.Join(_configuration?.RootContentDirectory, _configuration?.BookNotes);

        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var outputFile = Path.Join(outputDirectory, fileName);

        await WriteToFile(stringBuilder.ToString(), outputFile);
    }

    static async Task BuildReadingLog()
    {
        if (_apiConfiguration is null)
        {
            throw new NullReferenceException("API configuration is not defined");
        }

        if (_categoryConfiguration is null || _categoryConfiguration.Categories.Count == 0)
        {
            throw new NullReferenceException("Category configuration is not defined");
        }

        Console.Write("Please Enter Reading Log Number: ");

        var logNumberString = Console.ReadLine();

        if (!int.TryParse(logNumberString, out int logNumber) || logNumber == 0)
        {
            throw new InvalidInputException("Invalid reading log issue number");
        }

        var handler = new HttpClientHandler();
        handler.ClientCertificateOptions = ClientCertificateOption.Manual;
        handler.ServerCertificateCustomValidationCallback =
            (_, _, _, _) =>
            {
                return true;
            };

        var client = new HttpClient(handler);

        var links =
            await client.GetFromJsonAsync<IReadOnlyCollection<Link>>(
                $"{_apiConfiguration.ApiRootUrl}/link/reading-log/{logNumber}", new JsonSerializerOptions()
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                });

        if (links is null)
        {
            WriteConsoleError("Error retrieving links from media repository");
            return;
        }

        var articles = links
            .Select(l => new Article
            {
                Category = l.Category.Name,
                Title = l.Title,
                Author = l.Author,
                Url = l.Url,
            })
            .ToList();

        if (articles.Count == 0)
        {
            WriteWithColor($"No links found for Issue #{logNumber}", ConsoleColor.Yellow);
            return;
        }

        var markdownGenerator = new ReadingLogMarkdownGenerator(_categoryConfiguration);
        var markdown = markdownGenerator.GetMarkdownString(articles, logNumber);

        var fileName = $"{logNumber}.md";

        var outputDirectory = Path.Join(_configuration?.RootContentDirectory, _configuration?.ReadingLogs);

        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var outputFile = Path.Join(outputDirectory, fileName);

        await WriteToFile(markdown, outputFile);
    }

    static async Task BuildWeekNotesTemplate()
    {
        var utcDateTime = string.Format("{0:yyyy-MM-ddTHH:mm:ss.FFFZ}", DateTime.UtcNow);

        WriteWithColor("Building Week Notes Template", ConsoleColor.Magenta);
        Console.WriteLine("");

        var weekNotesStart = Utilities.GetDateTime("Enter Start Date");
        var weekNotesEnd = weekNotesStart.AddDays(6);

        var title = weekNotesStart.Month == weekNotesEnd.Month ? $"Week Notes for {weekNotesStart:M} - {weekNotesEnd.Day}" : $"Week Notes for {weekNotesStart:M} - {weekNotesEnd:M}";

        var description = $"My week notes for the week of {weekNotesStart:M} through {weekNotesEnd:M}";

        var tags = Utilities.GetTagInput("Enter Tags (Separated by Commas)");

        Console.WriteLine("");

        var stringBuilder = new StringBuilder();

        stringBuilder.AppendLine("---");
        stringBuilder.AppendLine($"title: \"{title}\"");
        stringBuilder.AppendLine($"date: '{utcDateTime}'");
        stringBuilder.AppendLine($"permalink: /posts/{DateTime.UtcNow.ToString("yyyy")}/{DateTime.UtcNow.ToString("MM")}/{DateTime.UtcNow.ToString("dd")}/week-notes/index.html");

        if (!string.IsNullOrWhiteSpace(description))
        {
            stringBuilder.AppendLine($"description: \"{description}\"");
        }

        if (tags.Count > 0)
        {
            stringBuilder.AppendLine("tags:");
            stringBuilder.AppendLine($"  - Week Notes");

            foreach (var tag in tags)
            {
                stringBuilder.AppendLine($"  - {tag}");
            }
        }

        stringBuilder.AppendLine("---");

        var fileName = $"{DateTime.UtcNow.ToString("yyyy")}-{DateTime.UtcNow.ToString("MM")}-{DateTime.UtcNow.ToString("dd")}-week-notes.md";

        var outputDirectory = Path.Join(_configuration?.RootContentDirectory, _configuration?.Blog, DateTime.UtcNow.ToString("yyyy"));

        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var outputFile = Path.Join(outputDirectory, fileName);

        await WriteToFile(stringBuilder.ToString(), outputFile);
    }

    static async Task BuildMonthlyCheckInTemplate()
    {
        var utcDateTime = string.Format("{0:yyyy-MM-ddTHH:mm:ss.FFFZ}", DateTime.UtcNow);

        WriteWithColor("Building Monthly Check-In Template", ConsoleColor.Magenta);
        Console.WriteLine("");

        var monthAndYear = Utilities.GetString("Enter Month & Year");

        var title = $"{monthAndYear} Check-In";

        var description = $"Looking back at my {monthAndYear}.";

        var tags = Utilities.GetTagInput("Enter Tags (Separated by Commas)");

        var urlSlug = BuildUrlSlug(title);

        Console.WriteLine("");

        var stringBuilder = new StringBuilder();

        stringBuilder.AppendLine("---");
        stringBuilder.AppendLine($"title: \"{title}\"");
        stringBuilder.AppendLine($"date: '{utcDateTime}'");
        stringBuilder.AppendLine($"permalink: /posts/{DateTime.UtcNow.ToString("yyyy")}/{DateTime.UtcNow.ToString("MM")}/{DateTime.UtcNow.ToString("dd")}/{urlSlug}/index.html");

        if (!string.IsNullOrWhiteSpace(description))
        {
            stringBuilder.AppendLine($"description: \"{description}\"");
        }

        stringBuilder.AppendLine("tags:");
        stringBuilder.AppendLine($"  - Monthly Check-In");

        if (tags.Count > 0)
        {
            foreach (var tag in tags)
            {
                stringBuilder.AppendLine($"  - {tag}");
            }
        }

        stringBuilder.AppendLine("---");

        stringBuilder.AppendLine("");
        stringBuilder.AppendLine("{% renderTemplate \"webc\" %}");
        stringBuilder.AppendLine("<monthly-roundup runs=\"0\" milesran=\"0\" walks=\"0\" mileswalked=\"0\" lifts=\"0\" volumelifted=\"0\" gaming=\"true\" tv=\"true\" movies=\"true\">");
        stringBuilder.AppendLine("  <ul slot=\"books-read\">");
        stringBuilder.AppendLine("    <li>Finished <a href=\"LINK\">TITLE</a> by AUTHOR</li>");
        stringBuilder.AppendLine("    <li>Started <a href=\"LINK\">TITLE</a> by AUTHOR</li>");
        stringBuilder.AppendLine("  </ul>");
        stringBuilder.AppendLine("");
        stringBuilder.AppendLine("  <ul slot=\"reading-logs\">");
        stringBuilder.AppendLine("    <li><a href=\"https://kpwags.com/reading-log/105/\"> (#)</a></li>");
        stringBuilder.AppendLine("  </ul>");
        stringBuilder.AppendLine("");
        stringBuilder.AppendLine("  <ul slot=\"week-notes\">");
        stringBuilder.AppendLine("    <li><a href=\"https://kpwags.com/posts/2025/01/05/week-notes/\">DATES</a></li>");
        stringBuilder.AppendLine("  </ul>");
        stringBuilder.AppendLine("");
        stringBuilder.AppendLine("  <ul slot=\"blogging\">");
        stringBuilder.AppendLine("    <li><a href=\"https://kpwags.com/posts/LINK/\">TITLE</a></li>");
        stringBuilder.AppendLine("  </ul>");
        stringBuilder.AppendLine("");
        stringBuilder.AppendLine("  <ul slot=\"gaming\">");
        stringBuilder.AppendLine("    <li>Continued <a href=\"LINK\">TITLE</a></li>");
        stringBuilder.AppendLine("  </ul>");
        stringBuilder.AppendLine("");
        stringBuilder.AppendLine("  <ul slot=\"tv\">");
        stringBuilder.AppendLine("    <li>Continued <a href=\"LINK\">TITLE</a></li>");
        stringBuilder.AppendLine("  </ul>");
        stringBuilder.AppendLine("");
        stringBuilder.AppendLine("  <ul slot=\"movies\">");
        stringBuilder.AppendLine("    <li><a href=\"LINK\">TITLE</a></li>");
        stringBuilder.AppendLine("  </ul>");
        stringBuilder.AppendLine("</monthly-roundup>");
        stringBuilder.AppendLine("{% endrenderTemplate %}");
        stringBuilder.AppendLine("");

        var fileName = $"{DateTime.UtcNow.ToString("yyyy")}-{DateTime.UtcNow.ToString("MM")}-{DateTime.UtcNow.ToString("dd")}-{urlSlug}.md";

        var outputDirectory = Path.Join(_configuration?.RootContentDirectory, _configuration?.Blog, DateTime.UtcNow.ToString("yyyy"));

        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var outputFile = Path.Join(outputDirectory, fileName);

        await WriteToFile(stringBuilder.ToString(), outputFile);
    }

    static string BuildUrlSlug(string title)
    {
        var noSpecialChars = new string(title.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) || c == '-').ToArray());

        return noSpecialChars.Replace(" ", "-").Replace("---", "-").ToLower();
    }

    static async Task WriteToFile(string content, string outputFile)
    {
        if (File.Exists(outputFile))
        {
            throw new IOException("File Already Exists");
        }

        await using var sw = new StreamWriter(outputFile, true);

        await sw.WriteAsync(content);
    }

    static void WriteConsoleError(string errorMessage)
    {
        WriteWithColor(errorMessage, ConsoleColor.Red);
    }

    static void WriteConsoleSuccess(string successMessage)
    {
        WriteWithColor(successMessage, ConsoleColor.Green);
    }

    static void WriteWithColor(string message, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}
