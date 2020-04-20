using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Fhir.Anonymizer.Core.Extensions;
using Fhir.Anonymizer.Core.Models;
using Fhir.Anonymizer.Core.Processors;
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

        public static ProcessResult RedactDateNode(ElementNode node, bool enablePartialDatesForRedact = false)
        {
            var processResult = new ProcessResult();
            if (!node.IsDateNode())
            {
                return processResult;
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

            processResult.AddProcessRecord(AnonymizationOperations.Redact, node);
            return processResult;
        }

        public static ProcessResult RedactDateTimeAndInstantNode(ElementNode node, bool enablePartialDatesForRedact = false)
        {
            var processResult = new ProcessResult();
            if (!node.IsDateTimeNode() && !node.IsInstantNode())
            {
                return processResult;
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

            processResult.AddProcessRecord(AnonymizationOperations.Redact, node);
            return processResult;
        }

        public static ProcessResult RedactAgeDecimalNode(ElementNode node, bool enablePartialAgesForRedact = false)
        {
            var processResult = new ProcessResult();
            if (!node.IsAgeDecimalNode())
            {
                return processResult;
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

            processResult.AddProcessRecord(AnonymizationOperations.Redact, node);

            return processResult;
        }

        public static ProcessResult ShiftDateNode(ElementNode node, string dateShiftKey, string dateShiftKeyPrefix, bool enablePartialDatesForRedact = false)
        {
            var processResult = new ProcessResult();
            if (!node.IsDateNode())
            {
                return processResult;
            }

            var matchedGroups = s_dateRegex.Match(node.Value.ToString()).Groups;
            if (matchedGroups[s_dayIndex].Captures.Any() && !IndicateAgeOverThreshold(matchedGroups))
            {
                int offset = GetDateShiftValue(node, dateShiftKey, dateShiftKeyPrefix);
                node.Value = DateTime.Parse(node.Value.ToString()).AddDays(offset).ToString(s_dateFormat);
                processResult.AddProcessRecord(AnonymizationOperations.Perturb, node);
            }
            else
            {
                processResult = RedactDateNode(node, enablePartialDatesForRedact);
            }

            return processResult;
        }

        public static ProcessResult ShiftDateTimeAndInstantNode(ElementNode node, string dateShiftKey, string dateShiftKeyPrefix, bool enablePartialDatesForRedact = false)
        {
            var processResult = new ProcessResult();
            if (!node.IsDateTimeNode() && !node.IsInstantNode())
            {
                return processResult;
            }

            var matchedGroups = s_dateTimeRegex.Match(node.Value.ToString()).Groups;
            if (matchedGroups[s_dayIndex].Captures.Any() && !IndicateAgeOverThreshold(matchedGroups))
            {
                int offset = GetDateShiftValue(node, dateShiftKey, dateShiftKeyPrefix);
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
                processResult.AddProcessRecord(AnonymizationOperations.Perturb, node);
            }
            else
            {
                processResult = RedactDateTimeAndInstantNode(node, enablePartialDatesForRedact);
            }

            return processResult;
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

        private static int GetDateShiftValue(ElementNode node, string dateShiftKey, string dateShiftKeyPrefix)
        {
            if (string.IsNullOrEmpty(dateShiftKeyPrefix))
            {
                dateShiftKeyPrefix = TryGetResourceId(node);
            }

            int offset = 0;
            var bytes = Encoding.UTF8.GetBytes(dateShiftKeyPrefix + dateShiftKey);
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
