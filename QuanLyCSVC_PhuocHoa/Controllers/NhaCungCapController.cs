using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyCSVC_PhuocHoa.Data;
using QuanLyCSVC_PhuocHoa.Models.Entities;
using QuanLyCSVC_PhuocHoa.Helpers;

namespace QuanLyCSVC_PhuocHoa.Controllers;

[Authorize]
public class NhaCungCapController : Controller
{
    private readonly ApplicationDbContext _context;

    public NhaCungCapController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var query = _context.NhaCungCap.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(n => n.TenNCC.Contains(search) || n.MaNCC.Contains(search) || (n.NguoiDaiDien != null && n.NguoiDaiDien.Contains(search)));

        ViewBag.Search = search;
        var pagedItems = await PaginatedList<NhaCungCap>.CreateAsync(query.OrderBy(n => n.MaNCC), page, 15);
        return View(pagedItems);
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.NhanVienThietBi}")]
    public IActionResult Create() => View(new NhaCungCap { TrangThai = true });

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.NhanVienThietBi}")]
    public async Task<IActionResult> Create(NhaCungCap model)
    {
        if (await _context.NhaCungCap.AnyAsync(n => n.MaNCC == model.MaNCC))
            ModelState.AddModelError(nameof(model.MaNCC), "Mã nhà cung cấp đã tồn tại.");

        if (!ModelState.IsValid) return View(model);

        _context.NhaCungCap.Add(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Thêm nhà cung cấp thành công.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.NhanVienThietBi}")]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _context.NhaCungCap.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.NhanVienThietBi}")]
    public async Task<IActionResult> Edit(int id, NhaCungCap model)
    {
        if (id != model.Id) return NotFound();
        
        if (await _context.NhaCungCap.AnyAsync(n => n.MaNCC == model.MaNCC && n.Id != id))
            ModelState.AddModelError(nameof(model.MaNCC), "Mã nhà cung cấp đã tồn tại.");

        if (!ModelState.IsValid) return View(model);

        _context.NhaCungCap.Update(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Cập nhật nhà cung cấp thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.NhanVienThietBi}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.NhaCungCap
            .Include(n => n.PhieuNhap)
            .Include(n => n.ThietBi)
            .FirstOrDefaultAsync(n => n.Id == id);
        if (item == null) return NotFound();

        if (item.PhieuNhap.Any() || item.ThietBi.Any(t => !t.IsDeleted))
        {
            TempData["Error"] = "Không thể xóa nhà cung cấp đang có phiếu nhập hoặc thiết bị liên kết.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            _context.NhaCungCap.Remove(item);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Xóa nhà cung cấp thành công.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "Không thể xóa nhà cung cấp này do có dữ liệu liên quan trong hệ thống.";
        }
        return RedirectToAction(nameof(Index));
    }
}
