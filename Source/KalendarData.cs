using System.Collections.Generic;

namespace KerbalKalendar
{
    /// <summary>
    /// Represents a single month in the Kerbal Kalendar.
    /// </summary>
    public class KalendarMonth
    {
        public int Number { get; private set; }       // 1-10
        public string Name { get; private set; }      // e.g. "Kelbris"
        public int Days { get; private set; }         // Days in this month

        public KalendarMonth(int number, string name, int days)
        {
            Number = number;
            Name = name;
            Days = days;
        }
    }

    /// <summary>
    /// Represents a fully resolved Kerbal date.
    /// </summary>
    public class KalendarDate
    {
        public int Year { get; set; }
        public KalendarMonth Month { get; set; }
        public int Day { get; set; }
        public bool IsKolvari { get; set; }           // True if this is the leap day

        public string Suffix => Year >= 0 ? "KY" : "BK";

        public override string ToString()
        {
            if (IsKolvari)
                return $"Kolvari, KY {Year}";

            return $"{Day} {Month.Name}, {Suffix} {System.Math.Abs(Year)}";
        }

        /// <summary>
        /// Short format for toolbar display: "23 Harven KY 415"
        /// </summary>
        public string ToShortString()
        {
            if (IsKolvari)
                return $"Kolvari KY {Year}";

            return $"{Day} {Month.Name} {Suffix} {System.Math.Abs(Year)}";
        }
    }

    /// <summary>
    /// All static data for the Kerbal Kalendar system.
    /// </summary>
    public static class KalendarData
    {
        // ── Time constants ────────────────────────────────────────────────

        /// <summary>Kerbin day in seconds (6 hours).</summary>
        public const double SecondsPerDay = 21600.0;

        /// <summary>Days in a standard (non-leap) Kerbin year.</summary>
        public const int DaysPerStandardYear = 426;

        /// <summary>Days in a leap year (includes Kolvari).</summary>
        public const int DaysPerLeapYear = 427;

        /// <summary>How often a leap year occurs.</summary>
        public const int LeapYearCycle = 4;

        /// <summary>
        /// Total days in one full 4-year cycle.
        /// 3 standard years + 1 leap year = (426 * 3) + 427 = 1705
        /// </summary>
        public const int DaysPerFourYearCycle = (DaysPerStandardYear * 3) + DaysPerLeapYear;

        // ── Epoch anchor ─────────────────────────────────────────────────

        /// <summary>
        /// The in-universe year that UT=0 corresponds to.
        /// UT=0 = 1 Kelbris, KY 412.
        /// </summary>
        public const int EpochYear = 412;

        /// <summary>
        /// The month index (1-based) at UT=0.
        /// UT=0 = 1st day of month 1 (Kelbris).
        /// </summary>
        public const int EpochMonth = 1;

        /// <summary>
        /// The day within the epoch month at UT=0.
        /// </summary>
        public const int EpochDay = 1;

        // ── Month definitions ─────────────────────────────────────────────

        /// <summary>
        /// The 10 months of the Kerbal Kalendar, in order.
        /// Total: 426 days per standard year.
        /// </summary>
        public static readonly List<KalendarMonth> Months = new List<KalendarMonth>
        {
            new KalendarMonth(1,  "Kelbris", 45),   // Ancient/Tribal  — founding tribe, new year
            new KalendarMonth(2,  "Solum",   41),   // Celestial       — Kerbol climbing, days lengthening
            new KalendarMonth(3,  "Verna",   44),   // Seasonal        — green month, growing season
            new KalendarMonth(4,  "Dunara",  42),   // Celestial       — Duna visible, ancient association
            new KalendarMonth(5,  "Jebrin",  43),   // Historical      — legendary ancient Kerbal explorer
            new KalendarMonth(6,  "Muna",    45),   // Celestial       — midsummer, Mun prominent
            new KalendarMonth(7,  "Kethis",  40),   // Ancient/Tribal  — rival tribe to Kelbris
            new KalendarMonth(8,  "Harven",  44),   // Seasonal        — harvest, days shortening
            new KalendarMonth(9,  "Evaris",  43),   // Celestial       — Eve in evening sky, reflection
            new KalendarMonth(10, "Duskol",  39),   // Seasonal        — dark month, Kerbal winter close
        };

        /// <summary>
        /// The leap day. Occurs once every 4 years, belongs to no month.
        /// Culturally treated as a day outside of normal time.
        /// </summary>
        public const string LeapDayName = "Kolvari";

        // ── Helpers ───────────────────────────────────────────────────────

        /// <summary>
        /// Returns the KalendarMonth for a given 1-based month number.
        /// Returns null if out of range.
        /// </summary>
        public static KalendarMonth GetMonth(int monthNumber)
        {
            if (monthNumber < 1 || monthNumber > Months.Count)
                return null;
            return Months[monthNumber - 1];
        }

        /// <summary>
        /// Returns true if the given KY year is a leap year.
        /// Leap years occur when (year % 4 == 0), anchored to KY 1.
        /// </summary>
        public static bool IsLeapYear(int year)
        {
            return year % LeapYearCycle == 0;
        }

        /// <summary>
        /// Returns the total number of days in a given KY year,
        /// including Kolvari if it is a leap year.
        /// </summary>
        public static int DaysInYear(int year)
        {
            return IsLeapYear(year) ? DaysPerLeapYear : DaysPerStandardYear;
        }
    }
}
