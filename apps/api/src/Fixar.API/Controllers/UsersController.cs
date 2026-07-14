using Asp.Versioning;
using Fixar.Application.Common.Models;
using Fixar.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fixar.API.Controllers;

[ApiController, ApiVersion("1.0"), Authorize(Policy = AuthorizationPolicies.CanManageUsers)]
[Route("api/v{version:apiVersion}/users")]
public sealed class UsersController(UserManager<ApplicationUser> users, RoleManager<ApplicationRole> roles) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = new List<object>();
        foreach (var user in await users.Users.AsNoTracking().OrderBy(x => x.Email).ToListAsync(ct))
            result.Add(new { user.Id, user.Email, user.FirstName, user.LastName, user.IsActive, Roles = await users.GetRolesAsync(user) });
        return Ok(ApiResponse<object>.SuccessResponse(result));
    }

    [HttpGet("roles")]
    public async Task<IActionResult> Roles(CancellationToken ct) => Ok(ApiResponse<object>.SuccessResponse(
        await roles.Roles.AsNoTracking().OrderBy(x => x.Name).Select(x => x.Name).ToListAsync(ct)));

    [HttpPut("{id:guid}/access")]
    public async Task<IActionResult> UpdateAccess(Guid id, UpdateUserAccessRequest request)
    {
        var user = await users.FindByIdAsync(id.ToString());
        if (user is null) return NotFound(ApiResponse<object>.Fail("Kullanıcı bulunamadı.", "USER_NOT_FOUND"));
        var validRoles = await roles.Roles.Where(x => x.Name != null).Select(x => x.Name!).ToListAsync();
        if (request.Roles.Except(validRoles, StringComparer.OrdinalIgnoreCase).Any())
            return BadRequest(ApiResponse<object>.Fail("Geçersiz rol seçimi.", "INVALID_ROLE"));
        var current = await users.GetRolesAsync(user);
        var remove = await users.RemoveFromRolesAsync(user, current);
        if (!remove.Succeeded) return BadRequest(ApiResponse<object>.Fail("Roller güncellenemedi.", "ROLE_UPDATE_FAILED"));
        var add = await users.AddToRolesAsync(user, request.Roles.Distinct(StringComparer.OrdinalIgnoreCase));
        if (!add.Succeeded) return BadRequest(ApiResponse<object>.Fail("Roller güncellenemedi.", "ROLE_UPDATE_FAILED"));
        user.IsActive = request.IsActive;
        await users.UpdateAsync(user);
        return Ok(ApiResponse<object>.SuccessResponse(new { user.Id, user.IsActive, Roles = await users.GetRolesAsync(user) }, "Kullanıcı erişimi güncellendi."));
    }
}

public sealed record UpdateUserAccessRequest(bool IsActive, IReadOnlyList<string> Roles);
