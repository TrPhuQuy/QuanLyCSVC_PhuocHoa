namespace QuanLyCSVC_PhuocHoa.Models.ViewModels;

public class DashboardViewModel
{
    public int TongThietBi { get; set; }
    public int TongPhong { get; set; }
    public int ThietBiTot { get; set; }
    public int ThietBiHuHong { get; set; }
    public int ThietBiDangSua { get; set; }
    public int ThietBiThanhLy { get; set; }
    public decimal TongGiaTriTaiSan { get; set; }
    public int PhieuMuonChoDuyet { get; set; }
    public int PhieuDieuChuyenChoDuyet { get; set; }
    public List<ThongKeTheoLoai> ThongKeTheoLoais { get; set; } = [];
}

public class ThongKeTheoLoai
{
    public string TenLoai { get; set; } = string.Empty;
    public int SoLuong { get; set; }
}
