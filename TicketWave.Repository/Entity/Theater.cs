using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketWave.Repository.Entity;


[Table("Theater")]
public partial class Theater
{
    [Key]
    public Guid TheaterId { get; set; }

    [Required]
    [StringLength(200)]
    public string TheaterName { get; set; }

    /// <summary>
    /// 表演類型 (音樂劇/話劇/舞蹈...)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string TheaterType { get; set; }

    /// <summary>
    /// 導演
    /// </summary>
    [StringLength(100)]
    public string Director { get; set; }

    /// <summary>
    /// 演出時長 (分鐘)
    /// </summary>
    public int? Duration { get; set; }

    /// <summary>
    /// 年齡分級 (普/保護級/輔導級...)
    /// </summary>
    [StringLength(10)]
    public string AgeRating { get; set; }

    /// <summary>
    /// 語言
    /// </summary>
    [StringLength(50)]
    public string Language { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime PerformanceDate { get; set; }

    [Required]
    [StringLength(100)]
    public string VenueName { get; set; }

    [StringLength(200)]
    public string VenueAddress { get; set; }

    [StringLength(1000)]
    public string Description { get; set; }

    [StringLength(500)]
    [Unicode(false)]
    public string PosterImageUrl { get; set; }

    public int Status { get; set; }

    public int TotalSeats { get; set; }

    public int AvailableSeats { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime SaleStartDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime SaleEndDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreateDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime UpdateDate { get; set; }

    [InverseProperty("Theater")]
    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();
}
