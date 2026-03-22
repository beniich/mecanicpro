using System.Threading;
using System.Threading.Tasks;
using MecaPro.Application.Common;
using MediatR;

namespace MecaPro.Application.Modules.Identity;

// DTOs
public record UserProfileDto(string Id, string Name, string Email, string Role, string? Avatar, string? GarageId);

// Queries
public record GetUserProfileQuery(string UserId) : IRequest<Result<UserProfileDto>>;

// Handlers
public class IdentityHandlers(ICurrentUserService currentU) : 
    IRequestHandler<GetUserProfileQuery, Result<UserProfileDto>>
{
    public Task<Result<UserProfileDto>> Handle(GetUserProfileQuery query, CancellationToken ct)
    {
        return Task.FromResult(Result<UserProfileDto>.Success(new UserProfileDto(query.UserId, "User", "user@email.com", "Mechanic", null, null)));
    }
}
