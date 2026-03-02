using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TicketWave.Repository.Entity
{
    /// <summary>
    /// 座位資料表
    /// 記錄每場演唱會的座位資訊及銷售狀態
    /// </summary>
    [Table("Seat")]
    [Index("ConcertId", "SeatZone", "SeatRow", "SeatNumber", Name = "IX_Seat_Unique", IsUnique = true)]
    public partial class Seat
    {
        /// <summary>
        /// 座位 ID（主鍵）
        /// </summary>
        [Key]
        public Guid SeatId { get; set; }

        /// <summary>
        /// 演唱會 ID（外鍵）
        /// </summary>
        public Guid ConcertId { get; set; }

        /// <summary>
        /// 座位區域（例如：A區、B區、VIP區、搖滾區）
        /// </summary>
        [Required]
        [StringLength(20)]
        public string SeatZone { get; set; }

        /// <summary>
        /// 座位排數（例如：1、2、3 或 A、B、C）
        /// </summary>
        [Required]
        [StringLength(10)]
        public string SeatRow { get; set; }

        /// <summary>
        /// 座位號碼（例如：1、2、3...）
        /// </summary>
        [Required]
        [StringLength(10)]
        public string SeatNumber { get; set; }

        /// <summary>
        /// 票價
        /// </summary>
        [Column(TypeName = "decimal(10, 2)")]
        public decimal Price { get; set; }

        /// <summary>
        /// 座位狀態（0:可購買, 1:已售出, 2:保留中, 3:已鎖定）
        /// </summary>
        public int SeatStatus { get; set; }

        /// <summary>
        /// 購買此座位的訂單明細 ID（如果已售出）
        /// </summary>
        public Guid? OrderDetailId { get; set; }

        /// <summary>
        /// 建立時間
        /// </summary>
        [Column(TypeName = "datetime")]
        public DateTime CreateDate { get; set; }

        /// <summary>
        /// 更新時間
        /// </summary>
        [Column(TypeName = "datetime")]
        public DateTime UpdateDate { get; set; }

        // Navigation Properties
        [ForeignKey("ConcertId")]
        public virtual Concert Concert { get; set; }
    }
}
