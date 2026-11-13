using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Enrollment_System
{
    internal class SeedData
    {
        static bool done = false;

        public static void Ensure()
        {
            if (done) return;
            done = true;

            string[] steps =
            {
                // 1. tables
                @"IF OBJECT_ID('dbo.Students') IS NULL
                  CREATE TABLE dbo.Students (
                      StudentNo  NVARCHAR(20) PRIMARY KEY,
                      Name       NVARCHAR(100) NOT NULL,
                      Grade      INT NOT NULL,
                      Assessment DECIMAL(10,2) NOT NULL DEFAULT 22500,
                      Paid       DECIMAL(10,2) NOT NULL DEFAULT 0,
                      Balance    DECIMAL(10,2) NOT NULL DEFAULT 22500,
                      Status     NVARCHAR(20) NULL)",

                @"IF OBJECT_ID('dbo.PendingPayments') IS NULL
                  CREATE TABLE dbo.PendingPayments (
                      PaymentID      INT IDENTITY(1,1) PRIMARY KEY,
                      StudentName    NVARCHAR(100) NOT NULL,
                      PaymentDetails NVARCHAR(100) NULL,
                      DateSubmitted  DATETIME NOT NULL DEFAULT GETDATE(),
                      Amount         DECIMAL(10,2) NOT NULL,
                      Status         NVARCHAR(20) NOT NULL DEFAULT 'Pending')",

                // 2. dagdag na columns (kung wala pa)
                @"IF COL_LENGTH('dbo.PendingPayments','StudentNo') IS NULL ALTER TABLE dbo.PendingPayments ADD StudentNo NVARCHAR(20) NULL",
                @"IF COL_LENGTH('dbo.PendingPayments','Method') IS NULL ALTER TABLE dbo.PendingPayments ADD Method NVARCHAR(20) NULL",
                @"IF COL_LENGTH('dbo.PendingPayments','VerifiedDate') IS NULL ALTER TABLE dbo.PendingPayments ADD VerifiedDate DATETIME NULL",
                @"IF COL_LENGTH('dbo.PendingPayments','ReceiptNo') IS NULL ALTER TABLE dbo.PendingPayments ADD ReceiptNo NVARCHAR(20) NULL",
                @"IF COL_LENGTH('dbo.PendingPayments','RejectReason') IS NULL ALTER TABLE dbo.PendingPayments ADD RejectReason NVARCHAR(200) NULL",

                // 3. 50 estudyante (kung walang laman ang Students)
                @"IF NOT EXISTS (SELECT 1 FROM dbo.Students)
                  BEGIN
                      ;WITH n AS (SELECT 1 AS i UNION ALL SELECT i + 1 FROM n WHERE i < 50)
                      INSERT INTO dbo.Students (StudentNo, Name, Grade, Assessment, Paid, Balance, Status)
                      SELECT '2026-' + RIGHT('0000' + CAST(141 + i AS VARCHAR(10)), 4),
                             CHOOSE(i % 10 + 1,'Juan','Maria','Ana','Pedro','Carlo','Bea','Miguel','Sofia','Rafael','Katrina')
                               + ' ' + CHOOSE((i / 10) % 5 + 1,'Dela Cruz','Santos','Lim','Reyes','Mendoza'),
                             7 + i % 4, 22500, p.Paid, 22500 - p.Paid, 'Enrolled'
                      FROM n CROSS APPLY (SELECT CASE i % 4 WHEN 0 THEN 0 WHEN 1 THEN 10000 WHEN 2 THEN 22500 ELSE 15000 END AS Paid) p
                      OPTION (MAXRECURSION 100)
                  END",

                // 4. payments (kung walang laman ang PendingPayments)
                @"IF NOT EXISTS (SELECT 1 FROM dbo.PendingPayments)
                  BEGIN
                      -- 10 na naghihintay ma-verify
                      INSERT INTO dbo.PendingPayments (StudentNo, StudentName, PaymentDetails, Method, Amount, DateSubmitted, Status)
                      SELECT TOP 10 StudentNo, Name,
                             'GCash - REF' + RIGHT('0000' + CAST(ABS(CHECKSUM(StudentNo)) % 10000 AS VARCHAR(10)), 4), 'GCash',
                             1000 + 500 * ROW_NUMBER() OVER (ORDER BY StudentNo),
                             DATEADD(MINUTE, -30 * ROW_NUMBER() OVER (ORDER BY StudentNo), GETDATE()), 'Pending'
                      FROM dbo.Students WHERE Balance > 6000 ORDER BY StudentNo;

                      -- 3 na na-verify ngayon
                      INSERT INTO dbo.PendingPayments (StudentNo, StudentName, PaymentDetails, Method, Amount, DateSubmitted, Status, VerifiedDate)
                      SELECT TOP 3 StudentNo, Name, 'Cash - Counter', 'Cash', 5000, GETDATE(), 'Verified', GETDATE()
                      FROM dbo.Students ORDER BY StudentNo DESC;
                      UPDATE dbo.PendingPayments SET ReceiptNo = RIGHT('000000' + CAST(PaymentID AS VARCHAR(10)), 6) WHERE Status = 'Verified';

                      -- 1 na rejected ngayon
                      INSERT INTO dbo.PendingPayments (StudentNo, StudentName, PaymentDetails, Method, Amount, DateSubmitted, Status, VerifiedDate)
                      SELECT TOP 1 StudentNo, Name, 'GCash - REF0000', 'GCash', 1000, GETDATE(), 'Rejected', GETDATE()
                      FROM dbo.Students ORDER BY StudentNo
                  END",
                @"IF OBJECT_ID('dbo.SeedFlags') IS NULL CREATE TABLE dbo.SeedFlags (Name NVARCHAR(50) PRIMARY KEY)",

                @"IF NOT EXISTS (SELECT 1 FROM dbo.SeedFlags WHERE Name = 'MorePending')
                  BEGIN
                      INSERT INTO dbo.PendingPayments (StudentNo, StudentName, PaymentDetails, Method, Amount, DateSubmitted, Status)
                      SELECT TOP 25 s.StudentNo, s.Name,
                             m.Method + ' - REF' + RIGHT('0000' + CAST(ABS(CHECKSUM(NEWID())) % 10000 AS VARCHAR(10)), 4),
                             m.Method,
                             500 * (1 + ABS(CHECKSUM(NEWID())) % 8),
                             DATEADD(MINUTE, -20 * (ABS(CHECKSUM(NEWID())) % 60), GETDATE()),
                             'Pending'
                      FROM dbo.Students s
                      CROSS APPLY (SELECT CHOOSE(ABS(CHECKSUM(s.StudentNo, NEWID())) % 3 + 1, 'GCash', 'Bank transfer', 'Cash') AS Method) m
                      WHERE s.Balance >= 4000
                      ORDER BY NEWID();
                      INSERT INTO dbo.SeedFlags (Name) VALUES ('MorePending');
                  END"
            };

            try
            {
                using (SqlConnection c = DatabaseConnection.GetConnection())
                {
                    c.Open();
                    foreach (string sql in steps)
                        using (SqlCommand cmd = new SqlCommand(sql, c))
                            cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hindi na-lagyan ng sample data: " + ex.Message, "Seed data");
            }
        }
    }
}
