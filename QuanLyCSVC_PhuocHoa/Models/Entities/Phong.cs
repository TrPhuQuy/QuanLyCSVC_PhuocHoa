using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyCSVC_PhuocHoa.Models.Entities;

public partial class Phong
{
    [Key]
    public int Id { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string MaPhong { get; set; } = null!;

    [StringLength(100)]
    public string TenPhong { get; set; } = null!;

    public int LoaiPhongId { get; set; }

    public int? SucChua { get; set; }

    [StringLength(150)]
    public string? ViTri { get; set; }

    public int? NguoiPhuTrachId { get; set; }

    [StringLength(50)]
    public string TrangThai { get; set; } = null!;

    [StringLength(255)]
    public string? GhiChu { get; set; }

    [InverseProperty("PhongDen")]
    public virtual ICollection<DieuChuyen> DieuChuyenPhongDen { get; set; } = new List<DieuChuyen>();

    [InverseProperty("PhongDi")]
    public virtual ICollection<DieuChuyen> DieuChuyenPhongDi { get; set; } = new List<DieuChuyen>();

    [ForeignKey("LoaiPhongId")]
    [InverseProperty("Phong")]
    public virtual LoaiPhong LoaiPhong { get; set; } = null!;

    [ForeignKey("NguoiPhuTrachId")]
    [InverseProperty("Phong")]
    public virtual NhanVien? NguoiPhuTrach { get; set; }

    [InverseProperty("Phong")]
    public virtual ICollection<ThietBi> ThietBi { get; set; } = new List<ThietBi>();
}
