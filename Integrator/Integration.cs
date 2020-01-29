using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace Integrator
{
    class Integration
    {
        public int seq { get; private set; }
        public int seqParent { get;  set; }
        public string name { get; set; }
        public string type { get; set; }
        public DateTime? baseTime { get; set; }
        public int interval { get; set; }
        public int delay { get; set; }
        public int maxTries { get; set; }
        private List<Segment> segmentList;
        private DateTime? initDate;
        private DateTime endDate;
        private bool working;
        public bool paused { get; private set; }

        private SyncTimer timer;

        public Integration(int seq)
        {
            this.seq = seq;
            segmentList = new List<Segment>();
            working = false;
            timer = new SyncTimer();
            timer.Elapsed += new ElapsedEventHandler(Timer_Elapsed);
        }

        public void Start(object obj)
        {
            DoWork();
            timer.Start();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!working)
            {
                DoWork();
            }
            else
            {
                Logger.AddToFile("A integração " + name + " não ocorreu no horário previsto porque o ciclo anterior ainda está em processamento. Ocorrerá nova tentativa em " + interval + " segundos.");
            }
        }

        private void DoWork()
        {
            working = true;
            Console.WriteLine(DateTime.Now);
            GetIntegrationParameters();
            if (!paused)
            {
                GetIntegrationStructure();
                GetData();
                GenerateFiles();
            }
            working = false;
        }

        #region IntegrationParameters
        public void GetIntegrationParameters()
        {
            using (DataSet1TableAdapters.INTEGRATOR_INTEGRATIONTableAdapter integrationTableAdapter = new DataSet1TableAdapters.INTEGRATOR_INTEGRATIONTableAdapter())
            {
                using (DataSet1.INTEGRATOR_INTEGRATIONDataTable integrationDataTable = new DataSet1.INTEGRATOR_INTEGRATIONDataTable())
                {
                    try
                    {
                        integrationTableAdapter.FillBySeq(integrationDataTable, seq);

                        int intTemp;
                        DateTime dateTimeTemp;

                        if (DateTime.TryParseExact(integrationDataTable[0]["BASE_TIME"].ToString(), "dd/MM/yyyy hh:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dateTimeTemp))
                        {
                            baseTime = dateTimeTemp;
                        }
                        else
                        {
                            baseTime = null;
                        }

                        paused = (integrationDataTable[0]["PAUSED"].ToString() == "Y") ? true : false;

                        if (paused)
                        {
                            //Quando pausado, a thread verificará a cada 15s se foi resumido
                            interval = 15; 
                        }
                        else
                        {
                            if (int.TryParse(integrationDataTable[0]["INTERVAL"].ToString(), out intTemp))
                            {
                                interval = intTemp;
                            }
                        }
                        
                        if (int.TryParse(integrationDataTable[0]["DELAY"].ToString(), out intTemp))
                        {
                            delay = intTemp;
                        }

                        if (int.TryParse(integrationDataTable[0]["MAX_TRIES"].ToString(), out intTemp))
                        {
                            maxTries = intTemp;
                        }
                        timer.Interval = interval * 1000;
                    }
                    catch (Exception ex)
                    {
                        Logger.AddToFile(ex.ToString());
                    }
                }
            }
        }
        #endregion
        #region IntegrationStructure
        private void GetIntegrationStructure()
        {
            GetSegments();
        }

        private void GetSegments()
        {
            using (DataSet1TableAdapters.INTEGRATOR_SEGMENTTableAdapter segmentTableAdapter = new DataSet1TableAdapters.INTEGRATOR_SEGMENTTableAdapter())
            {
                using (DataSet1.INTEGRATOR_SEGMENTDataTable rootSegmentDataTable = new DataSet1.INTEGRATOR_SEGMENTDataTable())
                {
                    try
                    {
                        segmentTableAdapter.FillRoot(rootSegmentDataTable, seq);
                        segmentList.Clear();

                        foreach (var rootSegmentRow in rootSegmentDataTable)
                        {
                            Segment segment = new Segment();
                            int intTemp;
                            string stringTemp;

                            if (int.TryParse(rootSegmentRow["SEQ"].ToString(), out intTemp))
                            {
                                segment.seq = intTemp;

                                segment.tag = rootSegmentRow["TAG"].ToString();
                                segment.dbTable = rootSegmentRow["DB_TABLE"].ToString();
                                segment.dbWhere = rootSegmentRow["DB_WHERE"].ToString();
                                segment.dateFilter = rootSegmentRow["DB_COLUMN_DATE_FILTER"].ToString();
                                segment.fileHeader = rootSegmentRow["FILE_HEADER"].ToString();

                                stringTemp = rootSegmentRow["MAIN_SEGMENT"].ToString();
                                segment.main = (stringTemp == "Y") ? true : false;

                                segment.GetFields();
                                segment.GenerateSQL();

                                segment.segments = GetChildren(segment.seq);
                                segmentList.Add(segment);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.AddToFile(ex.ToString());
                    }
                }
            }
        }

        private List<Segment> GetChildren(int seqSegment)
        {
            List<Segment> children = new List<Segment>();
            using (DataSet1TableAdapters.INTEGRATOR_SEGMENTTableAdapter segmentTableAdapter = new DataSet1TableAdapters.INTEGRATOR_SEGMENTTableAdapter())
            {
                using (DataSet1.INTEGRATOR_SEGMENTDataTable childrenSegmentDataTable = new DataSet1.INTEGRATOR_SEGMENTDataTable())
                {
                    try
                    {
                        segmentTableAdapter.FillByParent(childrenSegmentDataTable, seqSegment);

                        foreach (var childrenSegmentRow in childrenSegmentDataTable)
                        {
                            Segment segment = new Segment();
                            int intTemp;
                            string stringTemp;

                            if (int.TryParse(childrenSegmentRow["SEQ"].ToString(), out intTemp))
                            {
                                segment.seq = intTemp;

                                if (int.TryParse(childrenSegmentRow["SEQ_PARENT_SEGMENT"].ToString(), out intTemp))
                                {
                                    seqParent = intTemp;
                                }
                                segment.tag = childrenSegmentRow["TAG"].ToString();
                                segment.dbTable = childrenSegmentRow["DB_TABLE"].ToString();
                                segment.dbWhere = childrenSegmentRow["DB_WHERE"].ToString();
                                segment.dateFilter = childrenSegmentRow["DB_COLUMN_DATE_FILTER"].ToString();

                                stringTemp = childrenSegmentRow["MAIN_SEGMENT"].ToString();
                                segment.main = (stringTemp == "Y") ? true : false;

                                segment.GetFields();
                                segment.GenerateSQL();

                                segment.segments = GetChildren(segment.seq);
                                children.Add(segment);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.AddToFile(ex.ToString());
                    }
                }
            }
            return children;
        }

        #endregion
        #region IntegrationData
        private void GetData()
        {
            GetIntegrationInterval();
            foreach (Segment segment in segmentList)
            {
                GetData(segment);
            }
        }

        private void GetData(Segment segment)
        {
            segment.GetData(initDate, endDate);
            foreach (Segment childrenSegment in segment.segments)
            {
                GetData(childrenSegment);
            }
        }

        private void GetIntegrationInterval()
        {

            using (DataSet1TableAdapters.INTEGRATOR_LOGTableAdapter logTableAdapter = new DataSet1TableAdapters.INTEGRATOR_LOGTableAdapter())
            {
                endDate = DateTime.Now;
                endDate = endDate.AddSeconds(-delay);

                initDate = null;
                try
                {
                    initDate = logTableAdapter.GetLastIntegrationDate(seq);
                }
                catch (Exception ex)
                {
                    Logger.AddToFile(ex.ToString());
                }

                if (!initDate.HasValue)
                {
                    if (baseTime.HasValue && baseTime < endDate)
                    {
                        initDate = baseTime;
                    }
                    else
                    {
                        initDate = endDate.AddSeconds(-interval);
                    }
                }

                if (initDate.HasValue)
                {
                    if (initDate >= endDate)
                    {
                        initDate = endDate.AddSeconds(-interval);
                    }
                }
            }
        }
        #endregion
        #region GenerateFiles
        private void GenerateFiles()
        {
            IFile file;

            switch (type)
            {
                case "XML":
                    file = new FileTypes.XML();
                    break;

                case "HL7":
                    file = new FileTypes.HL7();
                    break;

                case "JSON":
                    file = new FileTypes.JSON();
                    break;

                default:
                    file = null;
                    break;
            }

            if (file != null)
            {
                List<File> files = file.GetFiles(segmentList);
                foreach (File item in files)
                {
                    Logger.AddToDb(seq, item.key, item.file, initDate.Value, endDate, "I", null);
                }
            }
            else
            {
                Logger.AddToFile("Tipo de arquivo não informado na integração " + name);
            }
        }
        #endregion
    }
}
