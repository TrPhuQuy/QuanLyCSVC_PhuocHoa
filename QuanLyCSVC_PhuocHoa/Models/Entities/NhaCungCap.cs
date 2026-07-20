using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyCSVC_PhuocHoa.Models.Entities;

public partial class NhaCungCap
{
    [Key]
    public int Id { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string MaNCC { get; set; } = null!;

    [StringLength(150)]
    public string TenNCC { get; set; } = null!;

    [StringLength(255)]
    public string? DiaChi { get; set; }

    [Column("SDT")]
    [StringLength(15)]
    [Unicode(false)]
    public string? Sdt { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? Email { get; set; }

    [StringLength(100)]
    public string? NguoiDaiDien { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string? MaSoThue { get; set; }

    public bool TrangThai { get; set; }

    [InverseProperty("NhaCungCap")]
    public virtual ICollection<PhieuNhap> PhieuNhap { get; set; } = new List<PhieuNhap>();

    [InverseProperty("NhaCungCap")]
    public virtual ICollection<ThietBi> ThietBi { get; set; } = new List<ThietBi>();
}
