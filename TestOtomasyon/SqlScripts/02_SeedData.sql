USE TestOtomasyonDb;
GO

-- ==========================================
-- 1. ORGANIZATION
-- ==========================================
DECLARE @OrgId UNIQUEIDENTIFIER = NEWID();

INSERT INTO [Organization] (id, name) VALUES
(@OrgId, 'Merkez Kurum');

-- ==========================================
-- 2. DEPARTMENTS
-- ==========================================
DECLARE @DeptGenelYonetim UNIQUEIDENTIFIER = NEWID();
DECLARE @DeptIK UNIQUEIDENTIFIER = NEWID();
DECLARE @DeptOtopark UNIQUEIDENTIFIER = NEWID();
DECLARE @DeptGuvenlik UNIQUEIDENTIFIER = NEWID();
DECLARE @DeptTemizlik UNIQUEIDENTIFIER = NEWID();
DECLARE @DeptBilgiIslem UNIQUEIDENTIFIER = NEWID();

INSERT INTO Department (id, organizationId, name) VALUES
(@DeptGenelYonetim, @OrgId, 'Genel Yönetim'),
(@DeptIK, @OrgId, 'İnsan Kaynakları'),
(@DeptOtopark, @OrgId, 'Otopark'),
(@DeptGuvenlik, @OrgId, 'Güvenlik'),
(@DeptTemizlik, @OrgId, 'Temizlik'),
(@DeptBilgiIslem, @OrgId, 'Bilgi İşlem');

-- ==========================================
-- 3. ROLES
-- ==========================================
DECLARE @RoleAdmin UNIQUEIDENTIFIER = NEWID();
DECLARE @RoleIK UNIQUEIDENTIFIER = NEWID();
DECLARE @RoleSahaAmiri UNIQUEIDENTIFIER = NEWID();
DECLARE @RolePersonel UNIQUEIDENTIFIER = NEWID();
DECLARE @RoleOtoparkGorevlisi UNIQUEIDENTIFIER = NEWID();
DECLARE @RoleGuvenlikGorevlisi UNIQUEIDENTIFIER = NEWID();
DECLARE @RoleTemizlikPersoneli UNIQUEIDENTIFIER = NEWID();

INSERT INTO [Role] (id, organizationId, name) VALUES
(@RoleAdmin, @OrgId, 'Admin'),
(@RoleIK, @OrgId, 'İnsan Kaynakları'),
(@RoleSahaAmiri, @OrgId, 'Saha Amiri'),
(@RolePersonel, @OrgId, 'Personel'),
(@RoleOtoparkGorevlisi, @OrgId, 'Otopark Görevlisi'),
(@RoleGuvenlikGorevlisi, @OrgId, 'Güvenlik Görevlisi'),
(@RoleTemizlikPersoneli, @OrgId, 'Temizlik Personeli');

-- ==========================================
-- 4. AUTHORITIES
-- ==========================================
DECLARE @AuthUserManage UNIQUEIDENTIFIER = NEWID();
DECLARE @AuthRoleManage UNIQUEIDENTIFIER = NEWID();
DECLARE @AuthDepartmentManage UNIQUEIDENTIFIER = NEWID();
DECLARE @AuthAuthorityManage UNIQUEIDENTIFIER = NEWID();
DECLARE @AuthTaskCreate UNIQUEIDENTIFIER = NEWID();
DECLARE @AuthTaskAssign UNIQUEIDENTIFIER = NEWID();
DECLARE @AuthTaskView UNIQUEIDENTIFIER = NEWID();
DECLARE @AuthTaskUpdate UNIQUEIDENTIFIER = NEWID();
DECLARE @AuthTaskViewAll UNIQUEIDENTIFIER = NEWID();
DECLARE @AuthDashboardView UNIQUEIDENTIFIER = NEWID();

INSERT INTO Authority (id, name) VALUES
(@AuthUserManage, 'User-Manage'),
(@AuthRoleManage, 'Role-Manage'),
(@AuthDepartmentManage, 'Department-Manage'),
(@AuthAuthorityManage, 'Authority-Manage'),
(@AuthTaskCreate, 'Task-Create'),
(@AuthTaskAssign, 'Task-Assign'),
(@AuthTaskView, 'Task-View'),
(@AuthTaskUpdate, 'Task-Update'),
(@AuthTaskViewAll, 'Task-ViewAll'),
(@AuthDashboardView, 'Dashboard-View');

-- ==========================================
-- 5. ROLE-AUTHORITY ATAMA
-- ==========================================
-- Admin: TÜM yetkiler
INSERT INTO RoleAuthority (roleId, authorityId)
SELECT @RoleAdmin, id FROM Authority;

-- IK: User yönetimi + Dashboard
INSERT INTO RoleAuthority (roleId, authorityId) VALUES
(@RoleIK, @AuthUserManage),
(@RoleIK, @AuthDashboardView);

-- Saha Amiri: Görev oluştur, ata, görüntüle, dashboard
INSERT INTO RoleAuthority (roleId, authorityId) VALUES
(@RoleSahaAmiri, @AuthTaskCreate),
(@RoleSahaAmiri, @AuthTaskAssign),
(@RoleSahaAmiri, @AuthTaskView),
(@RoleSahaAmiri, @AuthTaskUpdate),
(@RoleSahaAmiri, @AuthDashboardView);

-- Personel: Sadece kendi görevini görüntüle/güncelle, dashboard
INSERT INTO RoleAuthority (roleId, authorityId) VALUES
(@RolePersonel, @AuthTaskView),
(@RolePersonel, @AuthTaskUpdate),
(@RolePersonel, @AuthDashboardView);

-- Otopark, Güvenlik, Temizlik: Personel ile aynı (şimdilik)
INSERT INTO RoleAuthority (roleId, authorityId) VALUES
(@RoleOtoparkGorevlisi, @AuthTaskView),
(@RoleOtoparkGorevlisi, @AuthTaskUpdate),
(@RoleOtoparkGorevlisi, @AuthDashboardView),
(@RoleGuvenlikGorevlisi, @AuthTaskView),
(@RoleGuvenlikGorevlisi, @AuthTaskUpdate),
(@RoleGuvenlikGorevlisi, @AuthDashboardView),
(@RoleTemizlikPersoneli, @AuthTaskView),
(@RoleTemizlikPersoneli, @AuthTaskUpdate),
(@RoleTemizlikPersoneli, @AuthDashboardView);

-- ==========================================
-- 6. ADMIN KULLANICI
-- Şifre: Admin123!
-- Hash'i C# tarafında üretip buraya yapıştıracağım
-- Şimdilik geçici düz metin koyuyorum, sonra güncelleyeceğim
-- ==========================================
DECLARE @AdminUserId UNIQUEIDENTIFIER = NEWID();

INSERT INTO [User] (id, organizationId, departmentId, firstName, lastName, name, userName, password) VALUES
(@AdminUserId, @OrgId, @DeptBilgiIslem, 'Sistem', 'Yöneticisi', 'Sistem Yöneticisi', 'admin', '$2a$11$JYy.zNpY49SWM9SigTp8huceF2Da29giHu8nInqyWcMIi8BMDUpba');

-- Admin kullanıcısına Admin rolü ata
INSERT INTO UserRole (userId, roleId) VALUES (@AdminUserId, @RoleAdmin);

PRINT 'Seed data başarıyla eklendi.';
PRINT 'Admin user ID: ' + CAST(@AdminUserId AS NVARCHAR(50));
GO