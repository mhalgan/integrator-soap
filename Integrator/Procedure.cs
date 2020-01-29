using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Integrator
{
    class Procedure
    {
        public string owner { get; set; }
        public string name { get; set; }
        public int seq { get; set; }
        public Dictionary<string, string> parameters = new Dictionary<string, string>();
        private string sql;

        public void GetParameters(XDocument resultXML)
        {
            using (DataSet1TableAdapters.INTEGRATOR_PROCED_PARAMTableAdapter procedureParamTableAdapter = new DataSet1TableAdapters.INTEGRATOR_PROCED_PARAMTableAdapter())
            {
                using (DataSet1.INTEGRATOR_PROCED_PARAMDataTable procedParamDataTable = new DataSet1.INTEGRATOR_PROCED_PARAMDataTable())
                {
                    try
                    {
                        procedureParamTableAdapter.FillByProcedure(procedParamDataTable, seq);

                        foreach (DataRow row in procedParamDataTable.Rows)
                        {
                            string name = row["NAME"].ToString();
                            string value = row["VALUE"].ToString();
                            if (value.StartsWith("@") && resultXML != null)

                            {
                                try
                                {
                                    string key = value.Substring(1); //Remove o @
                                    value = resultXML.Descendants(key).FirstOrDefault().Value;
                                }
                                catch (Exception ex)
                                {
                                    throw ex;
                                }
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

        public void Execute()
        {
            sql = owner + "." + name;
            using (OracleConnection connection = new OracleConnection(Properties.Settings.Default.ConnectionString))
            {
                using (OracleCommand command = new OracleCommand(sql, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    double tempFloat;
                    foreach (var parameter in parameters)
                    {
                        
                        if (double.TryParse(parameter.Value, out tempFloat))
                        {
                            command.Parameters.Add(parameter.Key, OracleDbType.Double).Value = tempFloat;
                        }
                        else
                        {
                            command.Parameters.Add(parameter.Key, OracleDbType.Varchar2).Value = parameter.Value;
                        }
                    }

                    try
                    {
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                    catch (OracleException ex)
                    {
                        throw ex;
                    }
                    
                }
            }
        }
    }
}
