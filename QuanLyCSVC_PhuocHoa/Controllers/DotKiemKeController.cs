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
public class DotKiemKeController : Controller
{
    private readonly ApplicationDbContext _context;

    public DotKiemKeController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index(string? trangThai, string? namHoc, int page = 1)
    {
        var query = _context.DotKiemKe
            .Include(d => d.NguoiChuTri)
            .Include(d => d.ChiTietKiemKe)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(trangThai))
            query = query.Where(d => d.TrangThai == trangThai);

        if (!string.IsNullOrWhiteSpace(namHoc))
            query = query.Where(d => d.NamHoc == namHoc);

        ViewBag.TrangThai = trangThai;
        ViewBag.NamHoc = namHoc;
        ViewBag.TrangThais = new List<string> { "Đang thực hiện", "Hoàn thành" };
        ViewBag.NamHocs = await _context.DotKiemKe.Select(d => d.NamHoc).Distinct().OrderByDescending(n => n).ToListAsync();

        var pagedItems = await PaginatedList<DotKiemKe>.CreateAsync(query.OrderByDescending(d => d.NgayBatDau), page, 15);
        return View(pagedItems);
    }

    public async Task<IActionResult> Details(int id)
    {
        var item = await _context.DotKiemKe
            .Include(d => d.NguoiChuTri)
            .Include(d => d.ChiTietKiemKe).ThenInclude(ct => ct.ThietBi).ThenInclude(t => t.Phong)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (item == null) return NotFound();

        ViewBag.ThietBis = await _context.ThietBi
            .Include(t => t.Phong)
            .Where(t => !t.IsDeleted)
            .OrderBy(t => t.TenTb)
            .ToListAsync();

        return View(item);
    }

    /// <summary>
    /// In Biên bản kiểm kê tài sản cố định chuẩn Mẫu số C53-HD (Thông tư 107/2017/TT-BTC)
    /// </summary>
    public async Task<IActionResult> PrintC53(int id)
    {
        var item = await _context.DotKiemKe
            .Include(d => d.NguoiChuTri)
            .Include(d => d.ChiTietKiemKe).ThenInclude(ct => ct.ThietBi).ThenInclude(t => t.Phong)
            .Include(d => d.ChiTietKiemKe).ThenInclude(ct => ct.ThietBi).ThenInclude(t => t.LoaiThietBi)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (item == null) return NotFound();

        return View(item);
    }

    public async Task<IActionResult> Create()
    {
        await LoadSelectLists();
        var model = new DotKiemKe
        {
            MaDotKiemKe = $"KK-{DateTime.Now:yyyyMMdd}",
            NgayBatDau = DateOnly.FromDateTime(DateTime.Now),
            NgayKetThuc = DateOnly.FromDateTime(DateTime.Now.AddDays(7)),
            NamHoc = GetCurrentNamHoc(),
            TrangThai = "Đang thực hiện"
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DotKiemKe model)
    {
        ModelState.Remove("NguoiChuTri");
        ModelState.Remove("ChiTietKiemKe");

        if (!ModelState.IsValid)
        {
            await LoadSelectLists(model.NguoiChuTriId);
            return View(model);
        }

        _context.DotKiemKe.Add(model);
        await _context.SaveChangesAsync();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
        {
            await AuditLogHelper.LogAsync(_context, userId, $"Khởi tạo đợt kiểm kê: {model.MaDotKiemKe}", "Kiểm kê", HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        TempData["Success"] = $"Đã tạo đợt kiểm kê {model.MaDotKiemKe}. Bắt đầu nhập kết quả kiểm kê.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveKiemKe(int id, int[] thietBiIds, int[] soLuongSoSachs, int[] soLuongThucTes, string[] phanLoais, string[] deXuatXuLys)
    {
        var dot = await _context.DotKiemKe.Include(d => d.ChiTietKiemKe).FirstOrDefaultAsync(d => d.Id == id);
        if (dot == null) return NotFound();

        if (dot.TrangThai == "Hoàn thành")
        {
            TempData["Error"] = "Đợt kiểm kê này đã hoàn thành, không thể sửa đổi kết quả.";
            return RedirectToAction(nameof(Details), new { id });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Xóa chi tiết cũ và tạo lại
            _context.ChiTietKiemKe.RemoveRange(dot.ChiTietKiemKe);

            if (thietBiIds != null && thietBiIds.Length > 0)
            {
                for (int i = 0; i < thietBiIds.Length; i++)
                {
                    int ssl = (soLuongSoSachs != null && i < soLuongSoSachs.Length) ? soLuongSoSachs[i] : 0;
                    int stt = (soLuongThucTes != null && i < soLuongThucTes.Length) ? soLuongThucTes[i] : 0;
                    _context.ChiTietKiemKe.Add(new ChiTietKiemKe
                    {
                        DotKiemKeId = id,
                        ThietBiId = thietBiIds[i],
                        SoLuongSoSach = ssl,
                        SoLuongThucTe = stt,
                        ChenhLech = stt - ssl,
                        PhanLoai = (phanLoais != null && i < phanLoais.Length) ? phanLoais[i] : null,
                        DeXuatXuLy = (deXuatXuLys != null && i < deXuatXuLys.Length) ? deXuatXuLys[i] : null
                    });
                }
            }

            await _context.SaveChangesAsync();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                await AuditLogHelper.LogAsync(_context, userId, $"Lưu kết quả kiểm kê đợt: {dot.MaDotKiemKe} ({(thietBiIds?.Length ?? 0)} thiết bị)", "Kiểm kê", HttpContext.Connection.RemoteIpAddress?.ToString());
            }

            await transaction.CommitAsync();
            TempData["Success"] = "Đã lưu kết quả kiểm kê.";
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            TempData["Error"] = "Có lỗi xảy ra khi lưu kết quả kiểm kê: " + ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        var dot = await _context.DotKiemKe.FindAsync(id);
        if (dot == null) return NotFound();

        dot.TrangThai = "Hoàn thành";
        await _context.SaveChangesAsync();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
        {
            await AuditLogHelper.LogAsync(_context, userId, $"Hoàn tất đợt kiểm kê: {dot.MaDotKiemKe}", "Kiểm kê", HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        TempData["Success"] = "Đã hoàn tất đợt kiểm kê.";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Cân đối số lượng sổ sách theo số lượng thực tế kiểm kê được (áp dụng cho các thiết bị có chênh lệch)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> ApplyAdjustment(int id)
    {
        var dot = await _context.DotKiemKe
            .Include(d => d.ChiTietKiemKe).ThenInclude(ct => ct.ThietBi)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (dot == null) return NotFound();
        if (dot.TrangThai != "Hoàn thành")
        {
            TempData["Error"] = "Chỉ có thể cân đối tồn kho khi đợt kiểm kê đã ở trạng thái 'Hoàn thành'.";
            return RedirectToAction(nameof(Details), new { id });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            int updatedCount = 0;
            foreach (var ct in dot.ChiTietKiemKe)
            {
                if (ct.ChenhLech != 0 && ct.ThietBi != null && !ct.ThietBi.IsDeleted)
                {
                    ct.ThietBi.SoLuong = ct.SoLuongThucTe;
                    ct.ThietBi.GhiChu = $"[ĐIỀU CHỈNH KIỂM KÊ {dot.MaDotKiemKe}]: Tồn cũ {ct.SoLuongSoSach} -> Tồn thực tế {ct.SoLuongThucTe}. {ct.ThietBi.GhiChu}";
                    _context.ThietBi.Update(ct.ThietBi);
                    updatedCount++;
                }
            }

            await _context.SaveChangesAsync();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                await AuditLogHelper.LogAsync(_context, userId, $"Cân đối số lượng thực tế đợt kiểm kê {dot.MaDotKiemKe} cho {updatedCount} thiết bị", "Kiểm kê", HttpContext.Connection.RemoteIpAddress?.ToString());
            }

            await transaction.CommitAsync();
            TempData["Success"] = $"Đã cân đối số lượng sổ sách theo thực tế thành công cho {updatedCount} thiết bị có chênh lệch.";
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            TempData["Error"] = "Có lỗi xảy ra khi cân đối số lượng: " + ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private static string GetCurrentNamHoc()
    {
        var now = DateTime.Now;
        int startYear = now.Month >= 9 ? now.Year : now.Year - 1;
        return $"{startYear}-{startYear + 1}";
    }

    private async Task LoadSelectLists(int? nguoiChuTriId = null)
    {
        ViewBag.NguoiChuTriId = new SelectList(
            await _context.NhanVien.Where(n => n.TrangThai).OrderBy(n => n.HoTen).ToListAsync(),
            "Id", "HoTen", nguoiChuTriId);
        ViewBag.LoaiKiemKes = new List<string> { "Kiểm kê định kỳ", "Kiểm kê đột xuất", "Kiểm kê cuối năm" };
        ViewBag.ThietBis = await _context.ThietBi.Where(t => !t.IsDeleted)
            .Include(t => t.Phong)
            .OrderBy(t => t.TenTb).ToListAsync();
    }
}
