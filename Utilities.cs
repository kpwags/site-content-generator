using System;
using System.Collections.Generic;
using System.Linq;

namespace SiteContentGenerator;

public static class Utilities
{
    public static string GetString(string prompt)
    {
        Console.Write($"{prompt}: ");

        var input = Console.ReadLine();

        return input ?? "";
    }
    
    public static int GetInteger(string prompt)
    {
        Console.Write($"{prompt}: ");

        var numberInput = Console.ReadLine();

        if (!int.TryParse(numberInput, out int number))
        {
            throw new InvalidCastException("Improper numeric input");
        }

        return number;
    }
    
    public static DateTime GetDateTime(string prompt)
    {
        Console.Write($"{prompt}: ");

        var dateInput = Console.ReadLine();

        if (!DateTime.TryParse(dateInput, out DateTime date))
        {
            throw new InvalidCastException("Improper date input");
        }

        return date;
    }

    public static List<string> GetTagInput(string prompt)
    {
        Console.Write($"{prompt}: ");

        var tagString = Console.ReadLine();

        var tags = (tagString ?? "")
            .Split(",")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .ToList();

        return tags;
    }
}