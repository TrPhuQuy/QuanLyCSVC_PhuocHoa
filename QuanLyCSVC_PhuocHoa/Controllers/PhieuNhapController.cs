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
public class PhieuNhapController : Controller
{
    private readonly ApplicationDbContext _context;

    public PhieuNhapController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index(string? search, DateOnly? from, DateOnly? to, int page = 1)
    {
        var query = _context.PhieuNhap
            .Include(p => p.NhaCungCap)
            .Include(p => p.NguoiNhap)
            .Include(p => p.ChiTietPhieuNhap).ThenInclude(ct => ct.ThietBi)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.MaPhieuNhap.Contains(search) || (p.NhaCungCap != null && p.NhaCungCap.TenNCC.Contains(search)));

        if (from.HasValue)
            query = query.Where(p => p.NgayNhap >= from.Value);

        if (to.HasValue)
            query = query.Where(p => p.NgayNhap <= to.Value);

        ViewBag.Search = search;
        ViewBag.From = from?.ToString("yyyy-MM-dd");
        ViewBag.To = to?.ToString("yyyy-MM-dd");

        var pagedItems = await PaginatedList<PhieuNhap>.CreateAsync(query.OrderByDescending(p => p.NgayNhap), page, 15);
        return View(pagedItems);
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

    /// <summary>
    /// In Phiếu nhập kho chuẩn Mẫu C30-HD (Thông tư 107/2017/TT-BTC)
    /// </summary>
    public async Task<IActionResult> PrintC30(int id)
    {
        var item = await _context.PhieuNhap
            .Include(p => p.NhaCungCap)
            .Include(p => p.NguoiNhap)
            .Include(p => p.ChiTietPhieuNhap).ThenInclude(ct => ct.ThietBi).ThenInclude(t => t.Phong)
            .Include(p => p.ChiTietPhieuNhap).ThenInclude(ct => ct.ThietBi).ThenInclude(t => t.LoaiThietBi)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (item == null) return NotFound();

        return View(item);
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

        var validIndices = new List<int>();
        if (thietBiIds != null)
        {
            for (int i = 0; i < thietBiIds.Length; i++)
            {
                int sl = (soLuongs != null && i < soLuongs.Length) ? soLuongs[i] : 0;
                if (thietBiIds[i] > 0 && sl > 0)
                {
                    validIndices.Add(i);
                }
            }
        }

        if (validIndices.Count == 0)
            ModelState.AddModelError("", "Vui lòng chọn ít nhất một thiết bị hợp lệ với số lượng lớn hơn 0.");

        if (!ModelState.IsValid)
        {
            await LoadSelectLists(model.NhaCungCapId, model.NguoiNhapId);
            return View(model);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            model.CreatedAt = DateTime.Now;
            decimal tong = 0;

            foreach (var i in validIndices)
            {
                int tbId = thietBiIds![i];
                int sl = (soLuongs != null && i < soLuongs.Length) ? soLuongs[i] : 1;
                decimal dg = (donGias != null && i < donGias.Length && donGias[i] > 0) ? donGias[i] : 0;
                tong += sl * dg;
                model.ChiTietPhieuNhap.Add(new ChiTietPhieuNhap
                {
                    ThietBiId = tbId,
                    SoLuong = sl,
                    DonGia = dg,
                    ThanhTien = sl * dg
                });

                // Cập nhật tăng số lượng thiết bị trong kho và cập nhật đơn giá mua gần nhất
                var tb = await _context.ThietBi.FindAsync(tbId);
                if (tb != null)
                {
                    tb.SoLuong += sl;
                    if (dg > 0) tb.GiaMua = dg;
                    if (model.NhaCungCapId > 0 && !tb.NhaCungCapId.HasValue)
                    {
                        tb.NhaCungCapId = model.NhaCungCapId;
                    }
                    _context.ThietBi.Update(tb);
                }
            }

            model.TongTien = tong;
            _context.PhieuNhap.Add(model);
            await _context.SaveChangesAsync();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                await AuditLogHelper.LogAsync(_context, userId, $"Tạo phiếu nhập: {model.MaPhieuNhap} (Tổng tiền: {tong:N0} đ)", "Phiếu nhập", HttpContext.Connection.RemoteIpAddress?.ToString());
            }

            await transaction.CommitAsync();

            TempData["Success"] = $"Đã tạo phiếu nhập {model.MaPhieuNhap} thành công với tổng giá trị {tong:N0} đ. Tồn kho thiết bị đã được tự động cập nhật.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            TempData["Error"] = "Có lỗi xảy ra khi lưu phiếu nhập: " + ex.Message;
            await LoadSelectLists(model.NhaCungCapId, model.NguoiNhapId);
            return View(model);
        }
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

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
        {
            await AuditLogHelper.LogAsync(_context, userId, $"Cập nhật phiếu nhập: {model.MaPhieuNhap}", "Phiếu nhập", HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        TempData["Success"] = "Cập nhật phiếu nhập thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.PhieuNhap.Include(p => p.ChiTietPhieuNhap).FirstOrDefaultAsync(p => p.Id == id);
        if (item == null) return NotFound();

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Trừ lại số lượng thiết bị tương ứng khỏi kho khi hủy phiếu nhập
            foreach (var ct in item.ChiTietPhieuNhap)
            {
                var tb = await _context.ThietBi.FindAsync(ct.ThietBiId);
                if (tb != null)
                {
                    if (tb.SoLuong < ct.SoLuong)
                    {
                        TempData["Error"] = $"Không thể xóa phiếu nhập vì thiết bị '{tb.TenTb}' đã được sử dụng/mượn (Tồn kho hiện tại: {tb.SoLuong} < Số lượng nhập: {ct.SoLuong}).";
                        return RedirectToAction(nameof(Index));
                    }
                    tb.SoLuong -= ct.SoLuong;
                    _context.ThietBi.Update(tb);
                }
            }

            _context.ChiTietPhieuNhap.RemoveRange(item.ChiTietPhieuNhap);
            _context.PhieuNhap.Remove(item);
            await _context.SaveChangesAsync();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                await AuditLogHelper.LogAsync(_context, userId, $"Xóa phiếu nhập: {item.MaPhieuNhap}", "Phiếu nhập", HttpContext.Connection.RemoteIpAddress?.ToString());
            }

            await transaction.CommitAsync();
            TempData["Success"] = "Đã xóa phiếu nhập và hoàn tác tồn kho thiết bị thành công.";
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            TempData["Error"] = "Có lỗi xảy ra khi xóa phiếu nhập: " + ex.Message;
        }

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
