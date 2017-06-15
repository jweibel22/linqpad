<Query Kind="Program">
  <Connection>
    <ID>249ece1e-6daa-4ab4-9588-a58f1d1facf9</ID>
    <Persist>true</Persist>
    <Server>v5kf9wt87u.database.windows.net</Server>
    <SqlSecurity>true</SqlSecurity>
    <UserName>jweibel</UserName>
    <Password>AQAAANCMnd8BFdERjHoAwE/Cl+sBAAAACk6cLQqAHkSOTectF1TvtwAAAAACAAAAAAADZgAAwAAAABAAAADWUNEA6zO2S/69CfEnqMWoAAAAAASAAACgAAAAEAAAAOwL1NVrvOYA/r5KbpGqnsIQAAAAOo+8qye6LgqP23w8+mSW6RQAAACsexllqQWsd5wnxhZuWpOUZqvLiw==</Password>
    <DbVersion>Azure</DbVersion>
    <Database>Identity</Database>
    <ShowServer>true</ShowServer>
  </Connection>
  <NuGetReference>MathNet.Numerics</NuGetReference>
  <NuGetReference>morelinq</NuGetReference>
  <Namespace>MathNet.Numerics</Namespace>
  <Namespace>MoreLinq</Namespace>
  <Namespace>System.Collections.Generic</Namespace>
  <Namespace>MathNet.Numerics.LinearAlgebra</Namespace>
</Query>

class Document
{
	public long Id { get; set; }
	public string Title { get; set; }	
	public string Description { get; set; }	
	public Vector<double> WordVector2 { get; set; }
}

public static class Debug
{
	public static long? DocumentId = 373215;
}


IEnumerable<Document> FetchArticles(long[] cids)
{	
	var from = new DateTime(2017, 01, 04);
	var to = new DateTime(2017,01,05,0,0,0);

	return ChannelItems
		.Join(Posts, ci => ci.PostId, p => p.Id, (ci, p) => new { ci, p })
		.Where(x => cids.Contains(x.ci.ChannelId) && x.ci.Created < to && x.ci.Created > from)
		.OrderByDescending(x => x.ci.Created)
		.Select(x => new Document { Id = x.p.Id, Title = x.p.Title.Trim(), Description = x.p.Description })
		.DistinctBy(x => x.Id)
		.ToList();
}

static IList<string> CreateVocabList(IList<string[]> dataSet)
{
	return dataSet.SelectMany(l => l).Distinct().ToList();
}

static double[] GetWordVector(IList<string> vocabList, string[] text)
{
	return vocabList.Select(word => text.Contains(word) ? 1.0 : 0.0).ToArray();
}

static bool IsInt(string s)
{
	int i;
	return Int32.TryParse(s, out i);
}

static string[] Tokenize(string[] commonWords, string text)
{
	var ignoredCharacters = new[] {":", ",", "»", "«", "'"};
	return text.Trim()
		.Replace("-", " ").Replace("–", " ").Replace(".", " ")
		.Replace(ignoredCharacters.ToDictionary(c => c, c => ""))		
		.ToLower()
		.Split(' ')
		.Where(word => !String.IsNullOrEmpty(word) && !commonWords.Contains(word) && !IsInt(word)).ToArray();
}

static double CosineSim(Vector<double> s1, Vector<double> s2)
{
	return s1.DotProduct(s2) / (s1.L2Norm() * s2.L2Norm());
}

static double Distance(Vector<double> v1, Vector<double> v2)
{
	var sim = CosineSim(v1, v2);
	if (sim == 0.0)
		return Double.MaxValue;
	else
		return (1 / sim) - 1;
}

class Cluster
{
	public double[] Centroid { get; set; }
	
	public Vector<double> Centroid2 { get; set; }
	
	public double[] Sums { get; set; }
	
	public List<Document> Documents { get; set; }	
	
	private readonly int n;
	
	public Cluster(Document d)
	{		
		Documents = new List<Document>();		
		n = d.WordVector2.Count;
		Sums = new double[n];
		Add(d);		
	}
	
	public void Add(Document d)
	{
		if (Debug.DocumentId != null)
		{
			if (d.Id == Debug.DocumentId.Value)
			{
				String.Format("Adding Id={0}, Title={1}", d.Id, d.Title).Dump();
			}
			else if (Documents.Any() && Documents.First().Id == Debug.DocumentId.Value)
			{
				String.Format("Adding Id={0}, Title={3}. CosineSim={1},Distance={2}", d.Id, CosineSim(Centroid2, d.WordVector2), Distance(Centroid2, d.WordVector2), d.Title).Dump();
			}
		}

		if (d.WordVector2.Count != n)
		{
			throw new ApplicationException("Expected a vector with " + n + " elements");
		}
		Documents.Add(d);

		for (int i = 0; i < n; i++)
		{
			Sums[i] += d.WordVector2[i];
		}
		
		ComputeCentroid();
	}
	
	public void Merge(Cluster c)
	{
		//String.Format("Merging {0} into {1}", c.FriendlyName, FriendlyName).Dump();
		Documents.AddRange(c.Documents);

		for (int i = 0; i < n; i++)
		{
			Sums[i] += c.Documents.Sum(x => x.WordVector2[i]);
		}

		ComputeCentroid();
	}
	
	private void ComputeCentroid()
	{
		Centroid = Enumerable.Range(0,n).Select(i => Sums[i]/Documents.Count).ToArray();
		Centroid2 = Vector<double>.Build.SparseOfArray(Centroid);
	}

	public string FriendlyName
	{
		get { return Documents.First().Title; }
	}
}

class World
{
	public IList<Cluster> Clusters { get; set; }	
	
	private double threshold = 3;
	
	public World()
	{
		Clusters = new List<Cluster>();		
	}
	
	public void Add(Document d)
	{	
		if (!Clusters.Any())
		{
			Clusters.Add(new Cluster(d));
			return;
		}
		
		var distances = Clusters.Select(c => new { Cluster = c, Distance = Distance(c.Centroid2, d.WordVector2) }).ToList();
		var min = distances.Any() ? distances.MinBy(x => x.Distance) : null;

		if (min != null && min.Distance < threshold)
		{		
			min.Cluster.Add(d);
			Merge(min.Cluster);
		}
		else
		{
			Clusters.Add(new Cluster(d));
		}				
	}
	
	private void Merge(Cluster cluster)
	{
		var distances = Clusters.Where(c => c != cluster).Select(c => new { Cluster = c, Distance = Distance(c.Centroid2, cluster.Centroid2) }).ToList();
		var min = distances.Any() ? distances.MinBy(x => x.Distance) : null;

		if (min != null && min.Distance < threshold)
		{
			if (Debug.DocumentId != null)
			{
				if (cluster.Documents.First().Id == Debug.DocumentId.Value || min.Cluster.Documents.First().Id == Debug.DocumentId.Value)
				{
					String.Format("Merging {0} into {1}. Distance={2}", min.Cluster.FriendlyName, cluster.FriendlyName, min.Distance).Dump();
				}
			}

			cluster.Merge(min.Cluster);
			Clusters.Remove(min.Cluster);
			Merge(cluster);

		}
	}
}


void XX(IEnumerable<Document> articles)
{
	var world = new World();
	int i = 0;
	foreach (var article in articles)
	{
//		if ((i++) % 100 == 0)
//		{
//			Console.WriteLine(".");
//		}
//		else
//		{
//			Console.Write(".");
//		}
		
		world.Add(article);
	}
	
	var bigClusters = world.Clusters.Where(c => c.Documents.Count > 1).ToList();
	String.Format("{0} clusters found. {1} out of a total of {2} documents were clustered", bigClusters.Count, bigClusters.Sum(bc => bc.Documents.Count), articles.Count()).Dump();

	foreach (var bc in bigClusters)
	{
		String.Format("Cluster with {0} elements: ", bc.Documents.Count).Dump();
		bc.Documents.Select(d => new { d.Title, d.Id }).Dump();
	}
}

IEnumerable<string> Overlap(string[] s1, string[] s2)
{
	return s1.Intersect(s2);
}

IEnumerable<int> NonZero(Vector<double> s1)
{
	return s1.Select((v, i) => new { v, i }).Where(x => x.v > 0).Select(x => x.i);
}

void Compare(IList<string> vocabList, IEnumerable<Document> articles, long id1, long id2)
{
	var a1 = articles.Single(a => a.Id == id1);
	var a2 = articles.Single(a => a.Id == id2);
	(a1.Title + " " + a1.Description).Dump();
	(a2.Title + " " + a2.Description).Dump();
//	Overlap(tokenized[373170], tokenized[372779]).Dump();

	NonZero(a1.WordVector2).Dump();
	NonZero(a2.WordVector2).Dump();
	Distance(a1.WordVector2, a2.WordVector2).Dump();
	vocabList[29].Dump();

}

void Main()
{
	var commonWordsFile = "commonwords-english.txt";
//	var cids = new long[] { 70, 71, 61, 64, 65, 66, 67, 110 };
	var cids = new long[] { 118 };
	
	var commonWords = System.IO.File.ReadAllLines(@"C:\git\Identity\Source\Identity.Fetcher\Identity.Infrastructure\" + commonWordsFile).Select(w => w.Trim().ToLower()).ToArray();	
	var articles = FetchArticles(cids);
//	var tokenized = articles.ToDictionary(a => a.Id, a => Tokenize(commonWords, a.Title + " " + a.Description.Trim()));
	var tokenized = articles.ToDictionary(a => a.Id, a => Tokenize(commonWords, a.Title));
	var vocabList = CreateVocabList(tokenized.Values.ToList());

	foreach (var article in articles)
	{
		var wordVector = vocabList.Select(word => tokenized[article.Id].Contains(word) ? 1.0 : 0.0);
		article.WordVector2 = Vector<double>.Build.SparseOfEnumerable(wordVector);
	}
	
	articles = articles.Where(a => a.WordVector2.L2Norm() > 0);


	//Compare(vocabList, articles, 373215, 373106);	
	
	XX(articles);

}