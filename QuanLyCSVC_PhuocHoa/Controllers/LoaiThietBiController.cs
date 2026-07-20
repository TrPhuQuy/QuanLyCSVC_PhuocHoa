using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyCSVC_PhuocHoa.Data;

namespace QuanLyCSVC_PhuocHoa.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.NhanVienThietBi}")]
public class LoaiThietBiController : Controller
{
    private readonly ApplicationDbContext _context;

    public LoaiThietBiController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index() =>
        View(await _context.LoaiThietBi.OrderBy(l => l.TenLoaiTB).ToListAsync());

    public IActionResult Create() => View(new LoaiThietBi());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LoaiThietBi model)
    {
        if (!ModelState.IsValid) return View(model);

        _context.LoaiThietBi.Add(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Thêm loại thiết bị thành công.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _context.LoaiThietBi.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LoaiThietBi model)
    {
        if (id != model.Id) return NotFound();
        if (!ModelState.IsValid) return View(model);

        _context.LoaiThietBi.Update(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Cập nhật loại thiết bị thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.LoaiThietBi
            .Include(l => l.ThietBi)
            .FirstOrDefaultAsync(l => l.Id == id);
        if (item == null) return NotFound();
        if (item.ThietBi.Any(t => !t.IsDeleted))
        {
            TempData["Error"] = "Không thể xóa loại đang có thiết bị.";
            return RedirectToAction(nameof(Index));
        }
        _context.LoaiThietBi.Remove(item);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Xóa loại thiết bị thành công.";
        return RedirectToAction(nameof(Index));
    }
}
