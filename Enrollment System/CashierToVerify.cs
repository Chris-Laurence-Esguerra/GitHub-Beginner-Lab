using System;
using System.Data;
using System.Windows.Forms;

namespace Enrollment_System
{
    public partial class CashierToVerify : Form
    {
        private string loggedInUser;
        private int currentPage = 1;
        private const int PageSize = 8;
        private bool loading = false;

        
        private Button btnPrevPage, btnNextPage, btnPageNo;

        public CashierToVerify(string username)
        {
            InitializeComponent();
            loggedInUser = username;

            btnPrevPage = FindButton("btnPrev", "btnPrevious");
            btnNextPage = FindButton("btnNext");
            btnPageNo = FindButton("btnPageNumber", "btnPagenumber", "btnPage");

            SetupGrid();

            // may -= para hindi magdoble kung na-wire na sa designer
            txtSearch.TextChanged -= txtSearch_TextChanged;
            txtSearch.TextChanged += txtSearch_TextChanged;
            btnVerifyAndIssue.Click -= btnVerifyAndIssue_Click;
            btnVerifyAndIssue.Click += btnVerifyAndIssue_Click;
            btnReject.Click -= btnReject_Click;
            btnReject.Click += btnReject_Click;

            if (btnPrevPage != null) btnPrevPage.Click += (s, e) => { if (currentPage > 1) { currentPage--; LoadData(); } };
            if (btnNextPage != null) btnNextPage.Click += (s, e) => { currentPage++; LoadData(); };

            // sidebar
            CashierNav.WireCore(this, loggedInUser);       // Dashboard, Student, Record payment
            CashierNav.Wire(this, loggedInUser);           // To verify, Receipts, Transactions, Daily collection, Log out

            Load -= CashierToVerify_Load;
            Load += CashierToVerify_Load;
        }
        private void CashierToVerify_Load(object sender, EventArgs e)
        {
            LoadData();
        }
        private void SetupGrid()
        {
            DataGridView g = dgvToVerify;
            g.AutoGenerateColumns = false;
            g.Columns.Clear();
            g.AllowUserToAddRows = false;           // tinatanggal ang "*" row
            g.AllowUserToDeleteRows = false;
            g.ReadOnly = true;
            g.MultiSelect = false;
            g.RowHeadersVisible = false;
            g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            g.Columns.Add(new DataGridViewTextBoxColumn { Name = "PaymentID", DataPropertyName = "PaymentID", Visible = false });
            g.Columns.Add(new DataGridViewTextBoxColumn { Name = "Student", HeaderText = "Student", DataPropertyName = "Student", FillWeight = 110 });
            g.Columns.Add(new DataGridViewTextBoxColumn { Name = "Payment", HeaderText = "Payment", DataPropertyName = "Payment", FillWeight = 120 });
            g.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Amount",
                HeaderText = "Amount",
                DataPropertyName = "Amount",
                FillWeight = 80,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "₱#,##0.00", Alignment = DataGridViewContentAlignment.MiddleRight }
            });

            g.SelectionChanged += (s, e) => { if (!loading) ShowDetails(); };
        }

        private void LoadData()
        {

            try
            {
                loading = true;
                int total;
                DataTable dt = CashierData.SearchPending(txtSearch.Text.Trim(), currentPage, PageSize, out total);
                int pages = CashierData.Pages(total, PageSize);
                if (currentPage > pages)
                {
                    currentPage = pages;
                    dt = CashierData.SearchPending(txtSearch.Text.Trim(), currentPage, PageSize, out total);
                }

                dgvToVerify.DataSource = dt;
                if (dgvToVerify.Rows.Count > 0) dgvToVerify.CurrentCell = dgvToVerify.Rows[0].Cells["Student"];
                loading = false;

                if (btnPageNo != null) btnPageNo.Text = currentPage.ToString();
                if (btnPrevPage != null) btnPrevPage.Enabled = currentPage > 1;
                if (btnNextPage != null) btnNextPage.Enabled = currentPage < pages;

                ShowDetails();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
            finally
            {
                loading = false;
            }
        }
        private void label21_Click(object sender, EventArgs e)
        {

        }

        private void btnToVerify_Click(object sender, EventArgs e)
        {

        }

        private void panel4_Paint(object sender, PaintEventArgs e)
        {

        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            currentPage = 1;
            LoadData();
        }

        private void dgvToVerify_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (!loading) ShowDetails();
        }

        // ---------- Payment details (kanang panel) ----------
        private int SelectedPaymentId()
        {
            if (dgvToVerify.CurrentRow == null) return 0;
            return Convert.ToInt32(dgvToVerify.CurrentRow.Cells["PaymentID"].Value);
        }

        private void ShowDetails()
        {
            int id = SelectedPaymentId();
            DataRow r = id > 0 ? CashierData.GetPaymentInfo(id) : null;

            if (r == null)
            {
                lblStudentName.Text = "-";
                lblMethod.Text = "-";
                lblReceiptNo.Text = "-";
                lblAmount.Text = CashierData.Peso(0);
                lblBalanceNow.Text = CashierData.Peso(0);
                btnVerifyAndIssue.Enabled = false;
                btnReject.Enabled = false;
                return;
            }

            lblStudentName.Text = Convert.ToString(r["StudentName"]);
            lblMethod.Text = Convert.ToString(r["Method"]);
            lblReceiptNo.Text = id.ToString("D6");                       // ito ang receipt no. na ilalabas
            lblAmount.Text = CashierData.Peso(Convert.ToDecimal(r["Amount"]));
            lblBalanceNow.Text = CashierData.Peso(r["Balance"] == DBNull.Value ? 0 : Convert.ToDecimal(r["Balance"]));
            btnVerifyAndIssue.Enabled = true;
            btnReject.Enabled = true;
        }

        private void btnVerifyAndIssue_Click(object sender, EventArgs e)
        {
            int id = SelectedPaymentId();
            if (id == 0) { MessageBox.Show("Pumili muna ng payment sa listahan.", "To verify"); return; }

            string name = lblStudentName.Text;
            if (MessageBox.Show("I-verify ang bayad ni " + name + " na " + lblAmount.Text + " at mag-issue ng resibo?",
                "Verify payment", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            string error;
            string receiptNo = CashierData.VerifyPayment(id, out error);
            if (receiptNo == null)
            {
                MessageBox.Show(error, "Hindi ma-verify", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show("Na-verify na ang bayad ni " + name + "!\n\nReceipt no.: " + receiptNo,
                "Receipt issued", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadData();
        }

        private void btnReject_Click(object sender, EventArgs e)
        {
            int id = SelectedPaymentId();
            if (id == 0) { MessageBox.Show("Pumili muna ng payment sa listahan.", "To verify"); return; }

            string reason = AskReason();
            if (reason == null) return;                                   // Cancel
            if (reason.Length == 0)
            {
                MessageBox.Show("Kailangan ng dahilan para i-reject ang bayad.", "Reject payment", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string error;
            if (!CashierData.RejectPayment(id, reason, out error))
            {
                MessageBox.Show(error, "Hindi ma-reject", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            MessageBox.Show("Na-reject ang bayad ni " + lblStudentName.Text + ".", "Rejected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadData();
        }
        private string AskReason()
        {
            using (Form f = new Form())
            {
                f.Text = "Reject payment";
                f.Width = 420;
                f.Height = 190;
                f.StartPosition = FormStartPosition.CenterParent;
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.MaximizeBox = false;
                f.MinimizeBox = false;

                Label l = new Label(); l.Text = "Dahilan ng pag-reject:"; l.Left = 15; l.Top = 15; l.Width = 370;
                TextBox t = new TextBox(); t.Left = 15; t.Top = 45; t.Width = 370;
                Button ok = new Button(); ok.Text = "OK"; ok.Left = 215; ok.Top = 85; ok.Width = 80; ok.DialogResult = DialogResult.OK;
                Button cancel = new Button(); cancel.Text = "Cancel"; cancel.Left = 305; cancel.Top = 85; cancel.Width = 80; cancel.DialogResult = DialogResult.Cancel;

                f.Controls.Add(l);
                f.Controls.Add(t);
                f.Controls.Add(ok);
                f.Controls.Add(cancel);
                f.AcceptButton = ok;
                f.CancelButton = cancel;

                return f.ShowDialog(this) == DialogResult.OK ? t.Text.Trim() : null;
            }
        }

        private Button FindButton(params string[] names)
        {
            foreach (string n in names)
            {
                Control[] c = Controls.Find(n, true);
                if (c.Length > 0 && c[0] is Button) return (Button)c[0];
            }
            return null;
        }

        private void CashierToVerify_Load_1(object sender, EventArgs e)
        {

        }

        private void label17_Click(object sender, EventArgs e)
        {

        }

        private void panel3_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
