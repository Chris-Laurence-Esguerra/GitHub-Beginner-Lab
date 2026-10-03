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
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT Role FROM Users WHERE Username = @username AND Password = @password";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@password", password);

                        object result = cmd.ExecuteScalar();

                        if (result != null)
                        {
                            string role = result.ToString();

                            // 1. Check kung Admin / Cashier
                            if (role == "Admin")
                            {
                                MessageBox.Show("Login Successful! Welcome, Cashier.", "Access Granted", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                CashierEnrollmentDashboard dashboardForm = new CashierEnrollmentDashboard(username);
                                dashboardForm.Show();
                                this.Hide();
                            }
                            // 2. Check kung Student
                            else if (role == "Student")
                            {
                                MessageBox.Show("Login Successful! Welcome, Student.", "Access Granted", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                StudentEnrollmentDashboard studentDashboard = new StudentEnrollmentDashboard(username);
                                studentDashboard.Show();
                                this.Hide();
                            }
                        }
                        else
                        {
                            MessageBox.Show("Invalid username or password. Please try again.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            txtbPassword.Clear();
                            txtbUsername.Focus();
                        }
                    }
                }
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
