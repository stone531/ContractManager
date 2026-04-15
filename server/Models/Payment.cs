using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace server.Models;

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

    // 创建时间
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    // 关联的合�?
    public Contract Contract { get; set; } = null!;
}
