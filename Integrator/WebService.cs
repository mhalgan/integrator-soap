using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Web;
using System.Xml;
using System.Xml.Linq;

namespace Integrator
{
    public class WebService
    {
        public string url { get; set; }
        public string webservNamespace { get; set; }
        public string method { get; set; }
        public int seq { get; set; }
        public DataRow logRow = null;
        public Dictionary<string, string> parameters = new Dictionary<string, string>();
        public XDocument resultXML;
        public string resultString;

        public WebService()
        {

        }

        public WebService(int seq, string url, string webservNamespace, string method, DataRow logRow)
        {
            this.seq = seq;
            this.url = url;
            this.method = method;
            this.webservNamespace = webservNamespace;
            this.logRow = logRow;
        }

        public void GetParameters()
        {
            using (DataSet1TableAdapters.INTEGRATOR_WEBSERV_PARAMTableAdapter webServParamTableAdapter = new DataSet1TableAdapters.INTEGRATOR_WEBSERV_PARAMTableAdapter())
            {
                using (DataSet1.INTEGRATOR_WEBSERV_PARAMDataTable webServParamDataTable = new DataSet1.INTEGRATOR_WEBSERV_PARAMDataTable())
                {
                    try
                    {
                        webServParamTableAdapter.FillByWebservice(webServParamDataTable, seq);

                        foreach (DataRow row in webServParamDataTable.Rows)
                        {
                            string name = row["NAME"].ToString();
                            string value = row["VALUE"].ToString();
                            string stringTemp = row["CDATA"].ToString();
                            bool cdata = (stringTemp == "Y") ? true : false;

                            if (value.StartsWith("@") && logRow != null)
                            {
                                try
                                {
                                    string key = value.Substring(1); //Remove o @
                                    value = logRow[key].ToString();
                                }
                                catch (Exception ex)
                                {
                                    Logger.AddToFile(ex.ToString());
                                }
                            }
                            if (cdata)
                            {
                                value = "<![CDATA[" + value + "]]>";
                            }
                            parameters.Add(name, value);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
            }
        }

        public void Invoke()
        {
            XmlDocument SOAPReqBody = new XmlDocument();
            try
            {
                string soapEnv = MountEnvelope();

                SOAPReqBody.LoadXml(soapEnv);

                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Headers.Add("SOAPAction", "");
                req.ContentType = "text/xml;charset=\"utf-8\"";
                req.Method = "POST";

                using (Stream stream = req.GetRequestStream())
                {
                    SOAPReqBody.Save(stream);
                }

                using (WebResponse res = req.GetResponse())
                {
                    using (StreamReader reader = new StreamReader(res.GetResponseStream()))
                    {
                        string result = reader.ReadToEnd();
                        result = HttpUtility.HtmlDecode(result);
                        resultXML = XDocument.Parse(result);

                        resultString = result;
                        Console.WriteLine(resultString);
                    }
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
            
        }

        private string MountEnvelope()
        {
            string soapEnv = @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ser="""+ webservNamespace + @"""><soapenv:Header/>";
            soapEnv += @"<soapenv:Body><ser:" + method + @">";
            foreach (var parameter in parameters)
            {
                soapEnv += @"<" + parameter.Key + @">";
                soapEnv += parameter.Value;
                soapEnv += @"</" + parameter.Key + @">";
            }
            soapEnv += @" </ser:" + method + @"></soapenv:Body></soapenv:Envelope>";
            return soapEnv;
        }
       
    }
}