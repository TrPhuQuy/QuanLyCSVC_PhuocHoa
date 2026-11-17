using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyCSVC_PhuocHoa.Data;
using QuanLyCSVC_PhuocHoa.Helpers;
using QuanLyCSVC_PhuocHoa.Models;
using QuanLyCSVC_PhuocHoa.Models.Entities;

namespace QuanLyCSVC_PhuocHoa.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.NhanVienThietBi}")]
public class PhieuThanhLyController : Controller
{
    private readonly ApplicationDbContext _context;

    public PhieuThanhLyController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index(string? search, string? trangThai, int page = 1)
    {
        var query = _context.PhieuThanhLy
            .Include(p => p.ThietBi)
            .Include(p => p.ThanhVienHoiDong).ThenInclude(tv => tv.NhanVien)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.MaThanhLy.Contains(search) || p.ThietBi.TenTb.Contains(search));

        if (!string.IsNullOrWhiteSpace(trangThai))
            query = query.Where(p => p.TrangThai == trangThai);

        ViewBag.Search = search;
        ViewBag.TrangThai = trangThai;
        ViewBag.TrangThais = new List<string> { "Đề nghị", "Đã duyệt", "Đã hủy" };

        var pagedItems = await PaginatedList<PhieuThanhLy>.CreateAsync(query.OrderByDescending(p => p.NgayDeNghi), page, 15);
        return View(pagedItems);
    }

    public async Task<IActionResult> Details(int id)
    {
        var item = await _context.PhieuThanhLy
            .Include(p => p.ThietBi).ThenInclude(t => t.Phong)
            .Include(p => p.ThanhVienHoiDong).ThenInclude(tv => tv.NhanVien)
            .FirstOrDefaultAsync(p => p.Id == id);
        return item == null ? NotFound() : View(item);
    }

    public async Task<IActionResult> Create()
    {
        await LoadSelectLists();
        var model = new PhieuThanhLy
        {
            MaThanhLy = $"TL-{DateTime.Now:yyyyMMdd}-{DateTime.Now:HHmmss}",
            NgayDeNghi = DateOnly.FromDateTime(DateTime.Now),
            TrangThai = "Đề nghị"
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PhieuThanhLy model, int[] thanhVienIds, string[] vaiTros)
    {
        var thietBi = await _context.ThietBi.FindAsync(model.ThietBiId);
        if (thietBi == null)
            ModelState.AddModelError(nameof(model.ThietBiId), "Thiết bị không tồn tại.");

        ModelState.Remove("ThietBi");
        ModelState.Remove("ThanhVienHoiDong");

        if (!ModelState.IsValid)
        {
            await LoadSelectLists(model.ThietBiId);
            return View(model);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.PhieuThanhLy.Add(model);
            await _context.SaveChangesAsync();

            // Thêm thành viên hội đồng
            if (thanhVienIds != null && thanhVienIds.Length > 0)
            {
                for (int i = 0; i < thanhVienIds.Length; i++)
                {
                    if (thanhVienIds[i] <= 0) continue;
                    _context.ThanhVienHoiDong.Add(new ThanhVienHoiDong
                    {
                        PhieuThanhLyId = model.Id,
                        NhanVienId = thanhVienIds[i],
                        VaiTro = (vaiTros != null && i < vaiTros.Length && !string.IsNullOrWhiteSpace(vaiTros[i])) ? vaiTros[i] : "Thành viên"
                    });
                }
            }

            await _context.SaveChangesAsync();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                await AuditLogHelper.LogAsync(_context, userId, $"Lập đề nghị thanh lý: {model.MaThanhLy} ({thietBi?.TenTb})", "Thanh lý", HttpContext.Connection.RemoteIpAddress?.ToString());
            }

            await transaction.CommitAsync();

            TempData["Success"] = $"Đã lập phiếu đề nghị thanh lý {model.MaThanhLy}.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            TempData["Error"] = "Có lỗi xảy ra khi tạo phiếu đề nghị thanh lý: " + ex.Message;
            await LoadSelectLists(model.ThietBiId);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Approve(int id, string soQuyetDinh, int? soLuongThanhLy)
    {
        var item = await _context.PhieuThanhLy.Include(p => p.ThietBi).FirstOrDefaultAsync(p => p.Id == id);
        if (item == null) return NotFound();

        if (item.TrangThai != "Đề nghị")
        {
            TempData["Error"] = "Phiếu này đã được xử lý trước đó.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (string.IsNullOrWhiteSpace(soQuyetDinh))
        {
            TempData["Error"] = "Vui lòng nhập số quyết định thanh lý.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (item.ThietBi == null || item.ThietBi.IsDeleted)
        {
            TempData["Error"] = "Thiết bị không tồn tại hoặc đã bị xóa.";
            return RedirectToAction(nameof(Details), new { id });
        }

        int slTl = soLuongThanhLy ?? item.ThietBi.SoLuong;
        if (slTl <= 0)
        {
            TempData["Error"] = "Số lượng thanh lý phải lớn hơn 0.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (slTl > item.ThietBi.SoLuong)
        {
            TempData["Error"] = $"Số lượng thanh lý ({slTl}) vượt quá số lượng tồn kho hiện tại ({item.ThietBi.SoLuong}).";
            return RedirectToAction(nameof(Details), new { id });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            item.TrangThai = "Đã duyệt";
            item.NgayQuyetDinh = DateOnly.FromDateTime(DateTime.Now);
            item.SoQuyetDinh = soQuyetDinh.Trim();

            if (slTl >= item.ThietBi.SoLuong)
            {
                // Thanh lý toàn bộ: Đặt số lượng = 0 và tình trạng = Thanh lý (vẫn giữ bản ghi trong hệ thống để tra cứu lịch sử tài sản)
                item.ThietBi.SoLuong = 0;
                item.ThietBi.TinhTrang = "Thanh lý";
                item.ThietBi.GhiChu = $"[ĐÃ THANH LÝ TOÀN BỘ]: QĐ {soQuyetDinh} ngày {DateTime.Now:dd/MM/yyyy}. {item.ThietBi.GhiChu}";
                _context.ThietBi.Update(item.ThietBi);
            }
            else
            {
                // Thanh lý một phần số lượng, giữ lại số lượng còn lại tiếp tục sử dụng
                item.ThietBi.SoLuong -= slTl;
                item.ThietBi.GhiChu = $"[THANH LÝ {slTl} {item.ThietBi.DonViTinh}]: Theo QĐ {soQuyetDinh}. Còn lại {item.ThietBi.SoLuong} {item.ThietBi.DonViTinh}. {item.ThietBi.GhiChu}";
                _context.ThietBi.Update(item.ThietBi);
            }

            _context.PhieuThanhLy.Update(item);
            await _context.SaveChangesAsync();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                await AuditLogHelper.LogAsync(_context, userId, $"Duyệt thanh lý: {item.MaThanhLy} - QĐ: {soQuyetDinh}", "Thanh lý", HttpContext.Connection.RemoteIpAddress?.ToString());
            }

            await transaction.CommitAsync();

            TempData["Success"] = "Đã duyệt thanh lý thành công. Trạng thái và số lượng thiết bị đã được cập nhật.";
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            TempData["Error"] = "Có lỗi xảy ra khi duyệt thanh lý: " + ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Reject(int id, string? lyDoHuy)
    {
        var item = await _context.PhieuThanhLy.FindAsync(id);
        if (item == null) return NotFound();

        if (item.TrangThai != "Đề nghị")
        {
            TempData["Error"] = "Phiếu này đã được xử lý trước đó.";
            return RedirectToAction(nameof(Details), new { id });
        }

        item.TrangThai = "Đã hủy";
        if (!string.IsNullOrWhiteSpace(lyDoHuy))
        {
            item.LyDo = $"{item.LyDo} [LÝ DO HỦY: {lyDoHuy}]";
        }

        _context.PhieuThanhLy.Update(item);
        await _context.SaveChangesAsync();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
        {
            await AuditLogHelper.LogAsync(_context, userId, $"Hủy đề nghị thanh lý: {item.MaThanhLy}", "Thanh lý", HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        TempData["Success"] = "Đã hủy đề nghị thanh lý.";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// In Biên bản họp Hội đồng kiểm tra và thẩm định thanh lý tài sản
    /// </summary>
    public async Task<IActionResult> PrintLiquidation(int id)
    {
        var item = await _context.PhieuThanhLy
            .Include(p => p.ThietBi).ThenInclude(t => t.Phong)
            .Include(p => p.ThietBi).ThenInclude(t => t.LoaiThietBi)
            .Include(p => p.ThanhVienHoiDong).ThenInclude(tv => tv.NhanVien)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (item == null) return NotFound();

        return View(item);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _context.PhieuThanhLy.FindAsync(id);
        if (item == null) return NotFound();
        await LoadSelectLists(item.ThietBiId);
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PhieuThanhLy model)
    {
        if (id != model.Id) return NotFound();

        ModelState.Remove("ThietBi");
        ModelState.Remove("ThanhVienHoiDong");

        if (!ModelState.IsValid)
        {
            await LoadSelectLists(model.ThietBiId);
            return View(model);
        }

        _context.PhieuThanhLy.Update(model);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Cập nhật phiếu đề nghị thanh lý thành công.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadSelectLists(int? thietBiId = null)
    {
        ViewBag.ThietBiId = new SelectList(
            await _context.ThietBi.Where(t => !t.IsDeleted).OrderBy(t => t.TenTb)
                .Select(t => new { t.Id, Display = $"{t.TenTb} ({t.MaTb}) - {t.TinhTrang}" }).ToListAsync(),
            "Id", "Display", thietBiId);
        ViewBag.NhanViens = await _context.NhanVien.Where(n => n.TrangThai).OrderBy(n => n.HoTen).ToListAsync();
        ViewBag.TrangThais = new List<string> { "Đề nghị", "Đã duyệt", "Đã hủy" };
    }
}
