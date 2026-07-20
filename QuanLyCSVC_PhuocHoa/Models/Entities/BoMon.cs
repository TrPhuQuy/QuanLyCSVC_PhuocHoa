using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyCSVC_PhuocHoa.Models.Entities;

public partial class BoMon
{
    [Key]
    public int Id { get; set; }

    [StringLength(100)]
    public string TenBoMon { get; set; } = null!;

    [InverseProperty("BoMon")]
    public virtual ICollection<NhanVien> NhanVien { get; set; } = new List<NhanVien>();

    [InverseProperty("BoMon")]
    public virtual ICollection<ThietBi> ThietBi { get; set; } = new List<ThietBi>();
}
