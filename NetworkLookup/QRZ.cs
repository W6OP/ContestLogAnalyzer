using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NetworkLookup
{
    public class QRZ
    {
        private string _QRZSessionKey;
        private bool _IsLoggedOn = false;

        public QRZ()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="call"></param>
        /// <param name="prefixInfo"></param>
        /// <returns></returns>
        public string[] QRZLookup(string call, string[] info)
        {
            WebResponse response;
            XDocument xdoc;
            XNamespace xname;

            //bool isLoggedOn = false;

            string requestUri = null;  // string.Format("http://xmldata.qrz.com/xml/current/?s={0};callsign={1}", _QRZSessionKey, call);
            WebRequest request;

            try
            {
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
            catch (Exception ex)
            {
                throw;
                //_JLTrace.Write(String.Format("QRZLookup failed. - {0}", ex.Message));
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

            var requestUri = string.Format("http://xmldata.qrz.com/xml/current/?username={0};password={1};{2}={3}", userId, password, "DXA", "2.0");
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
