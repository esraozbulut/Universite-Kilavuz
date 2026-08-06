using System.Threading.Tasks;
using Kilavuz.Web.Application.Interfaces;
using Kilavuz.Web.Data;
using Kilavuz.Web.Domain.Entities;

namespace Kilavuz.Web.Application.Services
{
    public class ErrorLogService : IErrorLogService
    {
        private readonly IGenericRepository<ErrorLog> _repository;

        public ErrorLogService(IGenericRepository<ErrorLog> repository)
        {
            _repository = repository;
        }

        public async Task LogErrorAsync(ErrorLog errorLog)
        {
            await _repository.InsertAsync(errorLog);
        }
    }
}
