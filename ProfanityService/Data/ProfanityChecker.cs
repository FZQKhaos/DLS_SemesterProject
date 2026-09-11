using System.Text.RegularExpressions;

namespace ProfanityService.Data;

/// <summary>
/// The actual word-matching logic, kept separate from the controller and
/// the repository so it can be unit tested without a database. Matches
/// whole words only (via \b word boundaries) so a banned word like "ass"
/// doesn't also flag harmless text like "class" or "assist".
/// </summary>
public static class ProfanityChecker
{
    public static List<string> FindMatches(string text, IEnumerable<string> bannedWords) =>
        bannedWords
            .Where(word => Regex.IsMatch(text, $@"\b{Regex.Escape(word)}\b", RegexOptions.IgnoreCase))
            .ToList();
}
