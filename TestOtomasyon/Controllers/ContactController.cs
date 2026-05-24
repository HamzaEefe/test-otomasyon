using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using TestOtomasyon.Entities;
using TestOtomasyon.Helpers;
using TestOtomasyon.Repositories.Interfaces;
using TestOtomasyon.Resources.Languages;
using TestOtomasyon.Services;

namespace TestOtomasyon.Controllers
{
    public class ContactController : BaseController
    {
        private readonly IUserRepository _userRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly IEmailService _emailService;
        private readonly IStringLocalizer<Lang> _localizer;

        public ContactController(
            IUserRepository userRepository,
            IMessageRepository messageRepository,
            IEmailService emailService,
            IStringLocalizer<Lang> localizer)
            : base(messageRepository)
        {
            _userRepository = userRepository;
            _messageRepository = messageRepository;
            _emailService = emailService;
            _localizer = localizer;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            await SetNotificationCount();
            await PopulateCountsAsync();

            var users = await _userRepository.GetAllAsync();
            ViewBag.Users = users.Where(u => u.Status == 1).ToList();
            return View();
        }

        // MESAJ GÖNDER
        [HttpPost]
        public async Task<IActionResult> SendEmail(Guid recipientId, string subject, string message)
        {
            await SetNotificationCount();
            await PopulateCountsAsync();

            var users = (await _userRepository.GetAllAsync()).ToList();
            ViewBag.Users = users.Where(u => u.Status == 1).ToList();

            var recipient = users.FirstOrDefault(u => u.Id == recipientId);
            if (recipient == null)
            {
                TempData["Error"] = _localizer["Msg.RecipientNotFound"].Value;
                return View("Index");
            }

            User? sender = null;
            var senderIdStr = User.GetUserId();
            if (!string.IsNullOrEmpty(senderIdStr) && Guid.TryParse(senderIdStr, out var senderId))
            {
                sender = await _userRepository.GetByIdAsync(senderId);
            }

            if (sender == null)
            {
                TempData["Error"] = _localizer["Msg.SenderNotFound"].Value;
                return View("Index");
            }

            // 1) Uygulama içi mesajı kaydet
            var dbMessage = new Message
            {
                SenderId = sender.Id,
                RecipientId = recipientId,
                Subject = subject,
                Body = message,
                SentAt = DateTime.Now,
                IsRead = false,
                Status = 1
            };
            await _messageRepository.SendMessageAsync(dbMessage);

            bool emailSent = false;
            if (!string.IsNullOrEmpty(recipient.Email))
            {
                var senderDisplayName = sender.Name ?? sender.UserName;
                var emailHeading = _localizer["Contact.EmailHeading"].Value;
                var fromLabel = _localizer["Contact.From"].Value;
                var subjectLabel = _localizer["Contact.Subject"].Value;
                var emailBody = $@"
                    <h2>{emailHeading}</h2>
                    <p><strong>{fromLabel}:</strong> {senderDisplayName}</p>
                    <p><strong>{subjectLabel}:</strong> {subject}</p>
                    <hr>
                    <p>{message.Replace("\n", "<br>")}</p>
                ";

                try
                {
                    await _emailService.SendEmailAsync(recipient.Email, subject, emailBody);
                    emailSent = true;
                }
                catch
                {
                    // SMTP yok / hatalı — sessizce geç
                }
            }

            TempData["Success"] = emailSent
                ? _localizer["Msg.MessageSentAppEmail", recipient.Name].Value
                : _localizer["Msg.MessageSentApp", recipient.Name].Value;

            return RedirectToAction(nameof(Sent));
        }

        [HttpGet]
        public async Task<IActionResult> Inbox()
        {
            await SetNotificationCount();
            await PopulateCountsAsync();

            var userIdStr = User.GetUserId();
            if (!Guid.TryParse(userIdStr, out var currentUserId))
                return Unauthorized();

            var messages = await _messageRepository.GetInboxAsync(currentUserId);
            return View(messages);
        }

        [HttpGet]
        public async Task<IActionResult> Sent()
        {
            await SetNotificationCount();
            await PopulateCountsAsync();

            var userIdStr = User.GetUserId();
            if (!Guid.TryParse(userIdStr, out var currentUserId))
                return Unauthorized();

            var messages = await _messageRepository.GetSentAsync(currentUserId);
            return View(messages);
        }

        [HttpGet]
        public async Task<IActionResult> Read(Guid id)
        {
            var userIdStr = User.GetUserId();
            if (!Guid.TryParse(userIdStr, out var currentUserId))
                return Unauthorized();

            var msg = await _messageRepository.GetByIdAsync(id);
            if (msg == null)
            {
                TempData["Error"] = _localizer["Msg.MessageNotFound"].Value;
                return RedirectToAction(nameof(Inbox));
            }

            if (msg.SenderId != currentUserId && msg.RecipientId != currentUserId)
            {
                TempData["Error"] = _localizer["Msg.MessageNoPermission"].Value;
                return RedirectToAction(nameof(Inbox));
            }

            if (msg.RecipientId == currentUserId && !msg.IsRead)
            {
                await _messageRepository.MarkAsReadAsync(id);
                msg.IsRead = true;
            }

            await SetNotificationCount();
            await PopulateCountsAsync();

            ViewBag.IsRecipient = msg.RecipientId == currentUserId;
            return View(msg);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id, string? returnTo = null)
        {
            var userIdStr = User.GetUserId();
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                return Unauthorized();

            await _messageRepository.DeleteAsync(id, userId);
            TempData["Success"] = _localizer["Msg.MessageDeleted"].Value;

            if (returnTo == "sent")
                return RedirectToAction(nameof(Sent));
            return RedirectToAction(nameof(Inbox));
        }

        [HttpGet]
        public async Task<IActionResult> Reply(Guid id)
        {
            var userIdStr = User.GetUserId();
            if (!Guid.TryParse(userIdStr, out var currentUserId))
                return Unauthorized();

            var msg = await _messageRepository.GetByIdAsync(id);
            if (msg == null || (msg.SenderId != currentUserId && msg.RecipientId != currentUserId))
            {
                TempData["Error"] = _localizer["Msg.MessageNotFound"].Value;
                return RedirectToAction(nameof(Inbox));
            }

            await SetNotificationCount();
            await PopulateCountsAsync();

            var users = await _userRepository.GetAllAsync();
            ViewBag.Users = users.Where(u => u.Status == 1).ToList();

            ViewBag.PrefillRecipientId = msg.SenderId == currentUserId ? msg.RecipientId : msg.SenderId;
            ViewBag.PrefillSubject = msg.Subject.StartsWith("Re: ") ? msg.Subject : "Re: " + msg.Subject;
            ViewBag.PrefillBody = $"\n\n---\n{msg.SenderName} ({msg.SentAt:dd.MM.yyyy HH:mm}) {_localizer["Contact.ReplyOn"].Value}:\n{msg.Body}";

            return View("Index");
        }

        
        private async Task PopulateCountsAsync()
        {
            var userIdStr = User.GetUserId();
            if (Guid.TryParse(userIdStr, out var userId))
            {
                ViewBag.InboxUnread = await _messageRepository.GetUnreadCountAsync(userId);
                ViewBag.SentCount = await _messageRepository.GetSentCountAsync(userId);
            }
            else
            {
                ViewBag.InboxUnread = 0;
                ViewBag.SentCount = 0;
            }
        }
    }
}
