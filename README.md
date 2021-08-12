# Tools for Health Data Anonymization

[![Build Status](https://microsofthealthoss.visualstudio.com/FhirAnonymizer/_apis/build/status/CI%20Build?branchName=master)](https://microsofthealthoss.visualstudio.com/FhirAnonymizer/_build/latest?definitionId=23&branchName=master)

---
**Privacy Notice and Consent**

This project provides you the scripts and command line tools for your own use. It **does NOT** and **cannot** access, use, collect, or manage any of your data, including any personal or health-related data. You must bring your own data, and be 100% responsible for using our tools to work with your own data.

---

**Tools for Health Data Anonymization** is an open-source project that helps anonymize healthcare data, on-premises or in the cloud, for secondary usage such as research, public health, and more. The project first released the anonymization of [FHIR](https://www.hl7.org/fhir/) data to open source on Friday, March 6th, 2020. Currently, it supports both **FHIR data anonymization** and **DICOM data anonymization**.

* For information on FHIR data anonymization, please check out the [FHIR anonymization documentation](docs/FHIR-anonymization.md).
* For information on DICOM data anonymization, please check out the [DICOM anonymization documentation](docs/DICOM-anonymization.md).

The anonymization core engine uses a configuration file specifying different parameters as well as anonymization methods for different data-elements and datatypes. The repo contains a sample configuration file, which is based on the [HIPAA Safe Harbor](https://www.hhs.gov/hipaa/for-professionals/privacy/special-topics/anonymization/index.html#safeharborguidance) method. You can modify or create your own configuration file as needed.

This open source project is fully backed by the Microsoft Healthcare team, but we know that this project will only get better with your feedback and contributions. We are leading the development of this code base, and test builds and deployments daily.

FHIR® is the registered trademark of HL7 and is used with the permission of HL7. Use of the FHIR trademark does not constitute endorsement of this product by HL7.

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

FHIR® is the registered trademark of HL7 and is used with the permission of HL7.
