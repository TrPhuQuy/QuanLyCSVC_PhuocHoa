using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyCSVC_PhuocHoa.Data;
using QuanLyCSVC_PhuocHoa.Models.ViewModels;

namespace QuanLyCSVC_PhuocHoa.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var thietBis = await _context.ThietBi
            .Include(t => t.LoaiThietBi)
            .Where(t => !t.IsDeleted)
            .ToListAsync();

        var vm = new DashboardViewModel
        {
            TongThietBi      = thietBis.Sum(t => t.SoLuong),
            TongPhong        = await _context.Phong.CountAsync(p => p.TrangThai == "Đang sử dụng"),
            ThietBiTot       = thietBis.Where(t => t.TinhTrang == "Đang sử dụng").Sum(t => t.SoLuong),
            ThietBiHuHong    = thietBis.Where(t => t.TinhTrang == "Hư hỏng").Sum(t => t.SoLuong),
            ThietBiDangSua   = thietBis.Where(t => t.TinhTrang == "Đang sửa").Sum(t => t.SoLuong),
            ThietBiThanhLy   = thietBis.Where(t => t.TinhTrang == "Thanh lý").Sum(t => t.SoLuong),
            TongGiaTriTaiSan = thietBis.Where(t => t.TinhTrang != "Thanh lý").Sum(t => (t.GiaMua ?? 0) * t.SoLuong),
            PhieuMuonChoDuyet          = await _context.PhieuMuon.CountAsync(p => p.TrangThai == "Chờ duyệt"),
            PhieuDieuChuyenChoDuyet    = await _context.DieuChuyen.CountAsync(p => p.TrangThai == "Chờ duyệt"),
            ThongKeTheoLoais = thietBis
                .GroupBy(t => t.LoaiThietBi.TenLoaiTB)
                .Select(g => new ThongKeTheoLoai { TenLoai = g.Key, SoLuong = g.Sum(x => x.SoLuong) })
                .OrderByDescending(x => x.SoLuong)
                .ToList()
        };

        return View(vm);
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}
