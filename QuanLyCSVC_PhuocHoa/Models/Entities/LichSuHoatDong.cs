using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyCSVC_PhuocHoa.Models.Entities;

public partial class LichSuHoatDong
{
    [Key]
    public long Id { get; set; }

    public string UserId { get; set; } = null!;

    [StringLength(255)]
    public string HanhDong { get; set; } = null!;

    [StringLength(100)]
    public string DoiTuong { get; set; } = null!;

    public DateTime ThoiGian { get; set; }

    [StringLength(45)]
    [Unicode(false)]
    public string? DiaChiIP { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("LichSuHoatDong")]
    public virtual ApplicationUser User { get; set; } = null!;
}
