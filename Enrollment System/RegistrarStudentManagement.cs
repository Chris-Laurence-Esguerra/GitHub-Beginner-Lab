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
    public partial class RegistrarStudentManagement : Form
    {
        public string loggedInUser;
        private int currentPage = 1;
        private const int PageSize = 8;

        private DataGridView Grid { get { return dataGridViewRegistrarStudents; } }

        public RegistrarStudentManagement() : this(null) { }

        public RegistrarStudentManagement(string username)
        {
            InitializeComponent();
            loggedInUser = username;

            // filters
            cmbFilterGrade.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFilterGrade.Items.Clear();
            cmbFilterGrade.Items.AddRange(new object[] { "All grades", "Grade 7", "Grade 8", "Grade 9", "Grade 10" });
            cmbFilterGrade.SelectedIndex = 0;

            cmbFilterStatus.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFilterStatus.Items.Clear();
            cmbFilterStatus.Items.AddRange(new object[] { "All status", "Pending", "Enrolled", "Rejected" });
            cmbFilterStatus.SelectedIndex = 0;

            SetupGrid();

            // may -= para hindi magdoble kung na-wire na sa designer
            txtSearchStudent.TextChanged -= txtSearchStudent_TextChanged; txtSearchStudent.TextChanged += txtSearchStudent_TextChanged;
            cmbFilterGrade.SelectedIndexChanged -= cmbFilterGrade_SelectedIndexChanged; cmbFilterGrade.SelectedIndexChanged += cmbFilterGrade_SelectedIndexChanged;
            cmbFilterStatus.SelectedIndexChanged -= cmbFilterStatus_SelectedIndexChanged; cmbFilterStatus.SelectedIndexChanged += cmbFilterStatus_SelectedIndexChanged;
            btnPrevPage.Click -= btnPrevPage_Click; btnPrevPage.Click += btnPrevPage_Click;
            btnNextPage.Click -= btnNextPage_Click; btnNextPage.Click += btnNextPage_Click;
            btnAddStudent.Click -= btnAddStudent_Click; btnAddStudent.Click += btnAddStudent_Click;

            // Sidebar: Dashboard, Student, Enrollment, Assessment, Payment, Receipts, Report, Log out
            RegistrarNav.Wire(this, loggedInUser);

            Load -= RegistrarStudentManagement_Load;
            Load += RegistrarStudentManagement_Load;
        }

        private void RegistrarStudentManagement_Load(object sender, EventArgs e)
        {
            LoadStudents();
        }
        private void SetupGrid()
        {
            DataGridView g = Grid;
            g.AutoGenerateColumns = false;
            g.Columns.Clear();                      // tinatanggal ang columns na ginawa sa designer
            g.AllowUserToAddRows = false;           // tinatanggal ang "*" row
            g.AllowUserToDeleteRows = false;
            g.ReadOnly = true;
            g.MultiSelect = false;
            g.RowHeadersVisible = false;
            g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            g.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNo", HeaderText = "Student no.", DataPropertyName = "StudentNo", FillWeight = 80 });
            g.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Name", DataPropertyName = "Name", FillWeight = 130 });
            g.Columns.Add(new DataGridViewTextBoxColumn { Name = "colGrade", HeaderText = "Grade", DataPropertyName = "Grade", FillWeight = 45 });
            g.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSection", HeaderText = "Section", DataPropertyName = "Section", FillWeight = 70 });
            g.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "Status", DataPropertyName = "Status", FillWeight = 65 });
            g.Columns.Add(Btn("colApprove", "Actions", "Approve"));
            g.Columns.Add(Btn("colReject", "", "Reject"));
            g.Columns.Add(Btn("colDelete", "", "Delete"));

            // kulay ng Status
            Font bold = new Font(g.Font, FontStyle.Bold);
            g.CellFormatting += (s, e) =>
            {
                if (e.RowIndex >= 0 && g.Columns[e.ColumnIndex].Name == "colStatus" && e.Value != null)
                {
                    string v = e.Value.ToString();
                    e.CellStyle.ForeColor = v == "Enrolled" ? Color.SeaGreen : (v == "Rejected" ? Color.Firebrick : Color.DarkOrange);
                    e.CellStyle.Font = bold;
                }
            };

            g.CellContentClick += Grid_CellContentClick;
        }
        private DataGridViewButtonColumn Btn(string name, string header, string text)
        {
            DataGridViewButtonColumn b = new DataGridViewButtonColumn();
            b.Name = name;
            b.HeaderText = header;
            b.Text = text;
            b.UseColumnTextForButtonValue = true;
            b.FlatStyle = FlatStyle.Popup;
            b.FillWeight = 50;
            return b;
        }

        private void LoadStudents()
        {
            try
            {
                int grade = 0;
                if (cmbFilterGrade.SelectedIndex > 0) grade = cmbFilterGrade.SelectedIndex + 6;   // Grade 7 = index 1

                int total;
                DataTable dt = RegistrarData.SearchStudents(txtSearchStudent.Text.Trim(), grade, cmbFilterStatus.Text, currentPage, PageSize, out total);
                int pages = RegistrarData.Pages(total, PageSize);
                if (currentPage > pages)
                {
                    currentPage = pages;
                    dt = RegistrarData.SearchStudents(txtSearchStudent.Text.Trim(), grade, cmbFilterStatus.Text, currentPage, PageSize, out total);
                }

                Grid.DataSource = dt;
                btnPage1.Text = currentPage.ToString();
                btnPrevPage.Enabled = currentPage > 1;
                btnNextPage.Enabled = currentPage < pages;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Student Management");
            }
        }

        // ---------- Approve / Reject / Delete ----------
        private void Grid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            string col = Grid.Columns[e.ColumnIndex].Name;
            if (col != "colApprove" && col != "colReject" && col != "colDelete") return;

            DataGridViewRow row = Grid.Rows[e.RowIndex];
            string no = Convert.ToString(row.Cells["colNo"].Value);
            string name = Convert.ToString(row.Cells["colName"].Value);
            string status = Convert.ToString(row.Cells["colStatus"].Value);
            string error;

            // APPROVE
            if (col == "colApprove")
            {
                if (status == "Enrolled")
                {
                    MessageBox.Show(name + " ay Enrolled na.", "Approve", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                if (MessageBox.Show("I-approve ang enrollment ni " + name + "?", "Approve", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

                if (!RegistrarData.ApproveStudent(no, out error))
                {
                    MessageBox.Show(error, "Hindi ma-approve", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                LoadStudents();
            }

            // REJECT
            if (col == "colReject")
            {
                if (status != "Pending")
                {
                    MessageBox.Show("Pending lang ang puwedeng i-reject.", "Reject", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                string reason = RegistrarNav.AskText(this, "Reject enrollment", "Dahilan ng pag-reject:");
                if (reason == null) return;
                if (reason.Length == 0)
                {
                    MessageBox.Show("Kailangan ng dahilan para i-reject ang enrollment.", "Reject", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (!RegistrarData.RejectStudent(no, reason, out error))
                {
                    MessageBox.Show(error, "Hindi ma-reject", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                LoadStudents();
            }

            // DELETE
            if (col == "colDelete")
            {
                if (MessageBox.Show("I-delete si " + name + " (" + no + ")? Hindi na ito mababawi.", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

                if (!RegistrarData.DeleteStudent(no, out error))
                {
                    MessageBox.Show(error, "Hindi ma-delete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                LoadStudents();
            }
        }

        private void btnAddStudent_Click(object sender, EventArgs e)
        {
            using (NewEnrollmentForm f = new NewEnrollmentForm("Add student"))
            {
                if (f.ShowDialog(this) == DialogResult.OK)
                {
                    currentPage = 1;
                    LoadStudents();
                }
            }
        }

        private void txtSearchStudent_TextChanged(object sender, EventArgs e)
        {
            currentPage = 1;
            LoadStudents();
        }

        private void cmbFilterGrade_SelectedIndexChanged(object sender, EventArgs e)
        {
            currentPage = 1;
            LoadStudents();
        }

        private void cmbFilterStatus_SelectedIndexChanged(object sender, EventArgs e)
        {

            currentPage = 1;
            LoadStudents();
        }

        private void btnPrevPage_Click(object sender, EventArgs e)
        {
            if (currentPage > 1) { currentPage--; LoadStudents(); }
        }

        private void btnNextPage_Click(object sender, EventArgs e)
        {
            currentPage++;
            LoadStudents();
        }
    }
}
