using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyCSVC_PhuocHoa.Models.Entities;

public partial class NhanVien
{
    [Key]
    public int Id { get; set; }

    [Column("MaNV")]
    [StringLength(20)]
    [Unicode(false)]
    public string MaNv { get; set; } = null!;

    [StringLength(100)]
    public string HoTen { get; set; } = null!;

    public bool? GioiTinh { get; set; }

    public DateOnly? NgaySinh { get; set; }

    [StringLength(100)]
    public string ChucVu { get; set; } = null!;

    public int? BoMonId { get; set; }

    [Column("SDT")]
    [StringLength(15)]
    [Unicode(false)]
    public string? Sdt { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? Email { get; set; }

    public string UserId { get; set; } = null!;

    public bool TrangThai { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey("BoMonId")]
    [InverseProperty("NhanVien")]
    public virtual BoMon? BoMon { get; set; }

    [InverseProperty("NguoiDeNghi")]
    public virtual ICollection<DieuChuyen> DieuChuyenNguoiDeNghi { get; set; } = new List<DieuChuyen>();

    [InverseProperty("NguoiDuyet")]
    public virtual ICollection<DieuChuyen> DieuChuyenNguoiDuyet { get; set; } = new List<DieuChuyen>();

    [InverseProperty("NguoiChuTri")]
    public virtual ICollection<DotKiemKe> DotKiemKe { get; set; } = new List<DotKiemKe>();

    [InverseProperty("NguoiGui")]
    public virtual ICollection<PhieuBaoTri> PhieuBaoTri { get; set; } = new List<PhieuBaoTri>();

    [InverseProperty("GiaoVien")]
    public virtual ICollection<PhieuMuon> PhieuMuonGiaoVien { get; set; } = new List<PhieuMuon>();

    [InverseProperty("NguoiDuyet")]
    public virtual ICollection<PhieuMuon> PhieuMuonNguoiDuyet { get; set; } = new List<PhieuMuon>();

    [InverseProperty("NguoiNhap")]
    public virtual ICollection<PhieuNhap> PhieuNhap { get; set; } = new List<PhieuNhap>();

    [InverseProperty("NguoiPhuTrach")]
    public virtual ICollection<Phong> Phong { get; set; } = new List<Phong>();

    [InverseProperty("NhanVien")]
    public virtual ICollection<ThanhVienHoiDong> ThanhVienHoiDong { get; set; } = new List<ThanhVienHoiDong>();

    [ForeignKey("UserId")]
    [InverseProperty("NhanVien")]
    public virtual ApplicationUser User { get; set; } = null!;
}
