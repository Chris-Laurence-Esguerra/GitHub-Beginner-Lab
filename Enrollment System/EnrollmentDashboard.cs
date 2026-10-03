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
    public partial class CashierEnrollmentDashboard : Form
    {
        public string loggedInUser;
        public CashierEnrollmentDashboard()
        {
            InitializeComponent();
        }
        public CashierEnrollmentDashboard(string username)
        {
            InitializeComponent();
             loggedInUser = username;
        }   

        private void EnrollmentDashboard_Load(object sender, EventArgs e)
        {

        }
    }
}
