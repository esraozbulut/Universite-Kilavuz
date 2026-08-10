using System.Data;

namespace Kilavuz.Web.Application.Interfaces;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
