using EventHubASP.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EventHubASP.Controllers
{
    [Authorize(Roles = "User")]
    public class NotificationController : Controller
    {
        
    }

}
