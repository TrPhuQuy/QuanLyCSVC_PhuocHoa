using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyCSVC_PhuocHoa.Data;
using QuanLyCSVC_PhuocHoa.Helpers;
using QuanLyCSVC_PhuocHoa.Models;
using QuanLyCSVC_PhuocHoa.Models.Entities;
using System.Security.Claims;

namespace QuanLyCSVC_PhuocHoa.Controllers;

[Authorize]
public class PhieuMuonController : Controller
{
    private readonly ApplicationDbContext _context;

    public PhieuMuonController(ApplicationDbContext context) => _context = context;

    /// <summary>
    /// Lấy nhân viên gắn với tài khoản đang đăng nhập, tra theo UserId (khóa ngoại chuẩn)
    /// thay vì so khớp Email/UserName — tránh lỗi khi UserName không phải là địa chỉ Email.
    /// </summary>
    private Task<NhanVien?> GetCurrentEmployeeAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return _context.NhanVien.FirstOrDefaultAsync(n => n.UserId == userId);
    }

    public async Task<IActionResult> Index(string? search, string? trangThai, int page = 1)
    {
        var employee = await GetCurrentEmployeeAsync();

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
            query = query.Where(p => p.MaPhieuMuon.Contains(search) || (p.GiaoVien != null && p.GiaoVien.HoTen.Contains(search)));

        if (!string.IsNullOrWhiteSpace(trangThai))
            query = query.Where(p => p.TrangThai == trangThai);

        ViewBag.Search = search;
        ViewBag.TrangThai = trangThai;
        ViewBag.TrangThais = new List<string> { "Chờ duyệt", "Đang mượn", "Đã trả", "Từ chối" };

        var pagedItems = await PaginatedList<PhieuMuon>.CreateAsync(query.OrderByDescending(p => p.NgayDangKy), page, 15);
        return View(pagedItems);
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
        var employee = await GetCurrentEmployeeAsync();
        if (employee == null)
        {
            TempData["Error"] = "Tài khoản của bạn chưa liên kết với thông tin Nhân viên.";
            return RedirectToAction(nameof(Index));
        }

        var thietBis = await _context.ThietBi
            .Include(t => t.Phong)
            .Where(t => !t.IsDeleted && t.SoLuong > 0 && t.TinhTrang == "Đang sử dụng")
            .OrderBy(t => t.TenTb)
            .Select(t => new { Id = t.Id, Display = $"{t.TenTb} ({t.MaTb}) - Tại: {(t.Phong != null ? t.Phong.TenPhong : "-")} (SL: {t.SoLuong})" })
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
        if (model.NgayTraDuKien < model.NgayMuonDuKien)
        {
            ModelState.AddModelError(nameof(model.NgayTraDuKien), "Ngày trả dự kiến không được sớm hơn ngày mượn dự kiến.");
        }

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
                if (tb == null || tb.IsDeleted)
                {
                    ModelState.AddModelError(string.Empty, $"Thiết bị ID {tbId} không tồn tại hoặc đã bị xóa.");
                }
                else if (tb.TinhTrang != "Đang sử dụng")
                {
                    ModelState.AddModelError(string.Empty, $"Thiết bị '{tb.TenTb}' hiện ở tình trạng '{tb.TinhTrang}', không đủ điều kiện cho mượn.");
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
                .Where(t => !t.IsDeleted && t.SoLuong > 0 && t.TinhTrang == "Đang sử dụng")
                .OrderBy(t => t.TenTb)
                .Select(t => new { Id = t.Id, Display = $"{t.TenTb} ({t.MaTb}) - Tại: {(t.Phong != null ? t.Phong.TenPhong : "-")} (SL: {t.SoLuong})" })
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

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
        {
            await AuditLogHelper.LogAsync(_context, userId, $"Đăng ký phiếu mượn: {model.MaPhieuMuon}", "Phiếu mượn", HttpContext.Connection.RemoteIpAddress?.ToString());
        }

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

        var employee = await GetCurrentEmployeeAsync();
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

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
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

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(currentUserId))
            {
                await AuditLogHelper.LogAsync(_context, currentUserId, $"Duyệt cho mượn phiếu {item.MaPhieuMuon}", "Phiếu mượn", HttpContext.Connection.RemoteIpAddress?.ToString());
            }

            await transaction.CommitAsync();

            TempData["Success"] = "Đã duyệt cho mượn thiết bị và bàn giao thành công.";
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            TempData["Error"] = "Có lỗi xảy ra khi duyệt mượn thiết bị: " + ex.Message;
        }

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

        var employee = await GetCurrentEmployeeAsync();
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

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(currentUserId))
        {
            await AuditLogHelper.LogAsync(_context, currentUserId, $"Từ chối phiếu mượn {item.MaPhieuMuon}", "Phiếu mượn", HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        TempData["Success"] = "Đã từ chối phiếu mượn.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? lyDo)
    {
        var item = await _context.PhieuMuon.FindAsync(id);
        if (item == null) return NotFound();

        if (item.TrangThai != "Chờ duyệt")
        {
            TempData["Error"] = "Chỉ có thể hủy yêu cầu mượn khi đang ở trạng thái 'Chờ duyệt'.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var employee = await GetCurrentEmployeeAsync();
        var isStaff = User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.NhanVienThietBi);
        if (!isStaff && (employee == null || item.GiaoVienId != employee.Id))
        {
            TempData["Error"] = "Bạn không có quyền hủy phiếu mượn của người khác.";
            return RedirectToAction(nameof(Details), new { id });
        }

        item.TrangThai = "Từ chối";
        var cancelNote = string.IsNullOrWhiteSpace(lyDo) ? "Người mượn tự hủy yêu cầu" : $"Người mượn hủy: {lyDo}";
        item.GhiChu = $"[{cancelNote} ngày {DateTime.Now:dd/MM/yyyy HH:mm}]. {item.GhiChu}";

        _context.PhieuMuon.Update(item);
        await _context.SaveChangesAsync();

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(currentUserId))
        {
            await AuditLogHelper.LogAsync(_context, currentUserId, $"Hủy phiếu mượn {item.MaPhieuMuon}", "Phiếu mượn", HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        TempData["Success"] = "Đã hủy yêu cầu mượn thiết bị thành công.";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// In Phiếu theo dõi mượn - trả thiết bị dạy học
    /// </summary>
    public async Task<IActionResult> Print(int id)
    {
        var item = await _context.PhieuMuon
            .Include(p => p.GiaoVien).ThenInclude(g => g.BoMon)
            .Include(p => p.NguoiDuyet)
            .Include(p => p.ChiTietPhieuMuon).ThenInclude(c => c.ThietBi).ThenInclude(t => t.Phong)
            .Include(p => p.ChiTietPhieuMuon).ThenInclude(c => c.ThietBi).ThenInclude(t => t.LoaiThietBi)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (item == null) return NotFound();

        return View(item);
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

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            int slMat = 0;
            int slHong = 0;

            // Cập nhật chi tiết trả thiết bị
            if (ctIds != null && ctIds.Length > 0)
            {
                for (int i = 0; i < ctIds.Length; i++)
                {
                    var ctId = ctIds[i];
                    var ct = item.ChiTietPhieuMuon.FirstOrDefault(c => c.Id == ctId);
                    if (ct != null && ct.ThietBi != null)
                    {
                        var ttTra = (tinhTrangs != null && i < tinhTrangs.Length) ? tinhTrangs[i] : "Bình thường";
                        ct.TinhTrangKhiTra = ttTra;
                        ct.GhiChu = (ghiChus != null && i < ghiChus.Length) ? ghiChus[i] : null;
                        _context.ChiTietPhieuMuon.Update(ct);

                        if (ttTra == "Mất")
                        {
                            // Khi thiết bị bị mất: KHÔNG cộng lại vào số lượng khả dụng trong kho!
                            slMat += ct.SoLuongMuon;
                            ct.ThietBi.GhiChu = $"[CẢNH BÁO MẤT TÀI SẢN]: Bị mất {ct.SoLuongMuon} {ct.ThietBi.DonViTinh} khi trả ở phiếu {item.MaPhieuMuon}. {ct.ThietBi.GhiChu}";
                            _context.ThietBi.Update(ct.ThietBi);
                        }
                        else if (ttTra == "Hư hỏng")
                        {
                            // Hoàn trả về kho nhưng đánh dấu tình trạng thiết bị là Hư hỏng để thủ kho/quản trị đưa vào danh sách bảo trì
                            slHong += ct.SoLuongMuon;
                            ct.ThietBi.SoLuong += ct.SoLuongMuon;
                            ct.ThietBi.TinhTrang = "Hư hỏng";
                            ct.ThietBi.GhiChu = $"[HƯ HỎNG]: Phát hiện hỏng {ct.SoLuongMuon} {ct.ThietBi.DonViTinh} khi trả phiếu {item.MaPhieuMuon}. {ct.ThietBi.GhiChu}";
                            _context.ThietBi.Update(ct.ThietBi);
                        }
                        else
                        {
                            // Trả bình thường: Hoàn lại số lượng khả dụng vào kho
                            ct.ThietBi.SoLuong += ct.SoLuongMuon;
                            _context.ThietBi.Update(ct.ThietBi);
                        }
                    }
                }
            }

            // Cập nhật trạng thái phiếu
            item.TrangThai = "Đã trả";
            item.NgayTraThucTe = DateTime.Now;

            _context.PhieuMuon.Update(item);
            await _context.SaveChangesAsync();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(currentUserId))
            {
                var note = $"Ghi nhận trả phiếu {item.MaPhieuMuon}";
                if (slMat > 0) note += $" (Mất: {slMat})";
                if (slHong > 0) note += $" (Hỏng: {slHong})";
                await AuditLogHelper.LogAsync(_context, currentUserId, note, "Phiếu mượn", HttpContext.Connection.RemoteIpAddress?.ToString());
            }

            await transaction.CommitAsync();

            string msg = "Đã ghi nhận trả thiết bị thành công.";
            if (slMat > 0) msg += $" Lưu ý: Có {slMat} thiết bị bị mất không được hoàn kho.";
            if (slHong > 0) msg += $" Có {slHong} thiết bị bị hỏng đã được chuyển sang tình trạng Hư hỏng để bảo trì.";
            TempData["Success"] = msg;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            TempData["Error"] = "Có lỗi xảy ra khi ghi nhận trả thiết bị: " + ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}