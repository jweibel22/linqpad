<Query Kind="Program">
  <Reference Relative="..\..\..\Identity\Source\Identity.Fetcher\Identity.OAuth\bin\Identity.Infrastructure.dll">C:\git\Identity\Source\Identity.Fetcher\Identity.OAuth\bin\Identity.Infrastructure.dll</Reference>
  <NuGetReference>Dapper</NuGetReference>
  <NuGetReference>WindowsAzure.Storage</NuGetReference>
  <Namespace>Dapper</Namespace>
  <Namespace>Identity.Infrastructure.Helpers</Namespace>
  <Namespace>Microsoft.WindowsAzure.Storage</Namespace>
</Query>

//void InsertSubReddits(SqlConnection con, IEnumerable<string> subReddits)
//{
//	var table = new DataTable();
//	table.TableName = "SubReddits";
//	table.Columns.Add(new DataColumn("Name", typeof(string)));
//
//	var rows = subReddits.Select(x =>
//	{
//		var row = table.NewRow();
//		row["Name"] = x;
//		return row;
//	});
//
//	foreach (var row in rows)
//	{
//		table.Rows.Add(row);
//	}
//
//	BulkCopy.Copy(con, table);
//}

public static bool IsASCII(string value)
{
	return Encoding.UTF8.GetByteCount(value) == value.Length;
}

public static bool SubRedditNameIsOk(string subreddit)
{
	return !String.IsNullOrEmpty(subreddit) && subreddit.Length > 2 && subreddit.Length < 100 && IsASCII(subreddit);
}

void InsertSubRedditOccurences(SqlConnection con, long entityId, IEnumerable<Tuple<long, int>> subRedditCounts)
{
	var table = new DataTable();
	table.TableName = "EntitiesInSubReddits";
	table.Columns.Add(new DataColumn("EntityId", typeof(long)));
	table.Columns.Add(new DataColumn("SubRedditId", typeof(long)));
	table.Columns.Add(new DataColumn("Occurences", typeof(int)));

	var rows = subRedditCounts.Select(x =>
	{
		var row = table.NewRow();
		row["EntityId"] = entityId;
		row["SubRedditId"] = x.Item1;
		row["Occurences"] = x.Item2;
		return row;
	});

	foreach (var row in rows)
	{
		table.Rows.Add(row);
	}

	BulkCopy.Copy(con, table);
}

class SubRedditCache
{	
	private readonly SqlConnection con;
	
	public SubRedditCache(SqlConnection con)
	{
		this.con = con;
		NameToId = new Dictionary<string, long>();
	}
	
	public IDictionary<string, long> NameToId { get; private set; }
	
	public void Update()
	{
		var newStuff = con.Query("select id, name from SubReddits where Id > @Id", new { Id = NameToId.Count == 0 ? -1 : NameToId.Values.Max() })
							.ToDictionary(row => (string)row.name, row => (long)row.id);
							
		NameToId = NameToId.Union(newStuff).ToDictionary(pair => pair.Key, pair => pair.Value);
	}	
}

void ProcessAllEntities()
{
	CloudStorageAccount cloudStorageAccount = CloudStorageAccount.DevelopmentStorageAccount;
	if (!CloudStorageAccount.TryParse("DefaultEndpointsProtocol=https;AccountName=jweibel;AccountKey=mGw3mQxyZ9F2NHW8WHAK2qtwapm5PxvpfpUJlXNMaE0mXdzhV43QwTG3aUQGrtoSnEpJMSXap2dzi1wmiMQv/w==", out cloudStorageAccount))
	{
		throw new Exception("aaaa");
	}

	var connectionString = @"Data Source=v5kf9wt87u.database.windows.net;Initial Catalog=Identity;Persist Security Info=True;User ID=jweibel;Password=MiInPw01;Connect Timeout=30";
	using (SqlConnection con = new SqlConnection(connectionString))
	{
		con.Open();

		var subReddits = new SubRedditCache(con);
		subReddits.Update();
		var entities = con.Query("select Id, Name from NLPEntities where Processed = 0 AND (CommonWord = 0 OR Noun = 1)", new { }).ToDictionary(row => (long)row.Id, row => (string)row.Name);

		foreach (var kv in entities)
		{
			Console.WriteLine("Processing " + kv.Value);
			var counter = new Identity.Infrastructure.Services.SubRedditOccurences(cloudStorageAccount);
			var occurences = counter
								.Search("\"" + kv.Value + "\"")
								.ToDictionary(x => x.Key, x => x.Value);

			var subRedditOccurences = occurences
										.Where(x => subReddits.NameToId.ContainsKey(x.Key))
										.Select(o => new Tuple<long, int>(subReddits.NameToId[o.Key], (int)o.Value));
			InsertSubRedditOccurences(con, kv.Key, subRedditOccurences);
		}
	}

}



void Main()
{
	ProcessAllEntities();
}

// Define other methods and classes here