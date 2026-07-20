using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyCSVC_PhuocHoa.Data;
using QuanLyCSVC_PhuocHoa.Models;
using QuanLyCSVC_PhuocHoa.Models.Entities;

namespace QuanLyCSVC_PhuocHoa.Controllers;

[Authorize]
public class PhieuMuonController : Controller
{
    private readonly ApplicationDbContext _context;

    public PhieuMuonController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index(string? search, string? trangThai)
    {
        var userEmail = User.Identity?.Name;
        var employee = await _context.NhanVien.FirstOrDefaultAsync(n => n.Email == userEmail);

        var query = _context.PhieuMuon
            .Include(p => p.GiaoVien)
            .Include(p => p.NguoiDuyet)
            .Include(p => p.ChiTietPhieuMuon).ThenInclude(c => c.ThietBi)
            .AsQueryable();

        // Nếu là giáo viên, chỉ xem phiếu của mình đăng ký
        if (!User.IsInRole(AppRoles.Admin) && !User.IsInRole(AppRoles.NhanVienThietBi))
        {
            if (employee != null)
                query = query.Where(p => p.GiaoVienId == employee.Id);
            else
                query = query.Where(p => false);
        }

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.MaPhieuMuon.Contains(search) || p.GiaoVien.HoTen.Contains(search));

        if (!string.IsNullOrWhiteSpace(trangThai))
            query = query.Where(p => p.TrangThai == trangThai);

        ViewBag.Search = search;
        ViewBag.TrangThai = trangThai;
        ViewBag.TrangThais = new List<string> { "Chờ duyệt", "Đang mượn", "Đã trả", "Từ chối" };

        return View(await query.OrderByDescending(p => p.NgayDangKy).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var item = await _context.PhieuMuon
            .Include(p => p.GiaoVien)
            .Include(p => p.NguoiDuyet)
            .Include(p => p.ChiTietPhieuMuon).ThenInclude(c => c.ThietBi).ThenInclude(t => t.Phong)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (item == null) return NotFound();

        return View(item);
    }

    public async Task<IActionResult> Create()
    {
        var userEmail = User.Identity?.Name;
        var employee = await _context.NhanVien.FirstOrDefaultAsync(n => n.Email == userEmail);
        if (employee == null)
        {
            TempData["Error"] = "Tài khoản của bạn chưa liên kết với thông tin Nhân viên.";
            return RedirectToAction(nameof(Index));
        }

        var thietBis = await _context.ThietBi
            .Include(t => t.Phong)
            .Where(t => !t.IsDeleted && t.SoLuong > 0)
            .OrderBy(t => t.TenTb)
            .Select(t => new { Id = t.Id, Display = $"{t.TenTb} ({t.MaTb}) - Tại: {t.Phong.TenPhong} (SL: {t.SoLuong})" })
            .ToListAsync();

        ViewBag.ThietBis = thietBis;
        
        var model = new PhieuMuon
        {
            MaPhieuMuon = $"PM-{DateTime.Now:yyyyMMddHHmmss}",
            NgayDangKy = DateTime.Now,
            NgayMuonDuKien = DateOnly.FromDateTime(DateTime.Now),
            NgayTraDuKien = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
            TrangThai = "Chờ duyệt",
            GiaoVienId = employee.Id
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PhieuMuon model, int[] thietBiIds, int[] soLuongs)
    {
        if (thietBiIds == null || thietBiIds.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Vui lòng chọn ít nhất một thiết bị để mượn.");
        }
        else
        {
            for (int i = 0; i < thietBiIds.Length; i++)
            {
                var tbId = thietBiIds[i];
                var qty = soLuongs[i];
                var tb = await _context.ThietBi.FindAsync(tbId);
                if (tb == null)
                {
                    ModelState.AddModelError(string.Empty, $"Thiết bị ID {tbId} không tồn tại.");
                }
                else if (qty <= 0)
                {
                    ModelState.AddModelError(string.Empty, $"Số lượng mượn thiết bị '{tb.TenTb}' phải lớn hơn 0.");
                }
                else if (qty > tb.SoLuong)
                {
                    ModelState.AddModelError(string.Empty, $"Số lượng mượn thiết bị '{tb.TenTb}' vượt quá số lượng hiện có ({tb.SoLuong}).");
                }
            }
        }

        ModelState.Remove("GiaoVien");
        ModelState.Remove("NguoiDuyet");

        if (!ModelState.IsValid)
        {
            var thietBis = await _context.ThietBi
                .Include(t => t.Phong)
                .Where(t => !t.IsDeleted && t.SoLuong > 0)
                .OrderBy(t => t.TenTb)
                .Select(t => new { Id = t.Id, Display = $"{t.TenTb} ({t.MaTb}) - Tại: {t.Phong.TenPhong} (SL: {t.SoLuong})" })
                .ToListAsync();
            ViewBag.ThietBis = thietBis;
            return View(model);
        }

        // Tạo chi tiết phiếu mượn
        for (int i = 0; i < thietBiIds!.Length; i++)
        {
            model.ChiTietPhieuMuon.Add(new ChiTietPhieuMuon
            {
                ThietBiId = thietBiIds[i],
                SoLuongMuon = soLuongs[i],
                TinhTrangKhiTra = "Đang mượn"
            });
        }

        _context.PhieuMuon.Add(model);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Đăng ký phiếu mượn thiết bị thành công. Chờ quản trị viên phê duyệt.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.NhanVienThietBi}")]
    public async Task<IActionResult> Approve(int id)
    {
        var item = await _context.PhieuMuon
            .Include(p => p.ChiTietPhieuMuon).ThenInclude(c => c.ThietBi)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (item == null) return NotFound();
        if (item.TrangThai != "Chờ duyệt")
        {
            TempData["Error"] = "Phiếu này đã được xử lý trước đó.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var userEmail = User.Identity?.Name;
        var employee = await _context.NhanVien.FirstOrDefaultAsync(n => n.Email == userEmail);
        if (employee == null)
        {
            TempData["Error"] = "Tài khoản người duyệt chưa liên kết với Nhân viên.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Kiểm tra lại số lượng thiết bị trước khi cho mượn
        foreach (var ct in item.ChiTietPhieuMuon)
        {
            if (ct.ThietBi.IsDeleted)
            {
                TempData["Error"] = $"Thiết bị '{ct.ThietBi.TenTb}' đã bị xóa khỏi hệ thống.";
                return RedirectToAction(nameof(Details), new { id });
            }
            if (ct.SoLuongMuon > ct.ThietBi.SoLuong)
            {
                TempData["Error"] = $"Thiết bị '{ct.ThietBi.TenTb}' không đủ số lượng trong kho (Còn lại: {ct.ThietBi.SoLuong}).";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // Khấu trừ số lượng thiết bị
        foreach (var ct in item.ChiTietPhieuMuon)
        {
            ct.ThietBi.SoLuong -= ct.SoLuongMuon;
            _context.ThietBi.Update(ct.ThietBi);
        }

        // Cập nhật trạng thái phiếu mượn
        item.TrangThai = "Đang mượn";
        item.NguoiDuyetId = employee.Id;
        item.NgayMuonThucTe = DateTime.Now;

        _context.PhieuMuon.Update(item);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Đã duyệt cho mượn thiết bị và bàn giao thành công.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.NhanVienThietBi}")]
    public async Task<IActionResult> Reject(int id, string? lyDo)
    {
        var item = await _context.PhieuMuon.FindAsync(id);
        if (item == null) return NotFound();
        if (item.TrangThai != "Chờ duyệt")
        {
            TempData["Error"] = "Phiếu này đã được xử lý trước đó.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var userEmail = User.Identity?.Name;
        var employee = await _context.NhanVien.FirstOrDefaultAsync(n => n.Email == userEmail);
        if (employee == null)
        {
            TempData["Error"] = "Tài khoản người duyệt chưa liên kết với Nhân viên.";
            return RedirectToAction(nameof(Details), new { id });
        }

        item.TrangThai = "Từ chối";
        item.NguoiDuyetId = employee.Id;
        if (!string.IsNullOrWhiteSpace(lyDo))
            item.GhiChu = $"Từ chối duyệt: {lyDo}. {item.GhiChu}";

        _context.PhieuMuon.Update(item);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Đã từ chối phiếu mượn.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.NhanVienThietBi}")]
    public async Task<IActionResult> Return(int id, int[] ctIds, string[] tinhTrangs, string[] ghiChus)
    {
        var item = await _context.PhieuMuon
            .Include(p => p.ChiTietPhieuMuon).ThenInclude(c => c.ThietBi)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (item == null) return NotFound();
        if (item.TrangThai != "Đang mượn")
        {
            TempData["Error"] = "Phiếu này không ở trạng thái Đang mượn.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Cập nhật chi tiết trả thiết bị
        for (int i = 0; i < ctIds.Length; i++)
        {
            var ctId = ctIds[i];
            var ct = item.ChiTietPhieuMuon.FirstOrDefault(c => c.Id == ctId);
            if (ct != null)
            {
                ct.TinhTrangKhiTra = tinhTrangs[i];
                ct.GhiChu = ghiChus[i];
                _context.ChiTietPhieuMuon.Update(ct);

                // Hoàn lại số lượng thiết bị vào kho
                ct.ThietBi.SoLuong += ct.SoLuongMuon;

                // Nếu tình trạng lúc trả là hư hỏng, cập nhật tình trạng thiết bị (hoặc lưu ý)
                if (tinhTrangs[i] == "Hư hỏng" || tinhTrangs[i] == "Mất")
                {
                    // Đánh dấu thiết bị cần sửa chữa hoặc giảm trừ nếu cần
                    // Ở đây chỉ cập nhật ghi chú thiết bị để thủ kho biết
                    ct.ThietBi.GhiChu = $"Lưu ý: Bị {tinhTrangs[i]} khi trả ở phiếu {item.MaPhieuMuon}. {ct.ThietBi.GhiChu}";
                }
                
                _context.ThietBi.Update(ct.ThietBi);
            }
        }

        // Cập nhật trạng thái phiếu
        item.TrangThai = "Đã trả";
        item.NgayTraThucTe = DateTime.Now;

        _context.PhieuMuon.Update(item);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Đã ghi nhận trả thiết bị và cập nhật số lượng kho thành công.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
