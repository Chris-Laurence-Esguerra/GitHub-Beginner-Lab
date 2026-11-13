using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;


namespace Enrollment_System
{
    public class RegistrarStats
    {
        public int Total, Enrolled, Pending, Rejected, Assessed, HasPayment, G7, G8, G9, G10;
    }
    internal class RegistrarData
    {
        static RegistrarData()
        {
            SeedData.Ensure();      // gumagawa ng tables / 50 sample students kung walang laman
            EnsureSchema();         // nagdadagdag ng kulang na columns
        }

        // ---------- helpers ----------
        static SqlCommand Cmd(SqlConnection c, string sql, params object[] kv)
        {
            SqlCommand cmd = new SqlCommand(sql, c);
            for (int i = 0; i + 1 < kv.Length; i += 2)
                cmd.Parameters.AddWithValue((string)kv[i], kv[i + 1] ?? DBNull.Value);
            return cmd;
        }

        static DataTable Fill(string sql, params object[] kv)
        {
            using (SqlConnection c = DatabaseConnection.GetConnection())
            {
                c.Open();
                using (SqlCommand cmd = Cmd(c, sql, kv))
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
                using (SqlCommand cmd = Cmd(c, sql, kv)) return cmd.ExecuteScalar();
            }
        }

        static int NonQuery(string sql, params object[] kv)
        {
            using (SqlConnection c = DatabaseConnection.GetConnection())
            {
                c.Open();
                using (SqlCommand cmd = Cmd(c, sql, kv)) return cmd.ExecuteNonQuery();
            }
        }

        static int Num(DataRow r, string col)
        {
            return r[col] == DBNull.Value ? 0 : Convert.ToInt32(r[col]);
        }

        public static int Pages(int total, int size)
        {
            return Math.Max(1, (int)Math.Ceiling(total / (double)size));
        }

        // ---------- schema ----------
        static void EnsureSchema()
        {
            string[] steps =
            {
                "IF COL_LENGTH('dbo.Students','Section')        IS NULL ALTER TABLE dbo.Students ADD Section        NVARCHAR(30)  NULL",
                "IF COL_LENGTH('dbo.Students','EnrollmentType') IS NULL ALTER TABLE dbo.Students ADD EnrollmentType NVARCHAR(20)  NULL",
                "IF COL_LENGTH('dbo.Students','SchoolYear')     IS NULL ALTER TABLE dbo.Students ADD SchoolYear     NVARCHAR(20)  NULL",
                "IF COL_LENGTH('dbo.Students','RejectReason')   IS NULL ALTER TABLE dbo.Students ADD RejectReason   NVARCHAR(200) NULL",

                // default na section ng mga Enrolled na wala pang section (hindi ginagalaw ang Pending)
                "UPDATE dbo.Students SET Section = CASE Grade WHEN 7 THEN 'Mabini' WHEN 8 THEN 'Rizal' WHEN 9 THEN 'Bonifacio' ELSE 'Luna' END " +
                "WHERE Section IS NULL AND ISNULL(Status,'Enrolled') = 'Enrolled'",
                "UPDATE dbo.Students SET EnrollmentType = 'Returning' WHERE EnrollmentType IS NULL",
                "UPDATE dbo.Students SET SchoolYear = '2026-2027' WHERE SchoolYear IS NULL",
                "UPDATE dbo.Students SET Status = 'Enrolled' WHERE Status IS NULL",

                @"IF OBJECT_ID('dbo.FeeSchedule') IS NULL
                  CREATE TABLE dbo.FeeSchedule (Grade INT NOT NULL PRIMARY KEY, Tuition DECIMAL(10,2) NOT NULL, Misc DECIMAL(10,2) NOT NULL, Registration DECIMAL(10,2) NOT NULL)",
                @"IF NOT EXISTS (SELECT 1 FROM dbo.FeeSchedule)
                  INSERT INTO dbo.FeeSchedule (Grade, Tuition, Misc, Registration) VALUES (7,18000,3500,1000),(8,18000,3500,1000),(9,18000,3500,1000),(10,18000,3500,1000)"
            };

            try
            {
                using (SqlConnection c = DatabaseConnection.GetConnection())
                {
                    c.Open();
                    foreach (string sql in steps)
                        using (SqlCommand cmd = new SqlCommand(sql, c)) cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hindi na-update ang database: " + ex.Message, "Registrar");
            }
        }

        // ---------- Dashboard ----------
        public static RegistrarStats GetStats()
        {
            DataTable dt = Fill(@"SELECT COUNT(*) AS Total,
                SUM(CASE WHEN Status='Enrolled' THEN 1 ELSE 0 END) AS Enrolled,
                SUM(CASE WHEN Status='Pending'  THEN 1 ELSE 0 END) AS Pending,
                SUM(CASE WHEN Status='Rejected' THEN 1 ELSE 0 END) AS Rejected,
                SUM(CASE WHEN Status='Enrolled' AND Assessment > 0 THEN 1 ELSE 0 END) AS Assessed,
                SUM(CASE WHEN Paid > 0 THEN 1 ELSE 0 END) AS HasPayment,
                SUM(CASE WHEN Status='Enrolled' AND Grade = 7  THEN 1 ELSE 0 END) AS G7,
                SUM(CASE WHEN Status='Enrolled' AND Grade = 8  THEN 1 ELSE 0 END) AS G8,
                SUM(CASE WHEN Status='Enrolled' AND Grade = 9  THEN 1 ELSE 0 END) AS G9,
                SUM(CASE WHEN Status='Enrolled' AND Grade = 10 THEN 1 ELSE 0 END) AS G10
                FROM Students");

            DataRow r = dt.Rows[0];
            RegistrarStats s = new RegistrarStats();
            s.Total = Num(r, "Total"); s.Enrolled = Num(r, "Enrolled"); s.Pending = Num(r, "Pending"); s.Rejected = Num(r, "Rejected");
            s.Assessed = Num(r, "Assessed"); s.HasPayment = Num(r, "HasPayment");
            s.G7 = Num(r, "G7"); s.G8 = Num(r, "G8"); s.G9 = Num(r, "G9"); s.G10 = Num(r, "G10");
            return s;
        }

        // ---------- Student management ----------
        public static DataTable SearchStudents(string search, int grade, string status, int page, int size, out int total)
        {
            string src = " FROM Students WHERE (Name LIKE @q OR StudentNo LIKE @q) AND (@g = 0 OR Grade = @g) " +
                         "AND (@s = 'All status' OR Status = @s)";
            string q = "%" + search + "%";
            total = Convert.ToInt32(Scalar("SELECT COUNT(*)" + src, "@q", q, "@g", grade, "@s", status));
            return Fill("SELECT StudentNo, Name, Grade, ISNULL(Section,'') AS Section, Status" + src +
                        " ORDER BY StudentNo OFFSET @o ROWS FETCH NEXT @n ROWS ONLY",
                        "@q", q, "@g", grade, "@s", status, "@o", (page - 1) * size, "@n", size);
        }

        public static string[] SectionsFor(int grade)
        {
            switch (grade)
            {
                case 7: return new[] { "Mabini", "Aguinaldo" };
                case 8: return new[] { "Rizal", "Del Pilar" };
                case 9: return new[] { "Bonifacio", "Jacinto" };
                default: return new[] { "Luna", "Silang" };
            }
        }

        public static string NextStudentNo()
        {
            object last = Scalar("SELECT TOP 1 StudentNo FROM Students WHERE StudentNo LIKE '2026-%' ORDER BY StudentNo DESC");
            int n = 0;
            if (last != null && last != DBNull.Value) int.TryParse(Convert.ToString(last).Substring(5), out n);
            return "2026-" + (n + 1).ToString("D4");
        }

        // Nagdadagdag ng estudyante sa database. approve = true -> Enrolled agad (kailangan ng section).
        public static string AddStudent(string name, int grade, string section, string type, string schoolYear, bool approve, out string error)
        {
            error = null;
            try
            {
                if (Convert.ToInt32(Scalar("SELECT COUNT(*) FROM Students WHERE LOWER(Name) = LOWER(@n)", "@n", name)) > 0)
                {
                    error = "May estudyante nang may pangalang \"" + name + "\".";
                    return null;
                }

                object fee = Scalar("SELECT Tuition + Misc + Registration FROM FeeSchedule WHERE Grade = @g", "@g", grade);
                decimal total = (fee == null || fee == DBNull.Value) ? 22500m : Convert.ToDecimal(fee);

                string no = NextStudentNo();
                NonQuery("INSERT INTO Students (StudentNo, Name, Grade, Assessment, Paid, Balance, Status, Section, EnrollmentType, SchoolYear) " +
                         "VALUES (@no, @n, @g, @a, 0, @a, @st, @sec, @t, @y)",
                         "@no", no, "@n", name, "@g", grade, "@a", total,
                         "@st", approve ? "Enrolled" : "Pending",
                         "@sec", string.IsNullOrEmpty(section) ? null : section,
                         "@t", type, "@y", schoolYear);
                return no;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return null;
            }
        }

        public static bool ApproveStudent(string no, out string error)
        {
            error = null;
            DataTable dt = Fill("SELECT Grade, ISNULL(Section,'') AS Section, Status FROM Students WHERE StudentNo=@no", "@no", no);
            if (dt.Rows.Count == 0) { error = "Hindi nahanap ang estudyante."; return false; }
            if (Convert.ToString(dt.Rows[0]["Status"]) == "Enrolled") { error = "Enrolled na ang estudyanteng ito."; return false; }

            string section = Convert.ToString(dt.Rows[0]["Section"]);
            if (section.Length == 0) section = SectionsFor(Convert.ToInt32(dt.Rows[0]["Grade"]))[0];

            NonQuery("UPDATE Students SET Status='Enrolled', Section=@s, RejectReason=NULL WHERE StudentNo=@no", "@s", section, "@no", no);
            return true;
        }

        public static bool RejectStudent(string no, string reason, out string error)
        {
            error = null;
            if (NonQuery("UPDATE Students SET Status='Rejected', RejectReason=@r WHERE StudentNo=@no AND Status='Pending'", "@r", reason, "@no", no) > 0) return true;
            error = "Pending lang ang puwedeng i-reject.";
            return false;
        }

        // Hindi puwedeng i-delete kapag may bayad na (para hindi mawala ang record ng resibo)
        public static bool DeleteStudent(string no, out string error)
        {
            error = null;
            if (Convert.ToInt32(Scalar("SELECT COUNT(*) FROM PendingPayments WHERE StudentNo=@no", "@no", no)) > 0)
            {
                error = "Hindi puwedeng i-delete: may bayad na ang estudyanteng ito.";
                return false;
            }
            NonQuery("DELETE FROM Students WHERE StudentNo=@no", "@no", no);
            return true;
        }
    }

    // Sidebar ng Registrar forms. Hinahanap ang buttons sa pangalan; kapag wala, nilalaktawan lang.
    public static class RegistrarNav
    {
        public static void Wire(Form f, string user)
        {
            Hook(f, new[] { "btnDashboard" }, () => { if (!(f is RegistrarEnrollmentDashboard)) CashierNav.Open(f, new RegistrarEnrollmentDashboard(user)); });
            Hook(f, new[] { "btnStudent" }, () => { if (!(f is RegistrarStudentManagement)) CashierNav.Open(f, new RegistrarStudentManagement(user)); });

            // wala pang form: "Coming soon"
            Hook(f, new[] { "btnEnrollment" }, () => CashierNav.Soon("Enrollment"));
            Hook(f, new[] { "btnAssessment" }, () => CashierNav.Soon("Assessment"));
            Hook(f, new[] { "btnPayment", "btnPayments", "btnCashiering", "btnPaymentCashiering" }, () => CashierNav.Soon("Payment / Cashiering"));
            Hook(f, new[] { "btnReceipts" }, () => CashierNav.Soon("Receipts"));
            Hook(f, new[] { "btnReport", "btnReports" }, () => CashierNav.Soon("Report"));

            Hook(f, new[] { "btnLogout" }, () => CashierNav.Logout());

            // pag isinara ang window (X), titigil ang program
            f.FormClosed += (s, e) => { if (!CashierNav.Leaving) Application.Exit(); };
        }

        static void Hook(Form f, string[] names, Action action)
        {
            foreach (string n in names)
            {
                Control[] c = f.Controls.Find(n, true);
                if (c.Length > 0) { c[0].Click += (s, e) => action(); return; }
            }
        }

        // Maliit na input box (para sa dahilan ng reject)
        public static string AskText(IWin32Window owner, string title, string label)
        {
            using (Form f = new Form())
            {
                f.Text = title;
                f.Width = 420;
                f.Height = 190;
                f.StartPosition = FormStartPosition.CenterParent;
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.MaximizeBox = false;
                f.MinimizeBox = false;

                Label l = new Label(); l.Text = label; l.Left = 15; l.Top = 15; l.Width = 370;
                TextBox t = new TextBox(); t.Left = 15; t.Top = 45; t.Width = 370;
                Button ok = new Button(); ok.Text = "OK"; ok.Left = 215; ok.Top = 85; ok.Width = 80; ok.DialogResult = DialogResult.OK;
                Button cancel = new Button(); cancel.Text = "Cancel"; cancel.Left = 305; cancel.Top = 85; cancel.Width = 80; cancel.DialogResult = DialogResult.Cancel;

                f.Controls.Add(l); f.Controls.Add(t); f.Controls.Add(ok); f.Controls.Add(cancel);
                f.AcceptButton = ok;
                f.CancelButton = cancel;

                return f.ShowDialog(owner) == DialogResult.OK ? t.Text.Trim() : null;
            }
        }
    }

    // Window para magdagdag ng estudyante / bagong enrollment (ginawa sa code, walang designer)
    public class NewEnrollmentForm : Form
    {
        private TextBox txtNo = new TextBox();
        private TextBox txtName = new TextBox();
        private TextBox txtYear = new TextBox();
        private ComboBox cboGrade = new ComboBox();
        private ComboBox cboSection = new ComboBox();
        private ComboBox cboType = new ComboBox();

        public string SavedStudentNo = null;

        public NewEnrollmentForm(string title)
        {
            Text = title;
            Width = 460;
            Height = 400;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            int y = 20;
            AddRow("Student no.", txtNo, ref y);
            AddRow("Full name", txtName, ref y);
            AddRow("Grade level", cboGrade, ref y);
            AddRow("Section", cboSection, ref y);
            AddRow("Enrollment type", cboType, ref y);
            AddRow("School year", txtYear, ref y);

            txtNo.ReadOnly = true;
            txtNo.Text = RegistrarData.NextStudentNo();
            txtYear.ReadOnly = true;
            txtYear.Text = "2026-2027";

            cboGrade.DropDownStyle = ComboBoxStyle.DropDownList;
            cboGrade.Items.AddRange(new object[] { "Grade 7", "Grade 8", "Grade 9", "Grade 10" });
            cboGrade.SelectedIndexChanged += (s, e) => FillSections();

            cboSection.DropDownStyle = ComboBoxStyle.DropDownList;

            cboType.DropDownStyle = ComboBoxStyle.DropDownList;
            cboType.Items.AddRange(new object[] { "New", "Returning", "Transferee" });
            cboType.SelectedIndex = 0;

            cboGrade.SelectedIndex = 0;     // tatawag din sa FillSections

            Button btnPending = new Button(); btnPending.Text = "Save as pending"; btnPending.Left = 20; btnPending.Top = y + 10; btnPending.Width = 130; btnPending.Height = 32;
            Button btnApprove = new Button(); btnApprove.Text = "Save and approve"; btnApprove.Left = 160; btnApprove.Top = y + 10; btnApprove.Width = 150; btnApprove.Height = 32;
            Button btnCancel = new Button(); btnCancel.Text = "Cancel"; btnCancel.Left = 320; btnCancel.Top = y + 10; btnCancel.Width = 100; btnCancel.Height = 32; btnCancel.DialogResult = DialogResult.Cancel;

            btnPending.Click += (s, e) => Save(false);
            btnApprove.Click += (s, e) => Save(true);

            Controls.Add(btnPending);
            Controls.Add(btnApprove);
            Controls.Add(btnCancel);
            CancelButton = btnCancel;
        }

        private void AddRow(string label, Control box, ref int y)
        {
            Label l = new Label();
            l.Text = label;
            l.Left = 20;
            l.Top = y + 4;
            l.Width = 140;
            box.Left = 170;
            box.Top = y;
            box.Width = 250;
            Controls.Add(l);
            Controls.Add(box);
            y += 42;
        }

        private int SelectedGrade()
        {
            return cboGrade.SelectedIndex + 7;
        }

        private void FillSections()
        {
            cboSection.Items.Clear();
            cboSection.Items.Add("(Not assigned)");
            foreach (string s in RegistrarData.SectionsFor(SelectedGrade())) cboSection.Items.Add(s);
            cboSection.SelectedIndex = 0;
        }

        private void Save(bool approve)
        {
            string name = txtName.Text.Trim();
            if (name.Length == 0)
            {
                MessageBox.Show("Ilagay ang buong pangalan ng estudyante.", "New enrollment", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return;
            }

            string section = cboSection.SelectedIndex > 0 ? Convert.ToString(cboSection.SelectedItem) : "";
            if (approve && section.Length == 0)
            {
                MessageBox.Show("Pumili ng section bago i-approve.", "New enrollment", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string error;
            string no = RegistrarData.AddStudent(name, SelectedGrade(), section, Convert.ToString(cboType.SelectedItem), txtYear.Text, approve, out error);
            if (no == null)
            {
                MessageBox.Show(error, "Hindi na-save", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SavedStudentNo = no;
            MessageBox.Show("Na-save si " + name + " (" + no + ") bilang " + (approve ? "Enrolled" : "Pending") + ".",
                "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
        }
    }
}

