using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QuanLyCSVC_PhuocHoa.Models.Entities;

namespace QuanLyCSVC_PhuocHoa.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<BoMon> BoMon => Set<BoMon>();
    public DbSet<ChiTietKiemKe> ChiTietKiemKe => Set<ChiTietKiemKe>();
    public DbSet<ChiTietPhieuMuon> ChiTietPhieuMuon => Set<ChiTietPhieuMuon>();
    public DbSet<ChiTietPhieuNhap> ChiTietPhieuNhap => Set<ChiTietPhieuNhap>();
    public DbSet<DieuChuyen> DieuChuyen => Set<DieuChuyen>();
    public DbSet<DotKiemKe> DotKiemKe => Set<DotKiemKe>();
    public DbSet<LichSuHoatDong> LichSuHoatDong => Set<LichSuHoatDong>();
    public DbSet<LoaiPhong> LoaiPhong => Set<LoaiPhong>();
    public DbSet<LoaiThietBi> LoaiThietBi => Set<LoaiThietBi>();
    public DbSet<NhaCungCap> NhaCungCap => Set<NhaCungCap>();
    public DbSet<NhanVien> NhanVien => Set<NhanVien>();
    public DbSet<PhieuBaoTri> PhieuBaoTri => Set<PhieuBaoTri>();
    public DbSet<PhieuMuon> PhieuMuon => Set<PhieuMuon>();
    public DbSet<PhieuNhap> PhieuNhap => Set<PhieuNhap>();
    public DbSet<PhieuThanhLy> PhieuThanhLy => Set<PhieuThanhLy>();
    public DbSet<Phong> Phong => Set<Phong>();
    public DbSet<ThanhVienHoiDong> ThanhVienHoiDong => Set<ThanhVienHoiDong>();
    public DbSet<ThietBi> ThietBi => Set<ThietBi>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Customize the ASP.NET Identity model mappings to fit the database
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("AspNetUsers");
            entity.HasOne(d => d.NhanVien)
                  .WithOne(p => p.User)
                  .HasForeignKey<NhanVien>(d => d.UserId)
                  .HasConstraintName("FK_NhanVien_Users");
        });

        builder.Entity<BoMon>(entity =>
        {
            entity.ToTable("BoMon");
            entity.HasKey(e => e.Id).HasName("PK__BoMon__3214EC072665B10F");
        });

        builder.Entity<ChiTietKiemKe>(entity =>
        {
            entity.ToTable("ChiTietKiemKe");
            entity.HasKey(e => e.Id).HasName("PK__ChiTietK__3214EC07B7A278AE");
            entity.HasOne(d => d.DotKiemKe).WithMany(p => p.ChiTietKiemKe).HasForeignKey(d => d.DotKiemKeId).HasConstraintName("FK_CTKiemKe_DotKiemKe");
            entity.HasOne(d => d.ThietBi).WithMany(p => p.ChiTietKiemKe).HasForeignKey(d => d.ThietBiId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_CTKiemKe_ThietBi");
        });

        builder.Entity<ChiTietPhieuMuon>(entity =>
        {
            entity.ToTable("ChiTietPhieuMuon");
            entity.HasKey(e => e.Id).HasName("PK__ChiTietP__3214EC078F5E2870");
            entity.HasOne(d => d.PhieuMuon).WithMany(p => p.ChiTietPhieuMuon).HasForeignKey(d => d.PhieuMuonId).HasConstraintName("FK_CTPhieuMuon_PhieuMuon");
            entity.HasOne(d => d.ThietBi).WithMany(p => p.ChiTietPhieuMuon).HasForeignKey(d => d.ThietBiId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_CTPhieuMuon_ThietBi");
        });

        builder.Entity<ChiTietPhieuNhap>(entity =>
        {
            entity.ToTable("ChiTietPhieuNhap");
            entity.HasKey(e => e.Id).HasName("PK__ChiTietP__3214EC075920BBE3");
            entity.HasOne(d => d.PhieuNhap).WithMany(p => p.ChiTietPhieuNhap).HasForeignKey(d => d.PhieuNhapId).HasConstraintName("FK_CTPhieuNhap_PhieuNhap");
            entity.HasOne(d => d.ThietBi).WithMany(p => p.ChiTietPhieuNhap).HasForeignKey(d => d.ThietBiId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_CTPhieuNhap_ThietBi");
        });

        builder.Entity<DieuChuyen>(entity =>
        {
            entity.ToTable("DieuChuyen");
            entity.HasKey(e => e.Id).HasName("PK__DieuChuy__3214EC0707DB4B13");
            entity.Property(e => e.NgayDeNghi).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.TrangThai).HasDefaultValue("Chờ duyệt");
            entity.HasOne(d => d.NguoiDeNghi).WithMany(p => p.DieuChuyenNguoiDeNghi).HasForeignKey(d => d.NguoiDeNghiId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_DieuChuyen_NguoiDeNghi");
            entity.HasOne(d => d.NguoiDuyet).WithMany(p => p.DieuChuyenNguoiDuyet).HasForeignKey(d => d.NguoiDuyetId).HasConstraintName("FK_DieuChuyen_NguoiDuyet");
            entity.HasOne(d => d.PhongDen).WithMany(p => p.DieuChuyenPhongDen).HasForeignKey(d => d.PhongDenId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_DieuChuyen_PhongDen");
            entity.HasOne(d => d.PhongDi).WithMany(p => p.DieuChuyenPhongDi).HasForeignKey(d => d.PhongDiId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_DieuChuyen_PhongDi");
            entity.HasOne(d => d.ThietBi).WithMany(p => p.DieuChuyen).HasForeignKey(d => d.ThietBiId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_DieuChuyen_ThietBi");
        });

        builder.Entity<DotKiemKe>(entity =>
        {
            entity.ToTable("DotKiemKe");
            entity.HasKey(e => e.Id).HasName("PK__DotKiemK__3214EC07C52359E6");
            entity.Property(e => e.TrangThai).HasDefaultValue("Đang thực hiện");
            entity.HasOne(d => d.NguoiChuTri).WithMany(p => p.DotKiemKe).HasForeignKey(d => d.NguoiChuTriId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_DotKiemKe_NhanVien");
        });

        builder.Entity<LichSuHoatDong>(entity =>
        {
            entity.ToTable("LichSuHoatDong");
            entity.HasKey(e => e.Id).HasName("PK__LichSuHo__3214EC079FB82A55");
            entity.Property(e => e.ThoiGian).HasDefaultValueSql("(getdate())");
            entity.HasOne(d => d.User).WithMany(p => p.LichSuHoatDong).HasForeignKey(d => d.UserId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_AuditLog_Users");
        });

        builder.Entity<LoaiPhong>(entity =>
        {
            entity.ToTable("LoaiPhong");
            entity.HasKey(e => e.Id).HasName("PK__LoaiPhon__3214EC07BFB14B01");
        });

        builder.Entity<LoaiThietBi>(entity =>
        {
            entity.ToTable("LoaiThietBi");
            entity.HasKey(e => e.Id).HasName("PK__LoaiThie__3214EC07092B5303");
        });

        builder.Entity<NhaCungCap>(entity =>
        {
            entity.ToTable("NhaCungCap");
            entity.HasKey(e => e.Id).HasName("PK__NhaCungC__3214EC07CBE0BDDF");
            entity.Property(e => e.TrangThai).HasDefaultValue(true);
        });

        builder.Entity<NhanVien>(entity =>
        {
            entity.ToTable("NhanVien");
            entity.HasKey(e => e.Id).HasName("PK__NhanVien__3214EC07193D1370");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.TrangThai).HasDefaultValue(true);
            entity.HasOne(d => d.BoMon).WithMany(p => p.NhanVien).HasForeignKey(d => d.BoMonId).HasConstraintName("FK_NhanVien_BoMon");
        });

        builder.Entity<PhieuBaoTri>(entity =>
        {
            entity.ToTable("PhieuBaoTri");
            entity.HasKey(e => e.Id).HasName("PK__PhieuBao__3214EC07C37912F9");
            entity.Property(e => e.TrangThai).HasDefaultValue("Đang sửa");
            entity.HasOne(d => d.NguoiGui).WithMany(p => p.PhieuBaoTri).HasForeignKey(d => d.NguoiGuiId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_PhieuBaoTri_NhanVien");
            entity.HasOne(d => d.ThietBi).WithMany(p => p.PhieuBaoTri).HasForeignKey(d => d.ThietBiId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_PhieuBaoTri_ThietBi");
        });

        builder.Entity<PhieuMuon>(entity =>
        {
            entity.ToTable("PhieuMuon");
            entity.HasKey(e => e.Id).HasName("PK__PhieuMuo__3214EC07566CDC7D");
            entity.Property(e => e.NgayDangKy).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.TrangThai).HasDefaultValue("Chờ duyệt");
            entity.HasOne(d => d.GiaoVien).WithMany(p => p.PhieuMuonGiaoVien).HasForeignKey(d => d.GiaoVienId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_PhieuMuon_GiaoVien");
            entity.HasOne(d => d.NguoiDuyet).WithMany(p => p.PhieuMuonNguoiDuyet).HasForeignKey(d => d.NguoiDuyetId).HasConstraintName("FK_PhieuMuon_NguoiDuyet");
        });

        builder.Entity<PhieuNhap>(entity =>
        {
            entity.ToTable("PhieuNhap");
            entity.HasKey(e => e.Id).HasName("PK__PhieuNha__3214EC0751EC3B3E");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.HasOne(d => d.NguoiNhap).WithMany(p => p.PhieuNhap).HasForeignKey(d => d.NguoiNhapId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_PhieuNhap_NhanVien");
            entity.HasOne(d => d.NhaCungCap).WithMany(p => p.PhieuNhap).HasForeignKey(d => d.NhaCungCapId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_PhieuNhap_NhaCungCap");
        });

        builder.Entity<PhieuThanhLy>(entity =>
        {
            entity.ToTable("PhieuThanhLy");
            entity.HasKey(e => e.Id).HasName("PK__PhieuTha__3214EC07051460AC");
            entity.Property(e => e.TrangThai).HasDefaultValue("Đề nghị");
            entity.HasOne(d => d.ThietBi).WithMany(p => p.PhieuThanhLy).HasForeignKey(d => d.ThietBiId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_PhieuThanhLy_ThietBi");
        });

        builder.Entity<Phong>(entity =>
        {
            entity.ToTable("Phong");
            entity.HasKey(e => e.Id).HasName("PK__Phong__3214EC0728758B90");
            entity.Property(e => e.TrangThai).HasDefaultValue("Đang sử dụng");
            entity.HasOne(d => d.LoaiPhong).WithMany(p => p.Phong).HasForeignKey(d => d.LoaiPhongId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_Phong_LoaiPhong");
            entity.HasOne(d => d.NguoiPhuTrach).WithMany(p => p.Phong).HasForeignKey(d => d.NguoiPhuTrachId).HasConstraintName("FK_Phong_NhanVien");
        });

        builder.Entity<ThanhVienHoiDong>(entity =>
        {
            entity.ToTable("ThanhVienHoiDong");
            entity.HasKey(e => e.Id).HasName("PK__ThanhVie__3214EC074F728684");
            entity.HasOne(d => d.NhanVien).WithMany(p => p.ThanhVienHoiDong).HasForeignKey(d => d.NhanVienId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_HoiDongTL_NhanVien");
            entity.HasOne(d => d.PhieuThanhLy).WithMany(p => p.ThanhVienHoiDong).HasForeignKey(d => d.PhieuThanhLyId).HasConstraintName("FK_HoiDongTL_PhieuThanhLy");
        });

        builder.Entity<ThietBi>(entity =>
        {
            entity.ToTable("ThietBi");
            entity.HasKey(e => e.Id).HasName("PK__ThietBi__3214EC0704EB0300");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.SoLuong).HasDefaultValue(1);
            entity.Property(e => e.TinhTrang).HasDefaultValue("Đang sử dụng");
            entity.HasOne(d => d.BoMon).WithMany(p => p.ThietBi).HasForeignKey(d => d.BoMonId).HasConstraintName("FK_ThietBi_BoMon");
            entity.HasOne(d => d.LoaiThietBi).WithMany(p => p.ThietBi).HasForeignKey(d => d.LoaiThietBiId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_ThietBi_LoaiThietBi");
            entity.HasOne(d => d.NhaCungCap).WithMany(p => p.ThietBi).HasForeignKey(d => d.NhaCungCapId).HasConstraintName("FK_ThietBi_NhaCungCap");
            entity.HasOne(d => d.Phong).WithMany(p => p.ThietBi).HasForeignKey(d => d.PhongId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_ThietBi_Phong");
        });
    }
}
