using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
        private ContestLog _ContestLog;
        private LogHeader _LogHeader;
        private QSO _QSO;

        private string _LogFolder = null;
        //private List<string> _LogFileList;

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
            //string guidString = Guid.NewGuid().ToString();

            _LogFolder = TextBoxLogFolder.Text;
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
        /// 
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

        #region Start Log Analysis

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonAnalyzeLog_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(_LogFolder))
            {
                BuildFileList();
            }
            else
            {
                MessageBox.Show("You must selct a folder containg log files.", "Missing Folder Name", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        IEnumerable<System.IO.FileInfo> _LogFileList;

        /// <summary>
        /// 
        /// </summary>
        private void BuildFileList()
        {
            //string fileFullName = null;
            Int32 fileCount;

            // Take a snapshot of the file system. http://msdn.microsoft.com/en-us/library/bb546159.aspx
            DirectoryInfo dir = new DirectoryInfo(_LogFolder);

            // This method assumes that the application has discovery permissions for all folders under the specified path.
            IEnumerable<FileInfo> fileList = dir.GetFiles("*.log", System.IO.SearchOption.TopDirectoryOnly);

            //Create the query
            _LogFileList =
                from file in fileList
                where file.Extension.ToLower() == ".log"
                orderby file.CreationTime ascending
                select file;

            fileCount = fileList.Cast<object>().Count();

            BackgroundWorkerAnalyze.RunWorkerAsync(fileCount);
        }



        #endregion

        #region Analyze Header and Footer

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonValidateHeader_Click(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// See if there is a header and a footer - CHECK READING FILE FROM PREFIX LIST ON HAIDA
        /// Read the header
        /// 
        /// </summary>
        /// <param name="fileInfo"></param>
        private void AnalyzeLogs(FileInfo fileInfo)
        {
            string fullName = fileInfo.FullName;

            if (File.Exists(fullName))
            {
                _ContestLog = new ContestLog();
                _LogHeader = new LogHeader();
                _QSO = new QSO();

                List<string> lineList = File.ReadAllLines(fullName).Select(i => i.ToString()).ToList();

                BuildHeader(lineList);
                _ContestLog.LogHeader = _LogHeader;

                // Find DUPES in list
                //http://stackoverflow.com/questions/454601/how-to-count-duplicates-in-list-with-linq

                // this statement says to copy all QSO lines
                lineList = lineList.Where(x => x.IndexOf("QSO:", 0) != -1).ToList();
                CollectQSOs(lineList);

            }
        }

        /// <summary>
        /// LINQ SAMPLES
        /// http://code.msdn.microsoft.com/101-LINQ-Samples-3fb9811b
        /// 
        /// THERE ARE SOME ITEMS MISSING
        /// DATE, TIME
        /// TIME OFF
        /// </summary>
        /// <param name="lineList"></param>
        /// <param name="match"></param>
        private void BuildHeader(List<string> lineList)
        {
            // Merge the data sources using a named type. 
            // var could be used instead of an explicit type.
            IEnumerable<LogHeader> logHeader =
                from line in lineList
                select new LogHeader()
                {
                    Version = lineList.Where(l => l.StartsWith("START-OF-LOG:")).FirstOrDefault().Substring(13).Trim(),
                    Location = lineList.Where(l => l.StartsWith("LOCATION:")).FirstOrDefault().Substring(9).Trim(),
                    CallSign = lineList.Where(l => l.StartsWith("CALLSIGN:")).FirstOrDefault().Substring(9).Trim().ToUpper(),
                    Operator = Utility.GetValueFromDescription<CategoryOperator>(lineList.Where(l => l.StartsWith("CATEGORY-OPERATOR:")).FirstOrDefault().Substring(18).Trim().ToUpper()),
                    Assisted = Utility.GetValueFromDescription<CategoryAssisted>(lineList.Where(l => l.StartsWith("CATEGORY-ASSISTED:")).FirstOrDefault().Substring(18).Trim().ToUpper()),
                    Band = Utility.GetValueFromDescription<CategoryBand>(lineList.Where(l => l.StartsWith("CATEGORY-BAND:")).FirstOrDefault().Substring(14).Trim().ToUpper()),
                    Power = Utility.GetValueFromDescription<CategoryPower>(lineList.Where(l => l.StartsWith("CATEGORY-POWER:")).FirstOrDefault().Substring(15).Trim().ToUpper()),
                    Mode = Utility.GetValueFromDescription<CategoryMode>(lineList.Where(l => l.StartsWith("CATEGORY-MODE:")).FirstOrDefault().Substring(14).Trim().ToUpper()),
                    Station = Utility.GetValueFromDescription<CategoryStation>(lineList.Where(l => l.StartsWith("CATEGORY-STATION:")).FirstOrDefault().Substring(17).Trim().ToUpper()),
                    Transmitter = Utility.GetValueFromDescription<CategoryTransmitter>(lineList.Where(l => l.StartsWith("CATEGORY-TRANSMITTER:")).FirstOrDefault().Substring(21).Trim().ToUpper()),
                    ClaimedScore = Convert.ToInt32(lineList.Where(l => l.StartsWith("CLAIMED-SCORE:")).FirstOrDefault().Substring(14).Trim()),
                    Club = lineList.Where(l => l.StartsWith("CLUB:")).FirstOrDefault().Substring(5).Trim(),
                    Contest = Utility.GetValueFromDescription<ContestName>(lineList.Where(l => l.StartsWith("CONTEST:")).FirstOrDefault().Substring(9).Trim().ToUpper()),
                    CreatedBy = lineList.Where(l => l.StartsWith("CREATED-BY:")).FirstOrDefault().Substring(11).Trim(),
                    PrimaryName = lineList.Where(l => l.StartsWith("NAME:")).FirstOrDefault().Substring(5).Trim(),
                    // need to work on address
                    Operators = lineList.Where(l => l.StartsWith("OPERATORS:")).ToList(),
                    SoapBox = lineList.Where(l => l.StartsWith("SOAPBOX:")).FirstOrDefault().Substring(7).Trim()
                };

            _LogHeader = logHeader.FirstOrDefault();
        }

        private void CollectQSOs(List<string> lineList)
        {
            List<QSO> qsoList;

            //var list = lineList.GroupBy(x => x.Split(' ')[1])
            //   .ToList();

            IEnumerable<QSO> qso =
                 from line in lineList
                 select new QSO()
                 {
                     Frequency = lineList.GroupBy(x => x.Split(' ')[1]).ToString(),
                     Mode = lineList.GroupBy(x => x.Split(' ')[2]).ToString(),
                     QsoDate = lineList.GroupBy(x => x.Split(' ')[3]).ToString(),
                     QsoTime = lineList.GroupBy(x => x.Split(' ')[4]).ToString(),
                     OperatorCall = lineList.GroupBy(x => x.Split(' ')[5]).ToString(),
                     OperatorSerialNumber = lineList.GroupBy(x => x.Split(' ')[6]).ToString(),
                     OperatorName = lineList.GroupBy(x => x.Split(' ')[7]).ToString(),
                     ContactCall = lineList.GroupBy(x => x.Split(' ')[8]).ToString(),
                     ContactSerialNumber = lineList.GroupBy(x => x.Split(' ')[9]).ToString(),
                     ContactName = lineList.GroupBy(x => x.Split(' ')[10]).ToString()
                 };

            qsoList = qso.ToList();

            string a = "";
            //QSO: 14027 CW 2013-08-31 0005 W0BR 1 BOB W4BQF 10 TOM
        }



        #endregion

        #region Background Worker

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundWorkerAnalyze_DoWork(object sender, DoWorkEventArgs e)
        {
            foreach (FileInfo fileInfo in _LogFileList)
            {
                //fileFullName = fileInfo.FullName;
                AnalyzeLogs(fileInfo);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundWorkerAnalyze_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundWorkerAnalyze_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

        }

        #endregion



    } // end class
}
