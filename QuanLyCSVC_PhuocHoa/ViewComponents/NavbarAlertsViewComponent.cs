using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyCSVC_PhuocHoa.Data;
using QuanLyCSVC_PhuocHoa.Models.ViewModels;

namespace QuanLyCSVC_PhuocHoa.ViewComponents;

/// <summary>
/// Hiển thị badge số phiếu đang chờ duyệt (Mượn, Điều chuyển, Thanh lý)
/// và danh sách phiếu mượn thiết bị đã quá hạn trả trên thanh navbar.
/// Chỉ tính toán cho Admin / Nhân viên thiết bị vì đây là người xử lý duyệt phiếu.
/// </summary>
public class NavbarAlertsViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;

    public NavbarAlertsViewComponent(ApplicationDbContext context) => _context = context;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var isStaff = HttpContext.User.IsInRole(Models.AppRoles.Admin)
                   || HttpContext.User.IsInRole(Models.AppRoles.NhanVienThietBi);

        if (!isStaff)
            return View(new NavbarAlertsViewModel());

        var today = DateOnly.FromDateTime(DateTime.Now);

        var soPhieuMuonChoDuyet = await _context.PhieuMuon.CountAsync(p => p.TrangThai == "Chờ duyệt");
        var soDieuChuyenChoDuyet = await _context.DieuChuyen.CountAsync(p => p.TrangThai == "Chờ duyệt");
        var soThanhLyChoDuyet = await _context.PhieuThanhLy.CountAsync(p => p.TrangThai == "Đề nghị");

        var quaHan = await _context.PhieuMuon
            .Include(p => p.GiaoVien)
            .Where(p => p.TrangThai == "Đang mượn" && p.NgayTraDuKien < today)
            .OrderBy(p => p.NgayTraDuKien)
            .Take(10)
            .Select(p => new PhieuMuonQuaHan
            {
                Id = p.Id,
                MaPhieuMuon = p.MaPhieuMuon,
                TenGiaoVien = p.GiaoVien.HoTen,
                NgayTraDuKien = p.NgayTraDuKien,
                SoNgayTre = today.DayNumber - p.NgayTraDuKien.DayNumber
            })
            .ToListAsync();

        var vm = new NavbarAlertsViewModel
        {
            SoPhieuChoDuyet = soPhieuMuonChoDuyet + soDieuChuyenChoDuyet + soThanhLyChoDuyet,
            SoPhieuMuonChoDuyet = soPhieuMuonChoDuyet,
            SoDieuChuyenChoDuyet = soDieuChuyenChoDuyet,
            SoThanhLyChoDuyet = soThanhLyChoDuyet,
            DanhSachQuaHan = quaHan
        };

        return View(vm);
    }
}