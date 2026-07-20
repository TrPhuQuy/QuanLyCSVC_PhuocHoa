using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyCSVC_PhuocHoa.Models.Entities;

public partial class ThietBi
{
    [Key]
    public int Id { get; set; }

    [Column("MaTB")]
    [StringLength(20)]
    [Unicode(false)]
    public string MaTb { get; set; } = null!;

    [Column("TenTB")]
    [StringLength(150)]
    public string TenTb { get; set; } = null!;

    public int LoaiThietBiId { get; set; }

    [StringLength(100)]
    public string? HangSanXuat { get; set; }

    public int? NamSanXuat { get; set; }

    public DateOnly? NgayMua { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? GiaMua { get; set; }

    public int SoLuong { get; set; }

    [StringLength(20)]
    public string DonViTinh { get; set; } = null!;

    [StringLength(50)]
    public string TinhTrang { get; set; } = null!;

    [StringLength(255)]
    public string? HinhAnh { get; set; }

    [StringLength(150)]
    public string? ViTriSuDung { get; set; }

    public int PhongId { get; set; }

    public int? BoMonId { get; set; }

    public int? NhaCungCapId { get; set; }

    [StringLength(255)]
    public string? GhiChu { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsDeleted { get; set; }

    [ForeignKey("BoMonId")]
    [InverseProperty("ThietBi")]
    public virtual BoMon? BoMon { get; set; }

    [InverseProperty("ThietBi")]
    public virtual ICollection<ChiTietKiemKe> ChiTietKiemKe { get; set; } = new List<ChiTietKiemKe>();

    [InverseProperty("ThietBi")]
    public virtual ICollection<ChiTietPhieuMuon> ChiTietPhieuMuon { get; set; } = new List<ChiTietPhieuMuon>();

    [InverseProperty("ThietBi")]
    public virtual ICollection<ChiTietPhieuNhap> ChiTietPhieuNhap { get; set; } = new List<ChiTietPhieuNhap>();

    [InverseProperty("ThietBi")]
    public virtual ICollection<DieuChuyen> DieuChuyen { get; set; } = new List<DieuChuyen>();

    [ForeignKey("LoaiThietBiId")]
    [InverseProperty("ThietBi")]
    public virtual LoaiThietBi LoaiThietBi { get; set; } = null!;

    [ForeignKey("NhaCungCapId")]
    [InverseProperty("ThietBi")]
    public virtual NhaCungCap? NhaCungCap { get; set; }

    [InverseProperty("ThietBi")]
    public virtual ICollection<PhieuBaoTri> PhieuBaoTri { get; set; } = new List<PhieuBaoTri>();

    [InverseProperty("ThietBi")]
    public virtual ICollection<PhieuThanhLy> PhieuThanhLy { get; set; } = new List<PhieuThanhLy>();

    [ForeignKey("PhongId")]
    [InverseProperty("ThietBi")]
    public virtual Phong Phong { get; set; } = null!;
}
