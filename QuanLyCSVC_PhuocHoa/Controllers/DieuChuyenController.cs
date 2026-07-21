using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyCSVC_PhuocHoa.Data;
using QuanLyCSVC_PhuocHoa.Models;
using QuanLyCSVC_PhuocHoa.Models.Entities;
using System.Security.Claims;

namespace QuanLyCSVC_PhuocHoa.Controllers;

[Authorize]
public class DieuChuyenController : Controller
{
    private readonly ApplicationDbContext _context;

    public DieuChuyenController(ApplicationDbContext context) => _context = context;

    /// <summary>
    /// Lấy nhân viên gắn với tài khoản đang đăng nhập, tra theo UserId (khóa ngoại chuẩn)
    /// thay vì so khớp Email/UserName — tránh lỗi khi UserName không phải là địa chỉ Email.
    /// </summary>
    private Task<NhanVien?> GetCurrentEmployeeAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return _context.NhanVien.FirstOrDefaultAsync(n => n.UserId == userId);
    }

    public async Task<IActionResult> Index(string? search, string? trangThai)
    {
        var employee = await GetCurrentEmployeeAsync();

        var query = _context.DieuChuyen
            .Include(d => d.ThietBi)
            .Include(d => d.PhongDi)
            .Include(d => d.PhongDen)
            .Include(d => d.NguoiDeNghi)
            .Include(d => d.NguoiDuyet)
            .AsQueryable();

        // Nếu là giáo viên thường, chỉ xem phiếu của mình đề nghị
        if (!User.IsInRole(AppRoles.Admin) && !User.IsInRole(AppRoles.NhanVienThietBi))
        {
            if (employee != null)
                query = query.Where(d => d.NguoiDeNghiId == employee.Id);
            else
                query = query.Where(d => false); // Không thấy gì
        }

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(d => d.MaDieuChuyen.Contains(search) || d.ThietBi.TenTb.Contains(search));
        
        if (!string.IsNullOrWhiteSpace(trangThai))
            query = query.Where(d => d.TrangThai == trangThai);

        ViewBag.Search = search;
        ViewBag.TrangThai = trangThai;
        ViewBag.TrangThais = new List<string> { "Chờ duyệt", "Đã duyệt", "Từ chối" };

        return View(await query.OrderByDescending(d => d.NgayDeNghi).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var item = await _context.DieuChuyen
            .Include(d => d.ThietBi)
            .Include(d => d.PhongDi)
            .Include(d => d.PhongDen)
            .Include(d => d.NguoiDeNghi)
            .Include(d => d.NguoiDuyet)
            .FirstOrDefaultAsync(d => d.Id == id);

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

        await LoadSelectLists();
        
        var model = new DieuChuyen
        {
            MaDieuChuyen = $"DC-{DateTime.Now:yyyyMMddHHmmss}",
            NgayDeNghi = DateTime.Now,
            NgayDieuChuyen = DateOnly.FromDateTime(DateTime.Now),
            TrangThai = "Chờ duyệt",
            NguoiDeNghiId = employee.Id
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DieuChuyen model)
    {
        var thietBi = await _context.ThietBi.FindAsync(model.ThietBiId);
        if (thietBi == null)
        {
            ModelState.AddModelError(nameof(model.ThietBiId), "Thiết bị không tồn tại.");
        }
        else
        {
            model.PhongDiId = thietBi.PhongId; // Tự động lấy phòng hiện tại của thiết bị
            if (model.SoLuong <= 0)
                ModelState.AddModelError(nameof(model.SoLuong), "Số lượng điều chuyển phải lớn hơn 0.");
            else if (model.SoLuong > thietBi.SoLuong)
                ModelState.AddModelError(nameof(model.SoLuong), $"Số lượng thiết bị hiện tại ở phòng này chỉ còn {thietBi.SoLuong}.");
        }

        if (model.PhongDenId == model.PhongDiId)
            ModelState.AddModelError(nameof(model.PhongDenId), "Phòng đến phải khác phòng đi.");

        ModelState.Remove("NguoiDeNghi");
        ModelState.Remove("PhongDi");
        ModelState.Remove("PhongDen");
        ModelState.Remove("ThietBi");

        if (!ModelState.IsValid)
        {
            await LoadSelectLists(model.ThietBiId, model.PhongDenId);
            return View(model);
        }

        _context.DieuChuyen.Add(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Gửi yêu cầu điều chuyển thành công. Đang chờ phê duyệt.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.NhanVienThietBi}")]
    public async Task<IActionResult> Approve(int id)
    {
        var item = await _context.DieuChuyen
            .Include(d => d.ThietBi)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (item == null) return NotFound();
        if (item.TrangThai != "Chờ duyệt")
        {
            TempData["Error"] = "Yêu cầu này đã được xử lý trước đó.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var employee = await GetCurrentEmployeeAsync();
        if (employee == null)
        {
            TempData["Error"] = "Tài khoản người duyệt chưa liên kết với Nhân viên.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var thietBi = item.ThietBi;
        if (thietBi.IsDeleted)
        {
            TempData["Error"] = "Thiết bị này đã bị xóa khỏi hệ thống.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (item.SoLuong > thietBi.SoLuong)
        {
            TempData["Error"] = $"Không đủ số lượng thiết bị để điều chuyển. Hiện tại chỉ còn {thietBi.SoLuong}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Thực hiện điều chuyển thiết bị
        if (item.SoLuong == thietBi.SoLuong)
        {
            // Trường hợp 1: Chuyển toàn bộ -> chỉ cần cập nhật PhongId
            thietBi.PhongId = item.PhongDenId;
            _context.ThietBi.Update(thietBi);
        }
        else
        {
            // Trường hợp 2: Chuyển một phần -> trừ đi số lượng cũ, tạo mới 1 bản ghi thiết bị ở phòng mới
            thietBi.SoLuong -= item.SoLuong;
            _context.ThietBi.Update(thietBi);

            // Tạo mã thiết bị mới (thêm hậu tố để tránh trùng lặp mã duy nhất)
            var newMaTb = $"{thietBi.MaTb}-{DateTime.Now:fff}";
            if (newMaTb.Length > 20) newMaTb = newMaTb[..20];

            var newThietBi = new ThietBi
            {
                MaTb = newMaTb,
                TenTb = thietBi.TenTb,
                LoaiThietBiId = thietBi.LoaiThietBiId,
                HangSanXuat = thietBi.HangSanXuat,
                NamSanXuat = thietBi.NamSanXuat,
                NgayMua = thietBi.NgayMua,
                GiaMua = thietBi.GiaMua,
                SoLuong = item.SoLuong,
                DonViTinh = thietBi.DonViTinh,
                TinhTrang = thietBi.TinhTrang,
                HinhAnh = thietBi.HinhAnh,
                ViTriSuDung = item.PhongDenId.ToString(),
                PhongId = item.PhongDenId,
                BoMonId = thietBi.BoMonId,
                NhaCungCapId = thietBi.NhaCungCapId,
                GhiChu = $"Được điều chuyển từ phòng {item.PhongDiId} (Phiếu {item.MaDieuChuyen})",
                CreatedAt = DateTime.Now,
                IsDeleted = false
            };

            _context.ThietBi.Add(newThietBi);
        }

        // Cập nhật trạng thái phiếu điều chuyển
        item.TrangThai = "Đã duyệt";
        item.NguoiDuyetId = employee.Id;
        item.NgayDuyet = DateTime.Now;

        _context.DieuChuyen.Update(item);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Phê duyệt điều chuyển thiết bị thành công. Thiết bị đã được chuyển sang phòng mới.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.NhanVienThietBi}")]
    public async Task<IActionResult> Reject(int id, string? lyDoTuChoi)
    {
        var item = await _context.DieuChuyen.FindAsync(id);
        if (item == null) return NotFound();
        if (item.TrangThai != "Chờ duyệt")
        {
            TempData["Error"] = "Yêu cầu này đã được xử lý trước đó.";
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
        item.NgayDuyet = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(lyDoTuChoi))
            item.LyDo = $"{item.LyDo} (Bị từ chối: {lyDoTuChoi})";

        _context.DieuChuyen.Update(item);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Đã từ chối yêu cầu điều chuyển.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task LoadSelectLists(int? thietBiId = null, int? phongDenId = null)
    {
        var thietBis = await _context.ThietBi
            .Include(t => t.Phong)
            .Where(t => !t.IsDeleted && t.SoLuong > 0)
            .OrderBy(t => t.TenTb)
            .Select(t => new { Id = t.Id, Display = $"{t.TenTb} ({t.MaTb}) - Tại: {t.Phong.TenPhong} (SL: {t.SoLuong})" })
            .ToListAsync();

        ViewBag.ThietBiId = new SelectList(thietBis, "Id", "Display", thietBiId);
        ViewBag.PhongDenId = new SelectList(
            await _context.Phong.Where(p => p.TrangThai == "Đang sử dụng").OrderBy(p => p.TenPhong).ToListAsync(),
            "Id", "TenPhong", phongDenId);
    }
}