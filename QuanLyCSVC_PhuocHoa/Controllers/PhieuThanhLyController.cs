using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyCSVC_PhuocHoa.Data;
using QuanLyCSVC_PhuocHoa.Models;
using QuanLyCSVC_PhuocHoa.Models.Entities;

namespace QuanLyCSVC_PhuocHoa.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.NhanVienThietBi}")]
public class PhieuThanhLyController : Controller
{
    private readonly ApplicationDbContext _context;

    public PhieuThanhLyController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index(string? search, string? trangThai)
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

        return View(await query.OrderByDescending(p => p.NgayDeNghi).ToListAsync());
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

        _context.PhieuThanhLy.Add(model);
        await _context.SaveChangesAsync();

        // Thêm thành viên hội đồng
        for (int i = 0; i < thanhVienIds.Length; i++)
        {
            _context.ThanhVienHoiDong.Add(new ThanhVienHoiDong
            {
                PhieuThanhLyId = model.Id,
                NhanVienId = thanhVienIds[i],
                VaiTro = i < vaiTros.Length ? vaiTros[i] : "Thành viên"
            });
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã lập phiếu đề nghị thanh lý {model.MaThanhLy}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Approve(int id, string soQuyetDinh)
    {
        var item = await _context.PhieuThanhLy.Include(p => p.ThietBi).FirstOrDefaultAsync(p => p.Id == id);
        if (item == null) return NotFound();

        item.TrangThai = "Đã duyệt";
        item.NgayQuyetDinh = DateOnly.FromDateTime(DateTime.Now);
        item.SoQuyetDinh = soQuyetDinh;

        // Khi thanh lý được duyệt → ẩn thiết bị
        if (item.ThietBi != null)
        {
            item.ThietBi.TinhTrang = "Đã thanh lý";
            item.ThietBi.IsDeleted = true;
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "Đã duyệt thanh lý. Thiết bị đã được ghi nhận thanh lý.";
        return RedirectToAction(nameof(Details), new { id });
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
