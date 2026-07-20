using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyCSVC_PhuocHoa.Models.Entities;

public partial class ChiTietKiemKe
{
    [Key]
    public int Id { get; set; }

    public int DotKiemKeId { get; set; }

    public int ThietBiId { get; set; }

    public int SoLuongSoSach { get; set; }

    public int SoLuongThucTe { get; set; }

    public int ChenhLech { get; set; }

    [StringLength(50)]
    public string? PhanLoai { get; set; }

    [StringLength(255)]
    public string? DeXuatXuLy { get; set; }

    [ForeignKey("DotKiemKeId")]
    [InverseProperty("ChiTietKiemKe")]
    public virtual DotKiemKe DotKiemKe { get; set; } = null!;

    [ForeignKey("ThietBiId")]
    [InverseProperty("ChiTietKiemKe")]
    public virtual ThietBi ThietBi { get; set; } = null!;
}
