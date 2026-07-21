using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyCSVC_PhuocHoa.Models.Entities;

public partial class DieuChuyen
{
    [Key]
    public int Id { get; set; }

    [Column("MaDieuChuyen")]
    [StringLength(20)]
    [Unicode(false)]
    public string MaDieuChuyen { get; set; } = null!;

    public int ThietBiId { get; set; }

    public int PhongDiId { get; set; }

    public int PhongDenId { get; set; }

    public int SoLuong { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập lý do điều chuyển.")]
    [StringLength(255)]
    public string? LyDo { get; set; }

    public int NguoiDeNghiId { get; set; }

    public int? NguoiDuyetId { get; set; }

    public DateTime NgayDeNghi { get; set; }

    public DateTime? NgayDuyet { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày thực hiện điều chuyển dự kiến.")]
    public DateOnly? NgayDieuChuyen { get; set; }

    [StringLength(50)]
    public string TrangThai { get; set; } = null!;

    [ForeignKey("NguoiDeNghiId")]
    [InverseProperty("DieuChuyenNguoiDeNghi")]
    public virtual NhanVien NguoiDeNghi { get; set; } = null!;

    [ForeignKey("NguoiDuyetId")]
    [InverseProperty("DieuChuyenNguoiDuyet")]
    public virtual NhanVien? NguoiDuyet { get; set; } = null!;

    [ForeignKey("PhongDenId")]
    [InverseProperty("DieuChuyenPhongDen")]
    public virtual Phong PhongDen { get; set; } = null!;

    [ForeignKey("PhongDiId")]
    [InverseProperty("DieuChuyenPhongDi")]
    public virtual Phong PhongDi { get; set; } = null!;

    [ForeignKey("ThietBiId")]
    [InverseProperty("DieuChuyen")]
    public virtual ThietBi ThietBi { get; set; } = null!;
}