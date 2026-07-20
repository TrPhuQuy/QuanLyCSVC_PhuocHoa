using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyCSVC_PhuocHoa.Data;
using QuanLyCSVC_PhuocHoa.Models;
using QuanLyCSVC_PhuocHoa.Models.Entities;

namespace QuanLyCSVC_PhuocHoa.Controllers;

[Authorize]
public class PhieuBaoTriController : Controller
{
    private readonly ApplicationDbContext _context;

    public PhieuBaoTriController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index(string? search, string? trangThai)
    {
        var query = _context.PhieuBaoTri
            .Include(p => p.ThietBi)
            .Include(p => p.NguoiGui)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.MaBaoTri.Contains(search) || p.ThietBi.TenTb.Contains(search) || p.NguoiGui.HoTen.Contains(search));

        if (!string.IsNullOrWhiteSpace(trangThai))
            query = query.Where(p => p.TrangThai == trangThai);

        ViewBag.Search = search;
        ViewBag.TrangThai = trangThai;
        ViewBag.TrangThais = new List<string> { "Đang sửa", "Hoàn thành", "Không sửa được" };

        return View(await query.OrderByDescending(p => p.NgayGui).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var item = await _context.PhieuBaoTri
            .Include(p => p.ThietBi).ThenInclude(t => t.Phong)
            .Include(p => p.NguoiGui)
            .FirstOrDefaultAsync(p => p.Id == id);
        return item == null ? NotFound() : View(item);
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.NhanVienThietBi}")]
    public async Task<IActionResult> Create()
    {
        await LoadSelectLists();
        var model = new PhieuBaoTri
        {
            MaBaoTri = $"BT-{DateTime.Now:yyyyMMdd}-{DateTime.Now:HHmmss}",
            NgayGui = DateOnly.FromDateTime(DateTime.Now),
            TrangThai = "Đang sửa"
        };

        var userEmail = User.Identity?.Name;
        var employee = await _context.NhanVien.FirstOrDefaultAsync(n => n.Email == userEmail);
        if (employee != null)
            model.NguoiGuiId = employee.Id;

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.NhanVienThietBi}")]
    public async Task<IActionResult> Create(PhieuBaoTri model)
    {
        // Tự động cập nhật tình trạng thiết bị sang "Đang sửa"
        var thietBi = await _context.ThietBi.FindAsync(model.ThietBiId);
        if (thietBi == null)
            ModelState.AddModelError(nameof(model.ThietBiId), "Thiết bị không tồn tại.");

        ModelState.Remove("ThietBi");
        ModelState.Remove("NguoiGui");

        if (!ModelState.IsValid)
        {
            await LoadSelectLists(model.ThietBiId, model.NguoiGuiId);
            return View(model);
        }

        // Cập nhật tình trạng thiết bị
        thietBi!.TinhTrang = "Đang sửa";
        _context.ThietBi.Update(thietBi);

        _context.PhieuBaoTri.Add(model);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Ghi nhận phiếu bảo trì thành công. Tình trạng thiết bị đã được cập nhật sang 'Đang sửa'.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.NhanVienThietBi}")]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _context.PhieuBaoTri.FindAsync(id);
        if (item == null) return NotFound();
        await LoadSelectLists(item.ThietBiId, item.NguoiGuiId);
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.NhanVienThietBi}")]
    public async Task<IActionResult> Edit(int id, PhieuBaoTri model)
    {
        if (id != model.Id) return NotFound();

        ModelState.Remove("ThietBi");
        ModelState.Remove("NguoiGui");

        if (!ModelState.IsValid)
        {
            await LoadSelectLists(model.ThietBiId, model.NguoiGuiId);
            return View(model);
        }

        // Nếu hoàn thành bảo trì → cập nhật lại tình trạng thiết bị sang "Đang sử dụng"
        if (model.TrangThai == "Hoàn thành" && model.NgayNhanLai.HasValue)
        {
            var thietBi = await _context.ThietBi.FindAsync(model.ThietBiId);
            if (thietBi != null)
            {
                thietBi.TinhTrang = "Đang sử dụng";
                _context.ThietBi.Update(thietBi);
            }
        }
        else if (model.TrangThai == "Không sửa được")
        {
            var thietBi = await _context.ThietBi.FindAsync(model.ThietBiId);
            if (thietBi != null)
            {
                thietBi.TinhTrang = "Hư hỏng";
                _context.ThietBi.Update(thietBi);
            }
        }

        _context.PhieuBaoTri.Update(model);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Cập nhật phiếu bảo trì thành công.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadSelectLists(int? thietBiId = null, int? nguoiGuiId = null)
    {
        ViewBag.ThietBiId = new SelectList(
            await _context.ThietBi.Where(t => !t.IsDeleted).OrderBy(t => t.TenTb)
                .Select(t => new { t.Id, Display = $"{t.TenTb} ({t.MaTb})" }).ToListAsync(),
            "Id", "Display", thietBiId);
        ViewBag.NguoiGuiId = new SelectList(
            await _context.NhanVien.Where(n => n.TrangThai).OrderBy(n => n.HoTen).ToListAsync(),
            "Id", "HoTen", nguoiGuiId);
        ViewBag.TrangThais = new List<string> { "Đang sửa", "Hoàn thành", "Không sửa được" };
    }
}
