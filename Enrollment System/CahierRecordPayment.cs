using System;
using System.Data;
using System.Windows.Forms;

namespace Enrollment_System
{
    public partial class CahierRecordPayment : Form
    {
        private string loggedInUser;
        private string selectedStudentNo = null;
        private string selectedStudentName = null;
        private decimal balance = 0;

        public CahierRecordPayment(string username) : this(username, null) { }
        public CahierRecordPayment(string username, string studentNo)
        {
            InitializeComponent();
            loggedInUser = username;

            cmbPaymentMethod.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPaymentMethod.Items.Clear();
            cmbPaymentMethod.Items.AddRange(new object[] { "Cash", "Card" });
            cmbPaymentMethod.SelectedIndex = 0;

            txtChange.ReadOnly = true;
            txtDate.ReadOnly = true;
                             // laging petsa ngayon
            txtDate.Text = DateTime.Today.ToString("MMM d, yyyy");

            // events (may -= para hindi magdoble kung na-wire na sa designer)
            txtSearch.KeyDown -= txtSearch_KeyDown; txtSearch.KeyDown += txtSearch_KeyDown;
            txtSearch.Leave -= txtSearch_Leave; txtSearch.Leave += txtSearch_Leave;
            txtAmountReceived.TextChanged -= Recalculate; txtAmountReceived.TextChanged += Recalculate;
            txtCashTendered.TextChanged -= Recalculate; txtCashTendered.TextChanged += Recalculate;
            cmbPaymentMethod.SelectedIndexChanged -= Recalculate; cmbPaymentMethod.SelectedIndexChanged += Recalculate;
            btnClear.Click -= btnClear_Click; btnClear.Click += btnClear_Click;
            btnStudent.Click -= btnStudent_Click; btnStudent.Click += btnStudent_Click;

            CashierNav.Wire(this, loggedInUser);

            ClearForm();
            if (!string.IsNullOrEmpty(studentNo)) LookupStudent(studentNo, true);
        }
        private void CahierRecordPayment_Load(object sender, EventArgs e)
        {
            
        }

        private void btnSaveAndIssue_Click(object sender, EventArgs e)
        {

            if (selectedStudentNo == null)
            {
                MessageBox.Show("Pumili muna ng estudyante (i-type ang ID o pangalan, tapos Enter).", "Record payment", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal amountReceived;
            if (!CashierData.TryMoney(txtAmountReceived.Text, out amountReceived) || amountReceived <= 0)
            {
                MessageBox.Show("Mangyaring maglagay ng tamang halaga (amount).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (amountReceived > balance)
            {
                MessageBox.Show("Hindi puwedeng lampas sa natitirang balanse na " + CashierData.Peso(balance) + ".", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal tendered = 0;
            if (cmbPaymentMethod.Text == "Cash")
            {
                if (!CashierData.TryMoney(txtCashTendered.Text, out tendered) || tendered < amountReceived)
                {
                    MessageBox.Show("Ang cash tendered ay dapat hindi bababa sa amount received.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            try
            {
                string error;
                string receiptNo = CashierData.SavePayment(selectedStudentNo, selectedStudentName, cmbPaymentMethod.Text, amountReceived, out error);
                if (receiptNo == null)
                {
                    MessageBox.Show(error, "Hindi na-save", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string msg = "Matagumpay na na-save ang bayad at na-update ang balanse!\n\n" +
                             "Receipt no.: " + receiptNo + "\n" +
                             "Estudyante: " + selectedStudentName + "\n" +
                             "Method: " + cmbPaymentMethod.Text + "\n" +
                             "Amount: " + CashierData.Peso(amountReceived) + "\n" +
                             (cmbPaymentMethod.Text == "Cash" ? "Sukli: " + CashierData.Peso(tendered - amountReceived) + "\n" : "") +
                             "Bagong balanse: " + CashierData.Peso(balance - amountReceived);
                MessageBox.Show(msg, "Receipt issued", MessageBoxButtons.OK, MessageBoxIcon.Information);

                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Database Error: " + ex.Message);
            }
        }
        private void btnDashboard_Click(object sender, EventArgs e)
        {
            CashierEnrollmentDashboard dash = new CashierEnrollmentDashboard(loggedInUser);
            dash.Show();
            this.Hide();
        }
        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                LookupStudent(txtSearch.Text, true);
            }
        }

        private void txtSearch_Leave(object sender, EventArgs e)
        {
            string key = txtSearch.Text.Split('·')[0].Trim();
            if (key.Length > 0 && key != selectedStudentNo) LookupStudent(key, false);
        }

        private void LookupStudent(string text, bool showMessage)
        {
            string key = (text ?? "").Split('·')[0].Trim();     // gumagana rin sa "2026-0143 · Maria Santos"
            if (key.Length == 0) return;

            try
            {
                DataRow r = CashierData.FindStudent(key);
                if (r == null)
                {
                    if (showMessage) MessageBox.Show("Hindi natagpuan ang estudyante. Suriin ang pangalan o ID.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                selectedStudentNo = Convert.ToString(r["StudentNo"]);
                selectedStudentName = Convert.ToString(r["Name"]);
                balance = Convert.ToDecimal(r["Balance"]);

                txtSearch.Text = selectedStudentNo + " · " + selectedStudentName;
                lblTotalAssessment.Text = CashierData.Peso(Convert.ToDecimal(r["Assessment"]));
                lblTotalPaid.Text = CashierData.Peso(Convert.ToDecimal(r["Paid"]));
                lblRemainingBalance.Text = CashierData.Peso(balance);

                if (balance <= 0)
                    MessageBox.Show("Bayad na bayad na ang estudyanteng ito.", "Record payment", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Database Error: " + ex.Message);
            }
        }

        // ---------- change ----------
        private void Recalculate(object sender, EventArgs e)
        {
            bool cash = cmbPaymentMethod.Text == "Cash";
            txtCashTendered.Enabled = cash;

            if (!cash) { txtChange.Text = CashierData.Peso(0); return; }

            decimal amount, tendered;
            if (CashierData.TryMoney(txtAmountReceived.Text, out amount) &&
                CashierData.TryMoney(txtCashTendered.Text, out tendered) && tendered >= amount)
                txtChange.Text = CashierData.Peso(tendered - amount);
            else
                txtChange.Text = "";
        }
        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
        
        }

        private void txtCashTendered_TextChanged(object sender, EventArgs e)
        {
            decimal amountReceived = 0;
            decimal cashTendered = 0;

            decimal.TryParse(txtAmountReceived.Text, out amountReceived);
            decimal.TryParse(txtCashTendered.Text, out cashTendered);

            if (cashTendered >= amountReceived)
            {
                decimal change = cashTendered - amountReceived;
                txtChange.Text = change.ToString("N2");
            }
            else
            {
                txtChange.Text = "0.00";
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            ClearForm();
        }

        private void ClearForm()
        {
            selectedStudentNo = null;
            selectedStudentName = null;
            balance = 0;
            txtSearch.Clear();
            txtAmountReceived.Clear();
            txtCashTendered.Clear();
            txtChange.Clear();
            lblTotalAssessment.Text = CashierData.Peso(0);
            lblTotalPaid.Text = CashierData.Peso(0);
            lblRemainingBalance.Text = CashierData.Peso(0);
            cmbPaymentMethod.SelectedIndex = 0;
            txtCashTendered.Enabled = true;
            txtSearch.Focus();
        }

        private void btnStudent_Click(object sender, EventArgs e)
        {
            CashierStudentManagemen studentForm = new CashierStudentManagemen(loggedInUser);
            studentForm.Show();
            this.Hide();
        }

        private void btnToVerify_Click(object sender, EventArgs e)
        {
            CashierToVerify studentForm = new CashierToVerify(loggedInUser);
            studentForm.Show();
            this.Hide();
        }
    }
}
    
