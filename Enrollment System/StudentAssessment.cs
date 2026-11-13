using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Enrollment_System
{
    public partial class StudentAssessment : Form
    {
        private string loggedInUser;
        private StudentProfile profile = null;
        private DataRow fees = null;

        public StudentAssessment() : this(null) { }

        public StudentAssessment(string username)
        {
            InitializeComponent();
            loggedInUser = StudentData.Resolve(username);

            StudentNav.Wire(this, loggedInUser);

            btnPrintassessment.Click -= btnPrintassessment_Click;
            btnPrintassessment.Click += btnPrintassessment_Click;

            Load -= StudentAssessment_Load;
            Load += StudentAssessment_Load;
        }
        private void LoadAssessment()
        {
            try
            {
                profile = StudentData.GetProfile(loggedInUser);
                if (profile == null)
                {
                    MessageBox.Show("Walang student record. I-run muna ang SQL script.", "My Assessment");
                    return;
                }
                fees = StudentData.GetFees(profile.Grade);

                string section = profile.Enrolled ? profile.Section : "To be assigned";
                lblGradeandYearAssesment.Text = "Grade " + profile.Grade + " · " + section + " · SY " + profile.SchoolYear;
                lblSurnameStudentassessment.Text = profile.Name;

                // Fees (read-only; galing sa Registrar / FeeSchedule)
                lblTuitionfee.Text = StudentData.Peso(Fee("Tuition"));
                lblMiscellaneousfee.Text = StudentData.Peso(Fee("Misc"));
                lblRegistrationfee.Text = StudentData.Peso(Fee("Registration"));

                // Total assessment = tuition + miscellaneous + registration
                // Total paid = verified payments lang; Remaining balance = total assessment - total paid
                lblTotalassessment.Text = StudentData.Peso(profile.Assessment);
                lblTotalamountpaid.Text = StudentData.Peso(profile.Paid);
                lblRemainingbalance.Text = StudentData.Peso(profile.Balance);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error sa pag-load ng assessment: " + ex.Message + "\n\nNa-run na ba ang SQL script?", "My Assessment");
            }
        }

        private decimal Fee(string column)
        {
            if (fees == null || fees[column] == DBNull.Value) return 0;
            return Convert.ToDecimal(fees[column]);
        }


        private void lblScience_Click(object sender, EventArgs e)
        {

        }

        private void btnPrintassessment_Click(object sender, EventArgs e)
        {
            if (profile == null) { MessageBox.Show("Walang assessment na ipi-print.", "Print"); return; }

            string[] lines =
            {
                "Student: " + profile.Name + " (" + profile.StudentNo + ")",
                "Grade and section: Grade " + profile.Grade + " - " + profile.Section,
                "School year: " + profile.SchoolYear,
                "",
                "Tuition fee:            " + StudentData.Peso(Fee("Tuition")),
                "Miscellaneous fee:      " + StudentData.Peso(Fee("Misc")),
                "Registration fee:       " + StudentData.Peso(Fee("Registration")),
                "Total assessment:       " + StudentData.Peso(profile.Assessment),
                "Total amount paid:      " + StudentData.Peso(profile.Paid),
                "Remaining balance:      " + StudentData.Peso(profile.Balance),
                "",
                "Printed: " + DateTime.Now.ToString("MMM d, yyyy h:mm tt")
            };

            PrintDocument doc = new PrintDocument();
            doc.PrintPage += (s, ev) =>
            {
                using (Font fb = new Font("Segoe UI", 15, FontStyle.Bold))
                using (Font f = new Font("Segoe UI", 11))
                {
                    float y = 60;
                    ev.Graphics.DrawString("Sti College Balayan", fb, Brushes.Black, 60, y); y += 32;
                    ev.Graphics.DrawString("STUDENT ASSESSMENT", fb, Brushes.Black, 60, y); y += 46;
                    foreach (string line in lines) { ev.Graphics.DrawString(line, f, Brushes.Black, 60, y); y += 28; }
                }
            };

            using (PrintPreviewDialog dlg = new PrintPreviewDialog())
            {
                dlg.Document = doc;
                dlg.Width = 900;
                dlg.Height = 700;
                dlg.ShowDialog(this);
            }
        }

        // ---------- walang laman na handlers (baka naka-wire sa designer; huwag burahin) ----------
        private void btnDashboard_Click(object sender, EventArgs e) { }
        private void btnEnrollment_Click(object sender, EventArgs e) { }
        private void btnAssessment_Click(object sender, EventArgs e) { }
        private void btnPayments_Click(object sender, EventArgs e) { }
        private void btnReceipts_Click(object sender, EventArgs e) { }
        private void btnReport_Click(object sender, EventArgs e) { }
        private void btnLogout_Click(object sender, EventArgs e) { }

        private void StudentAssessment_Load(object sender, EventArgs e)
        {
            LoadAssessment();
        }
    }
}
  
