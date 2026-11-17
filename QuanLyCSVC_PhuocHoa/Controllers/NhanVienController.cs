using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyCSVC_PhuocHoa.Data;
using QuanLyCSVC_PhuocHoa.Models;
using QuanLyCSVC_PhuocHoa.Models.Entities;
using QuanLyCSVC_PhuocHoa.Helpers;

namespace QuanLyCSVC_PhuocHoa.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class NhanVienController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public NhanVienController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var query = _context.NhanVien
            .Include(n => n.BoMon)
            .Include(n => n.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(n => n.HoTen.Contains(search) || n.MaNv.Contains(search) || (n.Email != null && n.Email.Contains(search)));
        }

        ViewBag.Search = search;
        var pagedItems = await PaginatedList<NhanVien>.CreateAsync(query.OrderBy(n => n.MaNv), page, 15);
        return View(pagedItems);
    }

    public async Task<IActionResult> Details(int id)
    {
        var nv = await _context.NhanVien
            .Include(n => n.BoMon)
            .Include(n => n.User)
            .FirstOrDefaultAsync(n => n.Id == id);
        return nv == null ? NotFound() : View(nv);
    }

    public async Task<IActionResult> Create()
    {
        await LoadSelectLists();
        return View(new NhanVien { TrangThai = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(NhanVien model, string password)
    {
        if (await _context.NhanVien.AnyAsync(n => n.MaNv == model.MaNv))
            ModelState.AddModelError(nameof(model.MaNv), "Mã nhân viên đã tồn tại.");

        if (string.IsNullOrWhiteSpace(model.Email))
            ModelState.AddModelError(nameof(model.Email), "Email không được để trống.");
        else if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            ModelState.AddModelError(nameof(model.Email), "Email đã được sử dụng bởi tài khoản khác.");

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            ModelState.AddModelError("password", "Mật khẩu tối thiểu 6 ký tự.");

        ModelState.Remove("UserId");
        ModelState.Remove("User");

        if (!ModelState.IsValid)
        {
            await LoadSelectLists(model.BoMonId);
            return View(model);
        }

        // 1. Tạo Identity User trước
        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true,
            LockoutEnd = model.TrangThai ? null : DateTimeOffset.MaxValue,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        var identityResult = await _userManager.CreateAsync(user, password);
        if (!identityResult.Succeeded)
        {
            foreach (var err in identityResult.Errors)
                ModelState.AddModelError(string.Empty, err.Description);
            
            await LoadSelectLists(model.BoMonId);
            return View(model);
        }

        // 2. Gán Role dựa trên chức vụ
        string targetRole = DetermineRoleByPosition(model.ChucVu);
        await _userManager.AddToRoleAsync(user, targetRole);

        // 3. Tạo thông tin NhanVien
        model.UserId = user.Id;
        model.CreatedAt = DateTime.Now;
        _context.NhanVien.Add(model);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Thêm nhân viên và tạo tài khoản thành công.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var nv = await _context.NhanVien.Include(n => n.User).FirstOrDefaultAsync(n => n.Id == id);
        if (nv == null) return NotFound();
        await LoadSelectLists(nv.BoMonId);
        return View(nv);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, NhanVien model, string? password)
    {
        if (id != model.Id) return NotFound();

        var existingNv = await _context.NhanVien.Include(n => n.User).FirstOrDefaultAsync(n => n.Id == id);
        if (existingNv == null) return NotFound();

        if (await _context.NhanVien.AnyAsync(n => n.MaNv == model.MaNv && n.Id != id))
            ModelState.AddModelError(nameof(model.MaNv), "Mã nhân viên đã tồn tại.");

        if (string.IsNullOrWhiteSpace(model.Email))
            ModelState.AddModelError(nameof(model.Email), "Email không được để trống.");
        else if (await _context.Users.AnyAsync(u => u.Email == model.Email && u.Id != existingNv.UserId))
            ModelState.AddModelError(nameof(model.Email), "Email đã được sử dụng bởi tài khoản khác.");

        ModelState.Remove("UserId");
        ModelState.Remove("User");

        if (!ModelState.IsValid)
        {
            await LoadSelectLists(model.BoMonId);
            return View(model);
        }

        // Cập nhật thông tin Identity User
        var user = await _userManager.FindByIdAsync(existingNv.UserId);
        if (user != null)
        {
            user.Email = model.Email;
            user.UserName = model.Email;
            user.NormalizedEmail = model.Email?.ToUpperInvariant();
            user.NormalizedUserName = model.Email?.ToUpperInvariant();
            user.LockoutEnd = model.TrangThai ? null : DateTimeOffset.MaxValue; // Đồng bộ trạng thái khóa tài khoản
            await _userManager.UpdateAsync(user);

            if (!string.IsNullOrWhiteSpace(password) && password.Length >= 6)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                await _userManager.ResetPasswordAsync(user, token, password);
            }

            // Cập nhật quyền (role) dựa trên chức vụ mới
            var currentRoles = await _userManager.GetRolesAsync(user);
            string targetRole = DetermineRoleByPosition(model.ChucVu);
            if (!currentRoles.Contains(targetRole))
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, targetRole);
            }
        }

        // Cập nhật thông tin NhanVien
        existingNv.MaNv = model.MaNv;
        existingNv.HoTen = model.HoTen;
        existingNv.GioiTinh = model.GioiTinh;
        existingNv.NgaySinh = model.NgaySinh;
        existingNv.ChucVu = model.ChucVu;
        existingNv.BoMonId = model.BoMonId;
        existingNv.Sdt = model.Sdt;
        existingNv.Email = model.Email;
        existingNv.TrangThai = model.TrangThai;

        _context.NhanVien.Update(existingNv);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Cập nhật nhân viên thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var nv = await _context.NhanVien.FirstOrDefaultAsync(n => n.Id == id);
        if (nv == null) return NotFound();

        // Kiểm tra toàn diện các ràng buộc dữ liệu trước khi xóa
        var hasPhong = await _context.Phong.AnyAsync(p => p.NguoiPhuTrachId == id);
        var hasPhieuMuon = await _context.PhieuMuon.AnyAsync(p => p.GiaoVienId == id || p.NguoiDuyetId == id);
        var hasPhieuNhap = await _context.PhieuNhap.AnyAsync(p => p.NguoiNhapId == id);
        var hasKiemKe = await _context.DotKiemKe.AnyAsync(d => d.NguoiChuTriId == id);
        var hasDieuChuyen = await _context.DieuChuyen.AnyAsync(d => d.NguoiDeNghiId == id || d.NguoiDuyetId == id);
        var hasBaoTri = await _context.PhieuBaoTri.AnyAsync(b => b.NguoiGuiId == id);
        var hasHoiDong = await _context.ThanhVienHoiDong.AnyAsync(h => h.NhanVienId == id);
        var hasAuditLog = await _context.LichSuHoatDong.AnyAsync(l => l.UserId == nv.UserId);

        if (hasPhong || hasPhieuMuon || hasPhieuNhap || hasKiemKe || hasDieuChuyen || hasBaoTri || hasHoiDong || hasAuditLog)
        {
            TempData["Error"] = "Không thể xóa nhân viên này vì đã phát sinh dữ liệu lịch sử liên kết (phiếu mượn, nhập, điều chuyển, bảo trì, kiểm kê, nhật ký...). Vui lòng chỉnh sửa và chuyển trạng thái sang 'Ngưng hoạt động' để vô hiệu hóa tài khoản an toàn.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var user = await _userManager.FindByIdAsync(nv.UserId);
            _context.NhanVien.Remove(nv);
            await _context.SaveChangesAsync();

            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }

            TempData["Success"] = "Xóa nhân viên thành công.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "Không thể xóa nhân viên do còn ràng buộc dữ liệu. Vui lòng chuyển trạng thái sang 'Ngưng hoạt động'.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadSelectLists(int? boMonId = null)
    {
        ViewBag.BoMonId = new SelectList(
            await _context.BoMon.OrderBy(b => b.TenBoMon).ToListAsync(), "Id", "TenBoMon", boMonId);
    }

    private static string DetermineRoleByPosition(string? chucVu)
    {
        if (string.IsNullOrWhiteSpace(chucVu)) return AppRoles.GiaoVien;
        var chucVuLower = chucVu.ToLower();
        if (chucVuLower.Contains("hiệu trưởng") || chucVuLower.Contains("phó hiệu trưởng"))
            return AppRoles.Admin;
        if (chucVuLower.Contains("thiết bị") || chucVuLower.Contains("kế toán") || chucVuLower.Contains("văn thư"))
            return AppRoles.NhanVienThietBi;
        return AppRoles.GiaoVien;
    }
}
