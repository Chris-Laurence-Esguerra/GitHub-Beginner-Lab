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
    public partial class StudentEnrollmentDashboard : Form
    {
        public string loggedInUser;
        public StudentEnrollmentDashboard() : this(null) { }

        public StudentEnrollmentDashboard(string username)
        {
            InitializeComponent();
            loggedInUser = username;
            loggedInUser = StudentData.Resolve(username);

            // Sidebar: Dashboard, Enrollment, Assessment, Payments, Receipts, Report, Log out
            StudentNav.Wire(this, loggedInUser);

            Load -= StudentEnrollmentDashboard_Load;
            Load += StudentEnrollmentDashboard_Load;
        }

        private void StudentEnrollmentDashboard_Load(object sender, EventArgs e)
        {
            LoadDashboard();
        }

        private void btnViewPayments_Click(object sender, EventArgs e)
        {

        }
        private void LoadDashboard()
        {
            try
            {
                StudentProfile p = StudentData.GetProfile(loggedInUser);
                if (p == null)
                {
                    MessageBox.Show("Walang student record. I-run muna ang SQL script.", "Dashboard");
                    return;
                }

                string section = p.Enrolled ? p.Section : "To be assigned";

                // Header
                lblStudentName.Text = "HI, " + p.FirstName;
                lblGradeandYearDashboard.Text = "Grade " + p.Grade + " · " + section + " · SY " + p.SchoolYear;
                lblSurnameStudentdashboard.Text = p.Name;

                // Enrollment card
                int subjects = StudentData.GetSubjects(p.Grade).Rows.Count;
                lblGrade8Rizal.Text = "Grade " + p.Grade + " - " + section;
                lbl8subjectreturning.Text = (p.Enrolled ? subjects.ToString() : "0") + " subjects - " + p.EnrollmentType + " student";

                // Remaining balance card
                lblRemainingBalance.Text = StudentData.Peso(p.Balance);

                // Latest payment card
                DataRow last = StudentData.GetLatestPayment(p.StudentNo, false);
                if (last == null)
                {
                    lblLatestPayment.Text = StudentData.Peso(0);
                    lblGcashNov.Text = "No payment yet";
                }
                else
                {
                    string status = Convert.ToString(last["Status"]);
                    lblLatestPayment.Text = StudentData.Peso(Convert.ToDecimal(last["Amount"]));
                    lblGcashNov.Text = Convert.ToString(last["Method"]) + " - " +
                                       Convert.ToDateTime(last["DateSubmitted"]).ToString("MMM d, yyyy") +
                                       " (" + (status == "Pending" ? "Pending verification" : status) + ")";
                }

                // Next step (pangalanan ang label na lblNextStep sa design; kapag wala, lalaktawan)
                DataRow pending = StudentData.GetLatestPayment(p.StudentNo, true);
                string next;
                if (pending != null)
                    next = "Your " + Convert.ToString(pending["Method"]) + " payment of " +
                           StudentData.Peso(Convert.ToDecimal(pending["Amount"])) +
                           " is waiting for the cashier to verify it. You will see your receipt here once it is verified.";
                else if (p.Balance > 0)
                    next = "Pay your remaining balance of " + StudentData.Peso(p.Balance) +
                           " at the cashier. You will see your receipt here once it is verified.";
                else
                    next = "Everything is paid. Thank you!";
                StudentNav.SetText(this, "lblNextStep", next);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error sa pag-load ng dashboard: " + ex.Message + "\n\nNa-run na ba ang SQL script?", "Dashboard");
            }
        }
    }
}
    