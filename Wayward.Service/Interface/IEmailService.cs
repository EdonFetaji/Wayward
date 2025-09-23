using Wayward.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wayward.Service.Interface
{
    public interface IEmailService
    {
        Boolean SendEmailAsync(EmailMessage allMails);
    }
}
