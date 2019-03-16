# Azure-BlobToSqlSync
Function App designed to synchronize Blob uploads/deletions to a SQL database in Microsoft Azure.

## APPLICATION SETTINGS VARIABLES
- **AzureWebJobsStorage**: Connection string for the Cloud Storage Account (usually set for you)
- **containers_path**: Prefix path of the Blob containers path (e.g., /blobServices/default/containers/)
- **sqldb_connection**: Connection string for the SQL database
