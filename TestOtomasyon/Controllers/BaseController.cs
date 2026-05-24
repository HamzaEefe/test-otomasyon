using Microsoft.AspNetCore.Mvc;
using TestOtomasyon.Helpers;
using TestOtomasyon.Repositories.Interfaces;

namespace TestOtomasyon.Controllers
{
    public class BaseController : Controller
    {
        protected readonly IMessageRepository _messageRepository;

        public BaseController(IMessageRepository messageRepository)
        {
            _messageRepository = messageRepository;
        }

        public async Task SetNotificationCount()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdStr = User.GetUserId();
                if (!string.IsNullOrEmpty(userIdStr) && Guid.TryParse(userIdStr, out var userId))
                {
                    var count = await _messageRepository.GetUnreadCountAsync(userId);
                    ViewBag.UnreadCount = count;
                }
            }
        }
    }
}