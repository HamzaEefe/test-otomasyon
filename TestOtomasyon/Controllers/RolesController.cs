using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using TestOtomasyon.Entities;
using TestOtomasyon.Helpers;
using TestOtomasyon.Models;
using TestOtomasyon.Repositories.Interfaces;
using TestOtomasyon.Resources.Languages;

namespace TestOtomasyon.Controllers
{
    [HasAuthority("Role-Manage")]
    public class RolesController : Controller
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IAuthorityRepository _authorityRepository;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IStringLocalizer<Lang> _localizer;

        public RolesController(
            IRoleRepository roleRepository,
            IAuthorityRepository authorityRepository,
            IDbConnectionFactory dbFactory,
            IStringLocalizer<Lang> localizer)
        {
            _roleRepository = roleRepository;
            _authorityRepository = authorityRepository;
            _dbFactory = dbFactory;
            _localizer = localizer;
        }

        // LİSTE
        public async Task<IActionResult> Index()
        {
            var roles = await _roleRepository.GetAllAsync();
            return View(roles);
        }

        // YENİ - GET
        public IActionResult Create()
        {
            return View(new Role());
        }

        // YENİ - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Role role)
        {
            if (!ModelState.IsValid)
                return View(role);

            using var conn = _dbFactory.CreateConnection();
            var orgId = await Dapper.SqlMapper.QuerySingleAsync<Guid>(conn,
                "SELECT TOP 1 id FROM [Organization] WHERE status = 1");

            role.OrganizationId = orgId;
            await _roleRepository.CreateAsync(role);

            TempData["Success"] = _localizer["Msg.RoleCreated"].Value;
            return RedirectToAction(nameof(Index));
        }

        // DÜZENLE - GET
        public async Task<IActionResult> Edit(Guid id)
        {
            var role = await _roleRepository.GetByIdAsync(id);
            if (role == null)
                return NotFound();
            return View(role);
        }

        // DÜZENLE - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Role role)
        {
            if (!ModelState.IsValid)
                return View(role);

            await _roleRepository.UpdateAsync(role);
            TempData["Success"] = _localizer["Msg.RoleUpdated"].Value;
            return RedirectToAction(nameof(Index));
        }

        // SİL - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _roleRepository.SoftDeleteAsync(id);
            TempData["Success"] = _localizer["Msg.RoleDeleted"].Value;
            return RedirectToAction(nameof(Index));
        }

        // YETKİ ATAMA - GET
        public async Task<IActionResult> Authorities(Guid id)
        {
            var role = await _roleRepository.GetByIdAsync(id);
            if (role == null)
                return NotFound();

            var allAuthorities = await _authorityRepository.GetAllAsync();
            var assignedAuthorities = await _roleRepository.GetAuthoritiesAsync(id);
            var assignedIds = assignedAuthorities.Select(a => a.Id).ToHashSet();

            var viewModel = new RoleAuthoritiesViewModel
            {
                RoleId = role.Id,
                RoleName = role.Name,
                Authorities = allAuthorities.Select(a => new AuthorityCheckItem
                {
                    Id = a.Id,
                    Name = a.Name,
                    IsAssigned = assignedIds.Contains(a.Id)
                }).ToList()
            };

            return View(viewModel);
        }

        // YETKİ ATAMA - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Authorities(Guid id, List<Guid>? selectedAuthorities)
        {
            selectedAuthorities ??= new List<Guid>();
            await _roleRepository.AssignAuthoritiesAsync(id, selectedAuthorities);

            TempData["Success"] = _localizer["Msg.RoleAuthsUpdated"].Value;
            return RedirectToAction(nameof(Index));
        }
    }
}