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
    public partial class LogViewForm : Form
    {
       private ContestLog _ContestLog;

        public LogViewForm(ContestLog contestLog)
        {
            _ContestLog = contestLog;

            InitializeComponent();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LogViewForm_Load(object sender, EventArgs e)
        {
            LoadContestLog();
        }

        /// <summary>
        ///
        /// </summary>
        private void LoadContestLog()
        {
            DisplayHeader();

            DisplayQSOs();
        }

        private void DisplayQSOs()
        {
            DataGridViewQSO.AutoGenerateColumns = false;
            DataGridViewQSO.DataSource = _ContestLog.QSOCollection;
        }

        private void DisplayHeader()
        {
            LabelOperatorCall.Text = _ContestLog.LogHeader.OperatorCallSign;
            TextBoxOperator.Text = _ContestLog.LogHeader.NameSent;
            TextBoxAssisted.Text = _ContestLog.LogHeader.Assisted.ToString();
            TextBoxStation.Text = _ContestLog.LogHeader.Station.ToString();
            TextBoxBand.Text = _ContestLog.LogHeader.Band.ToString();
            TextBoxPower.Text =  _ContestLog.LogHeader.Power.ToString();
            TextBoxTransmitter.Text = _ContestLog.LogHeader.Transmitter.ToString();
        }


    } // end class
}
