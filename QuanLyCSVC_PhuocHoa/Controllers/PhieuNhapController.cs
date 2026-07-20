using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyCSVC_PhuocHoa.Data;
using QuanLyCSVC_PhuocHoa.Models;
using QuanLyCSVC_PhuocHoa.Models.Entities;

namespace QuanLyCSVC_PhuocHoa.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.NhanVienThietBi}")]
public class PhieuNhapController : Controller
{
    private readonly ApplicationDbContext _context;

    public PhieuNhapController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index(string? search, DateOnly? from, DateOnly? to)
    {
        var query = _context.PhieuNhap
            .Include(p => p.NhaCungCap)
            .Include(p => p.NguoiNhap)
            .Include(p => p.ChiTietPhieuNhap).ThenInclude(ct => ct.ThietBi)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.MaPhieuNhap.Contains(search) || p.NhaCungCap.TenNCC.Contains(search));

        if (from.HasValue)
            query = query.Where(p => p.NgayNhap >= from.Value);

        if (to.HasValue)
            query = query.Where(p => p.NgayNhap <= to.Value);

        ViewBag.Search = search;
        ViewBag.From = from?.ToString("yyyy-MM-dd");
        ViewBag.To = to?.ToString("yyyy-MM-dd");

        return View(await query.OrderByDescending(p => p.NgayNhap).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var item = await _context.PhieuNhap
            .Include(p => p.NhaCungCap)
            .Include(p => p.NguoiNhap)
            .Include(p => p.ChiTietPhieuNhap).ThenInclude(ct => ct.ThietBi).ThenInclude(t => t.Phong)
            .Include(p => p.ChiTietPhieuNhap).ThenInclude(ct => ct.ThietBi).ThenInclude(t => t.LoaiThietBi)
            .FirstOrDefaultAsync(p => p.Id == id);
        return item == null ? NotFound() : View(item);
    }

    public async Task<IActionResult> Create()
    {
        await LoadSelectLists();
        var model = new PhieuNhap
        {
            MaPhieuNhap = $"PN-{DateTime.Now:yyyyMMdd}-{DateTime.Now:HHmmss}",
            NgayNhap = DateOnly.FromDateTime(DateTime.Now),
            CreatedAt = DateTime.Now
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PhieuNhap model, int[] thietBiIds, int[] soLuongs, decimal[] donGias)
    {
        ModelState.Remove("NhaCungCap");
        ModelState.Remove("NguoiNhap");
        ModelState.Remove("ChiTietPhieuNhap");

        if (thietBiIds.Length == 0)
            ModelState.AddModelError("", "Vui lòng thêm ít nhất một thiết bị vào phiếu nhập.");

        if (!ModelState.IsValid)
        {
            await LoadSelectLists(model.NhaCungCapId, model.NguoiNhapId);
            return View(model);
        }

        model.CreatedAt = DateTime.Now;
        decimal tong = 0;

        for (int i = 0; i < thietBiIds.Length; i++)
        {
            int sl = i < soLuongs.Length ? soLuongs[i] : 1;
            decimal dg = i < donGias.Length ? donGias[i] : 0;
            tong += sl * dg;
            model.ChiTietPhieuNhap.Add(new ChiTietPhieuNhap
            {
                ThietBiId = thietBiIds[i],
                SoLuong = sl,
                DonGia = dg,
                ThanhTien = sl * dg
            });
        }

        model.TongTien = tong;
        _context.PhieuNhap.Add(model);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã tạo phiếu nhập {model.MaPhieuNhap} thành công với tổng giá trị {tong:N0} đ.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _context.PhieuNhap.FindAsync(id);
        if (item == null) return NotFound();
        await LoadSelectLists(item.NhaCungCapId, item.NguoiNhapId);
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PhieuNhap model)
    {
        if (id != model.Id) return NotFound();

        ModelState.Remove("NhaCungCap");
        ModelState.Remove("NguoiNhap");
        ModelState.Remove("ChiTietPhieuNhap");

        if (!ModelState.IsValid)
        {
            await LoadSelectLists(model.NhaCungCapId, model.NguoiNhapId);
            return View(model);
        }

        _context.PhieuNhap.Update(model);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Cập nhật phiếu nhập thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.PhieuNhap.Include(p => p.ChiTietPhieuNhap).FirstOrDefaultAsync(p => p.Id == id);
        if (item == null) return NotFound();
        _context.ChiTietPhieuNhap.RemoveRange(item.ChiTietPhieuNhap);
        _context.PhieuNhap.Remove(item);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Đã xóa phiếu nhập thành công.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadSelectLists(int? nhaCungCapId = null, int? nguoiNhapId = null)
    {
        ViewBag.NhaCungCapId = new SelectList(
            await _context.NhaCungCap.Where(n => n.TrangThai).OrderBy(n => n.TenNCC).ToListAsync(),
            "Id", "TenNCC", nhaCungCapId);
        ViewBag.NguoiNhapId = new SelectList(
            await _context.NhanVien.Where(n => n.TrangThai).OrderBy(n => n.HoTen).ToListAsync(),
            "Id", "HoTen", nguoiNhapId);
        ViewBag.ThietBis = await _context.ThietBi.Where(t => !t.IsDeleted).OrderBy(t => t.TenTb)
            .Select(t => new { t.Id, Display = $"{t.TenTb} ({t.MaTb})" }).ToListAsync();
    }
}
