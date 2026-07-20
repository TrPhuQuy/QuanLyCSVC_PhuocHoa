using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyCSVC_PhuocHoa.Models.Entities;

public partial class PhieuMuon
{
    [Key]
    public int Id { get; set; }

    [Column("MaPhieuMuon")]
    [StringLength(20)]
    [Unicode(false)]
    public string MaPhieuMuon { get; set; } = null!;

    public int GiaoVienId { get; set; }

    public int? NguoiDuyetId { get; set; }

    public DateTime NgayDangKy { get; set; }

    public DateOnly NgayMuonDuKien { get; set; }

    public DateOnly NgayTraDuKien { get; set; }

    public DateTime? NgayMuonThucTe { get; set; }

    public DateTime? NgayTraThucTe { get; set; }

    [StringLength(50)]
    public string TrangThai { get; set; } = null!;

    [StringLength(255)]
    public string? GhiChu { get; set; }

    [InverseProperty("PhieuMuon")]
    public virtual ICollection<ChiTietPhieuMuon> ChiTietPhieuMuon { get; set; } = new List<ChiTietPhieuMuon>();

    [ForeignKey("GiaoVienId")]
    [InverseProperty("PhieuMuonGiaoVien")]
    public virtual NhanVien GiaoVien { get; set; } = null!;

    [ForeignKey("NguoiDuyetId")]
    [InverseProperty("PhieuMuonNguoiDuyet")]
    public virtual NhanVien? NguoiDuyet { get; set; } = null!;
}
