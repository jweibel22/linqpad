<Query Kind="Program">
  <Reference Relative="..\..\..\Identity\Source\Identity.Fetcher\Identity.OAuth\bin\Identity.Domain.dll">C:\git\Identity\Source\Identity.Fetcher\Identity.OAuth\bin\Identity.Domain.dll</Reference>
  <Reference Relative="..\..\..\Identity\Source\Identity.Fetcher\Identity.OAuth\bin\Identity.Infrastructure.dll">C:\git\Identity\Source\Identity.Fetcher\Identity.OAuth\bin\Identity.Infrastructure.dll</Reference>
  <NuGetReference>Lucene.Net</NuGetReference>
  <NuGetReference>Lucene.Net.Store.Azure</NuGetReference>
  <NuGetReference>WindowsAzure.Storage</NuGetReference>
  <Namespace>Identity.Domain.RedditIndexes</Namespace>
  <Namespace>Identity.Infrastructure.Reddit</Namespace>
  <Namespace>Identity.Infrastructure.Services</Namespace>
  <Namespace>Lucene.Net</Namespace>
  <Namespace>Lucene.Net.Analysis</Namespace>
  <Namespace>Lucene.Net.Analysis.Standard</Namespace>
  <Namespace>Lucene.Net.Documents</Namespace>
  <Namespace>Lucene.Net.Index</Namespace>
  <Namespace>Lucene.Net.QueryParsers</Namespace>
  <Namespace>Lucene.Net.Search</Namespace>
  <Namespace>Lucene.Net.Store</Namespace>
  <Namespace>Lucene.Net.Store.Azure</Namespace>
  <Namespace>Microsoft.Azure</Namespace>
  <Namespace>Microsoft.WindowsAzure.Storage</Namespace>
  <Namespace>Microsoft.WindowsAzure.Storage.Blob</Namespace>
  <Namespace>Microsoft.WindowsAzure.Storage.File</Namespace>
</Query>

void Processing(int i)
{
	if (i % 10000 == 0)
	{
		if (i % 1000000 == 0)
		{
			Console.WriteLine(".");
		}
		else
		{
			Console.Write(".");
		}
	}
}

void Search(Lucene.Net.Store.Directory directory, string lookfor)
{
//	var directory = FSDirectory.Open(@"c:\transit\LuceneIndex");
	IndexSearcher searcher = new IndexSearcher(directory);
	Analyzer analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30);
	QueryParser parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_30, "text", analyzer);
	Query query = parser.Parse(lookfor);
	
//	QueryParser parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_30, "Title", analyzer);            
//	Query query = parser.Parse("Title:my");  

	var hits = searcher.Search(query, 100000);

	var subreddits = new Dictionary<string, int>();

	Console.WriteLine("Found {0} results", hits.TotalHits);
	Console.WriteLine("Found {0} docs", hits.ScoreDocs.Length);
	for (int i = 0; i < hits.ScoreDocs.Length; i++)
	{
	
		Document doc = searcher.Doc(hits.ScoreDocs[i].Doc);
		
		//float score = hits.ScoreDocs[i].Score;
		//		Console.WriteLine("Result num {0}, score {1}", i + 1, score);
		//		Console.WriteLine("ID: {0}", doc.Get("id"));
		//		Console.WriteLine("Text found: {0}" + Environment.NewLine, doc.Get("text"));
		var subreddit = doc.Get("subreddit");
		
//		Console.WriteLine("{0}\t\t\t\t\t\t{1}", subreddit, doc.Get("text"));
		
		
		if (!subreddits.ContainsKey(subreddit))
		{
			subreddits.Add(subreddit, 0);
		}
		subreddits[subreddit] = subreddits[subreddit] + 1;
	}

	foreach (var kv in subreddits.Where(x => x.Value > 3).OrderByDescending(x => x.Value))
	{
		Console.WriteLine("{0}: {1}", kv.Value, kv.Key);
	}

	searcher.Close();
	directory.Close();
}

void LoadFile()
{
	var connectionString = "DefaultEndpointsProtocol=https;AccountName=jweibel;AccountKey=mGw3mQxyZ9F2NHW8WHAK2qtwapm5PxvpfpUJlXNMaE0mXdzhV43QwTG3aUQGrtoSnEpJMSXap2dzi1wmiMQv/w==";
	CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

	// Create a CloudFileClient object for credentialed access to File storage.
	CloudFileClient fileClient = storageAccount.CreateCloudFileClient();

	// Get a reference to the file share we created previously.
	CloudFileShare share = fileClient.GetShareReference("identity-files");

	// Ensure that the share exists.
	if (share.Exists())
	{
		// Get a reference to the root directory for the share.
		CloudFileDirectory rootDir = share.GetRootDirectoryReference();

		// Get a reference to the directory we created previously.
		CloudFileDirectory sampleDir = rootDir.GetDirectoryReference("RedditPosts");

		// Ensure that the directory exists.
		if (sampleDir.Exists())
		{
			// Get a reference to the file we created previously.
			CloudFile file = sampleDir.GetFileReference("test.txt");

			// Ensure that the file exists.
			if (file.Exists())
			{
				// Write the contents of the file to the console window.
				Console.WriteLine(file.DownloadTextAsync().Result);
			}
		}
	}
}


//void CreateIndex(AzureDirectory directory)
//{
////	var directory = FSDirectory.Open(@"c:\transit\LuceneIndex");
//	Analyzer analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30);
//
//	using (IndexWriter writer = new IndexWriter(directory, analyzer, true, IndexWriter.MaxFieldLength.UNLIMITED))
//	{
//		using (StreamReader sr = new StreamReader(@"c:\transit\alltitles.csv"))
//		{
//			int i = 0;
//
//			while (sr.Peek() >= 0)
//			{
//				i++;
//				Processing(i);
//				var line = sr.ReadLine().Split(';');
//
//				if (line.Length < 2)
//					continue;
//
//				Document doc = new Document();
//				doc.Add(new Field("id", i.ToString(), Field.Store.YES, Field.Index.NO));
//				doc.Add(new Field("text", line[0], Field.Store.YES, Field.Index.ANALYZED));
//				doc.Add(new Field("subreddit", line[1], Field.Store.YES, Field.Index.NO));
//				writer.AddDocument(doc);
//			}
//		}
//
//		writer.Optimize();
//		writer.Commit();
//		writer.Flush(true, true, true);
//	}
//}

//public IEnumerable<Submission> LoadFromFiles(IEnumerable<string> files)
//{
//	foreach (var file in files)
//	{
//		Console.WriteLine("");
//		Console.WriteLine("Processing " + file);
//		foreach (var x in Submission.LoadFromFile(file, Processing))
//		{
//			yield return x;
//		}
//	}
//}

public IEnumerable<Submission> LoadFromCsvFile(string file)
{
	using (var sr = new StreamReader(file))
	{
		while (sr.Peek() >= 0)
		{
			var line = sr.ReadLine().Split(';');

			if (line.Length < 2)
				continue;

			yield return new Submission
			{
				title = line[0],
				subreddit = line[1]
			};
		}
	}
}

//void CovertToCsvFiles()
//{
//	var files = new[] { "RS_2010-01", "RS_2011-01", "RS_2012-01", "RS_2013-01", "RS_2014-01", "RS_2015-01", "RS_2016-10", "RS_2016-12" };
//	//	var submissions = LoadFromCsvFile(@"C:\transit\RedditPosts\Input\RS_2010-01");
//
//	foreach (var file in files)
//	{
//		var submissions = Submission.LoadFromFile(Path.Combine(@"C:\transit\RedditPosts\Input", file));
//		using (var writer = new StreamWriter(String.Format(@"C:\transit\RedditPosts\Input\{0}.csv", file)))
//		{
//			foreach (var s in submissions)
//			{
//				writer.WriteLine(String.Format("{0};{1}", s.title, s.subreddit));
//			}
//		}
//	}
//}

//void CreateIndex()
//{
//		CloudStorageAccount cloudStorageAccount = CloudStorageAccount.DevelopmentStorageAccount;
//		if (!CloudStorageAccount.TryParse("DefaultEndpointsProtocol=https;AccountName=jweibel;AccountKey=mGw3mQxyZ9F2NHW8WHAK2qtwapm5PxvpfpUJlXNMaE0mXdzhV43QwTG3aUQGrtoSnEpJMSXap2dzi1wmiMQv/w==", out cloudStorageAccount))
//		{
//			throw new Exception("aaaa");
//		}
//	
//		using (SqlConnection con = new SqlConnection(@"Data Source=v5kf9wt87u.database.windows.net;Initial Catalog=Identity;Persist Security Info=True;User ID=jweibel;Password=MiInPw01;Connect Timeout=30"))
//		{
//			con.Open();
//	
//			using (var trx = con.BeginTransaction())
//			{
//				var repo = new RedditIndexRepository(trx);
//				//repo.DeleteRedditIndex(3);
//				var directoryFactory = new LuceneDirectoryFactory(cloudStorageAccount, @"c:\transit\RedditPosts\Indexes");
//				var indexFactory = new RedditIndexFactory(repo, directoryFactory);
//	
//				var files = new[] {"RS_2010-01", "RS_2011-01", "RS_2012-01", "RS_2013-01", "RS_2014-01", "RS_2015-01", "RS_2016-10", "RS_2016-12"};						
//	//			var files = new[] {"RS_2010-01", "RS_2011-01"};						
//				var submissions = LoadFromFiles(files.Select(f => Path.Combine(@"C:\transit\RedditPosts\Input", f)));
//				
//				indexFactory.Build(IndexStorageLocation.Local, submissions);
//	
//				trx.Commit();
//			}
//		}	
//}

void Main()
{
	var dir = FSDirectory.Open(@"C:\transit\RedditPosts\Indexes\1");
	Search(dir, "\"exosphere\"");
	//Search(dir, "spacecraft closer pluto earth");
	
	//Search(dir, "\"penetration testing tools\"");	
}