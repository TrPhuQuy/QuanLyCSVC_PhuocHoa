using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyCSVC_PhuocHoa.Data;
using QuanLyCSVC_PhuocHoa.Helpers;

namespace QuanLyCSVC_PhuocHoa.Controllers;

[Authorize]
public class ThietBiController : Controller
{
    private readonly ApplicationDbContext _context;

    public ThietBiController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index(string? search, int? loaiId, int? phongId, string? tinhTrang)
    {
        var query = BuildFilterQuery(search, loaiId, phongId, tinhTrang);

        await LoadFilterLists(loaiId, phongId, tinhTrang);
        ViewBag.Search = search;
        return View(await query.OrderBy(t => t.MaTb).ToListAsync());
    }

    // Xuất danh sách thiết bị (đang áp dụng bộ lọc hiện tại) ra file Excel
    public async Task<IActionResult> ExportExcel(string? search, int? loaiId, int? phongId, string? tinhTrang)
    {
        var query = BuildFilterQuery(search, loaiId, phongId, tinhTrang);
        var items = await query.OrderBy(t => t.MaTb).ToListAsync();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Danh sách thiết bị");

        string[] headers =
        [
            "Mã TB", "Tên thiết bị", "Loại thiết bị", "Hãng SX", "Năm SX",
            "Ngày mua", "Giá mua", "Số lượng", "ĐVT", "Tình trạng",
            "Phòng", "Bộ môn", "Ghi chú"
        ];
        for (int c = 0; c < headers.Length; c++)
            sheet.Cell(1, c + 1).Value = headers[c];

        var headerRow = sheet.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#0d6efd");
        headerRow.Style.Font.FontColor = XLColor.White;

        int r = 2;
        foreach (var t in items)
        {
            sheet.Cell(r, 1).Value = t.MaTb;
            sheet.Cell(r, 2).Value = t.TenTb;
            sheet.Cell(r, 3).Value = t.LoaiThietBi?.TenLoaiTB;
            sheet.Cell(r, 4).Value = t.HangSanXuat;
            sheet.Cell(r, 5).Value = t.NamSanXuat;
            sheet.Cell(r, 6).Value = t.NgayMua?.ToString("dd/MM/yyyy");
            sheet.Cell(r, 7).Value = t.GiaMua;
            sheet.Cell(r, 7).Style.NumberFormat.Format = "#,##0";
            sheet.Cell(r, 8).Value = t.SoLuong;
            sheet.Cell(r, 9).Value = t.DonViTinh;
            sheet.Cell(r, 10).Value = EnumHelper.TenTinhTrang(t.TinhTrang);
            sheet.Cell(r, 11).Value = t.Phong?.TenPhong;
            sheet.Cell(r, 12).Value = t.BoMon?.TenBoMon;
            sheet.Cell(r, 13).Value = t.GhiChu;
            r++;
        }

        sheet.Columns().AdjustToContents();
        sheet.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var fileName = $"DanhSachThietBi_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    private IQueryable<Models.Entities.ThietBi> BuildFilterQuery(string? search, int? loaiId, int? phongId, string? tinhTrang)
    {
        var query = _context.ThietBi
            .Include(t => t.LoaiThietBi)
            .Include(t => t.Phong)
            .Include(t => t.BoMon)
            .Where(t => !t.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.TenTb.Contains(search) || t.MaTb.Contains(search));
        if (loaiId.HasValue)
            query = query.Where(t => t.LoaiThietBiId == loaiId);
        if (phongId.HasValue)
            query = query.Where(t => t.PhongId == phongId);
        if (!string.IsNullOrWhiteSpace(tinhTrang))
            query = query.Where(t => t.TinhTrang == tinhTrang);

        return query;
    }

    public async Task<IActionResult> Details(int id)
    {
        var item = await _context.ThietBi
            .Include(t => t.LoaiThietBi)
            .Include(t => t.Phong)
            .Include(t => t.BoMon)
            .Include(t => t.NhaCungCap)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
        return item == null ? NotFound() : View(item);
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.NhanVienThietBi}")]
    public async Task<IActionResult> Create()
    {
        await LoadFormSelectLists();
        return View(new ThietBi { DonViTinh = "Cái", SoLuong = 1, TinhTrang = "Đang sử dụng", CreatedAt = DateTime.Now, IsDeleted = false });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.NhanVienThietBi}")]
    public async Task<IActionResult> Create(ThietBi model, IFormFile? hinhAnh)
    {
        if (await _context.ThietBi.AnyAsync(t => t.MaTb == model.MaTb))
            ModelState.AddModelError(nameof(model.MaTb), "Mã thiết bị đã tồn tại.");

        ModelState.Remove("CreatedAt");
        ModelState.Remove("IsDeleted");
        ModelState.Remove("LoaiThietBi");
        ModelState.Remove("Phong");

        if (!ModelState.IsValid)
        {
            await LoadFormSelectLists(model.LoaiThietBiId, model.PhongId);
            return View(model);
        }

        model.HinhAnh = await SaveImageAsync(hinhAnh);
        model.CreatedAt = DateTime.Now;
        model.IsDeleted = false;
        _context.ThietBi.Add(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Thêm thiết bị thành công.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.NhanVienThietBi}")]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _context.ThietBi.FindAsync(id);
        if (item == null) return NotFound();
        await LoadFormSelectLists(item.LoaiThietBiId, item.PhongId);
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.NhanVienThietBi}")]
    public async Task<IActionResult> Edit(int id, ThietBi model, IFormFile? hinhAnh)
    {
        if (id != model.Id) return NotFound();
        if (await _context.ThietBi.AnyAsync(t => t.MaTb == model.MaTb && t.Id != id))
            ModelState.AddModelError(nameof(model.MaTb), "Mã thiết bị đã tồn tại.");

        ModelState.Remove("CreatedAt");
        ModelState.Remove("IsDeleted");
        ModelState.Remove("LoaiThietBi");
        ModelState.Remove("Phong");

        if (!ModelState.IsValid)
        {
            await LoadFormSelectLists(model.LoaiThietBiId, model.PhongId);
            return View(model);
        }

        if (hinhAnh != null)
            model.HinhAnh = await SaveImageAsync(hinhAnh);

        _context.ThietBi.Update(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Cập nhật thiết bị thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.NhanVienThietBi}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.ThietBi.FindAsync(id);
        if (item == null) return NotFound();
        // Soft delete
        item.IsDeleted = true;
        _context.ThietBi.Update(item);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Xóa thiết bị thành công.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadFilterLists(int? loaiId = null, int? phongId = null, string? tinhTrang = null)
    {
        ViewBag.LoaiThietBiId = new SelectList(
            await _context.LoaiThietBi.OrderBy(l => l.TenLoaiTB).ToListAsync(), "Id", "TenLoaiTB", loaiId);
        ViewBag.PhongId = new SelectList(
            await _context.Phong.OrderBy(p => p.MaPhong).ToListAsync(), "Id", "TenPhong", phongId);
        ViewBag.TinhTrang = new SelectList(EnumHelper.DanhSachTinhTrang, tinhTrang);
    }

    private async Task LoadFormSelectLists(int? loaiId = null, int? phongId = null)
    {
        ViewBag.LoaiThietBiId = new SelectList(
            await _context.LoaiThietBi.OrderBy(l => l.TenLoaiTB).ToListAsync(), "Id", "TenLoaiTB", loaiId);
        ViewBag.PhongId = new SelectList(
            await _context.Phong.OrderBy(p => p.MaPhong).ToListAsync(), "Id", "TenPhong", phongId);
        ViewBag.NhaCungCapId = new SelectList(
            await _context.NhaCungCap.Where(n => n.TrangThai).OrderBy(n => n.TenNCC).ToListAsync(), "Id", "TenNCC");
        ViewBag.BoMonId = new SelectList(
            await _context.BoMon.OrderBy(b => b.TenBoMon).ToListAsync(), "Id", "TenBoMon");
        ViewBag.TinhTrangs = EnumHelper.DanhSachTinhTrang;
    }

    private static async Task<string?> SaveImageAsync(IFormFile? file)
    {
        if (file == null || file.Length == 0) return null;
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "thietbi");
        Directory.CreateDirectory(path);
        var fullPath = Path.Combine(path, fileName);
        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);
        return $"/uploads/thietbi/{fileName}";
    }
}