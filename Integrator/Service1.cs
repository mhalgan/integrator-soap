using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace Integrator
{
    public partial class Service1 : ServiceBase
    {
        private List<Integration> integrationList;
        public Service1()
        {
            InitializeComponent();
            InitializeMyComponent();
        }

        protected override void OnStart(string[] args)
        {
            Start();
        }

        public void Start()
        {
            Logger.AddToFile("Serviço iniciado.");

            if (integrationList.Count == 0)
            {
                using (DataSet1TableAdapters.INTEGRATOR_INTEGRATIONTableAdapter integrationTableAdapter = new DataSet1TableAdapters.INTEGRATOR_INTEGRATIONTableAdapter())
                {
                    using (DataSet1.INTEGRATOR_INTEGRATIONDataTable integrationDataTable = new DataSet1.INTEGRATOR_INTEGRATIONDataTable())
                    {
                        try
                        {
                            integrationTableAdapter.Fill(integrationDataTable);

                            foreach (var row in integrationDataTable)
                            {
                                Integration integration;
                                int intTemp;

                                if (int.TryParse(row["SEQ"].ToString(), out intTemp))
                                {
                                    if (row["INSTANCE_NAME"].ToString() == Properties.Settings.Default.InstanceName)
                                    {
                                        integration = new Integration(intTemp);

                                        integration.name = row["INTEGRATION_NAME"].ToString();
                                        integration.type = row["TYPE"].ToString();

                                        integrationList.Add(integration);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.AddToFile(ex.ToString());
                        }
                    }
                }

                if (integrationList.Count == 0)
                {
                    Logger.AddToFile("Nenhuma integração encontrada para essa instância. Verfique a tag InstanceName do arquivo .config e a tabela INTEGRATOR_INTEGRATION.");
                }
                else
                {
                    foreach (Integration integration in integrationList)
                    {
                        ThreadPool.QueueUserWorkItem(new WaitCallback(integration.Start));
                    }
                }
            }

            ThreadPool.QueueUserWorkItem(new WaitCallback(Sender.Start));
        }

        protected override void OnStop()
        {
            Logger.AddToFile("Serviço parado.");
        }

        private void InitializeMyComponent()
        {
            integrationList = new List<Integration>();
            Logger logger = new Logger();
            ThreadPool.QueueUserWorkItem(new WaitCallback(logger.Start));
        }
    }
}
