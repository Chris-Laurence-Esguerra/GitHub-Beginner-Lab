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
    public partial class EnrollmentAdmin : Form
    {
        public EnrollmentAdmin()
        {
            InitializeComponent();
        }

        private void EnrollmentAdmin_Load(object sender, EventArgs e)
        {
            dataGridView1.Columns.Clear();

            DataGridViewCheckBoxColumn chkCol = new DataGridViewCheckBoxColumn();
            chkCol.Name = "Select";
            chkCol.HeaderText = "";
            chkCol.Width = 40;
            dataGridView1.Columns.Add(chkCol);

            dataGridView1.Columns.Add("Code", "Code");
            dataGridView1.Columns.Add("Subject", "Subject");
            dataGridView1.Columns.Add("Schedule", "Schedule");

            dataGridView1.Rows.Add(true, "ENG8", "English 8", "Mon-Fri 7:30");
            dataGridView1.Rows.Add(true, "MATH8", "Mathematics 8", "Mon-Fri 8:30");
            dataGridView1.Rows.Add(true, "SCI8", "Science 8", "Mon-Fri 9:30");
            dataGridView1.Rows.Add(true, "FIL8", "Filipino 8", "Mon-Fri 10:30");

            dataGridView1.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);

            dataGridView1.RowTemplate.Height = 28;
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                row.Height = 28;
            }

            dataGridView1.Columns[0].Width = 40;
            dataGridView1.Columns[1].Width = 90;
            dataGridView1.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridView1.Columns[3].Width = 140;

            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.ScrollBars = ScrollBars.None;
        }
    }
}
