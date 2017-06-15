<Query Kind="Program">
  <Reference Relative="..\..\..\dctradepower\DC.Trade.Power.Service\bin\Debug\RabbitMQ.Client.dll">C:\git\dctradepower\DC.Trade.Power.Service\bin\Debug\RabbitMQ.Client.dll</Reference>
  <Reference Relative="..\..\..\dctradepower\DC.Trade.Power.Service\bin\Debug\Rebus.dll">C:\git\dctradepower\DC.Trade.Power.Service\bin\Debug\Rebus.dll</Reference>
  <Reference Relative="..\..\..\dctradepower\DC.Trade.Power.Service\bin\Debug\Rebus.RabbitMQ.dll">C:\git\dctradepower\DC.Trade.Power.Service\bin\Debug\Rebus.RabbitMQ.dll</Reference>
  <NuGetReference>Lucene.Net</NuGetReference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>Lucene.Net</Namespace>
  <Namespace>Lucene.Net.Analysis</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>Rebus.Configuration</Namespace>
  <Namespace>Rebus.RabbitMQ</Namespace>
  <Namespace>Lucene.Net.Store</Namespace>
  <Namespace>Lucene.Net.Analysis.Standard</Namespace>
  <Namespace>Lucene.Net.Index</Namespace>
  <Namespace>Lucene.Net.Documents</Namespace>
  <Namespace>Lucene.Net.Search</Namespace>
  <Namespace>Lucene.Net.QueryParsers</Namespace>
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


void Search()
{
	var directory = FSDirectory.Open(@"c:\transit\LuceneIndex");
	IndexSearcher searcher = new IndexSearcher(directory);

	Analyzer analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30);
	QueryParser parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_30, "text", analyzer);
	Query query = parser.Parse("hitler");

	var hits = searcher.Search(query, 100000);

	var subreddits = new Dictionary<string, int>();
	
	Console.WriteLine("Found {0} results", hits.TotalHits);
	for (int i = 0; i < hits.TotalHits; i++)
	{
		Document doc = searcher.Doc(hits.ScoreDocs[i].Doc);
		//float score = hits.ScoreDocs[i].Score;
		//		Console.WriteLine("Result num {0}, score {1}", i + 1, score);
		//		Console.WriteLine("ID: {0}", doc.Get("id"));
		//		Console.WriteLine("Text found: {0}" + Environment.NewLine, doc.Get("text"));
		//Console.WriteLine(doc.Get("text"));
		var subreddit = doc.Get("subreddit");
		if (!subreddits.ContainsKey(subreddit))
		{
			subreddits.Add(subreddit, 0);
		}
		subreddits[subreddit] = subreddits[subreddit] + 1;
	}

	foreach (var kv in subreddits.Where(x => x.Value > 10))
	{
		Console.WriteLine("{0}: {1}", kv.Value, kv.Key);
	}

	searcher.Close();
	directory.Close();
}



void Main()
{
	var directory = FSDirectory.Open(@"c:\transit\RedditPosts\Index1");
//	CreateIndex();
//	Search();	

	
}