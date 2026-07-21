namespace QuanLyCSVC_PhuocHoa.Models.ViewModels;

public class NavbarAlertsViewModel
{
    public int SoPhieuChoDuyet { get; set; }
    public int SoPhieuMuonChoDuyet { get; set; }
    public int SoDieuChuyenChoDuyet { get; set; }
    public int SoThanhLyChoDuyet { get; set; }
    public List<PhieuMuonQuaHan> DanhSachQuaHan { get; set; } = [];

    public int SoQuaHan => DanhSachQuaHan.Count;
}

public class PhieuMuonQuaHan
{
    public int Id { get; set; }
    public string MaPhieuMuon { get; set; } = string.Empty;
    public string TenGiaoVien { get; set; } = string.Empty;
    public DateOnly NgayTraDuKien { get; set; }
    public int SoNgayTre { get; set; }
}
