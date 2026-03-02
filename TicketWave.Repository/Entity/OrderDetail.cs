using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TicketWave.Repository.Entity
{
    /// <summary>
    /// 訂單明細資料表
    /// 記錄訂單中每張票的座位資訊
    /// </summary>
    [Table("OrderDetail")]
    public partial class OrderDetail
    {
        /// <summary>
        /// 訂單明細 ID（主鍵）
        /// </summary>
        [Key]
        public Guid OrderDetailId { get; set; }

        /// <summary>
        /// 訂單 ID（外鍵）
        /// </summary>
        public Guid OrderId { get; set; }

        /// <summary>
        /// 座位區域（例如：A區、B區、搖滾區）
        /// </summary>
        [Required]
        [StringLength(20)]
        public string SeatZone { get; set; }

        /// <summary>
        /// 座位排數（例如：1、2、A、B）
        /// </summary>
        [Required]
        [StringLength(10)]
        public string SeatRow { get; set; }

        /// <summary>
        /// 座位號碼（例如：1、2、3）
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
        /// 票券狀態（0:未使用, 1:已使用, 2:已退票）
        /// </summary>
        public int TicketStatus { get; set; }

        /// <summary>
        /// 建立時間
        /// </summary>
        [Column(TypeName = "datetime")]
        public DateTime CreateDate { get; set; }

        // Navigation Properties
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }
    }
}

