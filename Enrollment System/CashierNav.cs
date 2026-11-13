using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Enrollment_System
{
    internal class CashierNav
    {
        public static bool Leaving = false;

        // Sidebar: To verify, Receipts, Transactions, Daily collection, Log out + exit kapag isinara ang window
        public static void Wire(Form f, string user)
        {
            Hook(f, "btnToVerify", () => { if (!(f is CashierToVerify)) Open(f, new CashierToVerify(user)); });
            Hook(f, "btnReceipts", () => Soon("Receipts"));
            Hook(f, "btnTransactions", () => Soon("Transactions"));
            Hook(f, "btnDailyCollection", () => Soon("Daily collection"));
            Hook(f, "btnLogout", () => Logout());

            f.FormClosed += (s, e) => { if (!Leaving) Application.Exit(); };
        }

        // Dashboard / Student / Record payment buttons (para sa form na wala pang ganitong click methods: CashierToVerify)
        public static void WireCore(Form f, string user)
        {
            Hook(f, "btnDashboard", () => Open(f, new CashierEnrollmentDashboard(user)));
            Hook(f, "btnStudent", () => Open(f, new CashierStudentManagemen(user)));
            Hook(f, "btnRecordPayment", () => Open(f, new CahierRecordPayment(user)));
        }

        static void Hook(Form f, string name, Action action)
        {
            Control[] c = f.Controls.Find(name, true);
            if (c.Length > 0) c[0].Click += (s, e) => action();
        }

        public static void Open(Form current, Form next)
        {
            next.Show();
            current.Hide();
        }

        public static void Soon(string page)
        {
            MessageBox.Show("Ang " + page + " ay ginagawa pa.", "Coming soon", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static void Logout()
        {
            if (MessageBox.Show("Mag-log out?", "Log out", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            Form login = null;
            foreach (Form f in Application.OpenForms)
                if (f is LogInForm) { login = f; break; }

            Leaving = true;                                           // para hindi mag-exit ang program
            foreach (Form f in Application.OpenForms.Cast<Form>().ToList())
                if (!(f is LogInForm)) f.Close();
            Leaving = false;

            if (login == null) login = new LogInForm();
            login.Show();
        }
    }
}

