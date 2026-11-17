namespace QuanLyCSVC_PhuocHoa.Models.ViewModels;

public class BaoCaoViewModel
{
    // ---- Chỉ số tổng hợp toàn trường ----
    public int TongThietBi { get; set; }
    public int TongPhong { get; set; }
    public decimal TongNguyenGia { get; set; }
    public decimal TongHaoMonLuyKe { get; set; }
    public decimal TongGiaTriConLai { get; set; }
    public decimal TongChiPhiBaoTri { get; set; }
    public int TongLuotMuon { get; set; }
    public int SoThietBiCanThanhLy { get; set; } // Hư hỏng hoặc đã hết khấu hao 100%

    // ---- Danh sách thống kê chi tiết ----
    public List<BaoCaoPhongItem> ThongKeTheoPhong { get; set; } = [];
    public List<BaoCaoLoaiTbItem> ThongKeTheoLoai { get; set; } = [];
    public List<BaoCaoBoMonItem> ThongKeTheoBoMon { get; set; } = [];
    public List<BaoCaoBaoTriItem> LichSuBaoTri { get; set; } = [];
}

public class BaoCaoPhongItem
{
    public int PhongId { get; set; }
    public string MaPhong { get; set; } = string.Empty;
    public string TenPhong { get; set; } = string.Empty;
    public string LoaiPhong { get; set; } = string.Empty;
    public string NguoiPhuTrach { get; set; } = string.Empty;
    public int SoChungLoai { get; set; }
    public int TongSoLuong { get; set; }
    public decimal TongGiaTri { get; set; }
    public decimal GiaTriConLai { get; set; }
}

public class BaoCaoLoaiTbItem
{
    public string TenLoaiTB { get; set; } = string.Empty;
    public int SoChungLoai { get; set; }
    public int TongSoLuong { get; set; }
    public decimal TongGiaTri { get; set; }
    public int SoLuongTot { get; set; }
    public int SoLuongHong { get; set; }
    public int SoLuongDangSua { get; set; }
}

public class BaoCaoBoMonItem
{
    public string TenBoMon { get; set; } = string.Empty;
    public int SoGiaoVien { get; set; }
    public int SoChungLoaiTb { get; set; }
    public int TongSoLuongTb { get; set; }
    public decimal TongGiaTri { get; set; }
}

public class BaoCaoBaoTriItem
{
    public string MaBaoTri { get; set; } = string.Empty;
    public string TenThietBi { get; set; } = string.Empty;
    public string TenPhong { get; set; } = string.Empty;
    public DateOnly NgayGui { get; set; }
    public DateOnly? NgayNhanLai { get; set; }
    public decimal ChiPhi { get; set; }
    public string? DonViSua { get; set; }
    public string TrangThai { get; set; } = string.Empty;
}
