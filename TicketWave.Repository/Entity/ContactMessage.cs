using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketWave.Repository.Entity;

[Table("ContactMessage")]
public partial class ContactMessage
{
    [Key]
    public Guid MessageId { get; set; }

    public Guid? MemberId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    [Required]
    [StringLength(200)]
    public string Email { get; set; }

    [Required]
    [StringLength(200)]
    public string Subject { get; set; }

    /// <summary>
    /// 問題類別：訂單問題 / 帳號問題 / 票券問題 / 其他
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Category { get; set; }

    [Required]
    [StringLength(2000)]
    public string Message { get; set; }

    /// <summary>
    /// 0: 未處理  1: 處理中  2: 已回覆
    /// </summary>
    public int Status { get; set; } = 0;

    [StringLength(2000)]
    public string AdminReply { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreateDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime UpdateDate { get; set; }

    [ForeignKey("MemberId")]
    public virtual Member Member { get; set; }
}
