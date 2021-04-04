using System.Globalization;

namespace TwitchOverlap.Extensions
{
    public static class Extensions
    {
        public static string KiloFormat(this int num)
        {
            if (num >= 1000000)
                return num.ToString("0,,.##M", CultureInfo.InvariantCulture);

            if (num >= 1000)
                return num.ToString("0,.#K", CultureInfo.InvariantCulture);

            return num.ToString("#,0");
        } 
    }
}