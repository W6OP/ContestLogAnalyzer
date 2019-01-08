using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace W6OP.ContestLogAnalyzer
{
    /// <summary>
    /// Represents the header of a Cabrillo log file.
    /// </summary>
    [Serializable()]
    public class LogHeader
    {
        /// <summary>
        /// Indicates if the log has passed validation and can be analysed.
        /// </summary>
        private bool _HeaderIsValid;
        public bool HeaderIsValid
        {
            get { return _HeaderIsValid; }
            set { _HeaderIsValid = value; }
        }

        private string _LogFileName;
        public string LogFileName
        {
            get { return _LogFileName; }
            set { _LogFileName = value; }
        }

        private string _Version;
        public string Version
        {
            get { return _Version; }
            set { _Version = value; }
        }

        /// <summary>
        /// The operaor or log owner call sign stripped of a prefix or suffix.
        /// </summary>
        public string OperatorCallSign { get; set; }

        /// <summary>
        /// The operator call sign prefix if it exists (HQP).
        /// </summary>
        public string OperatorPrefix { get; set; }

        /// <summary>
        /// The operator call sign suffix if it exists (HQP).
        /// </summary>
        public string OperatorSuffix { get; set; }

        private CategoryAssisted _Assisted;
        public CategoryAssisted Assisted
        {
            get { return _Assisted; }
            set { _Assisted = value; }
        }

        private CategoryBand _Band;
        public CategoryBand Band
        {
            get { return _Band; }
            set { _Band = value; }
        }

        private CategoryMode _Mode;
        public CategoryMode Mode
        {
            get { return _Mode; }
            set { _Mode = value; }
        }

        private CategoryOperator _OperatorCategory;
        public CategoryOperator OperatorCategory
        {
            get { return _OperatorCategory; }
            set { _OperatorCategory = value; }
        }

        private CategoryPower _Power;
        public CategoryPower Power
        {
            get { return _Power; }
            set { _Power = value; }
        }

        private CategoryStation _Station;
        public CategoryStation Station
        {
            get { return _Station; }
            set { _Station = value; }
        }

        //private CategoryTime _TimePeriod;
        //public CategoryTime TimePeriod
        //{
        //    get { return _TimePeriod; }
        //    set { _TimePeriod = value; }
        //}

        private CategoryTransmitter _Transmitter;
        public CategoryTransmitter Transmitter
        {
            get { return _Transmitter; }
            set { _Transmitter = value; }
        }


        //private CategoryOverlay _Overlay;
        //public CategoryOverlay Overlay
        //{
        //    get { return _Overlay; }
        //    set { _Overlay = value; }
        //}

        private Int32 _ClaimedScore;
        public Int32 ClaimedScore
        {
            get { return _ClaimedScore; }
            set { _ClaimedScore = value; }
        }

        private string _Club;
        public string Club
        {
            get { return _Club; }
            set { _Club = value; }
        }

        private ContestName _Contest;
        public ContestName Contest
        {
            get { return _Contest; }
            set { _Contest = value; }
        }

        private string _CreatedBy;
        public string CreatedBy
        {
            get { return _CreatedBy; }
            set { _CreatedBy = value; }
        }

        //private string _EmailAddress;
        //public string EmailAddress
        //{
        //    get { return _EmailAddress; }
        //    set { _EmailAddress = value; }
        //}

        private string _Location;
        public string Location
        {
            get { return _Location; }
            set { _Location = value; }
        }

        private string _PrimaryName;
        public string PrimaryName
        {
            get { return _PrimaryName; }
            set { _PrimaryName = value; }
        }

        private string _NameSent;
        public string NameSent
        {
            get { return _NameSent; }
            set { _NameSent = value; }
        }

        //private Address _PostalAddress;
        //public Address PostalAddress
        //{
        //    get { return _PostalAddress; }
        //    set { _PostalAddress = value; }
        //}

        private List<string> _Operators;
        public List<string> Operators
        {
            get { return _Operators; }
            set { _Operators = value; }
        }

        //private DateTime _OffTime;
        //public DateTime OffTime
        //{
        //    get { return _OffTime; }
        //    set { _OffTime = value; }
        //}

        private string _SoapBox;
        public string SoapBox
        {
            get { return _SoapBox; }
            set { _SoapBox = value; }
        }



    } // end class
}
/*
START-OF-LOG: 3.0
LOCATION: TN
CALLSIGN: K1GU
CATEGORY-OPERATOR: SINGLE-OP
CATEGORY-ASSISTED: ASSISTED
CATEGORY-BAND: ALL
CATEGORY-POWER: HIGH
CATEGORY-MODE: CW
CATEGORY-STATION: FIXED
CATEGORY-TRANSMITTER: ONE
CLAIMED-SCORE: 48411
CLUB: Tennessee Contest Group
CONTEST: CW Open
CREATED-BY: WriteLog V10.74J
NAME: Ned Swartz
ADDRESS: 205 W Vinegar Valley Rd
ADDRESS: Friendsville, TN  37737-2435
ADDRESS: USA
OPERATORS: K1GU
SOAPBOX: 
*/
