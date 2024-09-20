using Newtonsoft.Json.Linq;
using System.Data;

namespace PKApp.Tools
{
    public class DataTableHelper
    {
        public static DataTable ToDataTable(JArray jsonArray, dynamic Columns)
        {
            DataTable dataTable = new DataTable();

            // 从第一个对象中获取列
            foreach (JToken token in jsonArray)
            {
                if (token is JObject jObject)
                {
                    foreach (JProperty property in jObject.Properties())
                    {
                        string name = Columns.GetType().GetProperty(property.Name)?.GetValue(Columns);
                        if (!dataTable.Columns.Contains(name))
                        {

                            dataTable.Columns.Add(name);
                        }
                    }
                    //if (!dataTable.Columns.Contains("加總"))
                    //{
                    //    //最後放入加總的
                    //    dataTable.Columns.Add("加總");
                    //}

                }
            }

            int total = 0;
            // 填充数据
            foreach (JToken token in jsonArray)
            {
                if (token is JObject jObject)
                {
                    DataRow row = dataTable.NewRow();

                    foreach (JProperty property in jObject.Properties())
                    {
                        string name = Columns.GetType().GetProperty(property.Name)?.GetValue(Columns);
                        row[name] = property.Value;

                        if (name == "下載次數")
                        {
                            total += Convert.ToInt32(property.Value);
                        }
                    }

                    dataTable.Rows.Add(row);
                }
            }
            DataRow totalRow = dataTable.NewRow();
            totalRow["日期"] = "總下載數";
            totalRow["裝置"] = "iOS + Android";
            totalRow["下載次數"] = total;
            dataTable.Rows.Add(totalRow);

            return dataTable;
        }

        public static DataTable NewsToDataTable(JArray jsonArray, dynamic Columns)
        {
            DataTable dataTable = new DataTable();

            // 从第一个对象中获取列
            foreach (JToken token in jsonArray)
            {
                if (token is JObject jObject)
                {
                    foreach (JProperty property in jObject.Properties())
                    {
                        string name = Columns.GetType().GetProperty(property.Name)?.GetValue(Columns);
                        if (!dataTable.Columns.Contains(name))
                        {

                            dataTable.Columns.Add(name);
                        }
                    }
                    //if (!dataTable.Columns.Contains("加總"))
                    //{
                    //    //最後放入加總的
                    //    dataTable.Columns.Add("加總");
                    //}

                }
            }

            int total = 0;
            // 填充数据
            foreach (JToken token in jsonArray)
            {
                if (token is JObject jObject)
                {
                    DataRow row = dataTable.NewRow();

                    foreach (JProperty property in jObject.Properties())
                    {
                        string name = Columns.GetType().GetProperty(property.Name)?.GetValue(Columns);
                        row[name] = property.Value;
                    }

                    dataTable.Rows.Add(row);
                }
            }

            return dataTable;
        }
    }
}
