using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Xml.Linq;

namespace NetworkLookup
{
    public class QRZ
    {
        private string _QRZSessionKey;
        private bool _IsLoggedOn = false;

        /// <summary>
        ///  // Holds unique call signs and entity so we don't do dupe queries to QRZ.com
        /// </summary>
        private Hashtable _CallSignSet;
        public Hashtable CallSignSet { get => _CallSignSet; set => _CallSignSet = value; }
        public QRZ()
        {
            CallSignSet = new Hashtable();
        }

        /// <summary>
        ///  // Look up entity in Hashtable or from QRZ.com
        /// </summary>
        /// <param name="call"></param>
        /// <returns></returns>
        public string GetQRZInfo(string call)
        {
            string[] info = new string[2] { "0", "0" };
            string entity = null;

            if (_CallSignSet.Contains(call))
            {
                entity = (String)_CallSignSet[call];
            }
            else
            {
                info = QRZLookup(call, info, 1);

                if (info[0] != null && info[0] != "0")
                {
                    entity = info[0].ToUpper();
                    _CallSignSet.Add(call, entity);
                }
            }

            return entity;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="call"></param>
        /// <param name="prefixInfo"></param>
        /// <returns></returns>
        private string[] QRZLookup(string call, string[] info, int retryCount)
        {
            WebResponse response;
            XDocument xdoc;
            XNamespace xname;

            string requestUri = null;  // string.Format("http://xmldata.qrz.com/xml/current/?s={0};callsign={1}", _QRZSessionKey, call);
            WebRequest request;

            try
            {
                retryCount += 1;

                if (_QRZSessionKey == null)
                {
                    _IsLoggedOn = QRZLogon();
                }

                if (_IsLoggedOn)
                {
                    requestUri = string.Format("http://xmldata.qrz.com/xml/current/?s={0};callsign={1}", _QRZSessionKey, call);
                    request = WebRequest.Create(requestUri);

                    response = request.GetResponse();
                    xdoc = XDocument.Load(response.GetResponseStream());
                    xname = "http://xmldata.qrz.com";

                    var error = xdoc.Descendants(xname + "Session").Select(x => x.Element(xname + "Error")).FirstOrDefault();

                    if (error == null)
                    {
                        var key = xdoc.Descendants(xname + "Session").Select(x => x.Element(xname + "Key").Value).FirstOrDefault();

                        if (key != null)
                        {
                            // have to wait for my call parser, these are read only
                            //prefixInfo.Latitude = xdoc.Descendants(xname + "Callsign").Select(x => x.Element(xname + "lat")).FirstOrDefault();
                            //prefixInfo.Longitude = xdoc.Descendants(xname + "Callsign").Select(x => x.Element(xname + "lon")).FirstOrDefault();
                            info[0] = (string)xdoc.Descendants(xname + "Callsign").Select(x => x.Element(xname + "state")).FirstOrDefault();
                            info[1] = (string)xdoc.Descendants(xname + "Callsign").Select(x => x.Element(xname + "county")).FirstOrDefault();

                        }
                    }
                }
                else
                {
                    _QRZSessionKey = null;
                }
            }
            catch (Exception)
            {
                if (retryCount < 3)
                {
                    info = QRZLookup(call, info, retryCount);
                }
                else
                {
                    throw;
                }   
            }

            return info;
        }

        /// <summary>
        /// Logon to QRZ.com - TODO: Add credentials to Config screen
        /// Save the session key.
        /// </summary>
        /// <returns></returns>
        private bool QRZLogon()
        {
            string userId = "w6op";
            string password = "Car0less";
            bool isLoggedOn = false;

            var requestUri = string.Format("http://xmldata.qrz.com/xml/current/?username={0};password={1};{2}={3}", userId, password, "LogAnalyser", "2.0");
            var request = WebRequest.Create(requestUri);
            var response = request.GetResponse();
            XDocument xdoc = XDocument.Load(response.GetResponseStream());

            XNamespace xname = "http://xmldata.qrz.com";

            var error = xdoc.Descendants(xname + "Session").Select(x => x.Element(xname + "Error")).FirstOrDefault();

            if (error == null)
            {
                var key = xdoc.Descendants(xname + "Session").Select(x => x.Element(xname + "Key").Value).FirstOrDefault();
                _QRZSessionKey = key.ToString();
                return true;
            }
            else
            {
                //if (OnErrorDetected != null)
                //{
                //    OnErrorDetected(error.Value);
                //}
            }

            return isLoggedOn;
        }
    } // end class
}
