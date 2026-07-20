using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyCSVC_PhuocHoa.Models.Entities;

public partial class LoaiPhong
{
    [Key]
    public int Id { get; set; }

    [StringLength(100)]
    public string TenLoaiPhong { get; set; } = null!;

    [StringLength(255)]
    public string? MoTa { get; set; }

    [InverseProperty("LoaiPhong")]
    public virtual ICollection<Phong> Phong { get; set; } = new List<Phong>();
}
