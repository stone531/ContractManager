using System.ComponentModel.DataAnnotations;

namespace server.Models;

public enum NotificationType
{
    ContractApproval = 0,     // 合同审批通知
    AmountApproval = 1,       // 金额审批通知
    ContractApproved = 2,     // 合同审批通过
    ContractRejected = 3,     // 合同审批驳回
    AmountApproved = 4,       // 金额审批通过
    AmountRejected = 5        // 金额审批驳回
}

public class Notification
{
    public int Id { get; set; }

    public NotificationType Type { get; set; }

    [Required]
    [StringLength(500)]
    public string Message { get; set; } = string.Empty;

    // 关联合同ID
    public int? ContractId { get; set; }

    // 发送者用户ID
    public int FromUserId { get; set; }

    // 接收者用户ID
    public int ToUserId { get; set; }

    // 是否已读
    public bool IsRead { get; set; } = false;

    // 驳回理由
    [StringLength(500)]
    public string? RejectReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
