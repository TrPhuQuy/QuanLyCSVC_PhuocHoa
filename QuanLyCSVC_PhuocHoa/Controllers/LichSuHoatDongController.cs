using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyCSVC_PhuocHoa.Data;
using QuanLyCSVC_PhuocHoa.Models;
using QuanLyCSVC_PhuocHoa.Helpers;

namespace QuanLyCSVC_PhuocHoa.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class LichSuHoatDongController : Controller
{
    private readonly ApplicationDbContext _context;

    public LichSuHoatDongController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, string? doiTuong, string? fromDate, string? toDate, int page = 1)
    {
        var query = _context.LichSuHoatDong
            .Include(l => l.User)
            .ThenInclude(u => u.NhanVien)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(l => l.HanhDong.Contains(search) || 
                                     (l.User != null && l.User.Email != null && l.User.Email.Contains(search)) || 
                                     (l.User != null && l.User.NhanVien != null && l.User.NhanVien.HoTen.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(doiTuong))
        {
            query = query.Where(l => l.DoiTuong == doiTuong);
        }

        if (!string.IsNullOrWhiteSpace(fromDate) && DateTime.TryParse(fromDate, out var from))
        {
            query = query.Where(l => l.ThoiGian >= from);
        }

        if (!string.IsNullOrWhiteSpace(toDate) && DateTime.TryParse(toDate, out var to))
        {
            // End of day
            var toEnd = to.Date.AddDays(1).AddTicks(-1);
            query = query.Where(l => l.ThoiGian <= toEnd);
        }

        ViewBag.Search = search;
        ViewBag.DoiTuong = doiTuong;
        ViewBag.FromDate = fromDate;
        ViewBag.ToDate = toDate;

        ViewBag.DoiTuongs = await _context.LichSuHoatDong
            .Select(l => l.DoiTuong)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync();

        var pagedLogs = await PaginatedList<LichSuHoatDong>.CreateAsync(query.OrderByDescending(l => l.ThoiGian), page, 20);
        return View(pagedLogs);
    }
}
