using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyCSVC_PhuocHoa.Models.Entities;

public partial class LoaiThietBi
{
    [Key]
    public int Id { get; set; }

    [StringLength(100)]
    public string TenLoaiTB { get; set; } = null!;

    [StringLength(255)]
    public string? MoTa { get; set; }

    [InverseProperty("LoaiThietBi")]
    public virtual ICollection<ThietBi> ThietBi { get; set; } = new List<ThietBi>();
}
