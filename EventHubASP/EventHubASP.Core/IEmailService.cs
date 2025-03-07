using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHubASP.Core
{
    public interface IEmailService
    {
        Task SendRecoveryCodeAsync(string toEmail, string recoveryCode);
    }
}
