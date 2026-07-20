using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyCSVC_PhuocHoa.Models.Entities;

public partial class ChiTietPhieuNhap
{
    [Key]
    public int Id { get; set; }

    public int PhieuNhapId { get; set; }

    public int ThietBiId { get; set; }

    public int SoLuong { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal DonGia { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal ThanhTien { get; set; }

    [ForeignKey("PhieuNhapId")]
    [InverseProperty("ChiTietPhieuNhap")]
    public virtual PhieuNhap PhieuNhap { get; set; } = null!;

    [ForeignKey("ThietBiId")]
    [InverseProperty("ChiTietPhieuNhap")]
    public virtual ThietBi ThietBi { get; set; } = null!;
}
