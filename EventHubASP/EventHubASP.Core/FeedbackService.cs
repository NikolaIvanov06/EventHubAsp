using EventHubASP.DataAccess;
using EventHubASP.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHubASP.Core
{
    public class FeedbackService : IFeedbackService
    {
        private readonly ApplicationDbContext _context;

        public FeedbackService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SubmitFeedbackAsync(Feedback feedback)
        {
             _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();
        }

       public async Task<IEnumerable<Feedback>> GetAllFeedbackAsync()
       {
           return await _context.Feedbacks
               .OrderByDescending(f => f.SubmittedDate)
               .ToListAsync();
       }
    }
}
