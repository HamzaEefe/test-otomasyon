using Microsoft.AspNetCore.Mvc;
using TestOtomasyon.Entities;
using TestOtomasyon.Helpers;
using TestOtomasyon.Models;
using TestOtomasyon.Repositories.Interfaces;

namespace TestOtomasyon.Controllers
{
    [HasAuthority("Organization-View")]
    public class OrganizationController : Controller
    {
        private readonly IUserRepository _userRepository;

        public OrganizationController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<IActionResult> Chart()
        {
            // Tüm kullanıcıları çek
            var allUsers = (await _userRepository.GetAllAsync()).ToList();

            
            var topLevelUsers = allUsers.Where(u => u.ParentId == null).ToList();

            
            var childrenByParentId = allUsers
                .Where(u => u.ParentId.HasValue)
                .GroupBy(u => u.ParentId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            var nodes = new List<OrgChartNode>();
            foreach (var user in topLevelUsers)
            {
                nodes.Add(BuildNode(user, childrenByParentId, 0));
            }

            ViewBag.TotalUsers = allUsers.Count;
            ViewBag.TopLevelCount = topLevelUsers.Count;

            return View(nodes);
        }

        private OrgChartNode BuildNode(
            User user,
            Dictionary<Guid, List<User>> childrenMap,
            int level)
        {
            var node = new OrgChartNode
            {
                User = user,
                Level = level,
                Children = new List<OrgChartNode>()
            };

            if (childrenMap.TryGetValue(user.Id, out var children))
            {
                foreach (var child in children.OrderBy(c => c.Name))
                {
                    node.Children.Add(BuildNode(child, childrenMap, level + 1));
                }
            }

            return node;
        }
    }
}