using EventHubASP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHubASP.Core
{

        public interface INewsService
        {
            Task<News> CreateNewsAsync(News news);
            Task<List<News>> GetNewsForUserAsync(Guid userId);
        Task<List<Event>> GetEventsByOrganizerAsync(Guid organizerId);


        }
    
}
