using System.Text.RegularExpressions;
using ProfanityService.Data.Interface;
using ProfanityService.Service.Dto;
using ProfanityService.Service.Interface;

namespace ProfanityService.Service;

public sealed class ProfanityService(IProfanityRepository repository) : IProfanityService
{
    public async Task<FilterResponseDto> FilterAsync(string text, CancellationToken cancellationToken = default)
    {
        var words = await repository.GetAllWordsAsync(cancellationToken);
        var filtered = text;
        var found = false;

        foreach (var word in words.Where(w => !string.IsNullOrWhiteSpace(w)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var pattern = $@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(word)}(?![\p{{L}}\p{{N}}])";
            filtered = Regex.Replace(
                filtered,
                pattern,
                match =>
                {
                    found = true;
                    return new string('*', match.Length);
                },
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                TimeSpan.FromMilliseconds(250));
        }

        return new FilterResponseDto
        {
            FilteredText = filtered,
            ProfanityFound = found,
            ServiceAvailable = true
        };
    }
}
