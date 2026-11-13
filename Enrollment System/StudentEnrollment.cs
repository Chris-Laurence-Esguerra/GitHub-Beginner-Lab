using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Enrollment_System
{
    public partial class StudentEnrollment : Form
    {
        private string loggedInUser;

        public StudentEnrollment() : this(null) { }
        public StudentEnrollment(string username)
        {
            InitializeComponent();
            loggedInUser = StudentData.Resolve(username);

            StudentNav.Wire(this, loggedInUser);

            Load -= StudentEnrollment_Load;
            Load += StudentEnrollment_Load;
        }

        private void label20_Click(object sender, EventArgs e)
        {
            
        }

        private void StudentEnrollment_Load(object sender, EventArgs e)
        {
            LoadEnrollment();
        }
        private void LoadEnrollment()
        {
            try
            {
                StudentProfile p = StudentData.GetProfile(loggedInUser);
                if (p == null)
                {
                    MessageBox.Show("Walang student record. I-run muna ang SQL script.", "My Enrollment");
                    return;
                }

                // Habang hindi pa Enrolled, "To be assigned" ang section at subjects
                string section = p.Enrolled ? p.Section : "To be assigned";

                // Header
                lblGradeandYearEnrollment.Text = "Grade " + p.Grade + " · " + section + " · SY " + p.SchoolYear;
                lblSurnameEnrollment.Text = p.Name;

                // 4 na kahon
                lblSchoolyear.Text = p.SchoolYear;
                lblGrade8.Text = "Grade " + p.Grade;
                lblRizal.Text = section;
                lblReturning.Text = p.EnrollmentType;

                // Subjects (5 ang may label sa design mo)
                Label[] names = { lblEnglish, lblMath, lblScience, lblFilipino, lblAP };
                Label[] codes = { lblCodeEng, lblMethod, lblCodeScience, lblcodefil, lblcodeAP };   // lblMethod = code ng Mathematics
                string[] times = { "lblTimeEng", "lblTimeMath", "lblTimeScience", "lblTimeFil", "lblTimeAP" };   // optional

                DataTable subjects = StudentData.GetSubjects(p.Grade);
                for (int i = 0; i < names.Length; i++)
                {
                    if (p.Enrolled && i < subjects.Rows.Count)
                    {
                        names[i].Text = Convert.ToString(subjects.Rows[i]["SubjectName"]);
                        codes[i].Text = Convert.ToString(subjects.Rows[i]["Code"]);
                        StudentNav.SetText(this, times[i], Convert.ToString(subjects.Rows[i]["Schedule"]));
                    }
                    else
                    {
                        names[i].Text = "To be assigned";
                        codes[i].Text = "-";
                        StudentNav.SetText(this, times[i], "-");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error sa pag-load ng enrollment: " + ex.Message + "\n\nNa-run na ba ang SQL script?", "My Enrollment");
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
    }
}
    