
using System;
using System.ComponentModel;
using System.Reflection;

namespace W6OP.ContestLogAnalyzer
{
    public enum RejectReason
    {
        [Description("The band does not match")]
        Band,
        [Description("The mode does not match")]
        Mode,
        [Description("This QSO is not in the other operators log")]
        NoQSOMatch,
        [Description("FYI: call not in any other log - scored as unique")]
        NoQSO,
        [Description("Busted call")]
        BustedCallSign,
        [Description("The received serial number is incorrect")]
        SerialNumber,
        [Description("The operator name does not match")]
        OperatorName,
        [Description("The location name does not match")]
        EntityName,
        //[Description("The sent serial number does not match")]
        //Sent,
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
        [Description("None")]
        None
    }

    public enum QSOStatus
    {
        ValidQSO,
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
    public enum CategoryBand
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
    /*
     ((DescriptionAttribute)Attribute.GetCustomAttribute(
        typeof(myEnum).GetFields(BindingFlags.Public | BindingFlags.Static)
        .Single(x => (myEnum)x.GetValue(null) == enumValue),    
        typeof(DescriptionAttribute))).Description
     */

    
    public enum FaultType
    {
        Band,
        Mode
    }


    public enum CategoryMode
    {
        SSB,
        CW,
        DIGI,
        PH,
        RTTY,
        RY,
        MIXED
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
        [Description("SINGLE-OP ALL LOW CW")]
        SingleOpLowCW,
        [Description("SINGLE-OP ALL HIGH CW")]
        SingleOpHighCW,
        [Description("MULTI-OP")]
        MultiOp,
        [Description("CHECKLOG")]
        CheckLog,
        [Description("Unable to determine operator category")]
        Uknown
    }

    public enum CategoryPower
    {
        HIGH,
        LOW,
        QRP,
        UNKNOWN
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

    public enum CategoryTransmitter
    {
        ONE,
        TWO,
        LIMITED,
        UNLIMITED,
        SWL,
        UNKNOWN
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
        [ContestDescription("CWOPS-OPEN", "CW-OPEN", "CWOPEN", "CW OPEN")]
        CW_OPEN,
        [Description("HI-QSO-PARTY")]
        HQP,
        //[Description("AP-SPRINT")]
        //AP_SPRINT,
        //[Description("ARRL-10")]
        //ARRL_10,
        //[Description("ARRL-160")]
        //ARRL_160,
        //[Description("ARRL-DX-CW")]
        //ARRL_DX_CW,
        //[Description("ARRL-DX-SSB")]
        //ARRL_DX_SSB,
        //[Description("ARRL-SS-CW")]
        //ARRL_SS_CW,
        //[Description("ARRL-SS-SSB")]
        //ARRL_SS_SSB,
        //[Description("ARRL-UHF-AUG")]
        //ARRL_UHF_AUG,
        //[Description("ARRL-VHF-JAN")]
        //ARRL_VHF_JAN,
        //[Description("ARRL-VHF-JUN")]
        //ARRL_VHF_JUN,
        //[Description("ARRL-VHF-SEP")]
        //ARRL_VHF_SEP,
        //[Description("ARRL-RTTY")]
        //ARRL_RTTY,
        //[Description("BARTG-RTTY")]
        //BARTG_RTTY,
        //[Description("CQ-160-CW")]
        //CQ_160_CW,
        //[Description("CQ-160-SSB")]
        //CQ_160_SSB,
        //[Description("CQ-WPX-CW")]
        //CQ_WPX_CW,
        //[Description("CQ-WPX-RTTY")]
        //CQ_WPX_RTTY,
        //[Description("CQ-WPX-SSB")]
        //CQ_WPX_SSB,
        //[Description("CQ-VHF")]
        //CQ_VHF,
        //[Description("CQ-WW-CW")]
        //CQ_WW_CW,
        //[Description("CQ-WW-RTTY")]
        //CQ_WW_RTTY,
        //[Description("CQ-WW-SSB")]
        //CQ_WW_SSB,
        //[Description("DARC-WAEDC-CW")]
        //DARC_WAEDC_CW,
        //[Description("DARC-WAEDC-RTTY")]
        //DARC_WAEDC_RTTY,
        //[Description("DARC-WAEDC-SSB")]
        //DARC_WAEDC_SSB,
        //[Description("FCG-FQP")]
        //FCG_FQP,
        //[Description("IARU-HF")]
        //IARU_HF,
        //[Description("JIDX-CW")]
        //JIDX_CW,
        //[Description("JIDX-SSB")]
        //JIDX_SSB,
        //[Description("NA-SPRINT-CW")]
        //NA_SPRINT_CW,
        //[Description("NA-SPRINT-SSB")]
        //NA_SPRINT_SSB,
        //[Description("NCCC-CQP")]
        //NCCC_CQP,
        //[Description("NEQP")]
        //NEQP,
        //[Description("OCEANIA-DX-CW")]
        //OCEANIA_DX_CW,
        //[Description("OCEANIA-DX-SSB")]
        //OCEANIA_DX_SSB,
        //[Description("RDXC")]
        //RDXC,
        //[Description("RSGB-IOTA")]
        //RSGB_IOTA,
        //[Description("SAC-CW")]
        //SAC_CW,
        //[Description("SAC-SSB")]
        //SAC_SSB,
        //[Description("STEW-PERRY")]
        //STEW_PERRY,
        //[Description("TARA-RTTY")]
        //TARA_RTTY,
    }

    public enum HQPMults
    {
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
        LAN,
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
        WHN//,
        //Undefined
    }

    public class Enumerations
    {
    } // end class

    //https://www.horizonmb.com/threads/137828-Storing-additional-data-in-enums
    /// <summary>
    /// Extension class to allow multiple desctription on enumerations.
    /// </summary>
    [AttributeUsage(System.AttributeTargets.All, AllowMultiple = true)]
    public class ContestDescription :Attribute
    {
        //Some people prefer to have a privataly declared variable to return and set, but this works fine for demonstration purposes
        public string ContestNameOne { get; set; }
        public string ContestNameTwo { get; set; }
        public string ContestNameThree { get; set; }
        public string ContestNameFour { get; set; }

        //The values certainly do not all have to be strings
        public ContestDescription(string nameOne, string nameTwo, string nameThree, string nameFour)
        {
            ContestNameOne = nameOne;
            ContestNameTwo = nameTwo;
            ContestNameThree = nameThree;
            ContestNameFour = nameFour;
        }

    } // end class

    //public static string Description(this Enum value)
    //{
    //    // variables  
    //    var enumType = value.GetType();
    //    var field = enumType.GetField(value.ToString());
    //    var attributes = field.GetCustomAttributes(typeof(DescriptionAttribute), false);

    //    // return  
    //    return attributes.Length == 0 ? value.ToString() : ((DescriptionAttribute)attributes[0]).Description;
    //}


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
