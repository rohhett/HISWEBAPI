using System;

namespace HISWEBAPI.Utilities
{
    public class Utility
    {
        public static string getDate()
        {
            return DateTime.Now.ToString("dd-MMM-yyyy");
        }

        public static string getFormatedDate()
        {
            return DateTime.Now.ToString("M/d/yyyy h:mm tt");
        }

        public static string getTomorrow()
        {
            return DateTime.Now.AddDays(1).ToString();
        }

        public static int generateOTP(int size = 6)
        {
            const int max = 6;
            string pattern = "0123456789";
            string otp = "";
            Random random = new Random();
            int maxLength = pattern.Length;
            for (int i = 0; i < Math.Min(max, Math.Abs(size)); i++)
            {
                otp += pattern[random.Next(maxLength)];
            }
            return int.Parse(otp);
        }

        public static string getHours()
        {
            return DateTime.Now.ToString("hh");
        }

        public static string getMintus()
        {
            return DateTime.Now.ToString("mm");
        }

        public static string getYear()
        {
            return DateTime.Now.ToString("yyyy");
        }

        public static string getTime()
        {
            return DateTime.Now.ToString("hh mm ss");
        }

        public static bool CompareYear(string FromDate, string ToDate)
        {
            if (Convert.ToInt32(FromDate) > Convert.ToInt32(ToDate))
                return false;
            else
                return true;
        }

        public static int getInt(Object obj)
        {
            if (obj == null || Convert.IsDBNull(obj) || obj.ToString().Trim() == string.Empty)
                return 0;
            else
                return int.Parse(obj.ToString());
        }

        public static Int16 getShortInt(Object obj)
        {
            if (obj == null || Convert.IsDBNull(obj) || obj.ToString().Trim() == string.Empty)
                return 0;
            else
                return Int16.Parse(obj.ToString());
        }

        public static long getLong(Object obj)
        {
            if (obj == null || Convert.IsDBNull(obj) || obj.ToString().Trim() == string.Empty)
                return 0;
            else
                return long.Parse(obj.ToString());
        }

        public static decimal getDecimal(Object obj)
        {
            if (obj == null || Convert.IsDBNull(obj) || obj.ToString().Trim() == string.Empty)
                return 0;
            else
                return decimal.Parse(obj.ToString());
        }

        public static float getFloat(Object obj)
        {
            if (obj == null || Convert.IsDBNull(obj) || obj.ToString().Trim() == string.Empty)
                return 0F;
            else
                return float.Parse(obj.ToString());
        }

        public static double getDouble(Object obj)
        {
            if (obj == null || Convert.IsDBNull(obj) || obj.ToString().Trim() == string.Empty)
                return 0;
            else
                return double.Parse(obj.ToString());
        }

        public static DateTime getDateTime(Object obj)
        {
            if (obj == null || Convert.IsDBNull(obj) || obj.ToString().Trim() == string.Empty)
                return getMinDateTime();
            else
                return DateTime.Parse(obj.ToString());
        }

        public static DateTime getMinDateTime()
        {
            return DateTime.Parse("01/jan/0001");
        }

        public static string getString(Object obj)
        {
            if (obj == null || Convert.IsDBNull(obj) || obj.ToString().Trim() == string.Empty)
                return "";
            else
                return obj.ToString();
        }

        public static bool getBoolean(Object obj)
        {
            if (obj == null || Convert.IsDBNull(obj) || obj.ToString().Trim() == string.Empty)
                return false;
            else
                return bool.Parse(obj.ToString());
        }

        public static bool getbooleanTrueFalse(Object obj)
        {
            if (obj == null || Convert.IsDBNull(obj) || obj.ToString().Trim() == string.Empty)
                return false;
            else
                return true;
        }

        public static int getbooleanInt(bool obj)
        {
            if (obj == true)
                return 1;
            else
                return 0;
        }
    }
}