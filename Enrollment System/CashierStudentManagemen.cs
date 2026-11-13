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
    public partial class CashierStudentManagemen : Form
    {
        private string loggedInUser;
        private int currentPage = 1;
        private const int PageSize = 8;

        // Kung ang pangalan ng grid mo ay "dataGridView1studentmanagement", palitan ang dgvStudentManagement dito (isang line lang)
        private DataGridView Grid { get { return dataGridView1studentmanagement; } }
        public CashierStudentManagemen(string username)
        {
            InitializeComponent();
            loggedInUser = username;

            cmbFilterBalance.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFilterBalance.Items.Clear();
            cmbFilterBalance.Items.AddRange(new object[] { "All balances", "Unpaid", "Partial", "Fully paid" });
            cmbFilterBalance.SelectedIndex = 0;

            SetupGrid();

            // may -= para hindi magdoble kung na-wire na sa designer
            cmbFilterBalance.SelectedIndexChanged -= cmbFilterBalance_SelectedIndexChanged;
            cmbFilterBalance.SelectedIndexChanged += cmbFilterBalance_SelectedIndexChanged;
            btnPrev.Click -= btnPrev_Click; btnPrev.Click += btnPrev_Click;
            btnNext.Click -= btnNext_Click; btnNext.Click += btnNext_Click;
            btnDashboard.Click -= btnDashboard_Click; btnDashboard.Click += btnDashboard_Click;
            btnRecordPayment.Click -= btnRecordPayment_Click; btnRecordPayment.Click += btnRecordPayment_Click;

            // Sidebar: To verify, Receipts, Transactions, Daily collection, Log out
            // + pag isinara ang window (X), titigil ang buong program
            CashierNav.Wire(this, loggedInUser);
        }
        private void SetupGrid()
        {
            DataGridView g = Grid;
            g.AutoGenerateColumns = false;
            g.Columns.Clear();
            g.AllowUserToAddRows = false;
            g.AllowUserToDeleteRows = false;
            g.ReadOnly = true;
            g.RowHeadersVisible = false;
            g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            DataGridViewCellStyle money = new DataGridViewCellStyle { Format = "₱#,##0.00", Alignment = DataGridViewContentAlignment.MiddleRight };

            g.Columns.Add(new DataGridViewTextBoxColumn { Name = "StudentNo", HeaderText = "Student no.", DataPropertyName = "StudentNo" });
            g.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Name", DataPropertyName = "Name", FillWeight = 130 });
            g.Columns.Add(new DataGridViewTextBoxColumn { Name = "Grade", HeaderText = "Grade", DataPropertyName = "Grade", FillWeight = 45 });
            g.Columns.Add(new DataGridViewTextBoxColumn { Name = "Assessment", HeaderText = "Assessment", DataPropertyName = "Assessment", DefaultCellStyle = money });
            g.Columns.Add(new DataGridViewTextBoxColumn { Name = "Paid", HeaderText = "Paid", DataPropertyName = "Paid", DefaultCellStyle = money });
            g.Columns.Add(new DataGridViewTextBoxColumn { Name = "Balance", HeaderText = "Balance", DataPropertyName = "Balance", DefaultCellStyle = money });
            g.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", DataPropertyName = "Status", FillWeight = 80 });

            DataGridViewButtonColumn btnPay = new DataGridViewButtonColumn();
            btnPay.Name = "ActionRecord";
            btnPay.HeaderText = "Actions";
            btnPay.Text = "Record payment";
            btnPay.UseColumnTextForButtonValue = true;
            btnPay.FlatStyle = FlatStyle.Popup;
            btnPay.FillWeight = 95;
            g.Columns.Add(btnPay);

            // kulay ng Status
            Font bold = new Font(g.Font, FontStyle.Bold);
            g.CellFormatting += (s, e) =>
            {
                if (e.RowIndex >= 0 && g.Columns[e.ColumnIndex].Name == "Status" && e.Value != null)
                {
                    string v = e.Value.ToString();
                    e.CellStyle.ForeColor = v == "Fully paid" ? Color.SeaGreen : (v == "Unpaid" ? Color.Firebrick : Color.DarkOrange);
                    e.CellStyle.Font = bold;
                }
            };

            // "Record payment" button sa row
            g.CellContentClick += (s, e) =>
            {
                if (e.RowIndex < 0 || g.Columns[e.ColumnIndex].Name != "ActionRecord") return;
                DataGridViewRow row = g.Rows[e.RowIndex];
                if (Convert.ToDecimal(row.Cells["Balance"].Value) <= 0)
                {
                    MessageBox.Show("Bayad na bayad na ang estudyanteng ito.", "Record payment", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                CahierRecordPayment f = new CahierRecordPayment(loggedInUser, Convert.ToString(row.Cells["StudentNo"].Value));
                f.Show();
                this.Hide();
            };
        }
        private void CashierStudentManagemen_Load(object sender, EventArgs e)
        {
            LoadStudents();
         
        }

        private void LoadStudents()
        {
            try
            {
                int total;
                DataTable dt = CashierData.SearchStudents(txtSearchStudent.Text.Trim(), cmbFilterBalance.Text, currentPage, PageSize, out total);
                int pages = CashierData.Pages(total, PageSize);
                if (currentPage > pages)
                {
                    currentPage = pages;
                    dt = CashierData.SearchStudents(txtSearchStudent.Text.Trim(), cmbFilterBalance.Text, currentPage, PageSize, out total);
                }

                Grid.DataSource = dt;
                btnPagenumber.Text = currentPage.ToString();
                btnPrev.Enabled = currentPage > 1;
                btnNext.Enabled = currentPage < pages;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }
        private void btnRecordPayment_Click(object sender, EventArgs e)
        {
            CahierRecordPayment paymentForm = new CahierRecordPayment(loggedInUser);
            paymentForm.Show();
            this.Hide();
        }

        private void txtSearchStudent_TextChanged(object sender, EventArgs e)
        {
            currentPage = 1;
            LoadStudents();
        }


        private void btnDashboard_Click(object sender, EventArgs e)
        {
            CashierEnrollmentDashboard dash = new CashierEnrollmentDashboard(loggedInUser);
            dash.Show();
            this.Hide();
        }

        private void cmbFilterBalance_SelectedIndexChanged(object sender, EventArgs e)
        {
            currentPage = 1;
            LoadStudents();
        }

        private void btnStudent_Click(object sender, EventArgs e)
        {

        }

        private void btnPrev_Click(object sender, EventArgs e)
        {
            if (currentPage > 1) { currentPage--; LoadStudents(); }
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            currentPage++;
            LoadStudents();
        }

        private void panel3_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
