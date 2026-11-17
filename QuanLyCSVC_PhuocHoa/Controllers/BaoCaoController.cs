using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyCSVC_PhuocHoa.Data;
using QuanLyCSVC_PhuocHoa.Helpers;
using QuanLyCSVC_PhuocHoa.Models;
using QuanLyCSVC_PhuocHoa.Models.ViewModels;

namespace QuanLyCSVC_PhuocHoa.Controllers;

[Authorize]
public class BaoCaoController : Controller
{
    private readonly ApplicationDbContext _context;

    public BaoCaoController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var vm = await BuildBaoCaoViewModelAsync();
        return View(vm);
    }

    /// <summary>
    /// Trang in Báo cáo Tổng kết Cơ sở vật chất THCS Phước Hòa (Khổ A4)
    /// </summary>
    public async Task<IActionResult> PrintReport()
    {
        var vm = await BuildBaoCaoViewModelAsync();
        return View(vm);
    }

    /// <summary>
    /// Xuất file Excel báo cáo tổng hợp cơ sở vật chất (gồm 3 trang tính chi tiết)
    /// </summary>
    public async Task<IActionResult> ExportExcel()
    {
        var vm = await BuildBaoCaoViewModelAsync();

        using var workbook = new XLWorkbook();

        // ------------------ Sheet 1: Tổng hợp theo Phòng ------------------
        var wsPhong = workbook.Worksheets.Add("CSVC Theo Phòng");
        wsPhong.Cell(1, 1).Value = "TRƯỜNG THCS PHƯỚC HÒA";
        wsPhong.Cell(2, 1).Value = "BÁO CÁO TỔNG HỢP CƠ SỞ VẬT CHẤT THEO PHÒNG HỌC & KHU CHỨC NĂNG";
        wsPhong.Cell(2, 1).Style.Font.Bold = true;
        wsPhong.Cell(3, 1).Value = $"Thời điểm xuất báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm}";

        string[] headersPhong = ["STT", "Mã phòng", "Tên phòng", "Loại phòng", "Người phụ trách", "Số loại TB", "Tổng số lượng", "Tổng nguyên giá (đ)", "Giá trị còn lại (đ)"];
        for (int i = 0; i < headersPhong.Length; i++)
        {
            var cell = wsPhong.Cell(5, i + 1);
            cell.Value = headersPhong[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0d6efd");
            cell.Style.Font.FontColor = XLColor.White;
        }

        int r = 6;
        int stt = 1;
        foreach (var p in vm.ThongKeTheoPhong)
        {
            wsPhong.Cell(r, 1).Value = stt++;
            wsPhong.Cell(r, 2).Value = p.MaPhong;
            wsPhong.Cell(r, 3).Value = p.TenPhong;
            wsPhong.Cell(r, 4).Value = p.LoaiPhong;
            wsPhong.Cell(r, 5).Value = p.NguoiPhuTrach;
            wsPhong.Cell(r, 6).Value = p.SoChungLoai;
            wsPhong.Cell(r, 7).Value = p.TongSoLuong;
            wsPhong.Cell(r, 8).Value = p.TongGiaTri;
            wsPhong.Cell(r, 8).Style.NumberFormat.Format = "#,##0";
            wsPhong.Cell(r, 9).Value = p.GiaTriConLai;
            wsPhong.Cell(r, 9).Style.NumberFormat.Format = "#,##0";
            r++;
        }

        // Dòng tổng cộng
        wsPhong.Cell(r, 3).Value = "TỔNG CỘNG";
        wsPhong.Cell(r, 3).Style.Font.Bold = true;
        wsPhong.Cell(r, 7).FormulaA1 = $"SUM(G6:G{r - 1})";
        wsPhong.Cell(r, 8).FormulaA1 = $"SUM(H6:H{r - 1})";
        wsPhong.Cell(r, 9).FormulaA1 = $"SUM(I6:I{r - 1})";
        wsPhong.Row(r).Style.Font.Bold = true;
        wsPhong.Cell(r, 8).Style.NumberFormat.Format = "#,##0";
        wsPhong.Cell(r, 9).Style.NumberFormat.Format = "#,##0";
        wsPhong.Columns().AdjustToContents();

        // ------------------ Sheet 2: Tổng hợp theo Phân loại ------------------
        var wsLoai = workbook.Worksheets.Add("CSVC Theo Loại");
        wsLoai.Cell(1, 1).Value = "BÁO CÁO TỔNG HỢP THIẾT BỊ THEO PHÂN LOẠI";
        wsLoai.Cell(1, 1).Style.Font.Bold = true;

        string[] headersLoai = ["STT", "Loại thiết bị", "Số chủng loại", "Tổng số lượng", "Tổng nguyên giá (đ)", "Tốt", "Hư hỏng", "Đang sửa"];
        for (int i = 0; i < headersLoai.Length; i++)
        {
            var cell = wsLoai.Cell(3, i + 1);
            cell.Value = headersLoai[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#198754");
            cell.Style.Font.FontColor = XLColor.White;
        }

        r = 4;
        stt = 1;
        foreach (var l in vm.ThongKeTheoLoai)
        {
            wsLoai.Cell(r, 1).Value = stt++;
            wsLoai.Cell(r, 2).Value = l.TenLoaiTB;
            wsLoai.Cell(r, 3).Value = l.SoChungLoai;
            wsLoai.Cell(r, 4).Value = l.TongSoLuong;
            wsLoai.Cell(r, 5).Value = l.TongGiaTri;
            wsLoai.Cell(r, 5).Style.NumberFormat.Format = "#,##0";
            wsLoai.Cell(r, 6).Value = l.SoLuongTot;
            wsLoai.Cell(r, 7).Value = l.SoLuongHong;
            wsLoai.Cell(r, 8).Value = l.SoLuongDangSua;
            r++;
        }
        wsLoai.Columns().AdjustToContents();

        // ------------------ Sheet 3: Chi phí bảo trì ------------------
        var wsBaoTri = workbook.Worksheets.Add("Chi Phí Bảo Trì");
        wsBaoTri.Cell(1, 1).Value = "BÁO CÁO KINH PHÍ BẢO TRÌ VÀ SỬA CHỮA THIẾT BỊ";
        wsBaoTri.Cell(1, 1).Style.Font.Bold = true;

        string[] headersBt = ["STT", "Mã phiếu", "Tên thiết bị", "Vị trí", "Ngày gửi", "Ngày nhận lại", "Chi phí (đ)", "Đơn vị sửa", "Trạng thái"];
        for (int i = 0; i < headersBt.Length; i++)
        {
            var cell = wsBaoTri.Cell(3, i + 1);
            cell.Value = headersBt[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#d97706");
            cell.Style.Font.FontColor = XLColor.White;
        }

        r = 4;
        stt = 1;
        foreach (var bt in vm.LichSuBaoTri)
        {
            wsBaoTri.Cell(r, 1).Value = stt++;
            wsBaoTri.Cell(r, 2).Value = bt.MaBaoTri;
            wsBaoTri.Cell(r, 3).Value = bt.TenThietBi;
            wsBaoTri.Cell(r, 4).Value = bt.TenPhong;
            wsBaoTri.Cell(r, 5).Value = bt.NgayGui.ToString("dd/MM/yyyy");
            wsBaoTri.Cell(r, 6).Value = bt.NgayNhanLai?.ToString("dd/MM/yyyy") ?? "-";
            wsBaoTri.Cell(r, 7).Value = bt.ChiPhi;
            wsBaoTri.Cell(r, 7).Style.NumberFormat.Format = "#,##0";
            wsBaoTri.Cell(r, 8).Value = bt.DonViSua ?? "-";
            wsBaoTri.Cell(r, 9).Value = bt.TrangThai;
            r++;
        }
        wsBaoTri.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var fileName = $"BaoCaoTongHop_CSVC_PhuocHoa_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    private async Task<BaoCaoViewModel> BuildBaoCaoViewModelAsync()
    {
        var thietBis = await _context.ThietBi
            .Include(t => t.LoaiThietBi)
            .Include(t => t.Phong)
            .Include(t => t.BoMon)
            .Where(t => !t.IsDeleted)
            .ToListAsync();

        var phongs = await _context.Phong
            .Include(p => p.LoaiPhong)
            .Include(p => p.NguoiPhuTrach)
            .Include(p => p.ThietBi)
            .ToListAsync();

        var boMons = await _context.BoMon
            .Include(b => b.NhanVien)
            .Include(b => b.ThietBi)
            .ToListAsync();

        var baoTris = await _context.PhieuBaoTri
            .Include(p => p.ThietBi).ThenInclude(t => t.Phong)
            .OrderByDescending(p => p.NgayGui)
            .ToListAsync();

        var vm = new BaoCaoViewModel();

        // 1. Chỉ số tổng hợp
        var activeTbs = thietBis.Where(t => t.TinhTrang != "Thanh lý").ToList();
        vm.TongThietBi = activeTbs.Sum(t => t.SoLuong);
        vm.TongPhong = phongs.Count(p => p.TrangThai == "Đang sử dụng");
        vm.TongNguyenGia = activeTbs.Sum(t => (t.GiaMua ?? 0) * t.SoLuong);
        vm.TongLuotMuon = await _context.PhieuMuon.CountAsync(p => p.TrangThai == "Đang mượn" || p.TrangThai == "Đã trả");
        vm.TongChiPhiBaoTri = baoTris.Where(p => p.ChiPhi.HasValue).Sum(p => p.ChiPhi!.Value);

        decimal tongHaoMon = 0;
        decimal tongConLai = 0;
        int canThanhLy = 0;

        foreach (var tb in activeTbs)
        {
            var hm = EnumHelper.TinhHaoMon(tb.GiaMua, tb.SoLuong, tb.NamSanXuat, tb.NgayMua, tb.CreatedAt, tb.LoaiThietBi?.TenLoaiTB);
            tongHaoMon += hm.GiaTriHaoMonLuyKe;
            tongConLai += hm.GiaTriConLai;
            if (hm.DaHetKhauHao || tb.TinhTrang == "Hư hỏng")
            {
                canThanhLy++;
            }
        }

        vm.TongHaoMonLuyKe = tongHaoMon;
        vm.TongGiaTriConLai = tongConLai;
        vm.SoThietBiCanThanhLy = canThanhLy;

        // 2. Thống kê theo phòng
        vm.ThongKeTheoPhong = phongs.Select(p =>
        {
            var tbInPhong = p.ThietBi.Where(t => !t.IsDeleted && t.TinhTrang != "Thanh lý").ToList();
            decimal giaTriPhong = tbInPhong.Sum(t => (t.GiaMua ?? 0) * t.SoLuong);
            decimal conLaiPhong = 0;
            foreach (var t in tbInPhong)
            {
                var hm = EnumHelper.TinhHaoMon(t.GiaMua, t.SoLuong, t.NamSanXuat, t.NgayMua, t.CreatedAt, t.LoaiThietBi?.TenLoaiTB);
                conLaiPhong += hm.GiaTriConLai;
            }

            return new BaoCaoPhongItem
            {
                PhongId = p.Id,
                MaPhong = p.MaPhong,
                TenPhong = p.TenPhong,
                LoaiPhong = p.LoaiPhong?.TenLoaiPhong ?? "-",
                NguoiPhuTrach = p.NguoiPhuTrach?.HoTen ?? "-",
                SoChungLoai = tbInPhong.Count,
                TongSoLuong = tbInPhong.Sum(t => t.SoLuong),
                TongGiaTri = giaTriPhong,
                GiaTriConLai = conLaiPhong
            };
        }).OrderBy(p => p.MaPhong).ToList();

        // 3. Thống kê theo phân loại
        vm.ThongKeTheoLoai = activeTbs.GroupBy(t => t.LoaiThietBi?.TenLoaiTB ?? "Chưa phân loại")
            .Select(g => new BaoCaoLoaiTbItem
            {
                TenLoaiTB = g.Key,
                SoChungLoai = g.Count(),
                TongSoLuong = g.Sum(x => x.SoLuong),
                TongGiaTri = g.Sum(x => (x.GiaMua ?? 0) * x.SoLuong),
                SoLuongTot = g.Where(x => x.TinhTrang == "Đang sử dụng").Sum(x => x.SoLuong),
                SoLuongHong = g.Where(x => x.TinhTrang == "Hư hỏng").Sum(x => x.SoLuong),
                SoLuongDangSua = g.Where(x => x.TinhTrang == "Đang sửa").Sum(x => x.SoLuong)
            }).OrderByDescending(x => x.TongSoLuong).ToList();

        // 4. Thống kê theo tổ bộ môn
        vm.ThongKeTheoBoMon = boMons.Select(b =>
        {
            var tbBoMon = b.ThietBi.Where(t => !t.IsDeleted && t.TinhTrang != "Thanh lý").ToList();
            return new BaoCaoBoMonItem
            {
                TenBoMon = b.TenBoMon,
                SoGiaoVien = b.NhanVien.Count(n => n.TrangThai),
                SoChungLoaiTb = tbBoMon.Count,
                TongSoLuongTb = tbBoMon.Sum(t => t.SoLuong),
                TongGiaTri = tbBoMon.Sum(t => (t.GiaMua ?? 0) * t.SoLuong)
            };
        }).OrderByDescending(b => b.TongSoLuongTb).ToList();

        // 5. Lịch sử bảo trì
        vm.LichSuBaoTri = baoTris.Take(20).Select(bt => new BaoCaoBaoTriItem
        {
            MaBaoTri = bt.MaBaoTri,
            TenThietBi = bt.ThietBi?.TenTb ?? "Chưa xác định",
            TenPhong = bt.ThietBi?.Phong?.TenPhong ?? "-",
            NgayGui = bt.NgayGui,
            NgayNhanLai = bt.NgayNhanLai,
            ChiPhi = bt.ChiPhi ?? 0,
            DonViSua = bt.DonViSuaChua,
            TrangThai = bt.TrangThai
        }).ToList();

        return vm;
    }
}
