using System.ComponentModel.DataAnnotations;

namespace server.Models;

public class Contract
{
    public int Id { get; set; }

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

    // 剩余金额（计算属性）
    public decimal RemainingAmount => TotalAmount - PaidAmount;

    // 是否已完成支�?
    public bool IsFullyPaid => PaidAmount >= TotalAmount;

    // 创建时间
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // 更新时间
    public DateTime? UpdatedAt { get; set; }

    // 支付记录
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
