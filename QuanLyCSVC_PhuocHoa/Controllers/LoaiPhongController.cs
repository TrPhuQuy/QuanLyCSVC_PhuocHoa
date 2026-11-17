using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyCSVC_PhuocHoa.Data;
using QuanLyCSVC_PhuocHoa.Models;
using QuanLyCSVC_PhuocHoa.Models.Entities;

namespace QuanLyCSVC_PhuocHoa.Controllers;

[Authorize]
public class LoaiPhongController : Controller
{
    private readonly ApplicationDbContext _context;

    public LoaiPhongController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index(string? search)
    {
        var query = _context.LoaiPhong.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(l => l.TenLoaiPhong.Contains(search) || (l.MoTa != null && l.MoTa.Contains(search)));
        }

        ViewBag.Search = search;
        return View(await query.OrderBy(l => l.TenLoaiPhong).ToListAsync());
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.NhanVienThietBi}")]
    public IActionResult Create() => View(new LoaiPhong());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.NhanVienThietBi}")]
    public async Task<IActionResult> Create(LoaiPhong model)
    {
        if (await _context.LoaiPhong.AnyAsync(l => l.TenLoaiPhong == model.TenLoaiPhong))
            ModelState.AddModelError(nameof(model.TenLoaiPhong), "Tên loại phòng đã tồn tại.");

        if (!ModelState.IsValid) return View(model);

        _context.LoaiPhong.Add(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Thêm loại phòng thành công.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.NhanVienThietBi}")]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _context.LoaiPhong.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.NhanVienThietBi}")]
    public async Task<IActionResult> Edit(int id, LoaiPhong model)
    {
        if (id != model.Id) return NotFound();

        if (await _context.LoaiPhong.AnyAsync(l => l.TenLoaiPhong == model.TenLoaiPhong && l.Id != id))
            ModelState.AddModelError(nameof(model.TenLoaiPhong), "Tên loại phòng đã tồn tại.");

        if (!ModelState.IsValid) return View(model);

        _context.LoaiPhong.Update(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Cập nhật loại phòng thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.NhanVienThietBi}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.LoaiPhong
            .Include(l => l.Phong)
            .FirstOrDefaultAsync(l => l.Id == id);
        if (item == null) return NotFound();

        if (item.Phong.Any())
        {
            TempData["Error"] = "Không thể xóa loại phòng đang được sử dụng cho các phòng học/phòng chức năng.";
            return RedirectToAction(nameof(Index));
        }

        _context.LoaiPhong.Remove(item);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Xóa loại phòng thành công.";
        return RedirectToAction(nameof(Index));
    }
}
