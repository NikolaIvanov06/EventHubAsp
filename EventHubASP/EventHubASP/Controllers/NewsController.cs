using EventHubASP.Core;
using EventHubASP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace EventHubASP.Controllers
{
    [Authorize]
    public class NewsController : Controller
    {

    }
}
