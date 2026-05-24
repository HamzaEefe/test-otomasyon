using Dapper;
using TestOtomasyon.Repositories.Interfaces;

namespace TestOtomasyon.Helpers
{
    public static class DatabaseSeeder
    {
        // İdempotent: aktif kullanıcı sayısı 50'den azsa seed çalışır,
        // mevcut sistem yöneticisi (admin) korunur ve onun altına 100 demo kullanıcı eklenir.
        public static async Task SeedAsync(IDbConnectionFactory dbFactory)
        {
            using var conn = dbFactory.CreateConnection();
            conn.Open();

            var userCount = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM [User] WHERE status = 1");
            if (userCount >= 50)
                return;

            var orgId = await conn.QuerySingleOrDefaultAsync<Guid?>(
                "SELECT TOP 1 id FROM [Organization] WHERE status = 1");
            if (orgId == null || orgId == Guid.Empty)
            {
                Console.WriteLine("Seed atlandı: Organizasyon yok.");
                return;
            }

            var adminId = await conn.QuerySingleOrDefaultAsync<Guid?>(@"
                SELECT TOP 1 u.id FROM [User] u
                INNER JOIN UserRole ur ON u.id = ur.userId AND ur.status = 1
                INNER JOIN [Role] r ON ur.roleId = r.id AND r.status = 1
                INNER JOIN RoleAuthority ra ON r.id = ra.roleId AND ra.status = 1
                INNER JOIN Authority a ON ra.authorityId = a.id AND a.status = 1
                WHERE u.status = 1 AND a.name = 'User-Manage'");

            if (adminId == null || adminId == Guid.Empty)
            {
                adminId = await conn.QuerySingleOrDefaultAsync<Guid?>(
                    "SELECT TOP 1 id FROM [User] WHERE status = 1 ORDER BY createdOn");
            }
            if (adminId == null || adminId == Guid.Empty)
            {
                Console.WriteLine("Seed atlandı: Sistem yöneticisi (admin) bulunamadı.");
                return;
            }

            var depts = await EnsureDepartmentsAsync(conn, orgId.Value);
            var roles = await EnsureRolesAsync(conn, orgId.Value);

            await GenerateUsersAsync(conn, orgId.Value, depts, roles, adminId.Value);

            Console.WriteLine("Demo kullanıcılar başarıyla oluşturuldu.");
        }

        // ---------- Departmanlar ----------
        private static readonly string[] DeptNames = new[]
        {
            "Bilgi İşlem", "İnsan Kaynakları", "Muhasebe", "Satış", "Pazarlama",
            "Üretim", "Ar-Ge", "Lojistik", "Müşteri Hizmetleri", "Kalite Kontrol"
        };

        private static readonly string[] DeptCodes = new[]
        {
            "BIL", "IK", "MUH", "SAT", "PAZ",
            "URT", "ARG", "LOJ", "MUS", "KAL"
        };

        private static async Task<List<(Guid Id, string Name, string Code)>> EnsureDepartmentsAsync(
            System.Data.IDbConnection conn, Guid orgId)
        {
            var result = new List<(Guid, string, string)>();
            for (int i = 0; i < DeptNames.Length; i++)
            {
                var name = DeptNames[i];
                var existing = await conn.QuerySingleOrDefaultAsync<Guid?>(
                    "SELECT TOP 1 id FROM Department WHERE name = @Name AND status = 1",
                    new { Name = name });

                Guid id;
                if (existing.HasValue && existing.Value != Guid.Empty)
                {
                    id = existing.Value;
                }
                else
                {
                    id = Guid.NewGuid();
                    await conn.ExecuteAsync(@"
                        INSERT INTO Department (id, organizationId, name, createdOn, status)
                        VALUES (@Id, @OrgId, @Name, @Now, 1)",
                        new { Id = id, OrgId = orgId, Name = name, Now = DateTime.Now });
                }
                result.Add((id, name, DeptCodes[i]));
            }
            return result;
        }

        // ---------- Roller ----------
        private static async Task<Dictionary<string, Guid>> EnsureRolesAsync(
            System.Data.IDbConnection conn, Guid orgId)
        {
            var roles = new Dictionary<string, Guid>();

            var roleDefs = new (string Name, string[] Auths)[]
            {
                ("Departman Müdürü", new[]
                {
                    "Task-Create", "Task-Update", "Task-View", "Task-Approve",
                    "Organization-View", "Dashboard-View", "Task-SystemView"
                }),
                ("Saha Amiri", new[]
                {
                    "Task-Create", "Task-Update", "Task-View", "Task-Approve",
                    "Organization-View", "Dashboard-View", "Task-SystemView"
                }),
                ("Personel", new[]
                {
                    "Task-View", "Task-Update", "Task-Propose",
                    "Organization-View", "Dashboard-View", "Task-SystemView"
                }),
            };

            foreach (var (name, auths) in roleDefs)
            {
                var existing = await conn.QuerySingleOrDefaultAsync<Guid?>(
                    "SELECT TOP 1 id FROM [Role] WHERE name = @Name AND status = 1",
                    new { Name = name });

                Guid roleId;
                if (existing.HasValue && existing.Value != Guid.Empty)
                {
                    roleId = existing.Value;
                }
                else
                {
                    roleId = Guid.NewGuid();
                    await conn.ExecuteAsync(@"
                        INSERT INTO [Role] (id, organizationId, name, createdOn, status)
                        VALUES (@Id, @OrgId, @Name, @Now, 1)",
                        new { Id = roleId, OrgId = orgId, Name = name, Now = DateTime.Now });
                }
                roles[name] = roleId;

                // Yetkileri ata (eksikleri ekle, varsa atla)
                foreach (var authName in auths)
                {
                    var authId = await conn.QuerySingleOrDefaultAsync<Guid?>(
                        "SELECT TOP 1 id FROM Authority WHERE name = @Name AND status = 1",
                        new { Name = authName });
                    if (!authId.HasValue || authId.Value == Guid.Empty) continue;

                    var raExists = await conn.ExecuteScalarAsync<int>(@"
                        SELECT COUNT(*) FROM RoleAuthority
                        WHERE roleId = @RoleId AND authorityId = @AuthId AND status = 1",
                        new { RoleId = roleId, AuthId = authId.Value });
                    if (raExists > 0) continue;

                    await conn.ExecuteAsync(@"
                        INSERT INTO RoleAuthority (id, roleId, authorityId, createdOn, status)
                        VALUES (@Id, @RoleId, @AuthId, @Now, 1)",
                        new
                        {
                            Id = Guid.NewGuid(),
                            RoleId = roleId,
                            AuthId = authId.Value,
                            Now = DateTime.Now
                        });
                }
            }

            return roles;
        }

        // ---------- İsim havuzu ----------
        private static readonly string[] FirstNames = new[]
        {
            // Erkek
            "Ahmet", "Mehmet", "Mustafa", "Ali", "Hasan", "Hüseyin", "İbrahim", "İsmail",
            "Murat", "Osman", "Yusuf", "Emre", "Kemal", "Selim", "Tolga", "Burak",
            "Cem", "Deniz", "Ercan", "Fatih", "Gökhan", "Hakan", "İlker", "Kerem",
            "Levent", "Mert", "Oğuz", "Serkan", "Tuncay", "Umut", "Volkan", "Yiğit",
            "Barış", "Caner", "Doruk", "Eren", "Furkan", "Görkem",
            // Kadın
            "Ayşe", "Fatma", "Emine", "Hatice", "Zeynep", "Elif", "Sema", "Aynur",
            "Berna", "Ceren", "Damla", "Esra", "Gül", "Hilal", "İrem", "Kübra",
            "Leyla", "Melike", "Nilgün", "Özlem", "Pınar", "Rabia", "Selin", "Tuğba",
            "Yasemin", "Aylin", "Beste", "Cansu", "Derya", "Ece", "Funda", "Gülşen"
        };

        private static readonly string[] LastNames = new[]
        {
            "Yılmaz", "Kaya", "Demir", "Çelik", "Şahin", "Yıldız", "Yıldırım", "Öztürk",
            "Aydın", "Özdemir", "Arslan", "Doğan", "Kılıç", "Aslan", "Çetin", "Kara",
            "Koç", "Kurt", "Özkan", "Şimşek", "Polat", "Erdoğan", "Korkmaz", "Acar",
            "Aktaş", "Akın", "Altın", "Bal", "Bayrak", "Bulut", "Çakır", "Çoban",
            "Demirci", "Ekinci", "Erdem", "Erol", "Eren", "Güler", "Güneş", "Karaca",
            "Keskin", "Sezer", "Soylu", "Taş", "Yalçın", "Aksoy", "Tekin", "Uçar",
            "Tunç", "Yavuz"
        };

        // Şehir kodları (telefon ön ek havuzu için)
        private static readonly string[] PhonePrefixes = new[]
        {
            "532", "533", "534", "535", "536", "537", "538", "539",
            "541", "542", "543", "544", "545", "546", "547", "549",
            "551", "552", "553", "554", "555", "559", "505", "506"
        };

        // ---------- Kullanıcı üretimi ----------
        private static async Task GenerateUsersAsync(
            System.Data.IDbConnection conn,
            Guid orgId,
            List<(Guid Id, string Name, string Code)> depts,
            Dictionary<string, Guid> roles,
            Guid adminId)
        {
            var rng = new Random(42); // Sabit seed: her seferinde aynı kullanıcılar üretilir
            var pwdHash = BCrypt.Net.BCrypt.HashPassword("Test123!");

            var usedUserNames = (await conn.QueryAsync<string>(
                "SELECT userName FROM [User] WHERE status = 1")).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var seqByDept = new Dictionary<Guid, int>();
            int globalSeq = 1;

            // Her departman için: 1 müdür + 2 saha amiri + 7 personel = 10 kişi → 10*10 = 100
            foreach (var dept in depts)
            {
                // 1. Departman Müdürü
                var manager = MakeUser(rng, dept, ref globalSeq, seqByDept, pwdHash, orgId, parentId: adminId);
                manager.UserName = EnsureUniqueUserName(manager.UserName, usedUserNames);
                manager.Email = $"{manager.UserName}@testotomasyon.com";
                await InsertUserAsync(conn, manager);
                await AssignRoleAsync(conn, manager.Id, roles["Departman Müdürü"]);

                // 2 Saha Amiri
                var amirs = new List<UserRow>();
                for (int a = 0; a < 2; a++)
                {
                    var amir = MakeUser(rng, dept, ref globalSeq, seqByDept, pwdHash, orgId, parentId: manager.Id);
                    amir.UserName = EnsureUniqueUserName(amir.UserName, usedUserNames);
                    amir.Email = $"{amir.UserName}@testotomasyon.com";
                    await InsertUserAsync(conn, amir);
                    await AssignRoleAsync(conn, amir.Id, roles["Saha Amiri"]);
                    amirs.Add(amir);
                }

                // 7 Personel — amirlere dağıt (4 + 3)
                int[] personelDagilim = { 4, 3 };
                for (int ai = 0; ai < amirs.Count; ai++)
                {
                    for (int p = 0; p < personelDagilim[ai]; p++)
                    {
                        var personel = MakeUser(rng, dept, ref globalSeq, seqByDept, pwdHash, orgId, parentId: amirs[ai].Id);
                        personel.UserName = EnsureUniqueUserName(personel.UserName, usedUserNames);
                        personel.Email = $"{personel.UserName}@testotomasyon.com";
                        await InsertUserAsync(conn, personel);
                        await AssignRoleAsync(conn, personel.Id, roles["Personel"]);
                    }
                }
            }
        }

        private class UserRow
        {
            public Guid Id { get; set; }
            public Guid OrgId { get; set; }
            public Guid DeptId { get; set; }
            public string FirstName { get; set; } = "";
            public string LastName { get; set; } = "";
            public string Name => $"{FirstName} {LastName}";
            public string UserName { get; set; } = "";
            public string PasswordHash { get; set; } = "";
            public string Email { get; set; } = "";
            public string MobilePhone { get; set; } = "";
            public string AccountingCode { get; set; } = "";
            public Guid? ParentId { get; set; }
        }

        private static UserRow MakeUser(
            Random rng,
            (Guid Id, string Name, string Code) dept,
            ref int globalSeq,
            Dictionary<Guid, int> seqByDept,
            string pwdHash,
            Guid orgId,
            Guid parentId)
        {
            var first = FirstNames[rng.Next(FirstNames.Length)];
            var last = LastNames[rng.Next(LastNames.Length)];

            if (!seqByDept.ContainsKey(dept.Id)) seqByDept[dept.Id] = 0;
            seqByDept[dept.Id]++;

            var userName = (Normalize(first) + "." + Normalize(last)).ToLowerInvariant();
            var phone = $"{PhonePrefixes[rng.Next(PhonePrefixes.Length)]} {rng.Next(100, 999):000} {rng.Next(0, 99):00} {rng.Next(0, 99):00}";
            var accCode = $"{dept.Code}-{seqByDept[dept.Id]:000}";

            globalSeq++;

            return new UserRow
            {
                Id = Guid.NewGuid(),
                OrgId = orgId,
                DeptId = dept.Id,
                FirstName = first,
                LastName = last,
                UserName = userName,
                PasswordHash = pwdHash,
                MobilePhone = phone,
                AccountingCode = accCode,
                ParentId = parentId
            };
        }

        private static string EnsureUniqueUserName(string baseName, HashSet<string> used)
        {
            if (!used.Contains(baseName))
            {
                used.Add(baseName);
                return baseName;
            }
            int n = 2;
            while (used.Contains($"{baseName}{n}")) n++;
            var unique = $"{baseName}{n}";
            used.Add(unique);
            return unique;
        }

        // Türkçe karakterleri ASCII'ye çevir (kullanıcı adı / e-posta için)
        private static string Normalize(string s)
        {
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (var c in s)
            {
                char m = c;
                switch (c)
                {
                    case 'ı': m = 'i'; break;
                    case 'İ': m = 'I'; break;
                    case 'ğ': m = 'g'; break;
                    case 'Ğ': m = 'G'; break;
                    case 'ü': m = 'u'; break;
                    case 'Ü': m = 'U'; break;
                    case 'ş': m = 's'; break;
                    case 'Ş': m = 'S'; break;
                    case 'ö': m = 'o'; break;
                    case 'Ö': m = 'O'; break;
                    case 'ç': m = 'c'; break;
                    case 'Ç': m = 'C'; break;
                }
                if (char.IsLetterOrDigit(m)) sb.Append(m);
            }
            return sb.ToString();
        }

        private static async Task InsertUserAsync(System.Data.IDbConnection conn, UserRow u)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO [User]
                    (id, organizationId, departmentId, firstName, lastName, name, userName,
                     password, email, mobilePhone, accountingCode, userType, parentId, createdOn, status)
                VALUES
                    (@Id, @OrgId, @DeptId, @FirstName, @LastName, @Name, @UserName,
                     @Password, @Email, @Phone, @AccCode, 1, @ParentId, @Now, 1)",
                new
                {
                    u.Id,
                    u.OrgId,
                    u.DeptId,
                    u.FirstName,
                    u.LastName,
                    u.Name,
                    u.UserName,
                    Password = u.PasswordHash,
                    u.Email,
                    Phone = u.MobilePhone,
                    AccCode = u.AccountingCode,
                    u.ParentId,
                    Now = DateTime.Now
                });
        }

        private static async Task AssignRoleAsync(System.Data.IDbConnection conn, Guid userId, Guid roleId)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO UserRole (id, userId, roleId, createdOn, status)
                VALUES (@Id, @UserId, @RoleId, @Now, 1)",
                new
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    RoleId = roleId,
                    Now = DateTime.Now
                });
        }
    }
}
