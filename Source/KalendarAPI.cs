using System;
using System.Collections.Generic;

namespace KerbalKalendar
{
    /// <summary>
    /// Public API for the Kerbal Kalendar mod.
    ///
    /// Other mods (e.g. KerbalGeopolitics, KerbalColonies integration) should
    /// consume the Kalendar exclusively through this class. Never call
    /// KerbalKalendarConverter directly from an external assembly — this API
    /// provides stability guarantees that internal classes do not.
    ///
    /// Usage example:
    ///   using KerbalKalendar;
    ///   string date = KalendarAPI.GetCurrentDateString();
    ///   // "23 Harven KY 415"
    ///
    /// Versioning:
    ///   Check KalendarAPI.Version before calling any methods if you need
    ///   to guard against breaking changes in future releases.
    /// </summary>
    public static class KalendarAPI
    {
        // ── Version ───────────────────────────────────────────────────────

        /// <summary>
        /// Semantic version of this API surface.
        /// Increment major on breaking changes, minor on additions.
        /// </summary>
        public const string Version = "1.0.0";

        // ── Availability guard ────────────────────────────────────────────

        /// <summary>
        /// Returns true if the Kalendar system is ready to be queried.
        /// External mods should check this before calling any other method,
        /// particularly during scene transitions or early loading.
        /// </summary>
        public static bool IsAvailable =>
            HighLogic.LoadedScene != GameScenes.LOADING &&
            HighLogic.LoadedScene != GameScenes.LOADINGBUFFER;

        // ── Current date queries ──────────────────────────────────────────

        /// <summary>
        /// Returns the current in-game date as a fully resolved KalendarDate object.
        /// Use this when you need structured access to year, month, and day separately.
        /// Returns null if the Kalendar is not yet available.
        /// </summary>
        public static KalendarDate GetCurrentDate()
        {
            if (!IsAvailable) return null;
            return KerbalKalendarConverter.UTToDate(Planetarium.GetUniversalTime());
        }

        /// <summary>
        /// Returns the current date as a short display string.
        /// Format: "23 Harven KY 415" or "Kolvari KY 416"
        /// Returns empty string if not available.
        /// </summary>
        public static string GetCurrentDateString()
        {
            KalendarDate date = GetCurrentDate();
            return date?.ToShortString() ?? string.Empty;
        }

        /// <summary>
        /// Returns the current date and time as a full display string.
        /// Format: "23 Harven KY 415 | 03:15:22"
        /// Returns empty string if not available.
        /// </summary>
        public static string GetCurrentFullDisplay()
        {
            if (!IsAvailable) return string.Empty;
            return KerbalKalendarConverter.UTToFullDisplay(Planetarium.GetUniversalTime());
        }

        /// <summary>
        /// Returns the current KY year as an integer.
        /// Returns -1 if not available.
        /// </summary>
        public static int GetCurrentYear()
        {
            KalendarDate date = GetCurrentDate();
            return date?.Year ?? -1;
        }

        /// <summary>
        /// Returns the current month number (1-10).
        /// Returns -1 if not available or if today is Kolvari.
        /// </summary>
        public static int GetCurrentMonthNumber()
        {
            KalendarDate date = GetCurrentDate();
            if (date == null || date.IsKolvari) return -1;
            return date.Month?.Number ?? -1;
        }

        /// <summary>
        /// Returns the current month name (e.g. "Harven").
        /// Returns "Kolvari" if today is the leap day.
        /// Returns empty string if not available.
        /// </summary>
        public static string GetCurrentMonthName()
        {
            KalendarDate date = GetCurrentDate();
            if (date == null) return string.Empty;
            if (date.IsKolvari) return KalendarData.LeapDayName;
            return date.Month?.Name ?? string.Empty;
        }

        /// <summary>
        /// Returns the current day of the month (1-indexed).
        /// Returns -1 if not available or if today is Kolvari.
        /// </summary>
        public static int GetCurrentDay()
        {
            KalendarDate date = GetCurrentDate();
            if (date == null || date.IsKolvari) return -1;
            return date.Day;
        }

        /// <summary>
        /// Returns true if today is Kolvari (the leap day).
        /// Returns false if not available.
        /// </summary>
        public static bool IsKolvari()
        {
            KalendarDate date = GetCurrentDate();
            return date?.IsKolvari ?? false;
        }

        // ── Arbitrary UT queries ──────────────────────────────────────────

        /// <summary>
        /// Converts any arbitrary UT value to a KalendarDate.
        /// Useful for displaying timestamps on historical events,
        /// colony founding dates, mission logs, etc.
        /// </summary>
        public static KalendarDate DateFromUT(double ut)
        {
            return KerbalKalendarConverter.UTToDate(ut);
        }

        /// <summary>
        /// Converts any arbitrary UT value to a short date string.
        /// Format: "23 Harven KY 415"
        /// </summary>
        public static string DateStringFromUT(double ut)
        {
            return KerbalKalendarConverter.UTToDate(ut).ToShortString();
        }

        // ── Month data access ─────────────────────────────────────────────

        /// <summary>
        /// Returns a read-only list of all 10 Kalendar months in order.
        /// Useful for building UI dropdowns, calendar displays, etc.
        /// </summary>
        public static IReadOnlyList<KalendarMonth> GetAllMonths()
        {
            return KalendarData.Months.AsReadOnly();
        }

        /// <summary>
        /// Returns the KalendarMonth for a given 1-based month number.
        /// Returns null if out of range.
        /// </summary>
        public static KalendarMonth GetMonth(int monthNumber)
        {
            return KalendarData.GetMonth(monthNumber);
        }

        /// <summary>
        /// Returns true if the given KY year is a leap year (contains Kolvari).
        /// </summary>
        public static bool IsLeapYear(int year)
        {
            return KalendarData.IsLeapYear(year);
        }

        // ── Event hooks ───────────────────────────────────────────────────

        /// <summary>
        /// Returns true if two UT values fall on the same Kalendar day.
        /// Useful for checking whether a stored event timestamp is today.
        ///
        /// Example (geopolitical mod):
        ///   bool isAnniversary = KalendarAPI.IsSameDay(colony.FoundingUT,
        ///                                              Planetarium.GetUniversalTime());
        /// </summary>
        public static bool IsSameDay(double utA, double utB)
        {
            return KerbalKalendarConverter.IsSameDay(utA, utB);
        }

        /// <summary>
        /// Returns true if the current date falls within the named month.
        /// Case-insensitive. Returns false on Kolvari or if unavailable.
        ///
        /// Example:
        ///   if (KalendarAPI.IsCurrentMonth("Kethis")) { ... }
        /// </summary>
        public static bool IsCurrentMonth(string monthName)
        {
            if (!IsAvailable) return false;
            return KerbalKalendarConverter.IsMonth(
                Planetarium.GetUniversalTime(), monthName);
        }

        /// <summary>
        /// Returns the number of in-game seconds until the start of the next year.
        /// Useful for scheduling annual events (budget cycles, elections, etc.)
        /// Returns -1 if not available.
        /// </summary>
        public static double SecondsUntilNewYear()
        {
            if (!IsAvailable) return -1;

            double ut = Planetarium.GetUniversalTime();
            KalendarDate current = KerbalKalendarConverter.UTToDate(ut);

            // Calculate UT at start of next year
            int nextYear = current.Year + 1;
            long daysToNextYear = DaysFromKY1ToYear(nextYear);
            double utNextYear = daysToNextYear * KalendarData.SecondsPerDay;

            return utNextYear - ut;
        }

        // ── Internal helpers ──────────────────────────────────────────────

        /// <summary>
        /// Returns total days from KY 1 day 1 to the start of the given year.
        /// Mirrors the private method in KerbalKalendarConverter for API use.
        /// </summary>
        private static long DaysFromKY1ToYear(int targetYear)
        {
            if (targetYear <= 1) return 0;

            long days = 0;
            int fullCycles = (targetYear - 1) / KalendarData.LeapYearCycle;
            days += fullCycles * KalendarData.DaysPerFourYearCycle;

            int remainingYears = (targetYear - 1) % KalendarData.LeapYearCycle;
            int yearCounter    = (fullCycles * KalendarData.LeapYearCycle) + 1;

            for (int i = 0; i < remainingYears; i++)
            {
                days += KalendarData.DaysInYear(yearCounter);
                yearCounter++;
            }

            return days;
        }
    }
}
