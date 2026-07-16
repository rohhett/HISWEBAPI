using System.Data;

namespace HISWEBAPI.Utilities
{
    public static class DataTableExtensions
    {
        public static List<Dictionary<string, object>> ToRawList(this DataTable dt)
        {
            if (dt == null) return new List<Dictionary<string, object>>();

            return dt.AsEnumerable().Select(row =>
                dt.Columns.Cast<DataColumn>().ToDictionary(
                    col => col.ColumnName,
                    col => row[col] == DBNull.Value ? null : row[col]
                )
            ).ToList();
        }

        // Helper for in-memory filtering on a dictionary row (avoids repeating Convert.ToXxx everywhere)
        public static T GetValue<T>(this Dictionary<string, object> row, string key, T defaultValue = default)
        {
            if (!row.TryGetValue(key, out var val) || val == null) return defaultValue;
            try { return (T)Convert.ChangeType(val, typeof(T)); }
            catch { return defaultValue; }
        }
    }
}