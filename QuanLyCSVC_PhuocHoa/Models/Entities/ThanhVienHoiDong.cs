using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyCSVC_PhuocHoa.Models.Entities;

public partial class ThanhVienHoiDong
{
    [Key]
    public int Id { get; set; }

    public int PhieuThanhLyId { get; set; }

    public int NhanVienId { get; set; }

    [StringLength(100)]
    public string VaiTro { get; set; } = null!;

    [ForeignKey("NhanVienId")]
    [InverseProperty("ThanhVienHoiDong")]
    public virtual NhanVien NhanVien { get; set; } = null!;

    [ForeignKey("PhieuThanhLyId")]
    [InverseProperty("ThanhVienHoiDong")]
    public virtual PhieuThanhLy PhieuThanhLy { get; set; } = null!;
}
