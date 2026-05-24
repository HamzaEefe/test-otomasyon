using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using TestOtomasyon.Entities;
using TestOtomasyon.Helpers;
using TestOtomasyon.Repositories.Interfaces;
using TestOtomasyon.Resources.Languages;

namespace TestOtomasyon.Controllers
{
    [HasAuthority("Department-Manage")]
    public class DepartmentsController : Controller
    {
        private readonly IDepartmentRepository _departmentRepository;
        private readonly IStringLocalizer<Lang> _localizer;

        public DepartmentsController(IDepartmentRepository departmentRepository, IStringLocalizer<Lang> localizer)
        {
            _departmentRepository = departmentRepository;
            _localizer = localizer;
        }

        // LİSTE
        public async Task<IActionResult> Index()
        {
            var departments = await _departmentRepository.GetAllAsync();
            return View(departments);
        }

        // YENİ - GET
        public IActionResult Create()
        {
            return View(new Department());
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Department department)
        {
            if (!ModelState.IsValid)
                return View(department);

           
            var orgIdClaim = User.Claims.FirstOrDefault(c => c.Type == "OrganizationId")?.Value;

            
            using var conn = HttpContext.RequestServices
                .GetRequiredService<IDbConnectionFactory>()
                .CreateConnection();
            var orgId = await Dapper.SqlMapper.QuerySingleAsync<Guid>(conn,
                "SELECT TOP 1 id FROM [Organization] WHERE status = 1");

            department.OrganizationId = orgId;
            await _departmentRepository.CreateAsync(department);

            TempData["Success"] = _localizer["Msg.DeptCreated"].Value;
            return RedirectToAction(nameof(Index));
        }

        
        public async Task<IActionResult> Edit(Guid id)
        {
            var department = await _departmentRepository.GetByIdAsync(id);
            if (department == null)
                return NotFound();
            return View(department);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Department department)
        {
            if (!ModelState.IsValid)
                return View(department);

            await _departmentRepository.UpdateAsync(department);
            TempData["Success"] = _localizer["Msg.DeptUpdated"].Value;
            return RedirectToAction(nameof(Index));
        }

      
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _departmentRepository.SoftDeleteAsync(id);
            TempData["Success"] = _localizer["Msg.DeptDeleted"].Value;
            return RedirectToAction(nameof(Index));
        }
    }
}