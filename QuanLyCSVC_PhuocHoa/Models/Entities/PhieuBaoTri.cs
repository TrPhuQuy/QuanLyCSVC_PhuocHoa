using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyCSVC_PhuocHoa.Models.Entities;

public partial class PhieuBaoTri
{
    [Key]
    public int Id { get; set; }

    [Column("MaBaoTri")]
    [StringLength(20)]
    [Unicode(false)]
    public string MaBaoTri { get; set; } = null!;

    public int ThietBiId { get; set; }

    [StringLength(255)]
    public string NoiDungHongHoc { get; set; } = null!;

    [StringLength(150)]
    public string? DonViSuaChua { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? ChiPhi { get; set; }

    public int NguoiGuiId { get; set; }

    public DateOnly NgayGui { get; set; }

    public DateOnly? NgayNhanLai { get; set; }

    [StringLength(255)]
    public string? KetQua { get; set; }

    [StringLength(50)]
    public string TrangThai { get; set; } = null!;

    [ForeignKey("NguoiGuiId")]
    [InverseProperty("PhieuBaoTri")]
    public virtual NhanVien NguoiGui { get; set; } = null!;

    [ForeignKey("ThietBiId")]
    [InverseProperty("PhieuBaoTri")]
    public virtual ThietBi ThietBi { get; set; } = null!;
}
