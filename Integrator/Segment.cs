using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integrator
{
    class Segment
    {
        public List<Field> fields;
        public List<Segment> segments;
        public int seq { get; set; }
        public string tag { get; set; }
        public string dbTable { get; set; }
        public string dbWhere { get; set; }
        public string dateFilter { get; set; }
        public string fileHeader { get; set; }
        public bool main { get; set; }

        public string sql { get; set; }
        public DataTable data;

        public Segment()
        {
            fields = new List<Field>();
            segments = new List<Segment>();
            data = new DataTable();
        }

        public void GenerateSQL()
        {
            if (!string.IsNullOrEmpty(dbTable))
            {
                if (fields.Count > 0)
                {
                    string tempSql = "select ";
                    for (int i = 0; i < fields.Count; i++)
                    {
                        if (fields[i].size != null || !string.IsNullOrEmpty(fields[i].mask))
                        {
                            if (!string.IsNullOrEmpty(fields[i].mask))
                            {
                                tempSql += "to_char(" + fields[i].dbColumn + ", '" + fields[i].mask + "') " + fields[i].dbColumn;
                            }
                            if (fields[i].size != null)
                            {
                                tempSql += "substr(" + fields[i].dbColumn + ", 1, " + fields[i].size + ") " + fields[i].dbColumn;
                            }
                        }
                        else
                        {
                            tempSql += fields[i].dbColumn;
                        }
                        if (i < fields.Count - 1)
                        {
                            tempSql += ", ";
                        }
                    }
                    tempSql += " from " + dbTable;
                    tempSql += " where 1 = 1 ";

                    if (!string.IsNullOrEmpty(dbWhere))
                    {
                        tempSql += dbWhere;
                    }
   
                    if (!string.IsNullOrEmpty(dateFilter))
                    {
                        tempSql += " and " + dateFilter + " between :INIT_DATE and :END_DATE";
                    }
     
                    sql = tempSql;
                }
            }
        }

        public void GetData(DateTime? initDate, DateTime endDate)
        {
            if (!string.IsNullOrEmpty(sql))
            {
                if (sql.Contains(":INIT_DATE"))
                {
                    string initDateParam = initDate.Value.ToString("dd/MM/yyyy HH:mm:ss");
                    sql = sql.Replace(":INIT_DATE", "to_date('" + initDateParam + "', 'dd/mm/yyyy hh24:mi:ss')");
                }
                if (sql.Contains(":END_DATE"))
                {
                    string endDateParam = endDate.ToString("dd/MM/yyyy HH:mm:ss");
                    sql = sql.Replace(":END_DATE", "to_date('" + endDate + "', 'dd/mm/yyyy hh24:mi:ss')");
                }

                using (OracleConnection connection = new OracleConnection(Properties.Settings.Default.ConnectionString))
                {
                    using (OracleCommand command = new OracleCommand(sql, connection))
                    {
                        command.CommandType = CommandType.Text;
                        using (OracleDataAdapter dataAdapter = new OracleDataAdapter(command))
                        {
                            try
                            {
                                dataAdapter.Fill(data);
                            }
                            catch (Exception ex)
                            {
                                Logger.AddToFile(ex.ToString());
                            }
                        }
                    }
                }
            }
        }

        public List<Field> GetFields()
        {
            using (DataSet1TableAdapters.INTEGRATOR_FIELDTableAdapter fieldTableAdapter = new DataSet1TableAdapters.INTEGRATOR_FIELDTableAdapter())
            {
                using (DataSet1.INTEGRATOR_FIELDDataTable fieldDataTable = new DataSet1.INTEGRATOR_FIELDDataTable())
                {
                    try
                    {
                        fieldTableAdapter.Fill(fieldDataTable, seq);

                        foreach (var fieldRow in fieldDataTable)
                        {
                            Field field = new Field();
                            int intTemp;
                            string stringTemp;

                            if (int.TryParse(fieldRow["SEQ"].ToString(), out intTemp))
                            {
                                field.seq = intTemp;
                                field.tag = fieldRow["TAG"].ToString();
                                field.dbColumn = fieldRow["DB_COLUMN"].ToString();
                                if (int.TryParse(fieldRow["FIELD_SIZE"].ToString(), out intTemp))
                                {
                                    field.size = intTemp;
                                }

                                if (int.TryParse(fieldRow["FIELD_ORDER"].ToString(), out intTemp))
                                {
                                    field.order = intTemp;
                                }

                                stringTemp = fieldRow["KEY_FIELD"].ToString();
                                field.key = (stringTemp == "Y") ? true : false;

                                stringTemp = fieldRow["HIDE"].ToString();
                                field.hide = (stringTemp == "Y") ? true : false;

                                field.mask = fieldRow["MASK"].ToString();

                                fields.Add(field);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.AddToFile(ex.ToString());
                    }
                }
            }

            return fields;
        }
    }
}
