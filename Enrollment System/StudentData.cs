using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Enrollment_System
{
    public class StudentProfile
    {
        public string StudentNo, Name, FirstName, Section, EnrollmentType, SchoolYear, Status;
        public int Grade;
        public decimal Assessment, Paid, Balance;
        public bool Enrolled { get { return Status == "Enrolled"; } }
    }

    public static class StudentData
    {
        public static string CurrentUser;

        // Tinatandaan ang naka-login na user kahit parameterless ang constructor ng form
        public static string Resolve(string username)
        {
            if (!string.IsNullOrEmpty(username)) CurrentUser = username;
            return CurrentUser;
        }

        static DataTable Fill(string sql, params object[] kv)
        {
            using (SqlConnection c = DatabaseConnection.GetConnection())
            {
                c.Open();
                using (SqlCommand cmd = new SqlCommand(sql, c))
                {
                    for (int i = 0; i + 1 < kv.Length; i += 2)
                        cmd.Parameters.AddWithValue((string)kv[i], kv[i + 1] ?? DBNull.Value);
                    using (SqlDataReader rd = cmd.ExecuteReader())
                    {
                        DataTable dt = new DataTable();
                        dt.Load(rd);
                        return dt;
                    }
                }
            }
        }

        const string ProfileSql =
            "SELECT TOP 1 StudentNo, Name, Grade, ISNULL(Section,'') AS Section, " +
            "ISNULL(EnrollmentType,'Returning') AS EnrollmentType, ISNULL(SchoolYear,'2026-2027') AS SchoolYear, " +
            "ISNULL(Status,'Enrolled') AS Status, Assessment, Paid, Balance FROM Students ";

        // Hinahanap ang estudyante ng naka-login na user. Kung walang link, ginagamit ang demo na 2026-0142 para gumana pa rin.
        public static StudentProfile GetProfile(string username)
        {
            DataTable dt = Fill(ProfileSql + "WHERE StudentNo = (SELECT TOP 1 StudentNo FROM Users WHERE Username=@u AND StudentNo IS NOT NULL)",
                                "@u", username ?? "");
            if (dt.Rows.Count == 0) dt = Fill(ProfileSql + "WHERE StudentNo='2026-0142'");
            if (dt.Rows.Count == 0) dt = Fill(ProfileSql + "ORDER BY StudentNo");
            if (dt.Rows.Count == 0) return null;

            DataRow r = dt.Rows[0];
            StudentProfile p = new StudentProfile();
            p.StudentNo = Convert.ToString(r["StudentNo"]);
            p.Name = Convert.ToString(r["Name"]);
            p.FirstName = p.Name.Split(' ')[0];
            p.Grade = Convert.ToInt32(r["Grade"]);
            p.Section = Convert.ToString(r["Section"]);
            p.EnrollmentType = Convert.ToString(r["EnrollmentType"]);
            p.SchoolYear = Convert.ToString(r["SchoolYear"]);
            p.Status = Convert.ToString(r["Status"]);
            p.Assessment = Convert.ToDecimal(r["Assessment"]);
            p.Paid = Convert.ToDecimal(r["Paid"]);
            p.Balance = Convert.ToDecimal(r["Balance"]);
            return p;
        }

        public static DataRow GetFees(int grade)
        {
            DataTable dt = Fill("SELECT Tuition, Misc, Registration FROM FeeSchedule WHERE Grade=@g", "@g", grade);
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public static DataTable GetSubjects(int grade)
        {
            return Fill("SELECT Code, SubjectName, Schedule FROM Subjects WHERE Grade=@g ORDER BY SortOrder", "@g", grade);
        }

        // Pinakabagong bayad ng estudyante. onlyPending = ang naghihintay pa ma-verify.
        public static DataRow GetLatestPayment(string studentNo, bool onlyPending)
        {
            string where = onlyPending ? " AND Status='Pending'" : "";
            DataTable dt = Fill("SELECT TOP 1 Amount, " +
                                "ISNULL(Method, LEFT(PaymentDetails, CHARINDEX(' -', PaymentDetails + ' -') - 1)) AS Method, " +
                                "DateSubmitted, Status FROM PendingPayments WHERE StudentNo=@no" + where +
                                " ORDER BY DateSubmitted DESC", "@no", studentNo);
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public static string Peso(decimal v)
        {
            return CashierData.Peso(v);
        }
    }

    // Sidebar ng student forms. Hinahanap nito ang buttons sa pangalan; kapag wala ang button, nilalaktawan lang.
    public static class StudentNav
    {
        public static void Wire(Form f, string user)
        {
            Hook(f, "btnDashboard", () => { if (!(f is StudentEnrollmentDashboard)) CashierNav.Open(f, new StudentEnrollmentDashboard(user)); });
            Hook(f, "btnEnrollment", () => { if (!(f is StudentEnrollment)) CashierNav.Open(f, new StudentEnrollment(user)); });
            Hook(f, "btnAssessment", () => { if (!(f is StudentAssessment)) CashierNav.Open(f, new StudentAssessment(user)); });

            // wala pang form: "Coming soon"
            Hook(f, "btnPayments", () => CashierNav.Soon("Payments"));
            Hook(f, "btnViewPayments", () => CashierNav.Soon("Payments"));
            Hook(f, "btnReceipts", () => CashierNav.Soon("Receipts"));
            Hook(f, "btnReport", () => CashierNav.Soon("Report"));

            Hook(f, "btnLogout", () => CashierNav.Logout());

            // pag isinara ang window (X), titigil ang program
            f.FormClosed += (s, e) => { if (!CashierNav.Leaving) Application.Exit(); };
        }

        static void Hook(Form f, string name, Action action)
        {
            Control[] c = f.Controls.Find(name, true);
            if (c.Length > 0) c[0].Click += (s, e) => action();
        }

        // Para sa label na posibleng wala sa design mo (hindi mag-e-error kapag wala)
        public static void SetText(Form f, string name, string text)
        {
            Control[] c = f.Controls.Find(name, true);
            if (c.Length > 0) c[0].Text = text;
        }
    }
}
