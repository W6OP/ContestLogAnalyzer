﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using W6OP.ContestLogAnalyzer;

namespace ContestLogAnalyzer
{
    /// <summary>
    /// Select folder.
    /// Enumerate folders with extension of .log
    /// Load first file. 
    /// Look for header and footer.
    /// Validate header.
    /// Load QSOs. Actually I will have to compare all logs so probably just validate headers and footers on all logs first.
    /// Validate QSOs.
    /// Calculate score.
    /// </summary>
    public partial class MainForm : Form
    {
        private List<ContestLog> _ContestLogs;
        IEnumerable<System.IO.FileInfo> _LogFileList;
        LogProcessor _LogProcessor;
        LogAnalyzer _LogAnalyser;
        CWOpen _CWOpen;

        private string _LogFolder = null;


        /// <summary>
        /// this.BeginInvoke(new Action<string, MessageType>(this.DisplayMessageForm), fullFileName, messageType);
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_Load(object sender, EventArgs e)
        {
            _LogFolder = TextBoxLogFolder.Text;

            ComboBoxSelectContest.DataSource = Enum.GetValues(typeof(ContestName))
                .Cast<ContestName>()
                .Select(p => new { Key = (int)p, Value = p.ToString() })
                .ToList();

            ComboBoxSelectContest.DisplayMember = "Value";
            ComboBoxSelectContest.ValueMember = "Key";
        }


        #region Select Log folder

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonSelectFolder_Click(object sender, EventArgs e)
        {
            SelectWorkingFolder();
        }

        /// <summary>
        /// Launch the folder browser dialog to select the folder the logs reside in.
        /// </summary>
        private void SelectWorkingFolder()
        {
            LogFolderBrowserDialog.ShowDialog();

            if (!String.IsNullOrEmpty(LogFolderBrowserDialog.SelectedPath))
            {
                _LogFolder = LogFolderBrowserDialog.SelectedPath;
                TextBoxLogFolder.Text = _LogFolder;
            }
        }

        #endregion



        /// <summary>
        /// Handle the click event for the ButtonAnalyzeLog button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonLoadLogs_Click(object sender, EventArgs e)
        {
            LoadLogFiles();
        }


        /// <summary>
        /// Get a list and count of the files to be uploaded. Pass them off to another thread
        /// to preprocess the logs.
        /// </summary>
        private void LoadLogFiles()
        {
            Int32 fileCount = 0;
            ContestLog contestLog = null;

            try
            {
                UpdateListViewLoad("", true, contestLog);
                UpdateListViewAnalysis("", "", true);
                UpdateListViewScore("", "","", true);

                if (_LogProcessor == null)
                {
                    _LogProcessor = new LogProcessor(_LogFolder);
                    _LogProcessor.OnProgressUpdate += _LogProcessor_OnProgressUpdate;
                }

                if (!String.IsNullOrEmpty(_LogFolder))
                {
                    _ContestLogs = new List<ContestLog>();
                    fileCount = _LogProcessor.BuildFileList(out _LogFileList);

                    UpdateListViewLoad(fileCount.ToString() + " logs were loaded.", false, contestLog);
                    ButtonStartAnalysis.Enabled = true;

                    Cursor = Cursors.WaitCursor;
                    BackgroundWorkerLoadLogs.RunWorkerAsync(fileCount);
                }
                else
                {
                    MessageBox.Show("You must select a folder containing log files.", "Missing Folder Name", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Load or Preprocess Log Error", "Load and Preprocess Logs", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void _LogProcessor_OnProgressUpdate(string value, ContestLog contestLog)
        {
            //ContestLog contestLog = null;

            UpdateListViewLoad(value, false, contestLog);
        }

        #region Background Worker Load Logs

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundWorkerLoadLogs_DoWork(object sender, DoWorkEventArgs e)
        {
            foreach (FileInfo fileInfo in _LogFileList)
            {
                _LogProcessor.BuildContestLog(fileInfo, _ContestLogs);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundWorkerLoadLogs_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ContestLog contestLog = null;

            if (e.Error != null)
            {
                UpdateListViewLoad(e.Error.Message, false, contestLog);
            }
            else
            {
                UpdateListViewLoad("Logs completed loaded.", false, contestLog);
                Cursor = Cursors.Default;
            }
        }

        #endregion


        /// <summary>
        /// Open first log
        /// get first QSO
        /// get Contact call sign
        /// see if it is in any other logs
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonStartAnalysis_Click(object sender, EventArgs e)
        {
            UpdateListViewAnalysis("","", true);

            if (_LogAnalyser == null)
            {
                _LogAnalyser = new LogAnalyzer();
                _LogAnalyser.OnProgressUpdate += _LogAnalyser_OnProgressUpdate;
            }

            BackgroundWorkerAnalzeLogs.RunWorkerAsync();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundWorkerAnalyzeLogs_DoWork(object sender, DoWorkEventArgs e)
        {
            _LogAnalyser.PreProcessContestLogs(_ContestLogs);
        }

        /// <summary>
        /// Update the list view with the call sign last processed.
        /// </summary>
        /// <param name="value"></param>
        private void _LogAnalyser_OnProgressUpdate(string value, string count)
        {
            UpdateListViewAnalysis(value, count, false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundWorkerAnalyzeLogs_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                UpdateListViewAnalysis(e.Error.Message, "", false);
            }
            else
            {
                UpdateListViewAnalysis("Log analysis completed!", "", false);
                Cursor = Cursors.Default;
                ButtonScoreLogs.Enabled = true; // might be cross thread
            }
        }


        #region Score Logs

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonScoreLogs_Click(object sender, EventArgs e)
        {
            if (_CWOpen == null)
            {
                _CWOpen = new CWOpen();
                _CWOpen.OnProgressUpdate += _CWOpen_OnProgressUpdate;
            }

            UpdateListViewScore("", "", "", true);

            Cursor = Cursors.WaitCursor;

            // now score each log
            BackgroundWorkerScoreLogs.RunWorkerAsync();
        }

        private void _CWOpen_OnProgressUpdate(string logOwner, string claimed, string actual)
        {
            UpdateListViewScore(logOwner, claimed, actual, false);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundWorkerScoreLogs_DoWork(object sender, DoWorkEventArgs e)
        {
            _CWOpen.ScoreContestLogs(_ContestLogs);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundWorkerScoreLogs_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                UpdateListViewScore(e.Error.Message, "", "", false);
            }
            else
            {
                UpdateListViewScore("Log scoring complete.", "", "", false);
            }

            Cursor = Cursors.Default;
        }

        #endregion





        /// <summary>
        /// Look at the QSOs and see if they are valid - how do you know?
        /// need all the QSOs from two lists
        /// the qsos that match between the two logs
        /// </summary>
        /// <param name="log"></param>
        //private void AnalyzeQSOs(ContestLog log)
        //{
        //    List<QSO> qsoList;
        //    List<QSO> matchList = null;
        //    List<QSO> totalList = null;
        //    List<QSO> exceptionList = null;
        //    Hashtable callTable = new Hashtable();
        //    string logOwnerCall = log.LogHeader.OperatorCallSign;
        //    string operatorCall = null;
        //    string sentName = null;
        //    Int32 sent = 0;
        //    Int32 received = 0;
        //    Int32 band = 0;
        //    Int32 matchCount = 0;
        //    QSOStatus valid = QSOStatus.ValidQSO;


        //    foreach (ContestLog contestLog in log.MatchLogs)
        //    {
        //        //operatorCall = qso.OperatorCall;
        //        //sent = qso.SentSerialNumber;
        //        //received = qso.ReceivedSerialNumber;
        //        //band = qso.Band;
        //        //sentName = qso.OperatorName;

        //        // list of QSOs from the other guy where 
        //        qsoList = contestLog.QSOCollection.Where(a => a.ContactCall == logOwnerCall).ToList();


        //        //matchList = contestLog.QSOCollection.Where(a => a.ContactCall == operatorCall && a.SentSerialNumber == received && a.Band == band && a.ContactName == sentName).ToList<QSO>();

        //        return;
        //        //totalList = contestLog.QSOCollection.Where(a => a.ContactCall == logOwnerCall && a.SentSerialNumber == received && a.Band == band && a.ContactName == sentName).ToList<QSO>();
        //        // now query this guys log for matches
        //        foreach (QSO qso in qsoList)
        //        {
        //            operatorCall = qso.OperatorCall;
        //            sent = qso.SentSerialNumber;
        //            received = qso.ReceivedSerialNumber;
        //            band = qso.Band;
        //            sentName = qso.OperatorName; // check on this
        //            valid = QSOStatus.ValidQSO;

        //            //totalList = log.QSOCollection.Where(a => a.ContactCall == operatorCall).ToList<QSO>();
        //            // could there be more than one returned?
        //            matchList = log.QSOCollection.Where(a => a.ContactCall == operatorCall && a.SentSerialNumber == received && a.Band == band && a.ContactName == sentName).ToList<QSO>();
        //            foreach (QSO q in matchList)
        //            {
        //                q.Status = QSOStatus.ValidQSO;
        //                valid = QSOStatus.InvalidQSO;
        //                if (q.Status == QSOStatus.InvalidQSO)
        //                {
        //                    // later expand this with more information
        //                    q.RejectReason = "Dupe";
        //                }
        //            }

        //            //// see if there are any QSOs that need correction or looking at
        //            //if (totalList.Count != matchList.Count)
        //            //{
        //            //    exceptionList = totalList.Except(qsoList).ToList<QSO>();
        //            //}
        //        }
        //    }
        //}



        /// <summary>
        /// http://stackoverflow.com/questions/721395/linq-question-querying-nested-collections
        /// http://stackoverflow.com/questions/15996168/linq-query-to-filter-items-by-criteria-from-multiple-lists


        /*
         * http://stackoverflow.com/questions/14893924/for-vs-linq-performance-vs-future
            int matchIndex = array.Select((r, i) => new { value = r, index = i })
                         .Where(t => t.value == matchString)
                         .Select(s => s.index).First();
         */

        #region Update ListViews

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="clear"></param>
        private void UpdateListViewLoad(string message, bool clear, ContestLog contestLog)
        {
            if (InvokeRequired)
            {
                this.BeginInvoke(new Action<string, bool, ContestLog>(this.UpdateListViewLoad), message, clear, contestLog);
                return;
            }

            if (clear)
            {
                ListViewLoad.Items.Clear();
            }
            else
            {
                ListViewItem item = new ListViewItem(message);
                item.Tag = contestLog;
                ListViewLoad.Items.Insert(0, item);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="clear"></param>
        private void UpdateListViewAnalysis(string message, string count, bool clear)
        {
            if (InvokeRequired)
            {
                this.BeginInvoke(new Action<string, string, bool>(this.UpdateListViewAnalysis), message, count, clear);
                return;
            }

            if (clear)
            {
                ListViewAnalysis.Items.Clear();
            }
            else
            {
                ListViewItem item = new ListViewItem(message);
                item.SubItems.Add(count);
                ListViewAnalysis.Items.Insert(0, item);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logOwner"></param>
        /// <param name="clear"></param>
        private void UpdateListViewScore(string logOwner, string claimed, string actual, bool clear)
        {
            if (InvokeRequired)
            {
                this.BeginInvoke(new Action<string, string, string, bool>(this.UpdateListViewScore), logOwner, claimed, actual, clear);
                return;
            }

            if (clear)
            {
                ListViewScore.Items.Clear();
            }
            else
            {
                ListViewItem item = new ListViewItem(logOwner);
                item.SubItems.Add(claimed);
                item.SubItems.Add(actual);
                ListViewScore.Items.Insert(0, item);
            }
        }

        #endregion

        #region ListView Click Handling

        private void ListViewLoad_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListView listView = (ListView)sender;

            ListView.SelectedListViewItemCollection items = listView.SelectedItems;
            ListViewItem item = items[0];

            if (item.Tag != null)
            {
                ContestLog contestLog = (ContestLog)item.Tag;
                LogViewForm form = new LogViewForm(contestLog);
                form.Show();
            }
        }

        private void ListViewScore_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            // pop up form with all of the statistics on it
            // only allow one form to show at a time

        }

        private void ListViewAnalysis_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            // popup form with all of the log entries
            // mark ones to review in blue
            // allow changes to be made and saved
            // allow multiple forms so thay can be compared
        }


     
        #endregion

       



        //if (InvokeRequired)
        //   {
        //       this.BeginInvoke(new Action<string, DXAFileType>(this.MoveFileToBackupDirectory), uploadFile, fileType);
        //       return;
        //   }

    } // end class
}


/*
  tempLog = _ContestLogs.Where(q => q.QSOCollection.Any(a => a.ContactCall == operatorCall && a.ReceivedSerialNumber == sent && a.Band == band && a.ContactName == sentName && a.Status == QSOStatus.InvalidQSO)).ToList();
                    if (tempLog != null && tempLog.Count > 0)
                    {
                        contestLog.MatchLogs.AddRange(tempLog);
                    }

                    // some of these will be marked valid as we go along and need to be removed from this collection
                    tempLog = _ContestLogs.Where(q => q.QSOCollection.Any(a => a.ContactCall == operatorCall && (a.ReceivedSerialNumber != sent || a.Band == band || a.ContactName == sentName && a.Status == QSOStatus.InvalidQSO))).ToList();
                    if (tempLog != null && tempLog.Count > 0)
                    {
                        contestLog.ReviewLogs.AddRange(tempLog);
                    }

                    tempLog = _ContestLogs.Where(q => q.QSOCollection.All(a => a.ContactCall != operatorCall)).ToList();
                    if (tempLog != null && tempLog.Count > 0)
                    {
                        contestLog.OtherLogs.AddRange(tempLog);
                    }

 */