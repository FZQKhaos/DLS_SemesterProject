namespace ProfanityService.Data.Interface;

public interface IProfanityRepository
{
    Task<IReadOnlyList<string>> GetAllWordsAsync(CancellationToken cancellationToken = default);
}
