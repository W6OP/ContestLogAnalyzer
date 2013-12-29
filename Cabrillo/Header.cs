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
    internal class Header
    {
        private string _Version;
        internal string Version
        {
            get { return _Version; }
            set { _Version = value; }
        }

        private string _Location;
        internal string Location
        {
            get { return _Location; }
            set { _Location = value; }
        }

        private string _CallSign;
        internal string CallSign
        {
            get { return _CallSign; }
            set { _CallSign = value; }
        }

        private CategoryOperator _Operator;
        internal CategoryOperator Operator
        {
            get { return _Operator; }
            set { _Operator = value; }
        }

        private CategoryAssisted _Assisted;
        internal CategoryAssisted Assisted
        {
            get { return _Assisted; }
            set { _Assisted = value; }
        }

        private CategoryBand _Band;
        internal CategoryBand Band
        {
            get { return _Band; }
            set { _Band = value; }
        }

        private CategoryMode _Mode;
        internal CategoryMode Mode
        {
            get { return _Mode; }
            set { _Mode = value; }
        }

       

        private CategoryPower _Power;
        internal CategoryPower Power
        {
            get { return _Power; }
            set { _Power = value; }
        }

        private CategoryOverlay _Overlay;
        internal CategoryOverlay Overlay
        {
            get { return _Overlay; }
            set { _Overlay = value; }
        }

        private Int32 _ClaimedScore;
        internal Int32 ClaimedScore
        {
            get { return _ClaimedScore; }
            set { _ClaimedScore = value; }
        }

        private string _Club;
        internal string Club
        {
            get { return _Club; }
            set { _Club = value; }
        }

        private ContestName _Contest;
        internal ContestName Contest
        {
            get { return _Contest; }
            set { _Contest = value; }
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