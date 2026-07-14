using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Fixar.Application.Common.Interfaces;
using Fixar.Application.Common.Models;
using Fixar.Domain.Entities;
using Fixar.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace Fixar.API.Security;

public sealed class IdempotencyFilter(ApplicationDbContext db, ICurrentUserService currentUser) : IAsyncActionFilter
{
    private const int MaxStoredResponseLength = 32_768;
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var key = context.HttpContext.Request.Headers["Idempotency-Key"].ToString().Trim();
        if (key.Length is < 16 or > 128 || !key.All(c => char.IsLetterOrDigit(c) || c is '-' or '_'))
        {
            context.Result = new BadRequestObjectResult(ApiResponse<object>.Fail(
                "Geçerli bir Idempotency-Key başlığı zorunludur.", "IDEMPOTENCY_KEY_REQUIRED"));
            return;
        }

        var endpoint = $"{context.HttpContext.Request.Method}:{context.HttpContext.Request.Path.Value}";
        var requestJson = JsonSerializer.Serialize(context.ActionArguments
            .Where(x => x.Value is not CancellationToken)
            .OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value));
        var requestHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(requestJson)));
        var userId = currentUser.UserId;

        var existing = await db.IdempotencyRecords.AsNoTracking().SingleOrDefaultAsync(
            x => x.IdempotencyKey == key && x.UserId == userId && x.Endpoint == endpoint,
            context.HttpContext.RequestAborted);
        if (existing is not null)
        {
            if (existing.RequestHash != requestHash)
                context.Result = new ConflictObjectResult(ApiResponse<object>.Fail("Bu anahtar farklı bir istek için kullanılmış.", "IDEMPOTENCY_CONFLICT"));
            else if (existing.Status == "Completed" && existing.ResponseStatusCode.HasValue)
                context.Result = new ContentResult { StatusCode = existing.ResponseStatusCode, ContentType = "application/json", Content = existing.ResponseBody ?? "null" };
            else
                context.Result = new ConflictObjectResult(ApiResponse<object>.Fail("Aynı işlem halen yürütülüyor veya önceki deneme başarısız oldu.", "IDEMPOTENCY_IN_PROGRESS"));
            return;
        }

        var record = new IdempotencyRecord
        {
            IdempotencyKey = key, UserId = userId, UserName = currentUser.UserName ?? "authenticated-user",
            Endpoint = endpoint, HttpMethod = context.HttpContext.Request.Method, RequestHash = requestHash,
            CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddHours(24)
        };
        db.IdempotencyRecords.Add(record);
        try { await db.SaveChangesAsync(context.HttpContext.RequestAborted); }
        catch (DbUpdateException)
        {
            db.Entry(record).State = EntityState.Detached;
            context.Result = new ConflictObjectResult(ApiResponse<object>.Fail("Aynı işlem başka bir istekte yürütülüyor.", "IDEMPOTENCY_IN_PROGRESS"));
            return;
        }

        try
        {
            var executed = await next();
            var status = executed.Result switch
            {
                ObjectResult o => o.StatusCode ?? 200,
                StatusCodeResult s => s.StatusCode,
                _ => context.HttpContext.Response.StatusCode
            };
            object? value = executed.Result is ObjectResult result ? result.Value : null;
            var response = JsonSerializer.Serialize(value, WebJson);
            record.ResponseStatusCode = status;
            record.ResponseBody = response.Length <= MaxStoredResponseLength ? response : null;
            record.Status = status < 500 ? "Completed" : "Failed";
            record.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(context.HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            record.Status = "Failed";
            record.FailureMessage = ex.GetType().Name;
            record.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(CancellationToken.None);
            throw;
        }
    }
}
