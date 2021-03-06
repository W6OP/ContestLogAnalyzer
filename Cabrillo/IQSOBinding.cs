﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace W6OP.ContestLogAnalyzer
{
    /// <summary>
    ///  http://stackoverflow.com/questions/8054744/hide-columns-in-a-datagridview-with-a-list-as-datasource
    /// </summary>
    public interface IQSOBinding
    {
        QSOStatus Status { get; set; }
        string RejectReason { get; set; }
        int Band { get; set; }
        string Mode { get; set; }
        string QsoDate { get; set; }
        string QsoTime { get; set; }
        string OperatorCall { get; set; }
        string OperatorName { get; set; }
        int ReceivedSerialNumber { get; set; }
        int SentSerialNumber { get; set; }
        string ContactCall { get; set; }
        string ContactName { get; set; }

    } // end interface
}
