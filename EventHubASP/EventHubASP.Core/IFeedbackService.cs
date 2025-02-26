using EventHubASP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHubASP.Core
{
    public interface IFeedbackService
    {
        Task SubmitFeedbackAsync(Feedback feedback);
        Task<IEnumerable<Feedback>> GetAllFeedbackAsync();
    }
}
