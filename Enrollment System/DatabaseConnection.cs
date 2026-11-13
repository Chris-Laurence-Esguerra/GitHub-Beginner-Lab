using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Enrollment_System
{
    public static class DatabaseConnection
    {
        private static string connectionString = @"Data Source=ITTCHAN-PC\SQLEXPRESS;Initial Catalog=EnrollmentDB;Integrated Security=True";
        public static SqlConnection GetConnection()
        {
            return new SqlConnection(connectionString);
        }
    }
}
