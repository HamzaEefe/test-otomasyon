using Microsoft.AspNetCore.Mvc;
using TestOtomasyon.Helpers;
using TestOtomasyon.Models;
using TestOtomasyon.Repositories.Interfaces;
using System.Diagnostics;

namespace TestOtomasyon.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUserRepository _userRepository;
        private readonly IWorkTaskRepository _taskRepository;
        private readonly IDepartmentRepository _departmentRepository;

        public HomeController(
            ILogger<HomeController> logger,
            IUserRepository userRepository,
            IWorkTaskRepository taskRepository,
            IDepartmentRepository departmentRepository,
            IMessageRepository messageRepository) : base(messageRepository)
        {
            _logger = logger;
            _userRepository = userRepository;
            _taskRepository = taskRepository;
            _departmentRepository = departmentRepository;
        }

        public async Task<IActionResult> Index()
        {
            await SetNotificationCount();

            var userIdStr = User.GetUserId();
            if (string.IsNullOrEmpty(userIdStr))
                return RedirectToAction("Login", "Account");

            var userId = Guid.Parse(userIdStr);
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var model = new DashboardViewModel
            {
                UserName = user.UserName,
                FullName = user.Name,
                DepartmentName = user.DepartmentName,
                Roles = User.Claims
                    .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToList(),
                AuthorityCount = User.Claims.Count(c => c.Type == "Authority")
            };

            
            model.ShowPersonelPanel = User.HasAuthority("Task-View");
            model.ShowSahaAmiriPanel = User.HasAuthority("Task-Create");
            model.ShowAdminPanel = User.HasAuthority("Task-ViewAll") || User.HasAuthority("User-Manage");

            
            if (model.ShowPersonelPanel)
            {
                model.MyTaskAssigned = await _taskRepository.CountByAssigneeAndStatusAsync(userId, TaskStatusHelper.Atandi);
                model.MyTaskInProgress = await _taskRepository.CountByAssigneeAndStatusAsync(userId, TaskStatusHelper.Basladi);
                model.MyTaskCompleted = await _taskRepository.CountByAssigneeAndStatusAsync(userId, TaskStatusHelper.Tamamlandi);
                model.MyTaskCancelled = await _taskRepository.CountByAssigneeAndStatusAsync(userId, TaskStatusHelper.IptalEdildi);
            }

            if (model.ShowSahaAmiriPanel)
            {
                model.AssignedByMeTotal = await _taskRepository.CountByAssignerAsync(userId);

               
                var subordinates = await _userRepository.GetAllSubordinatesRecursiveAsync(userId);
                model.TeamSize = subordinates.Count();

               
                int teamActiveTasks = 0;
                foreach (var sub in subordinates)
                {
                    teamActiveTasks += await _taskRepository.CountByAssigneeAndStatusAsync(sub.Id, TaskStatusHelper.Atandi);
                    teamActiveTasks += await _taskRepository.CountByAssigneeAndStatusAsync(sub.Id, TaskStatusHelper.Basladi);
                }
                model.TeamActiveTasks = teamActiveTasks;
            }

            
            if (model.ShowAdminPanel)
            {
                model.TotalUsers = await _userRepository.CountAllAsync();
                model.TotalDepartments = await _departmentRepository.CountAllAsync();
                model.TotalTasks = await _taskRepository.CountAllAsync();
                model.SystemTaskBreakdown = await _taskRepository.GetTaskStatusBreakdownAsync();
            }

            return View(model);
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}