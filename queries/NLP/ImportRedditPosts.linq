<Query Kind="Program">
  <Reference Relative="..\..\..\Identity\Source\Identity.Fetcher\Identity.OAuth\bin\Identity.Infrastructure.dll">C:\git\Identity\Source\Identity.Fetcher\Identity.OAuth\bin\Identity.Infrastructure.dll</Reference>
  <NuGetReference>Dapper</NuGetReference>
  <NuGetReference>Lucene.Net</NuGetReference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>Dapper</Namespace>
  <Namespace>Identity.Infrastructure.Helpers</Namespace>
  <Namespace>Lucene.Net</Namespace>
  <Namespace>Lucene.Net.Analysis</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>Newtonsoft.Json.Linq</Namespace>
</Query>

class X
{
	public string title { get; set; }
	
	public string url { get; set; }
	
	public string subreddit { get; set; }
	
	public long created_utc { get; set; }
	
	public string author { get; set; }
}

string Encode(string s)
{	
	return s;
}

void Processing(int i)
{
	if (i % 1000 == 0)
	{
		if (i % 200000 == 0)
		{
			Console.WriteLine(".");
		}
		else
		{
			Console.Write(".");
		}
	}
}


IEnumerable<X> FetchAll(params string[] files)
{
	foreach (var file in files)
	{
		foreach (var x in Fetch(file))
		{
			yield return x;
		}
	}
}

IEnumerable<X> Fetch(string filename)
{
	using (StreamReader sr = new StreamReader(filename))
	{	
		{
			int i = 0;

			while (sr.Peek() >= 0) 
			{
				i++;
				Processing(i);
				var line = sr.ReadLine();
				yield return JsonConvert.DeserializeObject<X>(line);				
			}
		}
	}

}

DateTimeOffset UnixTimeStampToDateTime(double unixTimeStamp)
{
	// Unix timestamp is seconds past epoch
	System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
	dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
	return dtDateTime;
}

IEnumerable<Tuple<long, string>> Query(SqlConnection con, string sql, string keyName, string valueName)
{
		var cmd = new SqlCommand(sql, con);

		using (var reader = cmd.ExecuteReader())
		{
			while (reader.Read())
			{
				yield return new Tuple<long, string>((long)reader[keyName], (string)reader[valueName]);
			}
		}
}



void UpdatePostCounts()
{
	var connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=Reddit;Trusted_Connection=True;";
	using (SqlConnection con = new SqlConnection(connectionString))
	{
		con.Open();

		var sql = @"UPDATE sr SET sr.PostCount = t.Cnt
					FROM SubReddits AS sr
    				INNER JOIN(select SubRedditId, count(*) as Cnt from Posts group by SubRedditId) as t on t.SubRedditId = sr.Id";
		
		con.Execute(sql);		
	}
}

void InsertUsersAndSubReddits(IEnumerable<X> xs)
{
	var connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=Reddit;Trusted_Connection=True;";
	using (SqlConnection con = new SqlConnection(connectionString))
	{
		con.Open();
				
		var existing = Query(con, "select Id, Name from [Users]","Id","Name").ToDictionary(t => t.Item2, t => true);		
		Dictionary<string, bool> newUsers = new Dictionary<string, bool>(100000);

		var existingSubReddits = Query(con, "select Id, Name from SubReddits", "Id", "Name").ToDictionary(t => t.Item2, t => true);
		Dictionary<string, bool> newSubRedits = new Dictionary<string, bool>(100000);

		foreach (var d in xs)
		{
			if (d.subreddit != null && d.author != null && !existing.ContainsKey(d.author))
			{
				newUsers[d.author] = true;
			}

			if (d.subreddit != null && !existingSubReddits.ContainsKey(d.subreddit))
			{
				newSubRedits[d.subreddit] = true;
			}
		}

		Console.WriteLine("Found {0} users", newUsers.Count);
		Console.WriteLine("Found {0} sub reddits", newSubRedits.Count);

		var table = new DataTable();
		table.TableName = "Users";
		table.Columns.Add(new DataColumn("Name"));

		foreach (var sr in newUsers.Keys)
		{
			var row = table.NewRow();
			row["name"] = sr;
			table.Rows.Add(row);
		}

		BulkCopy.Copy(con, table);


		table = new DataTable();
		table.TableName = "SubReddits";
		table.Columns.Add(new DataColumn("Name"));

		foreach (var sr in newSubRedits.Keys)
		{
			var row = table.NewRow();
			row["name"] = sr;
			table.Rows.Add(row);
		}

		BulkCopy.Copy(con, table);
	}

}


void InsertPosts(IEnumerable<X> xs)
{
	var connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=Reddit;Trusted_Connection=True;";

	Dictionary<string, long> userIds = new Dictionary<string, long>();
	Dictionary<string, long> subredditIds = new Dictionary<string, long>();

	using (SqlConnection con = new SqlConnection(connectionString))
	{
		con.Open();
		var cmd = new SqlCommand("select Id,Name from SubReddits", con);
		using (SqlDataReader reader = cmd.ExecuteReader())
		{
			while (reader.Read())
			{
				subredditIds.Add((string)reader["Name"], (long)reader["Id"]);
			}
		}

		cmd = new SqlCommand("select Id,Name from [Users]", con);
		using (SqlDataReader reader = cmd.ExecuteReader())
		{
			while (reader.Read())
			{
				userIds.Add((string)reader["Name"], (long)reader["Id"]);
			}
		}


		var table = new DataTable();
		table.TableName = "Posts";
		table.Columns.Add(new DataColumn("Title"));
		table.Columns.Add(new DataColumn("Url"));
		table.Columns.Add(new DataColumn("SubRedditId"));
		table.Columns.Add(new DataColumn("UserId"));
		table.Columns.Add(new DataColumn("Created"));

		int idx = 0;

		foreach (var d in xs)
		{		
			if (d.subreddit != null)
			{
				var row = table.NewRow();
				row["Title"] = (d.title.Length > 400) ? d.title.Substring(0,400) : d.title;
				row["Url"] = d.url;
				row["SubRedditId"] = subredditIds[d.subreddit];
				row["UserId"] = d.author != null ? userIds[d.author] : 0;
				row["Created"] = UnixTimeStampToDateTime(d.created_utc);
				table.Rows.Add(row);
			}

			if (idx++ % 100000 == 0)
			{
				Console.WriteLine("Inserting rows");
				BulkCopy.Copy(con, table);
				table.Clear();
			}
		}

		BulkCopy.Copy(con, table);
		table.Clear();
	}
}

string IgnoreAll(string s, string[] ignored)
{
	var result = s;
	foreach (var x in ignored)
	{
		result = result.Replace(x," ");
	}
	return result;
}

IEnumerable<string> GetWords(string title, IDictionary<string, bool> commonWords)
{
	//TODO: ignore all emojis. like: \ud83d\ude18
	var ignoredCharacters = new[] { ":", ",", ";", "»", "«", "'", "?", "!", "(", ")", "[", "]", "{", "}", "\"", "'", "#", "*", "~", "`", "…", "|", "“","”" };
	var ss = IgnoreAll(title.Trim(), ignoredCharacters);
	var words = ss.Replace("-", " ").Replace("–", " ").Replace("_", " ").Replace(".", " ")
			.ToLower().Split(' ').Where(word => !String.IsNullOrEmpty(word) && word.Length > 2 && word.Length <= 100 && !commonWords.ContainsKey(word));
			
	return words;
}

void InsertWords(IEnumerable<string> titles)
{
	var connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=Reddit;Trusted_Connection=True;";
	var commonWordsFile = "commonwords-english.txt";
	var commonWords = System.IO.File.ReadAllLines(@"C:\git\Identity\Source\Identity.Fetcher\Identity.Infrastructure\" + commonWordsFile).Select(w => w.Trim().ToLower()).Take(1000).Distinct().ToDictionary(w => w, w => true);
	Dictionary<string, bool> seen = new Dictionary<string, bool>(100000);

	foreach (var t in titles)
	{
		var words = GetWords(t, commonWords);
		foreach (var word in words)
		{
			seen[word] = true;
		}	
	}

	Console.WriteLine("Found {0} items", seen.Count);

	var table = new DataTable();
	table.TableName = "Words";
	table.Columns.Add(new DataColumn("Contents"));
	var idx = 0;

	using (SqlConnection con = new SqlConnection(connectionString))
	{
		con.Open();
		
		foreach (var sr in seen.Keys)
		{
			var row = table.NewRow();
			row["Contents"] = sr;
			table.Rows.Add(row);

			if (++idx % 1000000 == 0)
			{
				BulkCopy.Copy(con, table);
				table.Clear();
			}
		}

		BulkCopy.Copy(con, table);
		table.Clear();
	}
}

void BC(DataTable table)
{
	var connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=Reddit;Trusted_Connection=True;";
	using (SqlConnection con = new SqlConnection(connectionString))
	{
		con.Open();
		BulkCopy.Copy(con, table);
	}
}

void ConnectPostsAndWords()
{
	var connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=Reddit;Trusted_Connection=True;";
	
	var commonWordsFile = "commonwords-english.txt";
	var commonWords = System.IO.File.ReadAllLines(@"C:\git\Identity\Source\Identity.Fetcher\Identity.Infrastructure\" + commonWordsFile).Select(w => w.Trim().ToLower()).Take(1000).Distinct().ToDictionary(w => w, w => true);
	
	Dictionary<string, long> words = new Dictionary<string, long>();

	var table = new DataTable();
	table.TableName = "WordsInPostTitle";
	table.Columns.Add(new DataColumn("PostId"));
	table.Columns.Add(new DataColumn("WordId"));

	using (SqlConnection con = new SqlConnection(connectionString))
	{
		con.Open();
		var cmd = new SqlCommand("select Id,Contents from Words", con);
		using (SqlDataReader reader = cmd.ExecuteReader())
		{
			while (reader.Read())
			{
				words[(string)reader["Contents"]] = (long)reader["Id"];
			}
		}

		cmd = new SqlCommand("select Id, Title from Posts", con);
		using (SqlDataReader reader = cmd.ExecuteReader())
		{
			var idx = 0;
			while (reader.Read())
			{
				var ws = GetWords((string)reader["Title"], commonWords).Distinct();

				foreach (var w in ws)
				{
					if (words.ContainsKey(w))
					{
						var row = table.NewRow();
						row["PostId"] = (long)reader["Id"];
						row["WordId"] = words[w];
						table.Rows.Add(row);

						if (++idx % 1000000 == 0)
						{
							BC(table);
							table.Clear();
						}
					}
				}				
			}
		}
		BC(table);
		table.Clear();
	}
	
	
}


//void UpdateSubRedditsWordCounts()
//{
//	var connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=Reddit;Trusted_Connection=True;";
//	
//	using (var con = new SqlConnection(connectionString))
//	{
//		con.Open();
//
//		con.Execute("delete from WordsInSubReddits");
//
//		var pageSize = 5000;
//		var idx = 0;
//		var count = 140000;
//		
//		while (idx * pageSize < count)
//		{
//			var sql = @"insert into WordsInSubReddits (SubRedditId, WordId, Occurences)
//			SELECT sr.Id as SubRedditId, WordId, count(*) as Occurences
//			FROM [Reddit].[dbo].[WordsInPostTitle]
//			join Posts p on p.Id = PostId
//			join SubReddits sr on sr.Id = p.SubRedditId
//			where sr.Id >= @Start and sr.Id < @End
//			group by sr.Id, WordId";
//
//			Console.WriteLine(String.Format("{0}. Writing gage {1}", DateTime.Now, idx));
//			con.Execute(sql, new { Start = idx * pageSize, End = (idx * pageSize) + pageSize }, null, 600);
//			Console.WriteLine(String.Format("{0}. Page {1} written", DateTime.Now, idx));
//			idx++;
//		}
//	}
//}

//1. Import all HackerNews articles in sql
//2. Analyze all articles for entities and store in sql
//3. Calculate entity appearence counts for all reddit posts and entities and store in sql
//4. Find the suggested SubReddit for each hacker news article by calculating the score:
// SubRedditFrequency: # of posts where entity appears in SubReddit / # of posts in SubReddit
// TotalFrequency: # of posts where entity appears in Reddit / # of posts in Reddit
// Score: Average over all entities appearing in HackerNews article: ( SubRedditFrequency/TotalFrequency )
void Main()
{
//	var analyzer = new Lucene.Net.Analysis.Standard.StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30);
//	Lucene.Net.QueryParsers.QueryParser parser = new Lucene.Net.QueryParsers.QueryParser(Lucene.Net.Util.Version.LUCENE_30, "text", analyzer );
//	var q = parser.Parse("4. Find the suggested SubReddit for each hacker news article by calculating the score:");
	
//	UpdateSubRedditsWordCounts();
//	ConnectPostsAndWords();
//	var all= FetchAll(@"C:\Users\jwe\Downloads\RS_2016-10", @"C:\Users\jwe\Downloads\RS_2016-11");	
	
	
	//InsertWords(all.Where(x => x.subreddit != null).Select(x => x.title));
//	InsertUsersAndSubReddits();
//	InsertPosts();
//	UpdatePostCounts();
}

// Define other methods and classes here