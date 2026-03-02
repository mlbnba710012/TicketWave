using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TicketWave.Repository.Entity
{
    /// <summary>
    /// 演唱會資料表
    /// </summary>
    [Table("Concert")]
    public partial class Concert
    {
        public Concert()
        {
            Seats = new HashSet<Seat>();
        }

        /// <summary>
        /// 演唱會 ID（主鍵）
        /// </summary>
        [Key]
        public Guid ConcertId { get; set; }

        /// <summary>
        /// 演唱會名稱
        /// </summary>
        [Required]
        [StringLength(200)]
        public string ConcertName { get; set; }

        /// <summary>
        /// 藝人/樂團名稱
        /// </summary>
        [Required]
        [StringLength(100)]
        public string ArtistName { get; set; }

        /// <summary>
        /// 演出日期時間
        /// </summary>
        [Column(TypeName = "datetime")]
        public DateTime PerformanceDate { get; set; }

        /// <summary>
        /// 場館名稱
        /// </summary>
        [StringLength(100)]
        public string VenueName { get; set; }

        /// <summary>
        /// 場館地址
        /// </summary>
        [StringLength(200)]
        public string VenueAddress { get; set; }

        /// <summary>
        /// 演唱會描述
        /// </summary>
        [StringLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// 海報圖片 URL
        /// </summary>
        [StringLength(500)]
        [Unicode(false)]
        public string? PosterImageUrl { get; set; }

        /// <summary>
        /// 演唱會狀態（0:未開賣, 1:售票中, 2:已售罄, 3:已結束, 4:已取消）
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// 總座位數
        /// </summary>
        public int TotalSeats { get; set; }

        /// <summary>
        /// 剩餘座位數
        /// </summary>
        public int AvailableSeats { get; set; }

        /// <summary>
        /// 開始售票時間
        /// </summary>
        [Column(TypeName = "datetime")]
        public DateTime SaleStartDate { get; set; }

        /// <summary>
        /// 結束售票時間
        /// </summary>
        [Column(TypeName = "datetime")]
        public DateTime SaleEndDate { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime CreateDate { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime UpdateDate { get; set; }

        // Navigation Properties
        public virtual ICollection<Seat> Seats { get; set; }
    }
}
