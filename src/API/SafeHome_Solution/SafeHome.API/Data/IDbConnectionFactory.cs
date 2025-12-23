using Microsoft.Data.SqlClient;

namespace SafeHome.API.Data
{
    public interface IDbConnectionFactory
    {
        SqlConnection CreateConnection();
    }
}
