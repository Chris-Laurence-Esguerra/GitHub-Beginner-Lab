using System;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace Enrollment_System
{
    public partial class CreateNewPasswordForm : Form
    {
        private string connectionString = @"Data Source=ITTCHAN-PC\SQLEXPRESS;Initial Catalog=EnrollmentDB;Integrated Security=True"; public CreateNewPasswordForm()
        {
            InitializeComponent();
        }

        private void btnCreatePassword_Click(object sender, EventArgs e)
        {
            string username = txtbUsername.Text.Trim();
            string newPassword = txtbNewPassword.Text.Trim();
            string confirmPassword = txtbConfirmPassword.Text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                MessageBox.Show("Please fill in all fields.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (newPassword != confirmPassword)
            {
                MessageBox.Show("Passwords do not match! Please check and try again.", "Password Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtbNewPassword.Clear();
                txtbConfirmPassword.Clear();
                txtbNewPassword.Focus();
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    // I-update ang password ng student sa database gamit ang SQL UPDATE command
                    string query = "UPDATE Users SET Password = @newPassword WHERE Username = @username AND Role = 'Student'";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@newPassword", newPassword);
                        cmd.Parameters.AddWithValue("@username", username);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Password successfully updated! You can now log in using your new student credentials.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            // Bumalik sa LogInForm pagka-confirm
                            LogInForm loginForm = new LogInForm();
                            loginForm.Show();
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("Username not found or is not a student account.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Database Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            txtbUsername.Clear();
            txtbNewPassword.Clear();
            txtbConfirmPassword.Clear();
            txtbUsername.Focus();
        }

        private void CreateNewPasswordForm_Load(object sender, EventArgs e)
        {

        }
    }
}
