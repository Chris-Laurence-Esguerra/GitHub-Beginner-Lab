using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Enrollment_System
{
    public partial class RegistrarEnrollmentDashboard : Form
    {
        public string loggedInUser;
        private Panel barRegistered, barEnrolled, barAssessed, barHasPayment;
        private Panel barG7, barG8, barG9, barG10;


        public RegistrarEnrollmentDashboard() : this(null) { }

        public RegistrarEnrollmentDashboard(string username)
        {
            InitializeComponent();
            loggedInUser = username;
            RegistrarNav.Wire(this, loggedInUser);

            btnNewEnrollment.Click -= btnNewEnrollment_Click;
            btnNewEnrollment.Click += btnNewEnrollment_Click;

            // mga bar
            barRegistered = MakeBar(lblRegisteredVal);
            barEnrolled = MakeBar(lblEnrolledVal);
            barAssessed = MakeBar(lblAssessedVal);
            barHasPayment = MakeBar(lblHasPaymentVal);
            barG10 = MakeBar(lblGrade10Val);
            barG9 = MakeBar(lblGrade9Val);
            barG8 = MakeBar(lblGrade8Val);
            barG7 = MakeBar(lblGrade7Val);

            Load -= EnrollmentDashboard_Load;
            Load += EnrollmentDashboard_Load;
            // Log out button (kung may btnLogout sa design mo)
            Control[] lg = Controls.Find("btnLogout", true);
            if (lg.Length > 0) lg[0].Click += (s, e) => CashierNav.Logout();

            // pag isinara ang window (X), titigil ang buong program
            FormClosed += (s, e) => { if (!CashierNav.Leaving) Application.Exit(); };
        }

        private void EnrollmentDashboard_Load(object sender, EventArgs e)
        {
            LoadDashboard();
        }
        private Panel MakeBar(Label valueLabel)
        {
            Panel track = new Panel();
            track.Width = 140;
            track.Height = 12;
            track.BackColor = Color.Gainsboro;
            track.Left = valueLabel.Left - track.Width - 12;
            track.Top = valueLabel.Top + (valueLabel.Height - track.Height) / 2;

            Panel fill = new Panel();
            fill.Dock = DockStyle.Left;
            fill.Width = 0;
            fill.BackColor = Color.FromArgb(0, 102, 204);
            track.Controls.Add(fill);

            valueLabel.Parent.Controls.Add(track);
            track.BringToFront();
            return fill;
        }

        private void SetBar(Panel fill, int value, int max)
        {
            Panel track = (Panel)fill.Parent;
            int w = max <= 0 ? 0 : (int)Math.Round(track.Width * (double)value / max);
            fill.Width = Math.Min(track.Width, Math.Max(0, w));
        }

        // ---------- data ----------
        private void LoadDashboard()
        {
            try
            {
                RegistrarStats s = RegistrarData.GetStats();

                // 4 na kahon sa taas
                lblTotalStudents.Text = s.Total.ToString();
                lblEnrolled.Text = s.Enrolled.ToString();
                lblPendingEnrollments.Text = s.Pending.ToString();
                lblRejected.Text = s.Rejected.ToString();

                // Enrollment pipeline
                lblRegisteredVal.Text = s.Total.ToString();
                lblEnrolledVal.Text = s.Enrolled.ToString();
                lblAssessedVal.Text = s.Assessed.ToString();
                lblHasPaymentVal.Text = s.HasPayment.ToString();

                SetBar(barRegistered, s.Total, s.Total);
                SetBar(barEnrolled, s.Enrolled, s.Total);
                SetBar(barAssessed, s.Assessed, s.Total);
                SetBar(barHasPayment, s.HasPayment, s.Total);

                // Enrolled by grade level
                lblGrade10Val.Text = s.G10.ToString();
                lblGrade9Val.Text = s.G9.ToString();
                lblGrade8Val.Text = s.G8.ToString();
                lblGrade7Val.Text = s.G7.ToString();

                int max = Math.Max(Math.Max(s.G7, s.G8), Math.Max(s.G9, s.G10));
                SetBar(barG10, s.G10, max);
                SetBar(barG9, s.G9, max);
                SetBar(barG8, s.G8, max);
                SetBar(barG7, s.G7, max);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error sa pag-load ng dashboard: " + ex.Message, "Dashboard");
            }
        }


        private void btnNewEnrollment_Click(object sender, EventArgs e)
        {
            using (NewEnrollmentForm f = new NewEnrollmentForm("New enrollment"))
            {
                if (f.ShowDialog(this) == DialogResult.OK) LoadDashboard();   // updated agad ang mga numero at bar
            }
        }
    }
}