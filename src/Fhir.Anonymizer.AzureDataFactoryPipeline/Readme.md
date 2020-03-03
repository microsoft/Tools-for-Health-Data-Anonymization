# How to run De-Identification task with Azure Data Factory Pipeline
This Azure Data Factory Pipeline contains two parts:
* The Azure Data Factory deployment script (*./scripts/DeployAzureDataFactoryPipeline.ps1*) builds the activity project, deploy the Azure Data Factory resource, and run the Azure Data Factory pipeline.
* The C# project performing the De-Identification task dispatched from Azure Data Factory

You can fill in your application config in *./scripts/AzureDataFactorySettings.json*, then open a powershell and type the following commands.
```
cd scripts
./DeployAzureDataFactoryPipeline.ps1
```