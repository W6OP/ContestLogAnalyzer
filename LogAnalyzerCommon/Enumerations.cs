
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace W6OP.ContestLogAnalyzer
{
        public enum RejectReason
    {
        [Description("The band does not match")]
        Band,
        [Description("The mode does not match")]
        Mode,
        [Description("This QSO is not in the other operators log or the call may be busted")]
        NoQSOMatch,
        [Description("FYI: call not in any other log - scored as unique")]
        NoQSO,
        [Description("Busted call")]
        BustedCallSign,
        [Description("The received serial number is incorrect")]
        SerialNumber,
        [Description("The operator name does not match")]
        OperatorName,
        [Description("The contact name does not match")]
        ContactName,
        [Description("The entity name does not match")]
        EntityName,
        [Description("There were duplicates of this QSO")]
        DuplicateQSO,
        [Description("The call sign is invalid")]
        InvalidCall,
        [Description("The time does not match within 5 minutes")]
        InvalidTime,
        [Description("The session is incorrect")]
        InvalidSession,
        [Description("The entity is incorrect")]
        InvalidEntity,
        [Description("The sent entity is incorrect")]
        InvalidSentEntity,
        [Description("This QSO will not be counted")]
        NotCounted,
        [Description("This X-QSO will not be counted")]
        Marked_XQSO,
        [Description("Missing Column")]
        MissingColumn,
        [Description("None")]
        None
    }

    public enum QSOStatus
    {
        ValidQSO,
        IncompleteQSO,
        InvalidQSO,
        ReviewQSO
    }

    public enum CategoryAssisted
    {
        [Description("ASSISTED")]
        Assisted,
        [Description("NON-ASSISTED")]
        NonAssisted,
        [Description("UNKNOWN")]
        Uknown
    }

    /// <summary>
    /// http://stackoverflow.com/questions/3916914/c-sharp-using-numbers-in-an-enum
    /// </summary>
    public enum QSOBand
    {
        [Description("ALL")]
        ALL,
        [Description("160M")]
        _160M,
        [Description("80M")]
        _80M,
        [Description("40M")]
        _40M,
        [Description("20M")]
        _20M,
        [Description("15M")]
        _15M,
        [Description("10M")]
        _10M,
        [Description("6M")]
        _6M,
        [Description("2M")]
        _2M,
        [Description("222M")]
        _222,
        [Description("432M")]
        _432,
        [Description("902M")]
        _902,
        [Description("1.2G")]
        _1_2G,
        [Description("2.3G")]
        _2_3G,
        [Description("3.4G")]
        _3_4G,
        [Description("5.7G")]
        _5_7G,
        [Description("10G")]
        _10G,
        [Description("24G")]
        _24G,
        [Description("47G")]
        _47G,
        [Description("75G")]
        _75G,
        [Description("119G")]
        _119G,
        [Description("142G")]
        _142G,
        [Description("241G")]
        _241G,
        [Description("Light")]
        Light
    }

    public enum FaultType
    {
        Band,
        Mode
    }


    public enum QSOMode
    {
        [Description("PH")]
        SSB,
        [Description("PH")]
        USB,
        [Description("CW")]
        CW,
        [Description("RY")]
        DIGI,
        [Description("RY")]
        DG,
        [Description("PH")]
        PH,
        [Description("RY")]
        RTTY,
        [Description("RY")]
        RY,
        [Description("RY")]
        FT8,
        [Description("MIXED")]
        MIXED
    }

    public enum CategoryTransmitterHQP
    {
        [Description("Single Transmitter")]
        ONE,
        [Description("Multi Transmitter")]
        TWO,
        [Description("Multi Transmitter")]
        LIMITED,
        [Description("Multi Transmitter")]
        UNLIMITED,
    }

    public enum CategoryTransmitter
    {
        [Description("Single Transmitter")]
        ONE,
        [Description("Multi Transmitter")]
        TWO,
        [Description("Multi Transmitter")]
        LIMITED,
        [Description("Multi Transmitter")]
        UNLIMITED,
        [Description("None")]
        SWL,
        [Description("Unknown")]
        UNKNOWN
    }

    //SINGLE-OP ALL LOW CW "SINGLE-OP ALL HIGH CW" "SINGLE-OP ALL\tLOW CW" SINGLE-OP LOW
    public enum CategoryOperator
    {
        [Description("SINGLE-OP")]
        SingleOp,
        [Description("SINGLE-OP QRP")]
        SingleOpQRP,
        [Description("SINGLE-OP LOW")]
        SingleOpLow,
        [Description("SINGLE-OP, HIGH")]
        SingleOpHigh, // (default if header has no such information)
        [Description("SINGLE-OP ALL LOW CW")]
        SingleOpLowCW,
        [Description("SINGLE-OP ALL HIGH CW")]
        SingleOpHighCW,
        [Description("MULTI-OP")]
        MultiOp,
        [Description("MULTI-SINGLE, QRP")]
        MultiOpSingleQRP,
        [Description("MULTI-SINGLE, LOW")]
        MultiOpSingleLow,
        [Description("MULTI-SINGLE, HIGH")]
        MultiOpSingleHigh,
        [Description("MULTI-MULTI, LOW")]
        MultiOpMultiLow,
        [Description("MULTI-MULTI, HIGH")]
        MultiOpMultiHigh,
        [Description("CHECKLOG")]
        CheckLog,
        [Description("Unable to determine operator category")]
        Uknown
    }

    public enum CategoryPower
    {
        [Description("HIGH")]
        HIGH,
        [Description("LOW")]
        LOW,
        [Description("QRP")]
        QRP
    }

    public enum CategoryStation
    {
        FIXED,
        MOBILE,
        PORTABLE,
        ROVER,
        EXPEDITION,
        HQ,
        SCHOOL,
        UNKNOWN
    }

    public enum CategoryTime
    {
        [Description("6-HOURS")]
        _6_HOURS,
        [Description("12-HOURS")]
        _12_HOURS,
        [Description("24-HOURS")]
        _24_HOURS
    }

    public enum CategoryOverlay
    {
        [Description("CLASSIC")]
        Classic,
        [Description("ROOKIE")]
        Rookie,
        [Description("TB-WIRES")]
        TBWires,
        [Description("NOVICE-TECH")]
        NoviceTech,
        [Description("OVER-50")]
        Over50,
    }

    public enum Session
    {
        [Description("No Session")]
        Session_0 = 0,
        [Description("Session 1")]
        Session_1 = 1,
        [Description("Session 2")]
        Session_2 = 2,
        [Description("Session 3")]
        Session_3 = 3
    }

    //[AttributeUsage(AllowMultiple = true)]
    public enum ContestName
    {
        [Description("Select")]
        Select,
        [CWOPENContestDescription("CWOPS-OPEN", "CW-OPEN", "CWOPEN", "CW OPEN")]
        CW_OPEN,
        [HQPContestDescription("HI-QSO-PARTY")]
        HQP
    }

    public enum HQPMults
    {
        // Standard HQP Mults
        [Description("HIL")]
        HIL,
        [Description("HON")]
        HON,
        [Description("KAL")]
        KAL,
        [Description("KAU")]
        KAU,
        [Description("KOH")]
        KOH,
        [Description("KON")]
        KON,
        [Description("LAN")]
        LNI,
        [Description("LHN")]
        LHN,
        [Description("MAU")]
        MAU,
        [Description("MOL")]
        MOL,
        [Description("NII")]
        NII,
        [Description("PRL")]
        PRL,
        [Description("VOL")]
        VOL,
        [Description("WHN")]
        WHN,
    }

    public enum ALTHQPMults
    {
        // Non Standard HQPMults
        [Description("HIL")]
        HILO,
        [Description("KOH")]
        KOHALA,
        [Description("KOH")]
        KOHA,
        [Description("MOL")]
        MOLOKAI,
        [Description("MOL")]
        MOLOL,
        [Description("VOL")]
        VOLCANO,
        [Description("VOL")]
        VOLCL,
        [Description("HON")]
        HNL,
        [Description("HON")]
        HONO,
        [Description("HON")]
        HONOLULU,
        [Description("KON")]
        KONA,
        [Description("KAL")]
        KALA,
        [Description("KAL")]
        KALAWAO,
        [Description("KAU")]
        KAUI,
        [Description("LNI")]
        LAN,
        [Description("LNI")]
        LANI,
        [Description("MAU")]
        MAUI,
        [Description("PRL")]
        PERL,
        [Description("PRL")]
        PEARL
    }

    //public enum CanadianProvince
    //{
    //    [Description("Newfoundland and Labrador")]
    //    NL,
    //    [Description("Prince Edward Island")]
    //    PE,
    //    [Description("Nova Scotia")]
    //    NS,
    //    [Description("New Brunswick")]
    //    NB,
    //    [Description("Quebec")]
    //    QC,
    //    [Description("Ontario")]
    //    ON,
    //    [Description("Manitoba")]
    //    MB,
    //    [Description("Saskatchewan")]
    //    SK,
    //    [Description("Alberta")]
    //    AB,
    //    [Description("British Columbia")]
    //    BC,
    //    [Description("Yukon")]
    //    YT,
    //    [Description("Northwest Territories")]
    //    NT,
    //    [Description("Nunavut")]
    //    NU
    //}


        //https://www.horizonmb.com/threads/137828-Storing-additional-data-in-enums
        /// <summary>
        /// Extension class to allow multiple desctription on enumerations.
        /// </summary>
        [AttributeUsage(System.AttributeTargets.All, AllowMultiple = true)]
    public class CWOPENContestDescription : Attribute
    {
        //Some people prefer to have a privataly declared variable to return and set, but this works fine for demonstration purposes
        public string ContestNameOne { get; set; }
        public string ContestNameTwo { get; set; }
        public string ContestNameThree { get; set; }
        public string ContestNameFour { get; set; }

        //The values certainly do not all have to be strings
        public CWOPENContestDescription(string nameOne, string nameTwo, string nameThree, string nameFour)
        {
            ContestNameOne = nameOne;
            ContestNameTwo = nameTwo;
            ContestNameThree = nameThree;
            ContestNameFour = nameFour;
        }
    } // end class

    [AttributeUsage(System.AttributeTargets.All, AllowMultiple = true)]
    public class HQPContestDescription : Attribute
    {
        //Some people prefer to have a privataly declared variable to return and set, but this works fine for demonstration purposes
        public string ContestNameOne { get; set; }

        //The values certainly do not all have to be strings
        public HQPContestDescription(string nameOne)
        {
            ContestNameOne = nameOne;
        }
    } // end class

    public class EnumHelper
    {
        /// <summary>
        /// Retrieve the description on the enum, e.g.
        /// [Description("Bright Pink")]
        /// BrightPink = 2,
        /// Then when you pass in the enum, it will retrieve the description
        /// </summary>
        /// <param name="en">The Enumeration</param>
        /// <returns>A string representing the friendly name</returns>
        public static string GetDescription(Enum en)
        {
            Type type = en.GetType();

            MemberInfo[] memInfo = type.GetMember(en.ToString());

            if (memInfo != null && memInfo.Length > 0)
            {
                object[] attrs = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);

                if (attrs != null && attrs.Length > 0)
                {
                    return ((DescriptionAttribute)attrs[0]).Description;
                }
            }

            return en.ToString();
        }

    } // end class

}
