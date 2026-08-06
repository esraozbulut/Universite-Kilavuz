using System.Threading.Tasks;
using Kilavuz.Web.Domain.Entities;

namespace Kilavuz.Web.Application.Interfaces
{
    public interface IErrorLogService
    {
        Task LogErrorAsync(ErrorLog errorLog);
    }
}
