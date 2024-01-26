using System.Text;

namespace ElectricFox.SondeAlert
{
    public static class Extensions
    {
        public static DateTime ToDateTime(this int unixTimeStamp)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dateTime;
        }

        public static string EscapeText(this string text)
        {
            const string EscapeChars = "-.=+*`_[]!";

            var sb = new StringBuilder();
            foreach (char c in text)
            {
                if (EscapeChars.Contains(c))
                {
                    sb.Append("\\");
                }

                sb.Append(c);
            }

            return sb.ToString();
        }

        public static string FormatCoordinate(this double value)
        {
            return string.Format("{0:N4}", value);
        }
    }
}
