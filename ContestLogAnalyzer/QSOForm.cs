using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace W6OP.ContestLogAnalyzer
{
    public partial class QSOForm : Form
    {
        private List<ContestLog> logList;

        public QSOForm()
        {
            InitializeComponent();
        }

        public QSOForm(List<ContestLog> logList)
        {
            InitializeComponent();

            // TODO: Complete member initialization
            this.logList = logList;
            DataGridViewQSOs.DataSource = logList[0].QSOCollection;
        }

        private void DataGridViewQSOs_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    } // end class
}
