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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace Enrollment_System
{
    public partial class CashierEnrollmentDashboard : Form
    {
        private string loggedInUser;
        private int currentPage = 1;
        private const int PageSize = 8;
        public CashierEnrollmentDashboard()
        {
            InitializeComponent();
            Init();
        }
        public CashierEnrollmentDashboard(string username)
        {
            InitializeComponent();
            loggedInUser = username;
            Init();
        }
        private void Init()
        {
            SetupGrid();

            dgvCashierDashboard.CellContentClick -= dgvCashierDashboard_CellContentClick;
            dgvCashierDashboard.CellContentClick += dgvCashierDashboard_CellContentClick;
            btnPrevious.Click -= btnPrevious_Click; btnPrevious.Click += btnPrevious_Click;
            btnNext.Click -= btnNext_Click; btnNext.Click += btnNext_Click;
            btnStudent.Click -= btnStudent_Click; btnStudent.Click += btnStudent_Click;
            btnRecordPayment.Click -= btnRecordPayment_Click; btnRecordPayment.Click += btnRecordPayment_Click;

            // Sidebar: To verify, Receipts, Transactions, Daily collection, Log out
            // + pag isinara ang window (X), titigil ang buong program
            CashierNav.Wire(this, loggedInUser);
        }
        private void SetupGrid()
        {
            DataGridView g = dgvCashierDashboard;
            g.AutoGenerateColumns = false;
            g.Columns.Clear();
            g.AllowUserToAddRows = false;      // tinatanggal ang extra "*" row
            g.AllowUserToDeleteRows = false;
            g.ReadOnly = true;
            g.RowHeadersVisible = false;
            g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            g.Columns.Add(new DataGridViewTextBoxColumn { Name = "PaymentID", DataPropertyName = "PaymentID", Visible = false });
            g.Columns.Add(new DataGridViewTextBoxColumn { Name = "Student", HeaderText = "Student", DataPropertyName = "Student", FillWeight = 120 });
            g.Columns.Add(new DataGridViewTextBoxColumn { Name = "Payment", HeaderText = "Payment", DataPropertyName = "Payment", FillWeight = 120 });
            g.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Submitted",
                HeaderText = "Submitted",
                DataPropertyName = "Submitted",
                FillWeight = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "MMM d, h:mm tt" }
            });
            g.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Amount",
                HeaderText = "Amount",
                DataPropertyName = "Amount",
                FillWeight = 80,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "₱#,##0.00", Alignment = DataGridViewContentAlignment.MiddleRight }
            });

            DataGridViewButtonColumn btnVerify = new DataGridViewButtonColumn();
            btnVerify.Name = "ActionVerify";
            btnVerify.HeaderText = "Action";
            btnVerify.Text = "Verify";
            btnVerify.UseColumnTextForButtonValue = true;
            btnVerify.FlatStyle = FlatStyle.Popup;
            btnVerify.FillWeight = 55;
            g.Columns.Add(btnVerify);

            DataGridViewButtonColumn btnDelete = new DataGridViewButtonColumn();
            btnDelete.Name = "ActionDelete";
            btnDelete.HeaderText = "";
            btnDelete.Text = "Delete";
            btnDelete.UseColumnTextForButtonValue = true;
            btnDelete.FlatStyle = FlatStyle.Popup;
            btnDelete.FillWeight = 55;
            g.Columns.Add(btnDelete);
        }


        private void CashierEnrollmentDashboard_Load(object sender, EventArgs e)
        {
          
            LoadDashboardData();
            
        }
        public void LoadDashboardData()
        {
            try
            {
                // 4 na kahon sa taas
                DashStats s = CashierData.GetStats();
                lblCollectedToday.Text = CashierData.Peso(s.Collected);
                lblReceiptsIssued.Text = s.Receipts.ToString();
                lblPaymentsToVerify.Text = s.ToVerify.ToString();
                lblRejectedToday.Text = s.Rejected.ToString();

                // table (may paging)
                int total;
                DataTable dt = CashierData.GetPendingPage(currentPage, PageSize, out total);
                int pages = CashierData.Pages(total, PageSize);
                if (currentPage > pages)
                {
                    currentPage = pages;
                    dt = CashierData.GetPendingPage(currentPage, PageSize, out total);
                }

                dgvCashierDashboard.DataSource = dt;
                btnPageNumber.Text = currentPage.ToString();
                btnPrevious.Enabled = currentPage > 1;
                btnNext.Enabled = currentPage < pages;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error sa pag-load ng dashboard data: " + ex.Message);
            }
        }
        private void LoadSummaryMetrics()
        {
            try
            {
                using (SqlConnection conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    // Halimbawa ng pag-compute ng mga metrics gamit ang SQL
                    SqlCommand cmd1 = new SqlCommand("SELECT ISNULL(SUM(Amount), 0) FROM PendingPayments WHERE Status = 'Verified'", conn);
                    lblCollectedToday.Text = "₱ " + Convert.ToDecimal(cmd1.ExecuteScalar()).ToString("N2");

                    SqlCommand cmd2 = new SqlCommand("SELECT COUNT(*) FROM PendingPayments WHERE Status = 'Verified'", conn);
                    lblReceiptsIssued.Text = cmd2.ExecuteScalar().ToString();

                    SqlCommand cmd3 = new SqlCommand("SELECT COUNT(*) FROM PendingPayments WHERE Status = 'Pending'", conn);
                    lblPaymentsToVerify.Text = cmd3.ExecuteScalar().ToString();

                    SqlCommand cmd4 = new SqlCommand("SELECT COUNT(*) FROM PendingPayments WHERE Status = 'Rejected'", conn);
                    lblRejectedToday.Text = cmd4.ExecuteScalar().ToString();
                }
            }
            catch (Exception)
            {
                // Silent catch para sa summary kung may kulang pang tables
            }
        }

        private void dgvCashierDashboard_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            string col = dgvCashierDashboard.Columns[e.ColumnIndex].Name;
            if (col != "ActionVerify" && col != "ActionDelete") return;

            DataGridViewRow row = dgvCashierDashboard.Rows[e.RowIndex];
            int paymentId = Convert.ToInt32(row.Cells["PaymentID"].Value);
            string studentName = Convert.ToString(row.Cells["Student"].Value);

            // VERIFY ACTION
            if (col == "ActionVerify")
            {
                DialogResult ask = MessageBox.Show("I-verify ang bayad ni " + studentName + " at mag-issue ng resibo?",
                    "Verify payment", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (ask != DialogResult.Yes) return;

                string error;
                string receiptNo = CashierData.VerifyPayment(paymentId, out error);
                if (receiptNo == null)
                {
                    MessageBox.Show(error, "Hindi ma-verify", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                MessageBox.Show("Na-verify na ang bayad ni " + studentName + "!\nReceipt no. " + receiptNo,
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadDashboardData();
            }

            // DELETE ACTION
            if (col == "ActionDelete")
            {
                DialogResult res = MessageBox.Show("Gusto mo bang i-delete ang transaksyong ito para kay " + studentName + "?",
                    "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (res != DialogResult.Yes) return;

                string error;
                if (!CashierData.DeletePayment(paymentId, out error))
                {
                    MessageBox.Show(error, "Hindi ma-delete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                LoadDashboardData();
            }
        }

        private void btnStudent_Click(object sender, EventArgs e)
        {

            CashierStudentManagemen studentForm = new CashierStudentManagemen(loggedInUser);
            studentForm.Show();
            this.Hide();
        }

        private void btnRecordPayment_Click(object sender, EventArgs e)
        {
            CahierRecordPayment paymentForm = new CahierRecordPayment(loggedInUser);
            paymentForm.Show();
            this.Hide();
        }

        private void btnDashboard_Click(object sender, EventArgs e)
        {
           
        }

        private void btnPrevious_Click(object sender, EventArgs e)
        {
            if (currentPage > 1) { currentPage--; LoadDashboardData(); }
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            currentPage++;
            LoadDashboardData();
        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
