using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integrator.FileTypes
{
    class XML : IFile
    {
        private string header;
        private DataTable data;
        private List<Parameter> mainParameters;

        public List<File> GetFiles(List<Segment> segments)
        {
            List<File> files = new List<File>();
            data = new DataTable();
            mainParameters = new List<Parameter>();

            foreach (Segment segment in segments)
            {
                if (!string.IsNullOrEmpty(segment.fileHeader))
                {
                    header = segment.fileHeader;
                }
                FindMain(segment);
            }

            foreach (DataRow row in data.Rows)
            {
                File file = new File();
                string xml = "";
                string parameters = "";

                xml += header;

                foreach (var parameter in mainParameters)
                {
                    parameter.value = row[parameter.dbColumn].ToString();
                    if (!string.IsNullOrEmpty(parameters))
                    {
                        parameters += ";";
                    }
                    parameters += parameter.value;
                }

                foreach (Segment segment in segments)
                {
                    xml += MountXML(segment, 0, mainParameters);
                }

                file.file = xml;
                file.key = parameters;

                files.Add(file);
            }
            
            return files;
        }

        private void FindMain(Segment segment)
        {
            if (segment.main)
            {
                foreach (Field field in segment.fields)
                {
                    if (field.key)
                    {
                        Parameter parameter = new Parameter();
                        parameter.tag = field.tag;
                        parameter.dbColumn = field.dbColumn;
                        mainParameters.Add(parameter);
                    }
                }
                data = segment.data;
                return;
            }
            foreach (Segment childrenSegment in segment.segments)
            {
                FindMain(childrenSegment);
            }
        }

        private string MountXML(Segment segment, int level, List<Parameter> parentParameters)
        {
            string xml = "";
            string filter = "";
            List<Parameter> localParameters = new List<Parameter>();

            using (DataView dataView = new DataView(segment.data))
            {
                //Se o segmento possuir SQL, localiza as tags correspondentes aos parâmetros do segmento pai e montra o filtro
                if (!string.IsNullOrEmpty(segment.sql))
                {
                    foreach (Parameter parameter in parentParameters)
                    {
                        string dbColumn = segment.fields.FirstOrDefault(x => x.tag == parameter.tag).dbColumn;
                        if (!string.IsNullOrEmpty(dbColumn))
                        {
                            if (!string.IsNullOrEmpty(filter))
                            {
                                filter += " and ";
                            }
                            filter += dbColumn + " = " + parameter.value;
                        }
                    }
                    try
                    {
                        dataView.RowFilter = filter;
                    }
                    catch (Exception ex)
                    {
                        Logger.AddToFile(ex.ToString());
                    }
                    
                }

                if (string.IsNullOrEmpty(segment.sql))
                {
                    localParameters = parentParameters;
                    string tab = "";
                    for (int j = 0; j < level; j++)
                    {
                        tab += "\t";
                    }

                    xml += tab + OpenTag(segment.tag) + "\n";

                    foreach (Segment childrenSegment in segment.segments)
                    {
                        xml += MountXML(childrenSegment, level + 1, localParameters);
                    }

                    xml += tab + CloseTag(segment.tag) + "\n";
                }
                else
                {
                    //Só monta a lista de parâmetros local se o segmento tiver filhos
                    if (segment.segments.Count > 0)
                    {
                        foreach (Field field in segment.fields)
                        {
                            if (field.key)
                            {
                                Parameter parameter = new Parameter();
                                parameter.tag = field.tag;
                                parameter.dbColumn = field.dbColumn;
                                localParameters.Add(parameter);
                            }
                        }
                    }
                    foreach (DataRowView rowView in dataView)
                    {
                        string tab = "";
                        for (int j = 0; j < level; j++)
                        {
                            tab += "\t";
                        }

                        foreach (var parameter in localParameters)
                        {
                            parameter.value = rowView[parameter.dbColumn].ToString();
                        }

                        xml += tab + OpenTag(segment.tag) + "\n";
                        foreach (Field field in segment.fields)
                        {
                            if (!field.hide && !string.IsNullOrEmpty(rowView[field.dbColumn].ToString()))
                            {
                                xml += tab + "\t" + OpenTag(field.tag);
                                xml += rowView[field.dbColumn];
                                xml += CloseTag(field.tag) + "\n";
                            }
                        }

                        foreach (Segment childrenSegment in segment.segments)
                        {
                            xml += MountXML(childrenSegment, level + 1, localParameters);
                        }

                        xml += tab + CloseTag(segment.tag) + "\n";
                        
                    }
                }
            }
            return xml;
        }

        private string OpenTag(string tag)
        {
            return "<" + tag + ">";
        }

        private string CloseTag(string tag)
        {
            return @"</" + tag + ">";
        }
    }
}
