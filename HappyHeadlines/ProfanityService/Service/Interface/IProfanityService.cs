using ProfanityService.Service.Dto;

namespace ProfanityService.Service.Interface;

public interface IProfanityService
{
    Task<FilterResponseDto> FilterAsync(string text, CancellationToken cancellationToken = default);
}
