using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace server.Models;

public enum ApprovalStatus
{
    Pending = 0,    // 待审核
    Approved = 1,   // 已批准
    Rejected = 2    // 已拒绝
}

public enum ContractStatus
{
    Initial = 0,       // 初始状态
    InProgress = 1,    // 进行中
    Completed = 2,     // 已完成
    Terminated = 3     // 已终止
}

public class Contract
{
    public int Id { get; set; }

    // 合同编号（如 HT260415001）
    [StringLength(50)]
    public string? ContractNumber { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    // 合同文件�?
    [StringLength(255)]
    public string? FileName { get; set; }

    // 合同文件路径
    [StringLength(500)]
    public string? FilePath { get; set; }

    // 合同总金�?
    [Required]
    public decimal TotalAmount { get; set; }

    // 原始金额（用于对比）
    public decimal OriginalAmount { get; set; }

    // 已支付金�?
    public decimal PaidAmount { get; set; } = 0;

    // 剩余金额（计算属性，不映射到数据库）
    [NotMapped]
    public decimal RemainingAmount => TotalAmount - PaidAmount;

    // 是否已完成支付
    [NotMapped]
    public bool IsFullyPaid => PaidAmount >= TotalAmount;

    // 创建时间
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // 更新时间
    public DateTime? UpdatedAt { get; set; }

    // 审批状态
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;

    // 合同状态
    public ContractStatus ContractStatus { get; set; } = ContractStatus.Initial;

    // 待审核金额
    public decimal SubmittedAmount { get; set; } = 0;

    // 提交者用户ID（谁提交的待审核金额）
    public int? SubmittedBy { get; set; }

    // 审批时间
    public DateTime? ApprovedAt { get; set; }

    // 创建者用户ID
    public int? CreatedBy { get; set; }

    // 合同生效日期
    public DateTime? StartDate { get; set; }

    // 合同到期日期
    public DateTime? EndDate { get; set; }

    // 终止时间
    public DateTime? TerminatedAt { get; set; }

    // 终止操作者用户ID
    public int? TerminatedBy { get; set; }

    // 支付记录
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
