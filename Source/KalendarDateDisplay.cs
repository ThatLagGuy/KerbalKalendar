using System;
using UnityEngine;

namespace KerbalKalendar
{
    /// <summary>
    /// KalendarDateDisplay — replaces KSP's stock UT date string with the
    /// Kerbal Kalendar format across all applicable scenes.
    ///
    /// IMPORTANT: The stock formatter is cached BEFORE we replace it.
    /// All time-delta methods (PrintTime, PrintTimeStamp, etc.) delegate to
    /// the cached stock formatter — NOT back through KSPUtil — to avoid
    /// infinite recursion. KSPUtil.PrintTime() routes through dateTimeFormatter,
    /// so calling it from inside our formatter causes a stack overflow.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class KalendarDateDisplay : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            // Cache the stock formatter FIRST, then replace it
            IDateTimeFormatter stockFormatter = KSPUtil.dateTimeFormatter;
            KSPUtil.dateTimeFormatter = new KalendarDateTimeFormatter(stockFormatter);

            Debug.Log("[KerbalKalendar] Stock date display replaced with Kerbal Kalendar.");
        }
    }

    /// <summary>
    /// IDateTimeFormatter implementation that outputs Kerbal Kalendar dates.
    /// Time-delta methods delegate to the cached stock formatter to avoid recursion.
    /// </summary>
    public class KalendarDateTimeFormatter : IDateTimeFormatter
    {
        // The original stock formatter — used for all time-delta methods
        private readonly IDateTimeFormatter _stock;

        public KalendarDateTimeFormatter(IDateTimeFormatter stockFormatter)
        {
            _stock = stockFormatter;
        }

        // ── Date formatting ───────────────────────────────────────────────

        /// <summary>
        /// Full date string — shown in the top bar UT clock.
        /// Example: "23 Harven KY 415"
        /// </summary>
        public string PrintDate(double ut, bool includeTime, bool includeSeconds)
        {
            KalendarDate date = KerbalKalendarConverter.UTToDate(ut);
            string dateStr    = date.ToShortString();

            if (!includeTime)
                return dateStr;

            string timeStr = KerbalKalendarConverter.UTToTimeOfDay(ut);

            return includeSeconds
                ? $"{dateStr}  |  {timeStr}"
                : $"{dateStr}  |  {timeStr.Substring(0, 5)}";
        }

        /// <summary>
        /// Compact date — used in some contract and CommNet contexts.
        /// Example: "Harven KY 415"
        /// </summary>
        public string PrintDateCompact(double ut, bool includeTime, bool includeSeconds)
        {
            KalendarDate date = KerbalKalendarConverter.UTToDate(ut);

            string dateStr = date.IsKolvari
                ? $"Kolvari KY {date.Year}"
                : $"{date.Month.Name} KY {date.Year}";

            if (!includeTime)
                return dateStr;

            string timeStr = KerbalKalendarConverter.UTToTimeOfDay(ut);

            return includeSeconds
                ? $"{dateStr} {timeStr}"
                : $"{dateStr} {timeStr.Substring(0, 5)}";
        }

        /// <summary>
        /// New-style date — KSP 1.8+ contexts.
        /// </summary>
        public string PrintDateNew(double ut, bool includeTime)
        {
            return PrintDate(ut, includeTime, includeTime);
        }

        /// <summary>
        /// New compact date — KSP 1.8+ contexts.
        /// </summary>
        public string PrintDateCompactNew(double ut, bool includeTime)
        {
            return PrintDateCompact(ut, includeTime, includeTime);
        }

        // ── Time delta formatting ─────────────────────────────────────────
        // All of these delegate to _stock (the cached original formatter).
        // NEVER call KSPUtil.PrintTime/PrintTimeStamp etc. here — those route
        // back through KSPUtil.dateTimeFormatter which is now US, causing
        // infinite recursion and a silent stack overflow crash.

        public string PrintTime(double seconds, int valuesOfInterest, bool explicitPositive)
        {
            return _stock.PrintTime(seconds, valuesOfInterest, explicitPositive);
        }

        public string PrintTime(double seconds, int valuesOfInterest, bool explicitPositive, bool logicalPositive)
        {
            return _stock.PrintTime(seconds, valuesOfInterest, explicitPositive);
        }

        public string PrintTimeCompact(double seconds, bool explicitPositive)
        {
            return _stock.PrintTimeCompact(seconds, explicitPositive);
        }

        public string PrintTimeStamp(double seconds, bool days, bool years)
        {
            return _stock.PrintTimeStamp(seconds, days, years);
        }

        public string PrintTimeStampCompact(double seconds, bool days, bool years)
        {
            return _stock.PrintTimeStampCompact(seconds, days, years);
        }

        public string PrintTimeLong(double seconds)
        {
            return _stock.PrintTimeLong(seconds);
        }

        public string PrintDateDelta(double time, bool includeTime, bool includeSeconds, bool useAbs)
        {
            return _stock.PrintDateDelta(time, includeTime, includeSeconds, useAbs);
        }

        public string PrintDateDeltaCompact(double time, bool includeTime, bool includeSeconds, bool useAbs)
        {
            return _stock.PrintDateDeltaCompact(time, includeTime, includeSeconds, useAbs);
        }

        public string PrintDateDeltaCompact(double time, bool includeTime, bool includeSeconds, bool useAbs, int interestedValues)
        {
            return _stock.PrintDateDeltaCompact(time, includeTime, includeSeconds, useAbs, interestedValues);
        }

        // ── Time unit accessors ───────────────────────────────────────────

        public int Minute => 60;
        public int Hour   => 3600;
        public int Day    => (int)KalendarData.SecondsPerDay;
        public int Year   => KalendarData.DaysPerStandardYear * (int)KalendarData.SecondsPerDay;
    }
}
