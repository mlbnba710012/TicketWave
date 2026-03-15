using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketWave.Repository.Entity;

[Table("Sport")]
public partial class Sport
{
    [Key]
    public Guid SportId { get; set; }

    [Required]
    [StringLength(200)]
    public string SportName { get; set; }

    [Required]
    [StringLength(50)]
    public string SportType { get; set; }

    [Required]
    [StringLength(100)]
    public string HomeTeam { get; set; }

    [Required]
    [StringLength(100)]
    public string AwayTeam { get; set; }

    /// <summary>
    /// 聯賽名稱
    /// </summary>
    [StringLength(100)]
    public string League { get; set; }

    /// <summary>
    /// 賽季 (e.g. 2024-25)
    /// </summary>
    [StringLength(20)]
    public string Season { get; set; }

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

    public int DisplayOrder { get; set; }

    [InverseProperty("Sport")]
    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();

}
