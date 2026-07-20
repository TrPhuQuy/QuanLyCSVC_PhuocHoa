namespace QuanLyCSVC_PhuocHoa.Helpers;

/// <summary>
/// Helper cung cấp hiển thị nhãn và badge cho các trạng thái dạng string trong CSDL.
/// </summary>
public static class EnumHelper
{
    // ---- Tình trạng thiết bị ----
    public static string TenTinhTrang(string? tt) => tt switch
    {
        "Đang sử dụng" => "Đang sử dụng",
        "Hư hỏng"      => "Hư hỏng",
        "Đang sửa"     => "Đang sửa chữa",
        "Thanh lý"     => "Đã thanh lý",
        _              => tt ?? "Không rõ"
    };

    public static string BadgeTinhTrang(string? tt) => tt switch
    {
        "Đang sử dụng" => "success",
        "Hư hỏng"      => "danger",
        "Đang sửa"     => "warning",
        "Thanh lý"     => "secondary",
        _              => "light"
    };

    // ---- Trạng thái phiếu ----
    public static string TenTrangThai(string? tt) => tt switch
    {
        "Chờ duyệt"       => "Chờ duyệt",
        "Đã duyệt"        => "Đã duyệt",
        "Từ chối"         => "Từ chối",
        "Hoàn thành"      => "Hoàn thành",
        "Đang thực hiện"  => "Đang thực hiện",
        "Đang sửa"        => "Đang sửa",
        "Đề nghị"         => "Đề nghị",
        "Đã thanh lý"     => "Đã thanh lý",
        _                 => tt ?? "Không rõ"
    };

    public static string BadgeTrangThai(string? tt) => tt switch
    {
        "Chờ duyệt"       => "warning",
        "Đã duyệt"        => "success",
        "Từ chối"         => "danger",
        "Hoàn thành"      => "primary",
        "Đang thực hiện"  => "info",
        "Đang sửa"        => "warning",
        "Đề nghị"         => "secondary",
        _                 => "light"
    };

    // ---- Loại phòng ----
    public static string TenLoaiPhong(string? loai) => loai ?? "Không rõ";

    // Danh sách tình trạng thiết bị
    public static List<string> DanhSachTinhTrang => new()
    {
        "Đang sử dụng", "Hư hỏng", "Đang sửa", "Thanh lý"
    };

    // Danh sách trạng thái phiếu
    public static List<string> DanhSachTrangThaiPhieu => new()
    {
        "Chờ duyệt", "Đã duyệt", "Từ chối", "Hoàn thành", "Đang thực hiện"
    };
}
