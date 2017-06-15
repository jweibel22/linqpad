<Query Kind="Program">
  <Reference Relative="..\..\Identity\Source\Identity.Fetcher\Identity.OAuth\bin\Identity.Infrastructure.dll">C:\git\Identity\Source\Identity.Fetcher\Identity.OAuth\bin\Identity.Infrastructure.dll</Reference>
  <NuGetReference>Google.Apis.Auth</NuGetReference>
  <NuGetReference Prerelease="true">Google.Cloud.BigQuery.V2</NuGetReference>
  <Namespace>Google.Apis.Auth.OAuth2</Namespace>
  <Namespace>Google.Apis.Bigquery.v2</Namespace>
  <Namespace>Google.Apis.Bigquery.v2.Data</Namespace>
  <Namespace>Google.Cloud.BigQuery.V2</Namespace>
</Query>

void Query(BigQueryClient client)
{
	//	var table = client.GetTable("bigquery-public-data", "samples", "shakespeare");
	var table = client.GetTable("fh-bigquery", "reddit_posts", "full_corpus_201509");
	//	string query = $@"SELECT corpus AS title, COUNT(*) AS unique_words FROM `{table.FullyQualifiedId}` GROUP BY title ORDER BY unique_words DESC LIMIT 42";

	var sql = $@"SELECT subreddit, count(*) as cnt
FROM `{table.FullyQualifiedId}`
where title like '%html%'
group by subreddit
order by cnt desc";


	var result = client.ExecuteQuery(sql);

	Console.Write("\nQuery Results:\n------------\n");
	foreach (var row in result.GetRows())
	{
		Console.WriteLine($"{row["subreddit"]}: {row["cnt"]}");
	}
}

private TableSchema CreateTableSchema()
{
	TableSchemaBuilder b = new TableSchemaBuilder();
	var id = new TableFieldSchema
	{
		Mode = "REQUIRED",
		Name = "Id",
		Type = "INTEGER"
	};

	var name = new TableFieldSchema
	{
		Mode = "REQUIRED",
		Name = "Name",
		Type = "STRING"
	};

	var type = new TableFieldSchema
	{
		Mode = "REQUIRED",
		Name = "Type",
		Type = "STRING"
	};

	var processed = new TableFieldSchema
	{
		Mode = "REQUIRED",
		Name = "Processed",
		Type = "BOOLEAN"
	};

	b.Add(id);
	b.Add(name);
	b.Add(type);
	b.Add(processed);
	
	return b.Build();
}


public void UploadJsonFromFile(string projectId, string datasetId, string tableId,
	string fileName, BigQueryClient client)
{
	using (FileStream stream = File.Open(fileName, FileMode.Open))
	{
		//client.DeleteTable(projectId, datasetId, tableId);

		UploadCsvOptions options = new UploadCsvOptions();
		options.FieldDelimiter = ";";
		options.SkipLeadingRows = 1;
			
		var job = client.UploadCsv(datasetId, tableId, CreateTableSchema(), stream, options);
		job.PollUntilCompleted();

	}
}


void Main()

{
	GoogleCredential credential = GoogleCredential.GetApplicationDefaultAsync().Result;
	string projectId = "identity-jweibel-88507";

	BigQueryClient client = BigQueryClient.Create(projectId, credential);



	UploadJsonFromFile(projectId, "Identity", "NLPEntities2", @"c:\transit\nlpentities_export3.csv", client);



}


// Define other methods and classes here