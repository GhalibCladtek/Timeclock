using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TimeClock.Helpers
{
    public static class DataToString
    {
        public static string FormatTimeClean(int seconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(seconds);
            List<string> parts = new List<string>();

            if (time.Days > 0) parts.Add($"{time.Days}d");
            if (time.Hours > 0) parts.Add($"{time.Hours}h");
            if (time.Minutes > 0) parts.Add($"{time.Minutes}m");

            // If less than 60 seconds, it would be empty, so handle that fallback
            if (parts.Count == 0) return "0m";

            return string.Join(" ", parts);
        }
    }
}