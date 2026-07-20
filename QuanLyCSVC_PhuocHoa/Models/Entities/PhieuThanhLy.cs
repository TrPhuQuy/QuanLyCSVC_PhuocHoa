using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyCSVC_PhuocHoa.Models.Entities;

public partial class PhieuThanhLy
{
    [Key]
    public int Id { get; set; }

    [Column("MaThanhLy")]
    [StringLength(20)]
    [Unicode(false)]
    public string MaThanhLy { get; set; } = null!;

    public int ThietBiId { get; set; }

    [StringLength(255)]
    public string LyDo { get; set; } = null!;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? GiaTriConLai { get; set; }

    public DateOnly NgayDeNghi { get; set; }

    public DateOnly? NgayQuyetDinh { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? SoQuyetDinh { get; set; }

    [StringLength(50)]
    public string TrangThai { get; set; } = null!;

    [InverseProperty("PhieuThanhLy")]
    public virtual ICollection<ThanhVienHoiDong> ThanhVienHoiDong { get; set; } = new List<ThanhVienHoiDong>();

    [ForeignKey("ThietBiId")]
    [InverseProperty("PhieuThanhLy")]
    public virtual ThietBi ThietBi { get; set; } = null!;
}
