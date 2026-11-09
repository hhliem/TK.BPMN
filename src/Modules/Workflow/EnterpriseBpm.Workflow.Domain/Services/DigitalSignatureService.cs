using System.Security.Cryptography;
using System.Text;
using EnterpriseBpm.Workflow.Domain.Enums;

namespace EnterpriseBpm.Workflow.Domain.Services;

public interface IDigitalSignatureService
{
    string ComputeHash(
        string workflowInstanceId,
        int stepOrder,
        Guid actorUserId,
        ApprovalDecision decision,
        DateTime timestamp,
        string salt);

    bool VerifyHash(
        string workflowInstanceId,
        int stepOrder,
        Guid actorUserId,
        ApprovalDecision decision,
        DateTime timestamp,
        string salt,
        string expectedHash);
}

public class DigitalSignatureService : IDigitalSignatureService
{
    public string ComputeHash(
        string workflowInstanceId,
        int stepOrder,
        Guid actorUserId,
        ApprovalDecision decision,
        DateTime timestamp,
        string salt)
    {
        // ISO 8601 UTC format để đảm bảo tính xác định (deterministic)
        var formattedTime = timestamp.ToUniversalTime().ToString("O");
        var rawPayload = $"{workflowInstanceId}:{stepOrder}:{actorUserId}:{decision}:{formattedTime}:{salt}";
        
        var bytes = Encoding.UTF8.GetBytes(rawPayload);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public bool VerifyHash(
        string workflowInstanceId,
        int stepOrder,
        Guid actorUserId,
        ApprovalDecision decision,
        DateTime timestamp,
        string salt,
        string expectedHash)
    {
        var computed = ComputeHash(workflowInstanceId, stepOrder, actorUserId, decision, timestamp, salt);
        return string.Equals(computed, expectedHash, StringComparison.OrdinalIgnoreCase);
    }
}

