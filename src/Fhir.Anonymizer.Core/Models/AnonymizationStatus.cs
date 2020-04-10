using System;

namespace Fhir.Anonymizer.Core.Models
{
    public class AnonymizationStatus
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

        public void UpdateStatus(AnonymizationStatus status)
        {
            IsRedacted = status.IsRedacted ? status.IsRedacted : IsRedacted;
            IsAbstracted = status.IsAbstracted ? status.IsAbstracted : IsAbstracted;
            IsPerturbed = status.IsPerturbed ? status.IsPerturbed : IsPerturbed;
        }
    }
}
