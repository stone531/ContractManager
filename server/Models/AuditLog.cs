using System.ComponentModel.DataAnnotations;

namespace server.Models;

public class AuditLog
{
    public int Id { get; set; }

    // 关联合同ID（可选，部分操作可能不关联合同）
    public int? ContractId { get; set; }

    // 操作者用户ID
    public int UserId { get; set; }

    // 操作者用户名
    [StringLength(100)]
    public string UserName { get; set; } = string.Empty;

    // 操作类型
    [Required]
    [StringLength(50)]
    public string Action { get; set; } = string.Empty;

    // 操作描述
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    // 操作时间
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
