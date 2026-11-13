using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Enrollment_System
{
    public class DashStats { public decimal Collected; public int Receipts, ToVerify, Rejected; }

    public static class CashierData
    {
        static CashierData()
        {
            SeedData.Ensure();      
        }

        public static string Peso(decimal v) { return "₱" + v.ToString("N2", CultureInfo.InvariantCulture); }

        public static bool TryMoney(string text, out decimal value)
        {
            string t = (text ?? "").Replace("₱", "").Replace(",", "").Trim();
            return decimal.TryParse(t, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }

        public static int Pages(int total, int size)
        {
            return Math.Max(1, (int)Math.Ceiling(total / (double)size));
        }

        static SqlCommand Cmd(SqlConnection c, SqlTransaction tx, string sql, params object[] kv)
        {
            SqlCommand cmd = new SqlCommand(sql, c, tx);
            for (int i = 0; i + 1 < kv.Length; i += 2)
                cmd.Parameters.AddWithValue((string)kv[i], kv[i + 1] ?? DBNull.Value);
            return cmd;
        }

        static DataTable Fill(string sql, params object[] kv)
        {
            using (SqlConnection c = DatabaseConnection.GetConnection())
            {
                c.Open();
                using (SqlCommand cmd = Cmd(c, null, sql, kv))
                using (SqlDataReader rd = cmd.ExecuteReader())
                {
                    DataTable dt = new DataTable();
                    dt.Load(rd);
                    return dt;
                }
            }
        }

        static object Scalar(string sql, params object[] kv)
        {
            using (SqlConnection c = DatabaseConnection.GetConnection())
            {
                c.Open();
                using (SqlCommand cmd = Cmd(c, null, sql, kv)) return cmd.ExecuteScalar();
            }
        }

        static int NonQuery(string sql, params object[] kv)
        {
            using (SqlConnection c = DatabaseConnection.GetConnection())
            {
                c.Open();
                using (SqlCommand cmd = Cmd(c, null, sql, kv)) return cmd.ExecuteNonQuery();
            }
        }

        // ---------- Dashboard ----------
        public static DashStats GetStats()
        {
            DashStats s = new DashStats();
            s.Collected = Convert.ToDecimal(Scalar("SELECT ISNULL(SUM(Amount),0) FROM PendingPayments WHERE Status='Verified' AND CAST(VerifiedDate AS DATE)=CAST(GETDATE() AS DATE)"));
            s.Receipts = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM PendingPayments WHERE Status='Verified' AND ReceiptNo IS NOT NULL AND CAST(VerifiedDate AS DATE)=CAST(GETDATE() AS DATE)"));
            s.ToVerify = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM PendingPayments WHERE Status='Pending'"));
            s.Rejected = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM PendingPayments WHERE Status='Rejected' AND CAST(VerifiedDate AS DATE)=CAST(GETDATE() AS DATE)"));
            return s;
        }

        public static DataTable GetPendingPage(int page, int size, out int total)
        {
            total = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM PendingPayments WHERE Status='Pending'"));
            return Fill("SELECT PaymentID, StudentName AS Student, PaymentDetails AS Payment, DateSubmitted AS Submitted, Amount " +
                        "FROM PendingPayments WHERE Status='Pending' ORDER BY DateSubmitted " +
                        "OFFSET @o ROWS FETCH NEXT @n ROWS ONLY", "@o", (page - 1) * size, "@n", size);
        }

        
        public static string VerifyPayment(int paymentId, out string error)
        {
            error = null;
            using (SqlConnection c = DatabaseConnection.GetConnection())
            {
                c.Open();
                using (SqlTransaction tx = c.BeginTransaction())
                {
                    try
                    {
                        DataTable p = new DataTable();
                        using (SqlDataReader rd = Cmd(c, tx, "SELECT StudentNo, StudentName, Amount, Status FROM PendingPayments WHERE PaymentID=@id", "@id", paymentId).ExecuteReader())
                            p.Load(rd);
                        if (p.Rows.Count == 0) { error = "Hindi nahanap ang payment."; return null; }

                        DataRow r = p.Rows[0];
                        if (Convert.ToString(r["Status"]) != "Pending") { error = "Naproseso na ang payment na ito."; return null; }

                        decimal amount = Convert.ToDecimal(r["Amount"]);
                        string no = r["StudentNo"] == DBNull.Value ? "" : Convert.ToString(r["StudentNo"]);
                        string name = Convert.ToString(r["StudentName"]);
                        string who = no.Length > 0 ? "StudentNo=@no" : "Name=@name";    

                        object bal = Cmd(c, tx, "SELECT TOP 1 Balance FROM Students WHERE " + who, "@no", no, "@name", name).ExecuteScalar();
                        if (bal == null) { error = "Hindi nahanap ang estudyante."; return null; }
                        if (amount > Convert.ToDecimal(bal))
                        { error = "Mas malaki ang bayad kaysa sa natitirang balanse (" + Peso(Convert.ToDecimal(bal)) + ")."; return null; }

                        Cmd(c, tx, "UPDATE Students SET Paid = Paid + @a, Balance = Balance - @a WHERE " + who,
                            "@a", amount, "@no", no, "@name", name).ExecuteNonQuery();

                        string receiptNo = Convert.ToString(Cmd(c, tx,
                            "UPDATE PendingPayments SET Status='Verified', VerifiedDate=GETDATE(), " +
                            "ReceiptNo=RIGHT('000000'+CAST(PaymentID AS VARCHAR(10)),6) WHERE PaymentID=@id; " +
                            "SELECT ReceiptNo FROM PendingPayments WHERE PaymentID=@id", "@id", paymentId).ExecuteScalar());
                        tx.Commit();
                        return receiptNo;
                    }
                    catch (Exception ex) { error = ex.Message; return null; }  
                }
            }
        }


     
        public static bool DeletePayment(int paymentId, out string error)
        {
            error = null;
            if (NonQuery("DELETE FROM PendingPayments WHERE PaymentID=@id AND Status<>'Verified'", "@id", paymentId) > 0) return true;
            error = "Hindi puwedeng i-delete ang verified na bayad dahil may resibo na ito.";
            return false;
        }

        
        public static DataTable SearchPending(string search, int page, int size, out int total)
        {
            string src = " FROM PendingPayments WHERE Status='Pending' AND (StudentName LIKE @q OR StudentNo LIKE @q OR PaymentDetails LIKE @q)";
            string q = "%" + search + "%";
            total = Convert.ToInt32(Scalar("SELECT COUNT(*)" + src, "@q", q));
            return Fill("SELECT PaymentID, StudentName AS Student, PaymentDetails AS Payment, Amount" + src +
                        " ORDER BY DateSubmitted OFFSET @o ROWS FETCH NEXT @n ROWS ONLY",
                        "@q", q, "@o", (page - 1) * size, "@n", size);
        }

        public static DataRow GetPaymentInfo(int paymentId)
        {
            DataTable dt = Fill(@"SELECT p.StudentName, p.Amount,
                    ISNULL(p.Method, LEFT(p.PaymentDetails, CHARINDEX(' -', p.PaymentDetails + ' -') - 1)) AS Method,
                    (SELECT TOP 1 s.Balance FROM Students s
                      WHERE (p.StudentNo IS NOT NULL AND s.StudentNo = p.StudentNo)
                         OR (p.StudentNo IS NULL AND s.Name = p.StudentName)) AS Balance
                FROM PendingPayments p WHERE p.PaymentID=@id", "@id", paymentId);
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public static bool RejectPayment(int paymentId, string reason, out string error)
        {
            error = null;
            if (NonQuery("UPDATE PendingPayments SET Status='Rejected', RejectReason=@r, VerifiedDate=GETDATE() " +
                         "WHERE PaymentID=@id AND Status='Pending'", "@r", reason, "@id", paymentId) > 0) return true;
            error = "Naproseso na ang payment na ito.";
            return false;
        }

        public static DataTable SearchStudents(string search, string filter, int page, int size, out int total)
        {
            const string statusExpr = "CASE WHEN Balance<=0 THEN 'Fully paid' WHEN Paid=0 THEN 'Unpaid' ELSE 'Partial' END";
            string src = " FROM Students WHERE (Name LIKE @q OR StudentNo LIKE @q) AND (@f='All balances' OR " + statusExpr + "=@f)";
            string q = "%" + search + "%";
            total = Convert.ToInt32(Scalar("SELECT COUNT(*)" + src, "@q", q, "@f", filter));
            return Fill("SELECT StudentNo, Name, Grade, Assessment, Paid, Balance, " + statusExpr + " AS Status" + src +
                        " ORDER BY StudentNo OFFSET @o ROWS FETCH NEXT @n ROWS ONLY",
                        "@q", q, "@f", filter, "@o", (page - 1) * size, "@n", size);
        }

     
        public static DataRow FindStudent(string text)
        {
            DataTable dt = Fill("SELECT TOP 1 StudentNo, Name, Assessment, Paid, Balance FROM Students " +
                                "WHERE StudentNo=@t OR Name=@t OR StudentNo LIKE @q OR Name LIKE @q " +
                                "ORDER BY CASE WHEN StudentNo=@t OR Name=@t THEN 0 ELSE 1 END, Name",
                                "@t", text, "@q", "%" + text + "%");
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        // Counter payment (cash/card): Verified agad, may receipt no.
        public static string SavePayment(string studentNo, string studentName, string method, decimal amount, out string error)
        {
            error = null;
            using (SqlConnection c = DatabaseConnection.GetConnection())
            {
                c.Open();
                using (SqlTransaction tx = c.BeginTransaction())
                {
                    try
                    {
                        object bal = Cmd(c, tx, "SELECT Balance FROM Students WHERE StudentNo=@no", "@no", studentNo).ExecuteScalar();
                        if (bal == null) { error = "Hindi nahanap ang estudyante."; return null; }
                        if (amount <= 0) { error = "Maglagay ng amount na higit sa ₱0."; return null; }
                        if (amount > Convert.ToDecimal(bal))
                        { error = "Hindi puwedeng lampas sa natitirang balanse na " + Peso(Convert.ToDecimal(bal)) + "."; return null; }

                        Cmd(c, tx, "UPDATE Students SET Paid = Paid + @a, Balance = Balance - @a WHERE StudentNo=@no",
                            "@a", amount, "@no", studentNo).ExecuteNonQuery();

                        int id = Convert.ToInt32(Cmd(c, tx,
                            "INSERT INTO PendingPayments (StudentNo, StudentName, PaymentDetails, Method, Amount, Status, DateSubmitted, VerifiedDate) " +
                            "OUTPUT INSERTED.PaymentID VALUES (@no, @name, @det, @m, @a, 'Verified', GETDATE(), GETDATE())",
                            "@no", studentNo, "@name", studentName, "@det", method + " - Counter", "@m", method, "@a", amount).ExecuteScalar());

                        string receiptNo = Convert.ToString(Cmd(c, tx,
                            "UPDATE PendingPayments SET ReceiptNo=RIGHT('000000'+CAST(PaymentID AS VARCHAR(10)),6) WHERE PaymentID=@id; " +
                            "SELECT ReceiptNo FROM PendingPayments WHERE PaymentID=@id", "@id", id).ExecuteScalar());
                        tx.Commit();
                        return receiptNo;
                    }
                    catch (Exception ex) { error = ex.Message; return null; }
                }
            }
        }
    }
}

