using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using TestOtomasyon.Entities;
using TestOtomasyon.Helpers;
using TestOtomasyon.Models;
using TestOtomasyon.Repositories.Interfaces;
using TestOtomasyon.Resources.Languages;

namespace TestOtomasyon.Controllers
{
    public class TasksController : BaseController
    {
        private readonly IWorkTaskRepository _taskRepository;
        private readonly IUserRepository _userRepository;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IStringLocalizer<Lang> _localizer;

        public TasksController(
            IWorkTaskRepository taskRepository,
            IUserRepository userRepository,
            IDbConnectionFactory dbFactory,
            IMessageRepository messageRepository,
            IStringLocalizer<Lang> localizer) : base(messageRepository)
        {
            _taskRepository = taskRepository;
            _userRepository = userRepository;
            _dbFactory = dbFactory;
            _localizer = localizer;
        }


        [HasAuthority("Task-Create")]
        public async Task<IActionResult> Index()
        {
            await SetNotificationCount();

            var currentUserId = Guid.Parse(User.GetUserId()!);
            var tasks = await _taskRepository.GetByAssignerAsync(currentUserId);
            ViewData["Title"] = _localizer["Menu.AssignedByMe"].Value;
            ViewBag.ListType = "assigned-by-me";
            return View("List", tasks);
        }

        
        [HasAuthority("Task-View")]
        public async Task<IActionResult> MyTasks()
        {
            var currentUserId = Guid.Parse(User.GetUserId()!);
            var tasks = await _taskRepository.GetByAssigneeAsync(currentUserId);
            ViewData["Title"] = _localizer["Menu.MyTasks"].Value;
            ViewBag.ListType = "assigned-to-me";
            return View("List", tasks);
        }

        
        public IActionResult All()
        {
            // Eski "Tüm Görevler" / "Ekip Görevleri" linkleri Kanban'a yönlensin
            return RedirectToAction(nameof(SystemDistribution));
        }

        // SİSTEM GÖREV DAĞILIMI (Kanban)
        public async Task<IActionResult> SystemDistribution()
        {
          
            if (!User.HasAuthority("Task-ViewAll") &&
                !User.HasAuthority("Task-Create") &&
                !User.HasAuthority("Task-View"))
                return RedirectToAction("AccessDenied", "Account");

            var currentUserId = Guid.Parse(User.GetUserId()!);
            IEnumerable<WorkTask> tasks;
            string scopeLabel;

            if (User.HasAuthority("Task-ViewAll"))
            {
                tasks = await _taskRepository.GetForKanbanAsync(includeAll: true);
                scopeLabel = _localizer["Kanban.ScopeAll"];
            }
            else if (User.HasAuthority("Task-Create"))
            {
                tasks = await _taskRepository.GetForKanbanAsync(scopeUserId: currentUserId);
                var subordinates = await _userRepository.GetAllSubordinatesRecursiveAsync(currentUserId);
                scopeLabel = _localizer["Kanban.ScopeTeam", subordinates.Count()];
            }
            else
            {
                tasks = await _taskRepository.GetForKanbanAsync(scopeUserId: currentUserId);
                scopeLabel = _localizer["Kanban.ScopeMine"];
            }

            var model = new SystemDistributionViewModel
            {
                ScopeLabel = scopeLabel,
                TotalTasks = tasks.Count()
            };

            var historyThreshold = DateTime.Now.AddDays(-1); // 24 saat öncesi

            foreach (var task in tasks)
            {
                switch (task.TaskStatus)
                {
                    case TaskStatusHelper.OnayBekliyor: // 4
                        model.PendingApproval.Tasks.Add(task);
                        break;
                    case TaskStatusHelper.Atandi: // 0
                        model.Assigned.Tasks.Add(task);
                        break;
                    case TaskStatusHelper.Basladi: // 1
                        model.InProgress.Tasks.Add(task);
                        break;
                    case TaskStatusHelper.Tamamlandi: // 2
                        if (task.CompletedOn.HasValue && task.CompletedOn.Value < historyThreshold)
                            model.History.Tasks.Add(task);
                        else
                            model.Completed.Tasks.Add(task);
                        break;
                    case TaskStatusHelper.IptalEdildi: // 3
                    case TaskStatusHelper.Reddedildi: // 5
                        model.History.Tasks.Add(task);
                        break;
                }
            }

            ViewData["Title"] = _localizer["Kanban.PageTitle"].Value;
            return View(model);
        }

        
        [HasAuthority("Task-Create")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Users = await GetAssignableUsersAsync();
            return View(new TaskCreateViewModel());
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasAuthority("Task-Create")]
        public async Task<IActionResult> Create(TaskCreateViewModel model)
        {
            
            var assignableUsers = await GetAssignableUsersAsync();
            if (!assignableUsers.Any(u => u.Id == model.AssigneeId))
            {
                ModelState.AddModelError(nameof(model.AssigneeId),
                    _localizer["Msg.CannotAssignTeam"]);
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Users = assignableUsers;
                return View(model);
            }

            using var conn = _dbFactory.CreateConnection();
            var orgId = await Dapper.SqlMapper.QuerySingleAsync<Guid>(conn,
                "SELECT TOP 1 id FROM [Organization] WHERE status = 1");

            var task = new WorkTask
            {
                OrganizationId = orgId,
                Title = model.Title,
                Description = model.Description,
                AssignerId = Guid.Parse(User.GetUserId()!),
                AssigneeId = model.AssigneeId
            };

            await _taskRepository.CreateAsync(task);

            TempData["Success"] = _localizer["Msg.TaskCreated"].Value;
            return RedirectToAction(nameof(Index));
        }

        
        [HasAuthority("Task-View")]
        public async Task<IActionResult> Details(Guid id)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task == null)
                return NotFound();

            var currentUserId = Guid.Parse(User.GetUserId()!);
            var canView = task.AssigneeId == currentUserId
                       || task.AssignerId == currentUserId
                       || User.HasAuthority("Task-ViewAll");

            
            if (!canView && User.HasAuthority("Task-Create"))
            {
                var subordinates = await _userRepository.GetAllSubordinatesRecursiveAsync(currentUserId);
                var subordinateIds = subordinates.Select(s => s.Id).ToHashSet();
                if (subordinateIds.Contains(task.AssigneeId) || subordinateIds.Contains(task.AssignerId))
                    canView = true;
            }

            if (!canView)
                return RedirectToAction("AccessDenied", "Account");

            return View(task);
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasAuthority("Task-Update")]
        public async Task<IActionResult> UpdateStatus(Guid id, int newStatus)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task == null)
                return NotFound();

            var currentUserId = Guid.Parse(User.GetUserId()!);
            if (task.AssigneeId != currentUserId)
            {
                TempData["Error"] = _localizer["Msg.OnlyAssigneeUpdate"].Value;
                return RedirectToAction(nameof(Details), new { id });
            }

            if (newStatus < 0 || newStatus > 3)
            {
                TempData["Error"] = _localizer["Msg.InvalidStatus"].Value;
                return RedirectToAction(nameof(Details), new { id });
            }

            await _taskRepository.UpdateStatusAsync(id, newStatus);
            TempData["Success"] = _localizer["Msg.TaskStatusUpdated", TaskStatusHelper.GetName(newStatus)].Value;
            return RedirectToAction(nameof(Details), new { id });
        }

        
        [HasAuthority("Task-Create")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task == null)
                return NotFound();

            var currentUserId = Guid.Parse(User.GetUserId()!);
            if (task.AssignerId != currentUserId)
            {
                TempData["Error"] = _localizer["Msg.OnlyAssignerEdit"].Value;
                return RedirectToAction(nameof(Index));
            }

            var model = new TaskEditViewModel
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                AssigneeId = task.AssigneeId
            };

            ViewBag.Users = await GetAssignableUsersAsync();
            return View(model);
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasAuthority("Task-Create")]
        public async Task<IActionResult> Edit(TaskEditViewModel model)
        {
            var assignableUsers = await GetAssignableUsersAsync();
            if (!assignableUsers.Any(u => u.Id == model.AssigneeId))
            {
                ModelState.AddModelError(nameof(model.AssigneeId),
                    _localizer["Msg.CannotAssign"]);
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Users = assignableUsers;
                return View(model);
            }

            var task = await _taskRepository.GetByIdAsync(model.Id);
            if (task == null)
                return NotFound();

            task.Title = model.Title;
            task.Description = model.Description;
            task.AssigneeId = model.AssigneeId;

            await _taskRepository.UpdateAsync(task);
            TempData["Success"] = _localizer["Msg.TaskUpdated"].Value;
            return RedirectToAction(nameof(Index));
        }

        // SİL
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasAuthority("Task-Create")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task == null)
                return NotFound();

            var currentUserId = Guid.Parse(User.GetUserId()!);
            if (task.AssignerId != currentUserId && !User.HasAuthority("Task-ViewAll"))
            {
                TempData["Error"] = _localizer["Msg.NoDeletePermission"].Value;
                return RedirectToAction(nameof(Index));
            }

            await _taskRepository.SoftDeleteAsync(id);
            TempData["Success"] = _localizer["Msg.TaskDeleted"].Value;
            return RedirectToAction(nameof(Index));
        }
        private async Task<List<User>> GetAssignableUsersAsync()
        {
            var currentUserId = Guid.Parse(User.GetUserId()!);

            if (User.HasAuthority("Task-ViewAll"))
            {
                
                var all = await _userRepository.GetAllAsync();
                return all.Where(u => u.Id != currentUserId).ToList();
            }
            else
            {
                
                var subordinates = await _userRepository.GetAllSubordinatesRecursiveAsync(currentUserId);
                return subordinates.ToList();
            }
        }
       

        [HasAuthority("Task-Propose")]
        public IActionResult Propose()
        {
            return View(new TaskProposeViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasAuthority("Task-Propose")]
        public async Task<IActionResult> Propose(TaskProposeViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            using var conn = _dbFactory.CreateConnection();
            var orgId = await Dapper.SqlMapper.QuerySingleAsync<Guid>(conn,
                "SELECT TOP 1 id FROM [Organization] WHERE status = 1");

            var currentUserId = Guid.Parse(User.GetUserId()!);

            var task = new WorkTask
            {
                OrganizationId = orgId,
                Title = model.Title,
                Description = model.Description,
                AssignerId = currentUserId,  // bildiren kişi
                AssigneeId = currentUserId   // kendisi yapacak (aynı kişi)
            };

            await _taskRepository.ProposeAsync(task);

            TempData["Success"] = _localizer["Msg.TaskProposed"].Value;
            return RedirectToAction(nameof(MyTasks));
        }

        

        [HasAuthority("Task-Approve")]
        public async Task<IActionResult> PendingApprovals()
        {
            var currentUserId = Guid.Parse(User.GetUserId()!);
            var tasks = await _taskRepository.GetPendingApprovalsAsync(currentUserId);
            return View(tasks);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasAuthority("Task-Approve")]
        public async Task<IActionResult> Approve(Guid id)
        {
            var currentUserId = Guid.Parse(User.GetUserId()!);
            await _taskRepository.ApproveAsync(id, currentUserId);
            TempData["Success"] = _localizer["Msg.TaskApproved"].Value;
            return RedirectToAction(nameof(PendingApprovals));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasAuthority("Task-Approve")]
        public async Task<IActionResult> Reject(Guid id, string rejectReason)
        {
            if (string.IsNullOrWhiteSpace(rejectReason))
            {
                TempData["Error"] = _localizer["Msg.RejectReasonRequired"].Value;
                return RedirectToAction(nameof(PendingApprovals));
            }

            var currentUserId = Guid.Parse(User.GetUserId()!);
            await _taskRepository.RejectAsync(id, currentUserId, rejectReason);
            TempData["Success"] = _localizer["Msg.TaskRejected"].Value;
            return RedirectToAction(nameof(PendingApprovals));
        }
    }
}