using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace server.Models;

public enum PaymentStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

public class Payment
{
    public int Id { get; set; }

    // 关联的合同ID
    [Required]
    public int ContractId { get; set; }

    // 支付金额
    [Required]
    public decimal Amount { get; set; }

    // 支付日期
    [Required]
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    // 支付说明
    [StringLength(500)]
    public string? Note { get; set; }

    // 审批状态
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    // 提交者用户ID
    public int? CreatedBy { get; set; }

    // 创建时间
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public Contract Contract { get; set; } = null!;
}
