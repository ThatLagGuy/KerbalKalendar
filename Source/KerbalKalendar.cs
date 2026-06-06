using System;
using System.Collections.Generic;

namespace KerbalKalendar
{
    /// <summary>
    /// Core conversion engine for the Kerbal Kalendar.
    /// Translates KSP Universal Time (seconds) into a KalendarDate.
    ///
    /// Epoch anchor: UT = 0 corresponds to 1 Kelbris, KY 412.
    ///
    /// Year structure:
    ///   - Standard year: 426 days
    ///   - Leap year:     427 days (Kolvari appended after Duskol)
    ///   - Leap cycle:    every 4th KY year (KY 412, 416, 420, ...)
    ///
    /// Day length: 6 hours = 21,600 seconds (stock Kerbin, unchanged)
    /// </summary>
    public static class KerbalKalendarConverter
    {
        // ── Public entry point ────────────────────────────────────────────

        /// <summary>
        /// Converts a KSP Universal Time value (in seconds) to a KalendarDate.
        /// </summary>
        /// <param name="ut">Universal Time in seconds (Planetarium.GetUniversalTime())</param>
        /// <returns>A fully resolved KalendarDate for display or comparison.</returns>
        public static KalendarDate UTToDate(double ut)
        {
            // Step 1: Convert UT seconds to total elapsed days since UT=0
            // Floor so we always work in whole days; time-of-day is handled separately
            long totalDaysElapsed = (long)Math.Floor(ut / KalendarData.SecondsPerDay);

            // Step 2: Offset by epoch. UT=0 = Day 0 of KY 412.
            // We need to find how many days have passed since the start of KY 1
            // so we can do clean year/month arithmetic.
            long daysBeforeEpoch = DaysFromKY1ToStartOfYear(KalendarData.EpochYear)
                                   + (KalendarData.EpochDay - 1);

            long absoluteDay = totalDaysElapsed + daysBeforeEpoch;

            // Step 3: Resolve absolute day count into a year
            int year = ResolveYear(absoluteDay, out long dayOfYear);

            // Step 4: Resolve day-of-year into month and day
            return ResolveMonthAndDay(year, dayOfYear);
        }

        /// <summary>
        /// Returns just the current KY year as an integer.
        /// Convenience wrapper around UTToDate.
        /// </summary>
        public static int UTToYear(double ut)
        {
            return UTToDate(ut).Year;
        }

        /// <summary>
        /// Returns the time-of-day string (HH:MM:SS) from a UT value.
        /// This is purely cosmetic and does not interact with the Kalendar date.
        /// </summary>
        public static string UTToTimeOfDay(double ut)
        {
            double secondsIntoDay = ut % KalendarData.SecondsPerDay;
            int totalSeconds = (int)Math.Floor(secondsIntoDay);

            int hours   = totalSeconds / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            int seconds = totalSeconds % 60;

            return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
        }

        /// <summary>
        /// Returns a full display string including date and time of day.
        /// Example: "23 Harven KY 415 | 03:15:22"
        /// </summary>
        public static string UTToFullDisplay(double ut)
        {
            KalendarDate date = UTToDate(ut);
            string time = UTToTimeOfDay(ut);
            return $"{date.ToShortString()} | {time}";
        }

        // ── Year resolution ───────────────────────────────────────────────

        /// <summary>
        /// Given an absolute day count from KY 1 day 1, determines which
        /// KY year it falls in, and outputs the day-of-year (0-indexed).
        /// </summary>
        private static int ResolveYear(long absoluteDay, out long dayOfYear)
        {
            // Work in 4-year cycles for efficiency
            long fullCycles    = absoluteDay / KalendarData.DaysPerFourYearCycle;
            long remainingDays = absoluteDay % KalendarData.DaysPerFourYearCycle;

            // Each full cycle = 4 years starting from KY 1
            int year = (int)(fullCycles * KalendarData.LeapYearCycle) + 1;

            // Walk through the remaining days one year at a time
            // (max 4 iterations per call — negligible cost)
            while (true)
            {
                int daysThisYear = KalendarData.DaysInYear(year);

                if (remainingDays < daysThisYear)
                {
                    dayOfYear = remainingDays; // 0-indexed day within the year
                    return year;
                }

                remainingDays -= daysThisYear;
                year++;
            }
        }

        /// <summary>
        /// Given a KY year and a 0-indexed day-of-year, resolves to a full KalendarDate.
        /// Handles Kolvari (leap day) which falls after Duskol in leap years.
        /// </summary>
        private static KalendarDate ResolveMonthAndDay(int year, long dayOfYear)
        {
            // Walk through months in order, subtracting days until we find the right month
            foreach (KalendarMonth month in KalendarData.Months)
            {
                if (dayOfYear < month.Days)
                {
                    // dayOfYear is 0-indexed, so add 1 for display
                    return new KalendarDate
                    {
                        Year     = year,
                        Month    = month,
                        Day      = (int)dayOfYear + 1,
                        IsKolvari = false
                    };
                }

                dayOfYear -= month.Days;
            }

            // If we've exhausted all months and still have a day left,
            // it must be Kolvari (only possible in a leap year)
            return new KalendarDate
            {
                Year      = year,
                Month     = null,
                Day       = 1,
                IsKolvari = true
            };
        }

        // ── Epoch math ────────────────────────────────────────────────────

        /// <summary>
        /// Calculates the total number of days from the start of KY 1
        /// to the start of the given year. Used to anchor the epoch.
        /// </summary>
        private static long DaysFromKY1ToStartOfYear(int targetYear)
        {
            if (targetYear <= 1)
                return 0;

            long days = 0;

            // Full 4-year cycles before the target year
            int fullCycles = (targetYear - 1) / KalendarData.LeapYearCycle;
            days += fullCycles * KalendarData.DaysPerFourYearCycle;

            // Remaining years after full cycles
            int remainingYears = (targetYear - 1) % KalendarData.LeapYearCycle;
            int yearCounter    = (fullCycles * KalendarData.LeapYearCycle) + 1;

            for (int i = 0; i < remainingYears; i++)
            {
                days += KalendarData.DaysInYear(yearCounter);
                yearCounter++;
            }

            return days;
        }

        // ── Comparison utilities ──────────────────────────────────────────

        /// <summary>
        /// Returns true if two UT values fall on the same Kalendar day.
        /// Useful for event triggering (e.g. Kolvari celebrations).
        /// </summary>
        public static bool IsSameDay(double utA, double utB)
        {
            KalendarDate a = UTToDate(utA);
            KalendarDate b = UTToDate(utB);

            return a.Year == b.Year
                && a.IsKolvari == b.IsKolvari
                && (a.IsKolvari || (a.Month?.Number == b.Month?.Number && a.Day == b.Day));
        }

        /// <summary>
        /// Returns true if the given UT value falls on Kolvari.
        /// </summary>
        public static bool IsKolvari(double ut)
        {
            return UTToDate(ut).IsKolvari;
        }

        /// <summary>
        /// Returns true if the given UT value falls within the given month name.
        /// Case-insensitive. Example: IsMonth(ut, "Harven")
        /// </summary>
        public static bool IsMonth(double ut, string monthName)
        {
            KalendarDate date = UTToDate(ut);
            if (date.IsKolvari || date.Month == null)
                return false;

            return string.Equals(date.Month.Name, monthName,
                                 StringComparison.OrdinalIgnoreCase);
        }
    }
}
