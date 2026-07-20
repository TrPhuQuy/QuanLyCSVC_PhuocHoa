using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyCSVC_PhuocHoa.Models.Entities;

public partial class PhieuNhap
{
    [Key]
    public int Id { get; set; }

    [Column("MaPhieuNhap")]
    [StringLength(20)]
    [Unicode(false)]
    public string MaPhieuNhap { get; set; } = null!;

    public int NhaCungCapId { get; set; }

    public DateOnly NgayNhap { get; set; }

    public int NguoiNhapId { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TongTien { get; set; }

    [StringLength(255)]
    public string? GhiChu { get; set; }

    public DateTime CreatedAt { get; set; }

    [InverseProperty("PhieuNhap")]
    public virtual ICollection<ChiTietPhieuNhap> ChiTietPhieuNhap { get; set; } = new List<ChiTietPhieuNhap>();

    [ForeignKey("NguoiNhapId")]
    [InverseProperty("PhieuNhap")]
    public virtual NhanVien NguoiNhap { get; set; } = null!;

    [ForeignKey("NhaCungCapId")]
    [InverseProperty("PhieuNhap")]
    public virtual NhaCungCap NhaCungCap { get; set; } = null!;
}
