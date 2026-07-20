using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyCSVC_PhuocHoa.Models.Entities;

public partial class DotKiemKe
{
    [Key]
    public int Id { get; set; }

    [Column("MaDotKiemKe")]
    [StringLength(20)]
    [Unicode(false)]
    public string MaDotKiemKe { get; set; } = null!;

    [StringLength(100)]
    public string LoaiKiemKe { get; set; } = null!;

    public int? HocKy { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string NamHoc { get; set; } = null!;

    public DateOnly NgayBatDau { get; set; }

    public DateOnly NgayKetThuc { get; set; }

    public int NguoiChuTriId { get; set; }

    [StringLength(50)]
    public string TrangThai { get; set; } = null!;

    [InverseProperty("DotKiemKe")]
    public virtual ICollection<ChiTietKiemKe> ChiTietKiemKe { get; set; } = new List<ChiTietKiemKe>();

    [ForeignKey("NguoiChuTriId")]
    [InverseProperty("DotKiemKe")]
    public virtual NhanVien NguoiChuTri { get; set; } = null!;
}
