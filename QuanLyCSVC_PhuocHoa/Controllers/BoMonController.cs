using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyCSVC_PhuocHoa.Data;
using QuanLyCSVC_PhuocHoa.Models.Entities;

namespace QuanLyCSVC_PhuocHoa.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.NhanVienThietBi}")]
public class BoMonController : Controller
{
    private readonly ApplicationDbContext _context;

    public BoMonController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index() =>
        View(await _context.BoMon.OrderBy(b => b.TenBoMon).ToListAsync());

    public IActionResult Create() => View(new BoMon());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BoMon model)
    {
        if (!ModelState.IsValid) return View(model);

        _context.BoMon.Add(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Thêm bộ môn thành công.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _context.BoMon.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BoMon model)
    {
        if (id != model.Id) return NotFound();
        if (!ModelState.IsValid) return View(model);

        _context.BoMon.Update(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Cập nhật bộ môn thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.BoMon
            .Include(b => b.NhanVien)
            .Include(b => b.ThietBi)
            .FirstOrDefaultAsync(b => b.Id == id);
        if (item == null) return NotFound();

        if (item.NhanVien.Any() || item.ThietBi.Any(t => !t.IsDeleted))
        {
            TempData["Error"] = "Không thể xóa bộ môn đang có nhân viên hoặc thiết bị hoạt động.";
            return RedirectToAction(nameof(Index));
        }

        _context.BoMon.Remove(item);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Xóa bộ môn thành công.";
        return RedirectToAction(nameof(Index));
    }
}
