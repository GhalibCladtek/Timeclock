using System;
using System.Collections.Generic;
using System.Linq;
using TimeClock.Models;

namespace TimeClock.Helpers
{
    public static class WorkingHoursHelper
    {
        // Adjust to your actual working hours / shift pattern.
        private static readonly TimeSpan WorkStart = new TimeSpan(8, 0, 0);
        private static readonly TimeSpan WorkEnd = new TimeSpan(20, 0, 0);

        public static bool IsWeekend(DateTime dt)
            => dt.DayOfWeek == DayOfWeek.Saturday || dt.DayOfWeek == DayOfWeek.Sunday;

        public static bool IsWithinWorkingHours(DateTime dt)
        {
            if (IsWeekend(dt)) return false;
            var t = dt.TimeOfDay;
            return t >= WorkStart && t <= WorkEnd;
        }

        /// <summary>
        /// Single source of truth for which JobType values are selectable right now.
        /// Called by both the "get options" endpoint (for the UI) and StartTask (for validation),
        /// so the rule can never be bypassed from the client.
        /// </summary>
        public static List<string> GetAllowedJobTypes(DateTime dt)
        {
            return IsWithinWorkingHours(dt)
                ? new List<string> { "Development", "Operational" }
                : new List<string> { "Operational" };
        }
    }
}