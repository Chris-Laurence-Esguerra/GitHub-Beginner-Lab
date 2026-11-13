using System;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace Enrollment_System
{
    public partial class LogInForm : Form
    {
        private string connectionString = @"Data Source=ITTCHAN-PC\SQLEXPRESS;Initial Catalog=EnrollmentDB;Integrated Security=True"; public LogInForm()
        {
            InitializeComponent();
            VisibleChanged += (s, e) =>
            {
                if (Visible)
                {
                    txtbPassword.Clear();
                    txtbUsername.Focus();
                }
            };
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtbUsername.Text.Trim();
            string password = txtbPassword.Text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter both username and password.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string role = null;

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT Role FROM Users WHERE Username = @username AND Password = @password";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@password", password);

                        object result = cmd.ExecuteScalar();
                        if (result != null) role = result.ToString().Trim();
                    }
                }

                if (role == null)
                {
                    MessageBox.Show("Invalid username or password. Please try again.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtbPassword.Clear();
                    txtbUsername.Focus();
                    return;
                }

                Form next = null;

                switch (role.ToLower())
                {
                    case "admin":
                    case "registrar":
                        // Walang hiwalay na AdminDashboard, kaya ang Registrar dashboard ang bubuksan para sa Admin at Registrar
                        MessageBox.Show("Login Successful! Welcome, " + role + ".", "Access Granted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        next = new RegistrarEnrollmentDashboard(username);
                        break;

                    case "cashier":
                        MessageBox.Show("Login Successful! Welcome, Cashier.", "Access Granted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        next = new CashierEnrollmentDashboard(username);
                        break;

                    case "student":
                        MessageBox.Show("Login Successful! Welcome, Student.", "Access Granted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        next = new StudentEnrollmentDashboard(username);
                        break;

                    default:
                        MessageBox.Show("Walang form para sa role na: " + role, "Login", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                }

                next.Show();
                this.Hide();      // naka-hide lang ang login form (ito ang main form); ibinabalik ito ng Log out
            }
            catch (Exception ex)
            {
                MessageBox.Show("Database Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    

        private void btnCancel_Click_1(object sender, EventArgs e)
        {
            // I-clear ang mga input kapag pinindot ang cancel
            txtbUsername.Clear();
            txtbPassword.Clear();
            txtbUsername.Focus();
        }

        private void lnklbForgotPassword_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            CreateNewPasswordForm forgotPasswordForm = new CreateNewPasswordForm();

            // Ipakita ang form at i-hide muna ang Login Form
            forgotPasswordForm.Show();
            this.Hide();
        }

        private void LogInForm_Load(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }
    }
}
