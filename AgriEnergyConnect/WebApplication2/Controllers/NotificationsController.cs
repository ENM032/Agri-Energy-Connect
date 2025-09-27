using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApplication2.Areas.Identity.Data;
using WebApplication2.Models;
using WebApplication2.Models.ViewModels;
using WebApplication2.Services;
using AutoMapper;

namespace WebApplication2.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<WebApplication2User> _userManager;
        private readonly ILogger<NotificationsController> _logger;
        private readonly IMapper _mapper;

        public NotificationsController(
            INotificationService notificationService,
            UserManager<WebApplication2User> userManager,
            ILogger<NotificationsController> logger,
            IMapper mapper)
        {
            _notificationService = notificationService;
            _userManager = userManager;
            _logger = logger;
            _mapper = mapper;
        }

        // GET: Notifications
        public async Task<IActionResult> Index(bool unreadOnly = false)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (userId == null)
                {
                    return RedirectToAction("Login", "Account", new { area = "Identity" });
                }

                var notifications = await _notificationService.GetUserNotificationsAsync(userId, unreadOnly);
                var notificationViewModels = _mapper.Map<List<NotificationViewModel>>(notifications);
                var unreadCount = await _notificationService.GetUnreadCountAsync(userId);
                
                var viewModel = new NotificationIndexViewModel
                {
                    Notifications = notificationViewModels,
                    UnreadOnly = unreadOnly,
                    UnreadCount = unreadCount
                };
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notifications for user");
                TempData["ErrorMessage"] = "An error occurred while loading notifications.";
                return View(new NotificationIndexViewModel
                {
                    Notifications = new List<NotificationViewModel>(),
                    UnreadOnly = unreadOnly,
                    UnreadCount = 0
                });
            }
        }

        // POST: Notifications/MarkAsRead/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (userId == null)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var result = await _notificationService.MarkAsReadAsync(id, userId);
                if (result)
                {
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, message = "Notification not found or access denied" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {NotificationId} as read", id);
                return Json(new { success = false, message = "An error occurred" });
            }
        }

        // POST: Notifications/MarkAllAsRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (userId == null)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var result = await _notificationService.MarkAllAsReadAsync(userId);
                if (result)
                {
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to mark all notifications as read" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read for user");
                return Json(new { success = false, message = "An error occurred" });
            }
        }

        // POST: Notifications/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (userId == null)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var result = await _notificationService.DeleteNotificationAsync(id, userId);
                if (result)
                {
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, message = "Notification not found or access denied" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification {NotificationId}", id);
                return Json(new { success = false, message = "An error occurred" });
            }
        }

        // POST: Notifications/DeleteAll
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAll()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (userId == null)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var result = await _notificationService.DeleteAllNotificationsAsync(userId);
                if (result)
                {
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to delete all notifications" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting all notifications for user");
                return Json(new { success = false, message = "An error occurred" });
            }
        }

        // GET: Notifications/GetUnreadCount
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (userId == null)
                {
                    return Json(new { success = false, count = 0 });
                }

                var count = await _notificationService.GetUnreadCountAsync(userId);
                return Json(new { success = true, count = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread notification count");
                return Json(new { success = false, count = 0 });
            }
        }

        // GET: Notifications/GetLatest
        [HttpGet]
        public async Task<IActionResult> GetLatest(int take = 5)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (userId == null)
                {
                    return Json(new { success = false, notifications = new List<object>() });
                }

                var notifications = await _notificationService.GetUserNotificationsAsync(userId, false, take);
                var result = notifications.Select(n => new
                {
                    id = n.Id,
                    title = n.Title,
                    message = n.Message,
                    type = n.Type.ToString(),
                    isRead = n.IsRead,
                    createdAt = n.CreatedAt.ToString("yyyy-MM-dd HH:mm")
                });

                return Json(new { success = true, notifications = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting latest notifications");
                return Json(new { success = false, notifications = new List<object>() });
            }
        }
    }
}