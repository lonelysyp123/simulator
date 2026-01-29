using CsvHelper;
using System.Globalization;

namespace IEC61850_simulatorServer2
{
    /// <summary>
    /// CSV 轉換成 Class
    /// @author syp
    /// @date 2024/06/17
    /// </summary>
    public static class CSVUtil
    {
        public static List<T>? CSV2Class<T>(string filePath)
        {
            List<T>? result = null;
            using (var reader = new StreamReader(filePath))
            {
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    result = csv.GetRecords<T>().ToList();
                }
            }
            return result;
        }
    }
}