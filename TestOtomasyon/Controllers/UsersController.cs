using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using TestOtomasyon.Entities;
using TestOtomasyon.Helpers;
using TestOtomasyon.Models;
using TestOtomasyon.Repositories.Interfaces;
using TestOtomasyon.Resources.Languages;

namespace TestOtomasyon.Controllers
{
    [HasAuthority("User-Manage")]
    public class UsersController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IDepartmentRepository _departmentRepository;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IStringLocalizer<Lang> _localizer;

        public UsersController(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IDepartmentRepository departmentRepository,
            IDbConnectionFactory dbFactory,
            IStringLocalizer<Lang> localizer)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _departmentRepository = departmentRepository;
            _dbFactory = dbFactory;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userRepository.GetAllAsync();
            var userList = users.ToList();

            var rolesByUser = new Dictionary<Guid, List<string>>();
            foreach (var u in userList)
            {
                var roles = await _userRepository.GetRolesAsync(u.Id);
                rolesByUser[u.Id] = roles.Select(r => r.Name).ToList();
            }

            ViewBag.RolesByUser = rolesByUser;
            return View(userList);
        }

        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns(null);
            return View(new UserCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateViewModel model)
        {
            if (await _userRepository.UserNameExistsAsync(model.UserName))
                ModelState.AddModelError(nameof(model.UserName), _localizer["Msg.UserNameExists"]);

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(null);
                return View(model);
            }

            using var conn = _dbFactory.CreateConnection();
            var orgId = await Dapper.SqlMapper.QuerySingleAsync<Guid>(conn,
                "SELECT TOP 1 id FROM [Organization] WHERE status = 1");

            var user = new User
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                UserName = model.UserName,
                Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
                OrganizationId = orgId,
                DepartmentId = model.DepartmentId,
                ParentId = model.ParentId,
                MobilePhone = model.MobilePhone,
                Email = model.Email,
                AccountingCode = model.AccountingCode,
                UserType = 1
            };

            var newId = await _userRepository.CreateAsync(user);

            if (model.SelectedRoleIds.Any())
                await _userRepository.AssignRolesAsync(newId, model.SelectedRoleIds);

            TempData["Success"] = _localizer["Msg.UserCreated"].Value;
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                return NotFound();

            var userRoles = await _userRepository.GetRolesAsync(id);

            var model = new UserEditViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.UserName,
                DepartmentId = user.DepartmentId,
                ParentId = user.ParentId,
                MobilePhone = user.MobilePhone,
                AccountingCode = user.AccountingCode,
                SelectedRoleIds = userRoles.Select(r => r.Id).ToList()
            };

            await PopulateDropdowns(id);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserEditViewModel model)
        {
            
            if (model.ParentId == model.Id)
                ModelState.AddModelError(nameof(model.ParentId), _localizer["Msg.SelfManager"]);

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(model.Id);
                return View(model);
            }

            var user = await _userRepository.GetByIdAsync(model.Id);
            if (user == null)
                return NotFound();

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.DepartmentId = model.DepartmentId;
            user.ParentId = model.ParentId;
            user.MobilePhone = model.MobilePhone;
            user.Email = model.Email;
            user.AccountingCode = model.AccountingCode;

            await _userRepository.UpdateAsync(user);
            await _userRepository.AssignRolesAsync(model.Id, model.SelectedRoleIds);

            TempData["Success"] = _localizer["Msg.UserUpdated"].Value;
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ResetPassword(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                return NotFound();

            var model = new UserResetPasswordViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                FullName = user.Name
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(UserResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            await _userRepository.ResetPasswordAsync(model.Id, model.NewPassword);
            TempData["Success"] = _localizer["Msg.PasswordReset", model.FullName].Value;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var currentUserId = User.GetUserId();
            if (currentUserId == id.ToString())
            {
                TempData["Error"] = _localizer["Msg.CannotDeleteSelf"].Value;
                return RedirectToAction(nameof(Index));
            }

            await _userRepository.SoftDeleteAsync(id);
            TempData["Success"] = _localizer["Msg.UserDeleted"].Value;
            return RedirectToAction(nameof(Index));
        }

        
        private async Task PopulateDropdowns(Guid? excludeUserId)
        {
            ViewBag.Departments = await _departmentRepository.GetAllAsync();
            ViewBag.Roles = await _roleRepository.GetAllAsync();

            
            var allUsers = await _userRepository.GetAllAsync();
            if (excludeUserId.HasValue)
                allUsers = allUsers.Where(u => u.Id != excludeUserId.Value);

            ViewBag.PossibleParents = allUsers.ToList();
        }
    }
}