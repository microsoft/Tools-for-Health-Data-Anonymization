using System;

namespace Fhir.Anonymizer.Core.Models
{
    public class AnonymizationSummary
    {
        public bool IsRedacted { get; set; }

        public bool IsAbstracted { get; set; }

        public bool IsPerturbed { get; set; }

        public void UpdateIsRedacted(string originalValue, string currentValue)
        {
            if (!string.Equals(originalValue, currentValue, StringComparison.InvariantCulture))
            {
                IsRedacted = true;
            }
        }

        public void UpdateIsAbstracted(string originalValue, string currentValue)
        {
            if (!string.Equals(originalValue, currentValue, StringComparison.InvariantCulture))
            {
                IsAbstracted = true;
            }
        }

        public void UpdateIsPerturbed(string originalValue, string currentValue)
        {
            if (!string.Equals(originalValue, currentValue, StringComparison.InvariantCulture))
            {
                IsPerturbed = true;
            }
        }

        public void UpdateSummary(AnonymizationSummary summary)
        {
            IsRedacted = summary.IsRedacted ? summary.IsRedacted : IsRedacted;
            IsAbstracted = summary.IsAbstracted ? summary.IsAbstracted : IsAbstracted;
            IsPerturbed = summary.IsPerturbed ? summary.IsPerturbed : IsPerturbed;
        }
    }
}
