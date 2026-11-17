using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyCSVC_PhuocHoa.Data;
using QuanLyCSVC_PhuocHoa.Helpers;
using System.Security.Claims;

namespace QuanLyCSVC_PhuocHoa.Controllers;

[Authorize]
public class PhongController : Controller
{
    private readonly ApplicationDbContext _context;

    public PhongController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var query = _context.Phong
            .Include(p => p.LoaiPhong)
            .Include(p => p.NguoiPhuTrach)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.TenPhong.Contains(search) || p.MaPhong.Contains(search));

        ViewBag.Search = search;
        var pagedItems = await PaginatedList<Phong>.CreateAsync(query.OrderBy(p => p.MaPhong), page, 15);
        return View(pagedItems);
    }

    public async Task<IActionResult> Details(int id)
    {
        var phong = await _context.Phong
            .Include(p => p.LoaiPhong)
            .Include(p => p.NguoiPhuTrach)
            .Include(p => p.ThietBi).ThenInclude(t => t.LoaiThietBi)
            .Include(p => p.ThietBi).ThenInclude(t => t.BoMon)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (phong == null) return NotFound();
        return View(phong);
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.NhanVienThietBi}")]
    public async Task<IActionResult> Create()
    {
        await LoadSelectLists();
        return View(new Phong { TrangThai = "Đang sử dụng" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.NhanVienThietBi}")]
    public async Task<IActionResult> Create(Phong phong)
    {
        if (await _context.Phong.AnyAsync(p => p.MaPhong == phong.MaPhong))
            ModelState.AddModelError(nameof(phong.MaPhong), "Mã phòng đã tồn tại.");

        ModelState.Remove("LoaiPhong");
        if (!ModelState.IsValid)
        {
            await LoadSelectLists(phong.LoaiPhongId, phong.NguoiPhuTrachId);
            return View(phong);
        }

        _context.Phong.Add(phong);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Thêm phòng thành công.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.NhanVienThietBi}")]
    public async Task<IActionResult> Edit(int id)
    {
        var phong = await _context.Phong.FindAsync(id);
        if (phong == null) return NotFound();
        await LoadSelectLists(phong.LoaiPhongId, phong.NguoiPhuTrachId);
        return View(phong);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.NhanVienThietBi}")]
    public async Task<IActionResult> Edit(int id, Phong phong)
    {
        if (id != phong.Id) return NotFound();
        if (await _context.Phong.AnyAsync(p => p.MaPhong == phong.MaPhong && p.Id != id))
            ModelState.AddModelError(nameof(phong.MaPhong), "Mã phòng đã tồn tại.");

        ModelState.Remove("LoaiPhong");
        if (!ModelState.IsValid)
        {
            await LoadSelectLists(phong.LoaiPhongId, phong.NguoiPhuTrachId);
            return View(phong);
        }

        _context.Phong.Update(phong);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Cập nhật phòng thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.NhanVienThietBi}")]
    public async Task<IActionResult> Delete(int id)
    {
        var phong = await _context.Phong
            .Include(p => p.ThietBi)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (phong == null) return NotFound();

        if (phong.ThietBi.Any(t => !t.IsDeleted))
        {
            TempData["Error"] = "Không thể xóa phòng đang có thiết bị.";
            return RedirectToAction(nameof(Index));
        }

        bool hasDieuChuyen = await _context.DieuChuyen.AnyAsync(d => d.PhongDiId == id || d.PhongDenId == id);
        if (hasDieuChuyen)
        {
            TempData["Error"] = "Không thể xóa phòng vì đã có lịch sử điều chuyển thiết bị liên quan đến phòng này. Bạn có thể đổi trạng thái phòng sang 'Tạm đóng cửa'.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            _context.Phong.Remove(phong);
            await _context.SaveChangesAsync();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                await AuditLogHelper.LogAsync(_context, userId, $"Xóa phòng: {phong.TenPhong} ({phong.MaPhong})", "Phòng", HttpContext.Connection.RemoteIpAddress?.ToString());
            }

            TempData["Success"] = "Xóa phòng thành công.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "Không thể xóa phòng này do có dữ liệu liên quan trong hệ thống. Vui lòng chuyển trạng thái phòng sang 'Tạm đóng cửa'.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadSelectLists(int? loaiPhongId = null, int? nguoiPhuTrachId = null)
    {
        ViewBag.LoaiPhongId = new SelectList(
            await _context.LoaiPhong.OrderBy(l => l.TenLoaiPhong).ToListAsync(), "Id", "TenLoaiPhong", loaiPhongId);
        ViewBag.NguoiPhuTrachId = new SelectList(
            await _context.NhanVien.Where(n => n.TrangThai).OrderBy(n => n.HoTen).ToListAsync(),
            "Id", "HoTen", nguoiPhuTrachId);
        ViewBag.TrangThais = new List<string> { "Đang sử dụng", "Đang sửa chữa", "Tạm đóng cửa" };
    }
}
