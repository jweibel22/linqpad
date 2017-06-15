<Query Kind="Program">
  <Connection>
    <ID>50a0748c-9d24-484e-9bf6-e75f55d4f1dc</ID>
    <Persist>true</Persist>
    <Server>.\SQLEXPRESS</Server>
    <Database>Reddit</Database>
    <ShowServer>true</ShowServer>
  </Connection>
  <Reference Relative="..\..\..\Identity\Source\Identity.Fetcher\Identity.OAuth\bin\Identity.Domain.dll">C:\git\Identity\Source\Identity.Fetcher\Identity.OAuth\bin\Identity.Domain.dll</Reference>
  <Reference Relative="..\..\..\Identity\Source\Identity.Fetcher\Identity.OAuth\bin\Identity.Infrastructure.dll">C:\git\Identity\Source\Identity.Fetcher\Identity.OAuth\bin\Identity.Infrastructure.dll</Reference>
  <NuGetReference>morelinq</NuGetReference>
  <Namespace>Identity.Infrastructure.Feeders</Namespace>
  <Namespace>Identity.Infrastructure.Helpers</Namespace>
  <Namespace>Identity.Infrastructure.Repositories</Namespace>
  <Namespace>Identity.Infrastructure.Services</Namespace>
  <Namespace>Identity.Infrastructure.Services.NLP</Namespace>
  <Namespace>MoreLinq</Namespace>
</Query>

//void InsertMissingEntities(NLPHelper helper, IEnumerable<Entity> allNewEntities)
//{
//	var allExistingEntities = NLPEntities.ToList();
//	var toInsert = allNewEntities.Where(e => !allExistingEntities.Any(x => x.Name == e.Name)).Select(e => new NLPEntities
//	{
//		Name = e.Name,
//		Type = e.Type,
//		CommonWord = helper.CommonWords.ContainsKey(e.Name),
//		Noun = helper.CommonNouns.ContainsKey(e.Name),
//		Processed = false
//	}).ToList();
//
//	NLPEntities.InsertAllOnSubmit(toInsert);
//	SubmitChanges();
//}
//
//
//DataTable FindEntities(NLPHelper helper, IDictionary<string, long> entities, SubstringLookup.TreeNode prefixes, IEnumerable<Tuple<long, string>> posts)
//{
//	var table = new DataTable();
//	table.TableName = "HNArticleEntities";
//	table.Columns.Add(new DataColumn("HNArticleId", typeof(long)));
//	table.Columns.Add(new DataColumn("NLPEntityId", typeof(long)));
//	table.Columns.Add(new DataColumn("IdentifiedByGoogleNLP", typeof(bool)));
//
//	foreach (var post in posts)
//	{
//		string title = " " + helper.IgnoreAll(post.Item2.ToLower()) + " ";
//		IList<string> matched = SubstringLookup.FindSubstrings(title, prefixes).Distinct().ToList();
//
//		foreach (var match in matched)
//		{
//			var row = table.NewRow();
//			row["HNArticleId"] = post.Item1;
//			row["NLPEntityId"] = entities[match.Trim()];
//			row["IdentifiedByGoogleNLP"] = false;
//			table.Rows.Add(row);
//		}
//	}
//
//	return table;
//}
//
//void ProcessArticles(NLPHelper helper, IList<Hnarticles> hna)
//{
//	var commonWords = new string[0];
//	var client = new GoogleNLPClient(commonWords, "AIzaSyBAPJ3LgmXm-DrmG6CiZ6AHslkMl8C999U", "https://language.googleapis.com/v1/documents:analyzeEntities");
//
//	var items = hna.ToDictionary(a => new Text { Content = a.Title }, a => a);
//	var articles = new Dictionary<Identity.Infrastructure.Services.NLP.Text, System.Collections.Generic.List<Entity>>();
//
//	foreach (var kv in client.Get(items.Keys.ToList()).Articles)
//	{
//		articles.Add(kv.Key, kv.Value.Where(s => s.Name.Length > 1).ToList());
//	}
//
//	InsertMissingEntities(helper, articles.SelectMany(kv => kv.Value).DistinctBy(e => e.Name));
//
//	var allEntities = NLPEntities.ToList();
//
//	foreach (var kv in articles)
//	{
//		var aId = items[kv.Key].Id;
//		
//		items[kv.Key].Analyzed = true;
//		
//		foreach (var s in kv.Value.DistinctBy(e => e.Name))
//		{
//			var eId = allEntities.Single(e => e.Name == s.Name).Id;
//			HNArticleEntities.InsertOnSubmit(new HNArticleEntities { HNArticleId = aId, NLPEntityId = eId, IdentifiedByGoogleNLP = true });
//		}								
//	}
//	
//	SubmitChanges();
//}


//void Analyse()
//{
//	var entities = NLPEntities.ToList();
//
//	var pageSize = 200;
//	var idx = 0;
//	var articles = Hnarticles.Where(a => !a.Analyzed).ToList();
//	while (idx * pageSize < articles.Count)
//	{
//		String.Format("Processing page {0}", idx).Dump();
//		var page = articles.Skip(idx * pageSize).Take(pageSize).ToList();
//		idx++;
//
//		var client = new GoogleNLPClient(new string[0], "AIzaSyBAPJ3LgmXm-DrmG6CiZ6AHslkMl8C999U", "https://language.googleapis.com/v1/documents:analyzeEntities");
//		var items = page.ToDictionary(a => new Text { Content = a.Title }, a => a.Id);
//		var agg = new Dictionary<Identity.Infrastructure.Services.NLP.Text, System.Collections.Generic.List<Entity>>();
//
//		foreach (var kv in client.Get(items.Keys.ToList()).Articles)
//		{
//			agg.Add(kv.Key, kv.Value.Where(s => s.Name.Length > 1).ToList());
//		}
//
//		var allstuff = agg.SelectMany(kv => kv.Value).DistinctBy(x => x.Name);
//
//		foreach (var entity in entities)
//		{
//			var dd = allstuff.SingleOrDefault(y => y.Name == entity.Name);
//
//			if (dd != null)
//			{
//				entity.Type = dd.Type;
//			}
//		}
//
//		SubmitChanges();
//
//	}
//}
//
//IEnumerable<Tuple<long, string>> FetchArticleTitles(SqlConnection con)
//{	
//	
//		var cmd = new SqlCommand("select Id, Title from hnarticles order by id", con);
//
//		using ( var reader = cmd.ExecuteReader())
//		{
//			while (reader.Read())
//			{
//				yield return new Tuple<long, string>((long)reader["Id"], (string)reader["Title"]);
//			}
//		}
//
//}
//
//void AddMissing(NLPHelper helper)
//{
//	var xx = HNArticleEntities.ToList(); //TODO: filter, we only need the rows of the unprocessed entities
//	var entities = NLPEntities.Where(e => !e.Processed).ToList().Where(e => !helper.CommonWords.ContainsKey(e.Name)).ToDictionary(e => e.Name, e => e.Id);
//	var prefixes = SubstringLookup.BuildPrefixTree(entities.Keys.Select(s => " " + s + " ").ToList()); //new SubstringLookup.TreeNode('c',"c"); // 
//
//	var connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=Reddit;Trusted_Connection=True;";
//
//	using (var con = new SqlConnection(connectionString))
//	{
//		con.Open();
//		var articles = FetchArticleTitles(con);
//		var table = FindEntities(helper, entities, prefixes, articles);
//
//		var i = 0;
//		while (i < table.Rows.Count)
//		{
//			var r = table.Rows[i];
//			if (xx.Any(x => x.NLPEntityId == (long)r["NLPEntityId"] && x.HNArticleId == (long)r["HNArticleId"]))
//			{
//				table.Rows.Remove(r);
//			}
//			else
//			{
//				i++;
//			}
//		}
//		
//		BulkCopy.Copy(con, table);
//	}
//
//}
//
//
//void BC(DataTable table)
//{
//	var connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=Reddit;Trusted_Connection=True;";
//	using (SqlConnection con = new SqlConnection(connectionString))
//	{
//		con.Open();
//		BulkCopy.Copy(con, table);
//	}
//}

//void ConnectPostsAndWords(SqlConnection con, NLPHelper helper)
//{
//	var table = new DataTable();
//	table.TableName = "WordsInHNArticles";
//	table.Columns.Add(new DataColumn("HNArticleId"));
//	table.Columns.Add(new DataColumn("WordId"));
//		
//		var words = helper.GetAllWords(con);
//		var titles = FetchArticleTitles(con);
//
//		var idx = 0;
//
//		foreach(var kv in titles)
//		{						
//				var ws = helper.GetWords(kv.Item2).Distinct();
//
//				foreach (var w in ws)
//				{
//					if (words.ContainsKey(w))
//					{
//						var row = table.NewRow();
//						row["HNArticleId"] = kv.Item1;
//						row["WordId"] = words[w];
//						table.Rows.Add(row);
//
//						if (++idx % 1000000 == 0)
//						{
//							BC(table);
//							table.Clear();
//						}
//					}
//			}
//		}
//		BC(table);
//		table.Clear();
//
//}



void Main()
{
						var commonWordsFile = @"C:\git\Identity\Source\Identity.Fetcher\Identity.Infrastructure\Resources\commonwords-english.txt";
						var commonNounsFile = @"C:\git\Identity\Source\Identity.Fetcher\Identity.Infrastructure\Resources\nouns.csv";						
				var helper = new EnglishLanguage(commonWordsFile, commonNounsFile);	
	var google = new GoogleNLPClient("AIzaSyBAPJ3LgmXm-DrmG6CiZ6AHslkMl8C999U", "https://language.googleapis.com/v1/documents:analyzeEntities", helper);

	var es = google.Get(new[] { new Text { Content = "An Infographic: How Bad Is U.S. Health Care?" } });
	es.Dump();

	//	var connectionString = @"Data Source=v5kf9wt87u.database.windows.net;Initial Catalog=Identity;Persist Security Info=True;User ID=jweibel;Password=MiInPw01;Connect Timeout=30";
//	using (var con = new SqlConnection(connectionString))
//	{
//		con.Open();
//
//		using (var trx = con.BeginTransaction())
//		{
//					var commonWordsFile = @"C:\git\Identity\Source\Identity.Fetcher\Identity.Infrastructure\Resources\commonwords-english.txt";
//					var commonNounsFile = @"C:\git\Identity\Source\Identity.Fetcher\Identity.Infrastructure\Resources\nouns.csv";						
//			var helper = new EnglishLanguage(commonWordsFile, commonNounsFile);
//			var repo = new NLPEntityRepository(trx);
//			var postRepo = new PostRepository(trx);
//			var channels = new ChannelRepository(trx);
//			var google = new GoogleNLPClient("AIzaSyBAPJ3LgmXm-DrmG6CiZ6AHslkMl8C999U", "https://language.googleapis.com/v1/documents:analyzeEntities", helper);
//
//			var analyzer = new PostNlpAnalyzer(repo, helper, google);
//
//			var posts = postRepo.XXX(118, 365000, 370000).ToList();
//			analyzer.AnalyzePosts(posts);
//		
//			trx.Commit();
//		}
//
//
//	}

}

//void Main()
//{
//	try
//	{
//		var connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=Reddit;Trusted_Connection=True;";
//		var commonWordsFile = @"C:\git\Identity\Source\Identity.Fetcher\Identity.Infrastructure\commonwords-english.txt";
//		var commonNounsFile = @"C:\transit\nouns.csv";						
//		var helper = new NLPHelper(commonWordsFile, commonNounsFile);
//
//		//		ProcessArticles(Hnarticles.Where(a => !a.Analyzed).ToList());
//		//		AddMissing();
//
//		using (SqlConnection con = new SqlConnection(connectionString))
//		{
//			con.Open();
////			ConnectPostsAndWords(con, helper);
////			helper.InsertWords(con, Hnarticles.Select(a => a.Title));
//		}	
//
//		//ConnectPostsAndWords();
//	}
//	catch (Exception ex)
//	{
//		Console.WriteLine("Unhandled exception: " + ex.Message);
//	}
//}