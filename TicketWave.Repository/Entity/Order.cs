using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TicketWave.Repository.Entity
{
    /// <summary>
    /// 訂單資料表
    /// 記錄會員購買演唱會門票的訂單資訊
    /// </summary>
    [Table("Order")]
    [Index("OrderNumber", Name = "IX_Order_OrderNumber", IsUnique = true)]
    public partial class Order
    {
        public Order()
        {
            OrderDetails = new HashSet<OrderDetail>();
        }

        /// <summary>
        /// 訂單 ID（主鍵）
        /// </summary>
        [Key]
        public Guid OrderId { get; set; }

        /// <summary>
        /// 訂單編號（顯示用，例如：ORD20260220001）
        /// </summary>
        [Required]
        [StringLength(50)]
        [Unicode(false)]
        public string OrderNumber { get; set; }

        /// <summary>
        /// 會員 ID
        /// </summary>
        public Guid MemberId { get; set; }

        /// <summary>
        /// 演唱會 ID
        /// </summary>
        public Guid ConcertId { get; set; }

        /// <summary>
        /// 演唱會名稱（冗余欄位，方便查詢）
        /// </summary>
        [StringLength(200)]
        public string ConcertName { get; set; }

        /// <summary>
        /// 訂單總金額
        /// </summary>
        [Column(TypeName = "decimal(10, 2)")]
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// 購票數量
        /// </summary>
        public int TicketCount { get; set; }

        /// <summary>
        /// 訂單狀態（0:待付款, 1:已付款, 2:已取消, 3:已退款）
        /// </summary>
        public int OrderStatus { get; set; }

        /// <summary>
        /// 訂單建立時間
        /// </summary>
        [Column(TypeName = "datetime")]
        public DateTime CreateDate { get; set; }

        /// <summary>
        /// 訂單修改時間
        /// </summary>
        [Column(TypeName = "datetime")]
        public DateTime UpdateDate { get; set; }

        // Navigation Properties
        [ForeignKey("MemberId")]
        public virtual Member Member { get; set; }

        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
    }
}
