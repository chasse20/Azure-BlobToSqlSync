#r "Microsoft.Azure.EventGrid"
#r "Microsoft.WindowsAzure.Storage"

using Microsoft.Azure.EventGrid.Models;
using System.Data.SqlClient;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

/// <summary>Handler for Blob change grid events that will synchronize creations and deletions with an SQL database</summary>
/// <param name="tEvent">Blob storage event</param>
/// <param name="tLogger">Logger instance</param>
public static void Run( EventGridEvent tEvent, ILogger tLogger )
{
	// Log
	tLogger.LogInformation( tEvent.EventType );
	tLogger.LogInformation( tEvent.Subject );

	// Sync
	int tempContainerStart = Environment.GetEnvironmentVariable( "containers_path" ).Length;
	if ( tempContainerStart <= tEvent.Subject.Length )
	{
		// Grab container
		int tempContainerEnd = tEvent.Subject.IndexOf( "/blobs/" );
		if ( tempContainerEnd >= 0 )
		{
			CloudStorageAccount tempAccount = CloudStorageAccount.Parse( Environment.GetEnvironmentVariable( "AzureWebJobsStorage" ) );
			CloudBlobClient tempBlobClient = tempAccount.CreateCloudBlobClient();
			CloudBlobContainer tempContainer = tempBlobClient.GetContainerReference( tEvent.Subject.Substring( tempContainerStart, tempContainerEnd - tempContainerStart ) );
			CloudBlob tempBlob = tempContainer.GetBlobReference( tEvent.Subject.Substring( tempContainerEnd + 7 ) );

			string tempID;
			bool tempIsID = tempBlob.Metadata.TryGetValue( "sql_id", out tempID );
			bool tempIsCreated = tEvent.EventType == "Microsoft.Storage.BlobCreated";
			if ( ( tempIsCreated && !tempIsID ) || !tempIsCreated ) // don't override updated ones
			{
				// Connect to SQL
				using ( SqlConnection tempConnection = new SqlConnection( Environment.GetEnvironmentVariable( "sqldb_connection" ) ) )
				{
					tempConnection.Open();
					SqlCommand tempCommand;

					// Create
					if ( tempIsCreated )
					{
						// SQL
						tempCommand = new SqlCommand( "INSERT INTO docs VALUES (); SELECT CAST( SCOPE_IDENTITY() AS int );", tempConnection ); // MODIFY THIS STATEMENT
						string tempIndex = ( (int)tempCommand.ExecuteScalar() ).ToString();
						tLogger.LogInformation( "Created new doc row with id: " + tempIndex );

						// Blob metadata
						tempBlob.Metadata.Add( "sql_id", tempIndex );
						tempBlob.SetMetadataAsync();
					}
					// Delete
					else
					{
						tempCommand = new SqlCommand( "DELETE FROM docs WHERE id=" + tempID, tempConnection );
						tempCommand.ExecuteNonQuery();
						tLogger.LogInformation( "Removed doc row with id: " + tempID );
					}
				}
			}
		}
	}
}
