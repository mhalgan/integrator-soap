using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Concurrent;
using System.Data;
using System.Globalization;
using System.IO;

namespace Integrator
{
    class Logger
    {
        //Instância estática da fila de mensagens
        private static ConcurrentQueue<string> queue = new ConcurrentQueue<string>();

        //Classe estática que permite adicionar itens à fila a partir de qualquer parte do programa
        public static void AddToFile(string item)
        {
            try
            {
                item = " -> " + item;
                queue.Enqueue(item);
            }
            catch (Exception ex)
            {
                Logger.AddToFile("Logger", ex.ToString());
            }
        }

        public static void AddToFile(string caller, string item)
        {
            try
            {
                item = caller + " -> " + item;
                queue.Enqueue(item);
            }
            catch (Exception ex)
            {
                Logger.AddToFile("Logger", ex.ToString());
            }
        }

        public static void AddToDb(long seqIntegration, string key, string integratedFile, DateTime initDate, DateTime endDate, string logStatus, string log)
        {

#if (DEBUG)
            string timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture);
            Console.WriteLine(timestamp + " ID:" + key + " " + integratedFile);
#endif
            string sql = @"INSERT INTO TASY.INTEGRATOR_LOG(SEQ, LOG_DATE, SEQ_INTEGRATION, LOG_KEY, INTEGRATED_FILE, INIT_DATE, END_DATE, LOG_STATUS, LOG, TRIES) VALUES(TASY.INTEGRATOR_LOG_SEQ.NEXTVAL, SYSDATE, :SEQ_INTEGRATION, :LOG_KEY, :INTEGRATED_FILE, :INIT_DATE, :END_DATE, :LOG_STATUS, :LOG, 0)";
            using (OracleConnection connection = new OracleConnection(Properties.Settings.Default.ConnectionString))
            {
                using (OracleCommand command = new OracleCommand(sql, connection))
                {
                    command.CommandType = CommandType.Text;

                    command.Parameters.Add("SEQ_INTEGRATION", OracleDbType.Int64).Value = seqIntegration;
                    command.Parameters.Add("LOG_KEY", OracleDbType.Varchar2).Value = key;
                    command.Parameters.Add("INTEGRATED_FILE", OracleDbType.Clob).Value = integratedFile;
                    command.Parameters.Add("INIT_DATE", OracleDbType.Date).Value = initDate;
                    command.Parameters.Add("END_DATE", OracleDbType.Date).Value = endDate;
                    command.Parameters.Add("LOG_STATUS", OracleDbType.Varchar2).Value = logStatus;
                    command.Parameters.Add("LOG", OracleDbType.Clob).Value = log;

                    try
                    {
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Logger.AddToFile("Logger", ex.ToString());
                    }
                }
            }
        }

        public static void UpdateLog(long seqIntegration, string returnFile, string log, string logStatus, bool exhausted)
        {
            string sql;

            if (exhausted)
            {
                sql = @"UPDATE TASY.INTEGRATOR_LOG SET LOG_STATUS = :LOG_STATUS, LOG = :LOG WHERE SEQ = :SEQ";
                logStatus = "X";
            }
            else
            {
                sql = @"UPDATE TASY.INTEGRATOR_LOG SET RETURN_FILE = :RETURN_FILE, LOG_STATUS = :LOG_STATUS, LOG = :LOG, TRIES = TRIES +1, LAST_TRY_DATE = SYSDATE WHERE SEQ = :SEQ";
            }
            using (OracleConnection connection = new OracleConnection(Properties.Settings.Default.ConnectionString))
            {
                using (OracleCommand command = new OracleCommand(sql, connection))
                {
                    command.CommandType = CommandType.Text;

                    if (!exhausted)
                    {
                        command.Parameters.Add("RETURN_FILE", OracleDbType.Clob).Value = returnFile;
                    }

                    command.Parameters.Add("LOG_STATUS", OracleDbType.Varchar2).Value = logStatus;
                    command.Parameters.Add("LOG", OracleDbType.Clob).Value = log;
                    command.Parameters.Add("SEQ", OracleDbType.Int64).Value = seqIntegration;

                    try
                    {
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                    catch (OracleException ex)
                    {
                        Logger.AddToFile("Logger", ex.ToString());
                    }
                }
            }
        }

        //Método que verifica constantemente a fila de logs pendentes e os salva em arquivo de texto
        public void Start(object obj)
        { 
            while (true)
            {
                try
                {
                    if (queue.Count > 0)
                    {
                        string log;
                        queue.TryDequeue(out log);
                        if (!string.IsNullOrEmpty(log))
                        {
                            SaveLog(log);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.AddToFile("Logger", ex.ToString());
                }
            }
        }

        //Método que salva o log em arquivo de texto, organizando as pastas por data
        private void SaveLog(string log)
        {
            try
            {
                string date = DateTime.Now.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
                string timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff",CultureInfo.InvariantCulture);

                string fullPath = Path.Combine(Properties.Settings.Default.LogPath, date);
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                }

                fullPath = Path.Combine(fullPath, "log.txt");

                log = timestamp + " " + log;
#if(DEBUG)
                Console.WriteLine(log);
#endif

                using (StreamWriter tw = new StreamWriter(fullPath, true))
                {
                    tw.WriteLine(log);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
