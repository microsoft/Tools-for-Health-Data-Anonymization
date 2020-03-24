using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Fhir.Anonymizer.Core.Extensions;
using Hl7.Fhir.ElementModel;

namespace Fhir.Anonymizer.Core.Utility
{
    public class DateTimeUtility
    {
        private static readonly int s_yearIndex = 1;
        private static readonly int s_monthIndex = 5;
        private static readonly int s_dayIndex = 7;
        private static readonly int s_timeIndex = 8;
        private static readonly int s_dateShiftSeed = 131;
        private static readonly int s_dateShiftRange = 50;
        private static readonly int s_ageThreshold = 89;
        private static readonly string s_dateFormat = "yyyy-MM-dd";
        // The regex of date is defined in: https://www.hl7.org/fhir/datatypes.html#date
        private static readonly Regex s_dateRegex = new Regex(@"([0-9]([0-9]([0-9][1-9]|[1-9]0)|[1-9]00)|[1-9]000)(-(0[1-9]|1[0-2])(-(0[1-9]|[1-2][0-9]|3[0-1]))?)?",
            RegexOptions.IgnorePatternWhitespace);
        // The regex of dateTime is defined in: https://www.hl7.org/fhir/datatypes.html#datetime
        private static readonly Regex s_dateTimeRegex = new Regex(@"([0-9]([0-9]([0-9][1-9]|[1-9]0)|[1-9]00)|[1-9]000)(-(0[1-9]|1[0-2])(-(0[1-9]|[1-2][0-9]|3[0-1])(T([01][0-9]|2[0-3]):[0-5][0-9]:([0-5][0-9]|60)(\.[0-9]+)?(Z|(\+|-)((0[0-9]|1[0-3]):[0-5][0-9]|14:00)))?)?)?",
            RegexOptions.IgnorePatternWhitespace);
        // The regex of time is defined in: https://www.hl7.org/fhir/datatypes.html#time    
        private static readonly Regex s_timeRegex = new Regex(@"([01][0-9]|2[0-3]):[0-5][0-9]:([0-5][0-9]|60)(\.[0-9]+)?");

        public static void RedactDateNode(ElementNode node, bool enablePartialDatesForRedact = false)
        {
            if (!node.IsDateNode())
            {
                return;
            }

            if (enablePartialDatesForRedact)
            {
                var matchedGroups = s_dateRegex.Match(node.Value.ToString()).Groups;
                if (matchedGroups[s_yearIndex].Captures.Any())
                {
                    string yearOfDate = matchedGroups[s_yearIndex].Value;
                    node.Value = IndicateAgeOverThreshold(matchedGroups) ? null : yearOfDate;
                }
            }
            else
            {
                node.Value = null;
            }
        }

        public static void RedactDateTimeAndInstantNode(ElementNode node, bool enablePartialDatesForRedact = false)
        {
            if (!node.IsDateTimeNode() && !node.IsInstantNode())
            {
                return;
            }

            if (enablePartialDatesForRedact)
            {
                var matchedGroups = s_dateTimeRegex.Match(node.Value.ToString()).Groups;
                if (matchedGroups[s_yearIndex].Captures.Any())
                {
                    string yearOfDateTime = matchedGroups[s_yearIndex].Value;
                    node.Value = IndicateAgeOverThreshold(matchedGroups) ? null : yearOfDateTime;
                }
            }
            else
            {
                node.Value = null;
            }
        }

        public static void RedactAgeDecimalNode(ElementNode node, bool enablePartialAgesForRedact = false)
        {
            if (!node.IsAgeDecimalNode())
            {
                return;
            }

            if (enablePartialAgesForRedact)
            {
                if (int.Parse(node.Value.ToString()) > s_ageThreshold)
                {
                    node.Value = null;
                }
            }
            else
            {
                node.Value = null;
            }
        }

        public static void ShiftDateNode(ElementNode node, string dateShiftKey, bool enablePartialDatesForRedact = false)
        {
            if (!node.IsDateNode())
            {
                return;
            }

            var matchedGroups = s_dateRegex.Match(node.Value.ToString()).Groups;
            if (matchedGroups[s_dayIndex].Captures.Any() && !IndicateAgeOverThreshold(matchedGroups))
            {
                int offset = GetDateShiftValue(node, dateShiftKey);
                node.Value = DateTime.Parse(node.Value.ToString()).AddDays(offset).ToString(s_dateFormat);
            }
            else
            {
                RedactDateNode(node, enablePartialDatesForRedact);
            }
        }

        public static void ShiftDateTimeAndInstantNode(ElementNode node, string dateShiftKey, bool enablePartialDatesForRedact = false)
        {
            if (!node.IsDateTimeNode() && !node.IsInstantNode())
            {
                return;
            }

            var matchedGroups = s_dateTimeRegex.Match(node.Value.ToString()).Groups;
            if (matchedGroups[s_dayIndex].Captures.Any() && !IndicateAgeOverThreshold(matchedGroups))
            {
                int offset = GetDateShiftValue(node, dateShiftKey);
                if (matchedGroups[s_timeIndex].Captures.Any())
                {
                    var newDate = DateTimeOffset.Parse(node.Value.ToString()).AddDays(offset).ToString(s_dateFormat);
                    var timestamp = matchedGroups[s_timeIndex].Value;
                    var timeMatch = s_timeRegex.Match(timestamp);
                    if (timeMatch.Captures.Any())
                    { 
                        string time = timeMatch.Captures.First().Value;
                        string newTime = Regex.Replace(time, @"\d", "0");
                        timestamp = timestamp.Replace(time, newTime);
                    }
                    node.Value = $"{newDate}{timestamp}";
                }
                else
                {
                    node.Value = DateTime.Parse(node.Value.ToString()).AddDays(offset).ToString(s_dateFormat);
                }
            }
            else
            {
                RedactDateTimeAndInstantNode(node, enablePartialDatesForRedact);
            }
        }

        private static bool IndicateAgeOverThreshold(GroupCollection groups)
        {
            int year = int.Parse(groups[s_yearIndex].Value);
            int month = groups[s_monthIndex].Captures.Any() ? int.Parse(groups[s_monthIndex].Value) : 1;
            int day = groups[s_dayIndex].Captures.Any() ? int.Parse(groups[s_dayIndex].Value) : 1;
            int age = DateTime.Now.Year - year -
                (DateTime.Now.Month < month || (DateTime.Now.Month == month && DateTime.Now.Day < day) ? 1 : 0);

            return age > s_ageThreshold; 
        }

        private static int GetDateShiftValue(ElementNode node, string dateShiftKey)
        {
            string resourceId = TryGetResourceId(node);
            int offset = 0;

            var bytes = Encoding.UTF8.GetBytes(resourceId + dateShiftKey);
            foreach (byte b in bytes)
            {
                offset = (offset * s_dateShiftSeed + (int)b) % (2 * s_dateShiftRange + 1);
            }

            offset -= s_dateShiftRange;

            return offset;
        }

        private static string TryGetResourceId(ElementNode node)
        {
            while (node.Parent != null)
            {
                node = node.Parent;
            }

            return node.GetNodeId();
        }
    }
}
