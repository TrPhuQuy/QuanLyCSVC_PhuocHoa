using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyCSVC_PhuocHoa.Models.Entities;

public partial class ChiTietPhieuMuon
{
    [Key]
    public int Id { get; set; }

    public int PhieuMuonId { get; set; }

    public int ThietBiId { get; set; }

    public int SoLuongMuon { get; set; }

    [StringLength(50)]
    public string? TinhTrangKhiTra { get; set; }

    [StringLength(255)]
    public string? GhiChu { get; set; }

    [ForeignKey("PhieuMuonId")]
    [InverseProperty("ChiTietPhieuMuon")]
    public virtual PhieuMuon PhieuMuon { get; set; } = null!;

    [ForeignKey("ThietBiId")]
    [InverseProperty("ChiTietPhieuMuon")]
    public virtual ThietBi ThietBi { get; set; } = null!;
}
