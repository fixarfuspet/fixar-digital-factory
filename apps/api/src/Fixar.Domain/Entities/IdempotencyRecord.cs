using Fixar.Domain.Common;

namespace Fixar.Domain.Entities;

public class IdempotencyRecord : BaseEntity
{
    public string IdempotencyKey { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public int? ResponseStatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public string? ResourceType { get; set; }
    public Guid? ResourceId { get; set; }
    public string Status { get; set; } = "Processing";
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? FailureMessage { get; set; }
}
