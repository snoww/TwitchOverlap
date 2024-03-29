using System;
using System.Globalization;

namespace TwitchOverlapApi.Extensions
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

        public static TimeSpan GetCacheDuration(this DateTime time)
        {
            if (time.Minute is <= 5 or >= 30 and <= 35)
            {
                return TimeSpan.FromMinutes(2);
            }
            int duration = (60 - time.Minute) % 30;
            return TimeSpan.FromMinutes(duration == 0 ? 1 : duration);
        }
        
        public static TimeSpan GetDailyCacheDuration(this DateTime time)
        {
            if (time.Subtract(TimeSpan.FromMinutes(15)).Day != time.Day)
            {
                return TimeSpan.FromMinutes(7);
            }
            
            return TimeSpan.FromHours(24) - time.TimeOfDay;
        }
    }
}