using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ContestLogAnalyzer
{
    /// <summary>
    /// Select folder.
    /// Enumerate folders with extension of .log
    /// Load first file. 
    /// Look for header and footer.
    /// Validate header.
    /// Load QSOs. Actually I will have to compare all logs so probably just validate headers and 
    /// footers on all logs first.
    /// Validate QSOs.
    /// Calculate score.
    /// </summary>
    public partial class MainForm : Form
    {

        private string _LogFolder = null;

        public MainForm()
        {
            InitializeComponent();
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

        /// <summary>
        /// Get a list of all of the log files.
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
            IEnumerable<System.IO.FileInfo> fileQuery =
                from file in fileList
                where file.Extension.ToLower() == ".log"
                orderby file.CreationTime ascending
                select file;

            fileCount = fileList.Cast<object>().Count();

            foreach (FileInfo fileInfo in fileQuery)
            {
                //fileFullName = fileInfo.FullName;
                AnalyzeHeaders(fileInfo);
            }
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
        private void AnalyzeHeaders(FileInfo fileInfo)
        {
            
        }

        #endregion

       


    } // end class
}
