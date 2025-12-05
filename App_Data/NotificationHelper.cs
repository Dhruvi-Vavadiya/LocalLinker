using Microsoft.AspNetCore.Mvc;

namespace LocalLinker.App_Data
{
    public static class NotificationHelper
    {
        public static void ShowNotification(Controller controller, string message, string type = "success")
        {
            controller.TempData["NotificationMessage"] = message;
            controller.TempData["NotificationType"] = type; // success, error, warning, info
        }
    }
}
