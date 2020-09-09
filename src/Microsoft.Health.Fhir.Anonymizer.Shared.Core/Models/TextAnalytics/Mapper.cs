using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations.TextAnalytics;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Models.TextAnalytics
{
    public class Mapper
    {
        public static string CategoryMapping(MappingConfiguration mappingConfig, string category, string subcategory)
        {
            if (TryMatchCategory(mappingConfig.Location, category, subcategory)) return "LOCATION";
            if (TryMatchCategory(mappingConfig.Contact, category, subcategory)) return "CONTACT";
            if (TryMatchCategory(mappingConfig.Name, category, subcategory)) return "NAME";
            if (TryMatchCategory(mappingConfig.Profession, category, subcategory)) return "PROFESSION";
            if (TryMatchCategory(mappingConfig.Age, category, subcategory)) return "AGE";
            if (TryMatchCategory(mappingConfig.Date, category, subcategory)) return "DATE";
            if (TryMatchCategory(mappingConfig.Id, category, subcategory)) return "ID";
            return string.Empty;
        }

        public static bool TryMatchCategory(List<BaseCategory> baseCategories, string category, string subcategory)
        {
            foreach (BaseCategory baseCategory in baseCategories)
            {
                if (Regex.IsMatch(category, baseCategory.Category) && Regex.IsMatch(subcategory, baseCategory.Subcategory))
                {
                    return true;
                }
            }
            return false;
        }

        public static string SubCategoryMapping(string category, string subcategory)
        {
            return string.Empty;
        }
    }
}
