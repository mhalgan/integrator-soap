using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Integrator
{
    class Sender
    {
        public static void Start(object obj)
        {
            while (true)
            {
                using (DataSet1TableAdapters.INTEGRATOR_INTEGRATIONTableAdapter integrationTableAdapter = new DataSet1TableAdapters.INTEGRATOR_INTEGRATIONTableAdapter())
                {
                    using (DataSet1TableAdapters.INTEGRATOR_LOGTableAdapter logTableAdapter = new DataSet1TableAdapters.INTEGRATOR_LOGTableAdapter())
                    {
                        using (DataSet1.INTEGRATOR_LOGDataTable logDataTable = new DataSet1.INTEGRATOR_LOGDataTable())
                        {
                            try
                            {
                                int seq;
                                int intTemp;
                                int seqIntegration;
                                int tries = 0;
                                int maxTries = 0;
                                string log;
                                string resultString = string.Empty;

                                logTableAdapter.UpdatePending();
                                logTableAdapter.FillByPending(logDataTable);
                                foreach (DataRow row in logDataTable.Rows)
                                {
                                    if (int.TryParse(row["SEQ"].ToString(), out intTemp))
                                    {
                                        seq = intTemp;
                                        if (int.TryParse(row["SEQ_INTEGRATION"].ToString(), out intTemp))
                                        {
                                            seqIntegration = intTemp;
                                            Integration integration = new Integration(seqIntegration);
                                            integration.GetIntegrationParameters();

                                            if (integration.paused)
                                            {
                                                continue;
                                            }

                                            maxTries = integration.maxTries;

                                            if (int.TryParse(row["TRIES"].ToString(), out intTemp))
                                            {
                                                tries = intTemp;
                                            }
                                            log = row["LOG"].ToString();

                                            if (tries >= maxTries)
                                            {
                                                log = IncrementLog(log , "O número máximo de tentativas foi excedido (" + tries + "/" + maxTries + ").");
                                                Logger.UpdateLog(seq, string.Empty, log, "X", true);
                                            }
                                            else
                                            {
                                                using (DataSet1TableAdapters.INTEGRATOR_WEBSERVICETableAdapter webserviceTableAdapter = new DataSet1TableAdapters.INTEGRATOR_WEBSERVICETableAdapter())
                                                {
                                                    using (DataSet1.INTEGRATOR_WEBSERVICEDataTable webserviceDataTable = new DataSet1.INTEGRATOR_WEBSERVICEDataTable())
                                                    {
                                                        try
                                                        {
                                                            webserviceTableAdapter.FillByIntegration(webserviceDataTable, seqIntegration);
                                                            foreach (DataRow webserviceRow in webserviceDataTable.Rows)
                                                            {
                                                                if (int.TryParse(webserviceRow["SEQ"].ToString(), out intTemp))
                                                                {
                                                                    WebService webservice = new WebService();
                                                                    webservice.seq = intTemp;
                                                                    webservice.url = webserviceRow["URL"].ToString();
                                                                    webservice.webservNamespace = webserviceRow["NAMESPACE"].ToString();
                                                                    webservice.method = webserviceRow["METHOD"].ToString();
                                                                    webservice.logRow = row;
                                                                    webservice.GetParameters();
                                                                    bool parseError = false;
                                                                    string error = string.Empty;

                                                                    webservice.Invoke();
                                                                    resultString = webservice.resultString;

                                                                    using (DataSet1TableAdapters.INTEGRATOR_PROCEDURETableAdapter procedureTableAdapter = new DataSet1TableAdapters.INTEGRATOR_PROCEDURETableAdapter())
                                                                    {
                                                                        using (DataSet1.INTEGRATOR_PROCEDUREDataTable procedureDataTable = new DataSet1.INTEGRATOR_PROCEDUREDataTable())
                                                                        {

                                                                            procedureTableAdapter.FillByWebservice(procedureDataTable, webservice.seq);

                                                                            foreach (DataRow procedureRow in procedureDataTable.Rows)
                                                                            {
                                                                                if (int.TryParse(procedureRow["SEQ"].ToString(), out intTemp))
                                                                                {
                                                                                    Procedure procedure = new Procedure();
                                                                                    procedure.seq = intTemp;
                                                                                    procedure.owner = procedureRow["OWNER"].ToString();
                                                                                    procedure.name = procedureRow["NAME"].ToString();
                                                                                    try
                                                                                    {
                                                                                        procedure.GetParameters(webservice.resultXML);
                                                                                        
                                                                                    }
                                                                                    catch (Exception)
                                                                                    {
                                                                                        parseError = true;
                                                                                        try
                                                                                        {
                                                                                            error = webservice.resultXML.Descendants().Where(x => x.Name.ToString().ToLower().Contains("erro")).FirstOrDefault().Value;
                                                                                        }
                                                                                        catch (Exception)
                                                                                        {
                                                                                            //Bypass error
                                                                                        }
                                                                                    }
                                                                                    if (!parseError)
                                                                                    {
                                                                                        procedure.Execute();
                                                                                    }
                                                                                }
                                                                            }
                                                                        }
                                                                    }

                                                                    if (parseError)
                                                                    {
                                                                        log = IncrementLog(log, "Erro ao interpretar retorno: " + error);
                                                                        Logger.UpdateLog(seq, resultString, log, "D", false);
                                                                    }
                                                                    else
                                                                    {
                                                                        log = IncrementLog(log, "SUCESSO!");
                                                                        Logger.UpdateLog(seq, resultString, log, "S", false);
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            log = IncrementLog(log, ex.ToString());
                                                            Logger.UpdateLog(seq, resultString, log, "F", false);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.AddToFile(ex.ToString());
                            }

                            Thread.Sleep(15 * 1000); //Aguarda antes de tentar novamente.
                        }
                    }
                }
            }
        }

        private static string IncrementLog(string log, string newText)
        {
            string timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture);
            string newLine = (string.IsNullOrEmpty(log)) ? string.Empty : Environment.NewLine;
            log += newLine + timestamp + " -> " + newText;
            return log;
        }
    }
}
