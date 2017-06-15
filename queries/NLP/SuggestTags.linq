<Query Kind="Program">
  <Reference Relative="..\..\..\Identity\Source\Identity.Fetcher\Identity.OAuth\bin\Identity.Domain.dll">C:\git\Identity\Source\Identity.Fetcher\Identity.OAuth\bin\Identity.Domain.dll</Reference>
  <Reference Relative="..\..\..\Identity\Source\Identity.Fetcher\Identity.OAuth\bin\Identity.Infrastructure.dll">C:\git\Identity\Source\Identity.Fetcher\Identity.OAuth\bin\Identity.Infrastructure.dll</Reference>
  <NuGetReference>Accord</NuGetReference>
  <NuGetReference>Accord.MachineLearning</NuGetReference>
  <NuGetReference>Dapper</NuGetReference>
  <NuGetReference>Lucene.Net.Store.Azure</NuGetReference>
  <Namespace>Accord.IO</Namespace>
  <Namespace>Accord.MachineLearning.Bayes</Namespace>
  <Namespace>Accord.Statistics.Distributions.Fitting</Namespace>
  <Namespace>Accord.Statistics.Distributions.Univariate</Namespace>
  <Namespace>Dapper</Namespace>
  <Namespace>Identity.Domain</Namespace>
  <Namespace>Identity.Domain.RedditIndexes</Namespace>
  <Namespace>Identity.Infrastructure.Helpers</Namespace>
  <Namespace>Identity.Infrastructure.Reddit</Namespace>
  <Namespace>Identity.Infrastructure.Repositories</Namespace>
  <Namespace>Identity.Infrastructure.Services</Namespace>
  <Namespace>Identity.Infrastructure.Services.AutoTagger</Namespace>
  <Namespace>Identity.Infrastructure.Services.NLP</Namespace>
  <Namespace>Microsoft.WindowsAzure.Storage</Namespace>
</Query>

void Log(String s)
{
	Console.WriteLine(String.Format("{0}. {1}", DateTime.Now.ToString("HH:mm:ss.fff"), s));
}

void Processing(int i)
{
	if (i % 20 == 0)
	{
		if (i % 1000 == 0)
		{
			Console.WriteLine(".");
		}
		else
		{
			Console.Write(".");
		}
	}
}

IDictionary<long, double> newSuggest4(RedditIndex index, IEnumerable<IGrouping<long, Occurences>> xx, int textCount, IDictionary<long, int> subredditPostCounts,
IDictionary<long, int> totalTextOccurences)
{
//	var stopwatch = new Stopwatch();
//	var prepare = 0L;
//	stopwatch.Reset();
//	stopwatch.Start();

	var totalRedditPosts = index.TotalPostCount;
	var logTotalRedditPosts = Math.Log(index.TotalPostCount);

	Func<IEnumerable<Occurences>, double> scorer = g =>
		g.Sum(a => (Math.Log(a.Count) / Math.Log(subredditPostCounts[a.SubRedditId])) / (Math.Log(totalTextOccurences[a.TextId]) / logTotalRedditPosts)) / (textCount);

	//	Func<IEnumerable<Occurences>, double> scorer = g =>
	//		g.Max(a => (Math.Log(a.Count) / Math.Log(subredditPostCounts[a.SubRedditId])) / (Math.Log(totalCounts[a.TextId]) / logTotalRedditPosts));

	var weighted = xx.SelectMany(o => o)
					.GroupBy(x => x.SubRedditId)
					.Select(g => new SubRedditScore { Id = g.Key, Score = scorer(g) });

//	stopwatch.Stop();
//	prepare = stopwatch.ElapsedMilliseconds;
//	stopwatch.Reset();
//	stopwatch.Start();

	//	var temp = xx.Select(x => new SuggestedSubRedditsDebugInfo
//	{
//		Id = x.SubRedditId,
//		EntityName = texts.Single(e => e.Id == x.TextId).Content,
//		Name = subredditNames[x.SubRedditId],
//		Occurences = x.Count,
//		PostCount = subredditPostCounts[x.SubRedditId],
//		TotalEntityOccurences = totalCounts[x.TextId],
//		TotalRedditPosts = totalRedditPosts
//	}).OrderBy(x => x.Name).ThenByDescending(x => x.Score).ToList();
	//.Where(x => x.LogSubRedditFreq > 0.2)
	//		temp.Dump();

	//	Log("Suggestions found");

	var result = weighted.ToDictionary(w => w.Id, w => w.Score);

//	Console.WriteLine(String.Format("Time: {0} + {1}", prepare, stopwatch.ElapsedMilliseconds));
//	stopwatch.Stop();

	return result;
}

void PrintToCsv(IDictionary<long, string> articleNames, IDictionary<long, string> subRedditNames, IEnumerable<SuggestedSubReddits> suggestions)
{
	foreach (var suggestion in suggestions)
	{
		var tags = suggestion.TopSubReddits;
		Func<int, string> gg = i =>
		 {
			 var d = i < tags.Count ? subRedditNames[tags[i].Id] : null;
			 return d == null ? "" : d;
		 };

		Func<int, double> score = i =>
		{
			return i < tags.Count ? tags[i].Score : 0;
		};

		var sb = new StringBuilder();
		sb.Append(articleNames[suggestion.ArticleId].Trim());
		for (int i=0;i<20;i++)
			sb.Append(";" + gg(i));
		for (int i = 0; i < 20; i++)
			sb.Append(";" + score(i));

		Console.WriteLine(sb.ToString()); // "{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10}", articleNames[suggestion.ArticleId].Trim(), gg(0), gg(1), gg(2), gg(3), gg(4), score(0), score(1), score(2), score(3), score(4));
	}
}

void CreateChannels(IDbTransaction con, IDictionary<long, String> subRedditNames, IEnumerable<SuggestedSubReddits> suggestions)
{
	var channelRepo = new ChannelRepository(con);
	var userRepo = new UserRepository(con);
	
	var rssFeederUserId = 5;
	var hackernewsChannelId = 117;

	var allChannels = con.Connection.Query("select id, name from ChannelLink cl join Channel c on c.Id = cl.ChildId where cl.ParentId = " + hackernewsChannelId, new {}, con)
					 	.Cast<IDictionary<string, object>>()
						.ToDictionary(row => (string)row["name"], row => (long)row["id"]);

	var allExistingChannelNames = allChannels.Keys.ToList();
	var allSubReddits = suggestions.SelectMany(s => s.TopSubReddits.Where(x => x.Score >= 0.4)).Select(x => x.Id).Distinct().ToDictionary(id => id, id => subRedditNames[id]);

	foreach (var sr in allSubReddits)
	{
		if (!allExistingChannelNames.Contains(sr.Value))
		{
			var newChannel = new Identity.Domain.Channel
			{
				Created = DateTimeOffset.Now,
				IsPublic = true,
				Name = sr.Value
			};
            
			channelRepo.AddChannel(newChannel);
			channelRepo.AddSubscription(hackernewsChannelId, newChannel.Id);
			
			allChannels[newChannel.Name] = newChannel.Id;
		}
	}

	foreach (var suggestion in suggestions)
	{
		foreach (var sr in suggestion.TopSubReddits.Where(x => x.Score >= 0.4))
		{
			userRepo.Publish(rssFeederUserId, allChannels[subRedditNames[sr.Id]], suggestion.ArticleId);
		}
	}
}

IEnumerable<PostAndText> GetPostTexts(SqlTransaction trx)
{
	var sql = @"
select t.Id as TextId, eip.PostId from EntitiesInPosts eip
join ChannelItem ci on ci.PostId = eip.PostId
join NLPEntity e on e.Id = eip.NLPEntityId
join RedditIndex_Text t on t.Content = e.Name and t.IndexId = 8
where ci.ChannelId = 30115 or (eip.PostId >= 372500 and eip.PostId < 372900 and ci.ChannelId = 118)";

	return trx.Connection.Query<PostAndText>(sql, new {}, trx).ToList();
}



class PostWithTitle
{
	public long PostId { get; set; }

	public string Title { get; set; }
}

IEnumerable<PostWithTitle> GetTexts(SqlTransaction trx)
{
	var sql = @"select Id as PostId ,Content as Title from RedditIndex_Text where IndexId = 8";
	return trx.Connection.Query<PostWithTitle>(sql, new { }, trx).ToList();
}

IEnumerable<PostAndId> FindTrainingPosts(SqlTransaction trx)
{
	var sql = @"select p.Title, ci.PostId, ci.ChannelId from ChannelItem ci join Post p on p.Id = ci.PostId where ci.ChannelId in (30141,30133,30136,30122,30118,30140)";
	return trx.Connection.Query<PostAndId>(sql, new { }, trx).ToList();
}

IEnumerable<PostAndId> FindTestPosts(SqlTransaction trx)
{
	var sql = @"select ci.PostId, p.Title from ChannelItem ci join Post p on p.Id = ci.PostId where ci.ChannelId = 118 and ci.PostId >= 372500 and ci.PostId < 380000";
	return trx.Connection.Query<PostAndId>(sql, new { }, trx).ToList();
}


void AddToIndex(NLPEntityRepository nlpRepo, SubRedditOccurences service, Dictionary<int, List<Identity.Domain.Post>> allPosts)
{
	var allEntities = nlpRepo.Entities();
	var allEntityRelations = nlpRepo.EntitiesInPosts();
	Func<long, IEnumerable<NLPEntity>> f = postId =>
				from r in allEntityRelations
				join e in allEntities on r.Value equals e.Id
				where r.Key == postId
				select e;
	
	var entitiesToAdd = allPosts.SelectMany(kv => kv.Value).SelectMany(post => f(post.Id)).ToList();

	var count = entitiesToAdd.Count;
	var idx = 0;

	foreach (var entity in entitiesToAdd)
	{
		service.Add(entity.Name, entity.Type);
		idx++;
	}
}

public IEnumerable<Submission> LoadFromFiles(IEnumerable<string> files)
{
	foreach (var file in files)
	{
		Console.WriteLine("");
		Console.WriteLine("Processing " + file);
		foreach (var x in Submission.LoadFromCsvFile(file, Processing))
		{
			yield return x;
		}
	}
}


PostAndId Parse(string s)
{
	var line = s.Split(';');
	long postId;
	long channelId;
	if (line.Length == 3 && Int64.TryParse(line[0], out postId) && Int64.TryParse(line[2], out channelId))
	{
		return new PostAndId
		{
			PostId = postId,
			Title = line[1].Trim(),
			Id = channelId
		};
	}
	else
	{
		return null;
	}
}


void Run()
{
	var commonWordsFile = @"C:\git\Identity\Source\Identity.Fetcher\Identity.Infrastructure\commonwords-english.txt";
	var commonNounsFile = @"C:\transit\RedditPosts\nouns.csv";
	var helper = new EnglishLanguage(commonWordsFile, commonNounsFile);

	var options = new WordExtractionOptions
	{
		IgnoreCommonWords = true,
		RemovePunctuation = true,
		Stem = false
	};
	var wordExtractor = new WordExtractor(commonWordsFile, commonNounsFile, options);

	CloudStorageAccount cloudStorageAccount = CloudStorageAccount.DevelopmentStorageAccount;
	if (!CloudStorageAccount.TryParse("DefaultEndpointsProtocol=https;AccountName=jweibel;AccountKey=mGw3mQxyZ9F2NHW8WHAK2qtwapm5PxvpfpUJlXNMaE0mXdzhV43QwTG3aUQGrtoSnEpJMSXap2dzi1wmiMQv/w==", out cloudStorageAccount))
	{
		throw new Exception("aaaa");
	}
	
	IList<PostAndId> trainingPosts;
	IList<PostAndId> testPosts;
	IList<PostWithTitle> texts;
	IList<PostAndId> textsInPosts;

	//	using (SqlConnection con = new SqlConnection(@"Data Source=v5kf9wt87u.database.windows.net;Initial Catalog=Identity;Persist Security Info=True;User ID=jweibel;Password=MiInPw01;Connect Timeout=30"))
	//	{
	//		con.Open();
	//
	//		using (var trx = con.BeginTransaction())
	//		{
	//			//trainingPosts = FindTrainingPosts(trx).ToList(); //Skip(20).Take(50).
	//			//testPosts = FindTestPosts(trx).ToList();
	//			//texts = GetTexts(trx).ToList();
	//
	//		}
	//	}

	var inputChannelMap = new Dictionary<long, int>
			{
				{30141, 0},
				{30133, 1},
				{30136, 2},
				{30122, 3},
				{30118, 4},
				{30140, 5},
			};


	trainingPosts = File
					.ReadAllLines(@"C:\transit\RedditPosts\TrainingPosts.csv")
					.Select(Parse)
					.Where(x => x != null)
					.Select(p => new PostAndId { PostId = p.PostId, Title = p.Title, Id = inputChannelMap[p.Id] })
					.ToList();

	testPosts = File
					.ReadAllLines(@"C:\transit\RedditPosts\labels.csv")
					.Select(Parse)		
					.Take(1000)
					.Where(x => x != null && x.Id != 0)					
					.ToList();

//	texts = File
//				   .ReadAllLines(@"C:\transit\RedditPosts\Texts.csv")
//				   .Select(line => line.Split(';'))
//				   .Select(line => new PostWithTitle { PostId = Int64.Parse(line[0]), Title = line[1].Trim() })
//				   .ToList();
//				   
//	textsInPosts = File
//					   .ReadAllLines(@"C:\transit\RedditPosts\TextsInPosts.csv")
//					   .Select(line => line.Split(';'))
//					   .Select(line => new PostAndId { PostId = Int64.Parse(line[0]), Id = Int64.Parse(line[1]) })
//					   .ToList();
//
//	var xxx = from tip in textsInPosts
//				join t in texts on tip.Id equals t.PostId
//				join p in trainingPosts on tip.PostId equals p.PostId
//				select new Tuple<long, string>(p.PostId, t.Title);

//
//	var yyy = from tip in textsInPosts
//			  join t in texts on tip.Id equals t.PostId
//			  join p in trainingPosts on tip.PostId equals p.PostId
//			  select new Tuple<String, string>(p.Title, t.Title);
//
//	foreach (var g in yyy.GroupBy(t => t.Item1))
//	{
//		Console.WriteLine("{0};{1}", g.Key, String.Join(";", g.Select(t => t.Item2)));
//	}

	//	File.WriteAllLines(@"C:\transit\RedditPosts\TrainingPosts.csv", trainingPosts.Select(pac => String.Format("{0};{1};{2}", pac.PostId, pac.Title, pac.ChannelId)));
//	File.WriteAllLines(@"C:\transit\RedditPosts\TestPosts.csv", testPosts.Select(pac => String.Format("{0};{1};{2}", pac.PostId, pac.Title, pac.ChannelId)));
//	File.WriteAllLines(@"C:\transit\RedditPosts\Texts.csv", texts.Select(pac => String.Format("{0};{1}", pac.PostId, pac.Title)));


	using ( SqlConnection con = new SqlConnection(@"Data Source=.\SQLEXPRESS;Initial Catalog=Reddit;Trusted_Connection=True;"))
	{
		con.Open();

		using (var trx = con.BeginTransaction())
		{			
			var postRepo = new PostRepository(trx);

			var bigSubReddits = File.ReadAllLines(@"C:\transit\RedditPosts\Indexes\1\BigSubReddits2.csv").Select(l => Int64.Parse(l)).Take(200).ToDictionary(l => l, l => l);
			
			//var subRedditNames = con.Query("select id, name from RedditIndex_SubReddit", new { }, trx).Casst<IDictionary<string, object>>().ToDictionary(row => (long)row["id"], row => (string)row["name"]);
			//var articleNames = posts.ToDictionary(g => g.Id, g => g.Title);
						
			var repo = new RedditIndexRepository(trx);
			var index = repo.GetFullRedditIndex(1);
			var textExtractor = new TextExtractor(wordExtractor, null, index);
			
			var subredditPostCounts = index.SubReddits.ToDictionary(sr => sr.Id, sr => sr.PostCount);
			
//			var textExtractor2 = new TextExtractor(wordExtractor, xxx.ToList(), index);
			var featureExtractor = new Identity.Infrastructure.Services.AutoTagger.FeatureExtractor(index,o => subredditPostCounts[o.SubRedditId] > 100, textExtractor);
//			var featureExtractor2 = new Identity.Infrastructure.Services.AutoTagger.FeatureExtractor(index, o => bigSubReddits.ContainsKey(o.SubRedditId), textExtractor2);
			var shuffled = trainingPosts.Shuffle();
			var xSet = shuffled;
			var ySet = testPosts; // shuffled.Skip(8000).ToList();
			
//			var posts = trainingPosts.Take(1).ToList();
//			var p = posts.Single();
//			var xx = featureExtractor.ComputeFeatureVectors(posts);
//			var a = textExtractor.GetTexts(posts);
//			var textIds = a.Where(tip => tip.PostId == p.PostId).Select(t => t.TextId).ToList();
//			var fv = featureExtractor.ComputeFeatureVectors(textIds);


			//
			//			 var nb = PredictionModel.Train(index, featureExtractor, "ScoringTestWithNlp", xSet.ToList());
			//			var serialized = nb.Serialize();
			//			new PredictionModelRepository(trx).AddModel(serialized);
			////			

//			var nb2 = PredictionModel.Train(index, featureExtractor2, "ScoringTestWithNlp", xSet.ToList());
//			var answers2 = nb2.Decide(ySet.ToList());
						
			var nb1 = PredictionModel.Train(index, featureExtractor, "ScoringTestWithNlp", xSet.ToList());
			var answers = nb1.Decide(ySet.ToList());

			var answerMap = new Dictionary<int, string>
			{
				{0, "other"},
				{1, "programming"},
				{2, "politics"},
				{3, "technology"},
				{4, "science"},
				{5, "history"}
			};

			var inverseTexts = index.Texts.ToDictionary(t => t.Id, t => t.Content);
			var subredditNames = index.SubReddits.ToDictionary(sr => sr.Id, sr => sr.Name);

			var score1 = Enumerable.Range(0, answers.Length).Sum(i => answers[i] == ySet[i].Id ? 1.0 : 0.0) / (double)answers.Length;
//			var score2 = Enumerable.Range(0, answers2.Length).Sum(i => answers2[i] == ySet[i].Id ? 1.0 : 0.0) / (double)answers2.Length;
			Console.WriteLine("Score1 = {0}, Score2 = {1}", score1, score1);

			for (int i = 0; i < ySet.Count; i++)
			{
//				Console.WriteLine("{0};{1};{2};{3};{4};{5};{6}", ySet[i].Title.Trim().Replace("\"", ""), answerMap[(int)ySet[i].Id], answerMap[answers[i]],answerMap[answers2[i]], ySet[i].Id == answers[i], ySet[i].Id == answers2[i], answers[i] == answers2[i]);
				Console.WriteLine("{0};{1};{2};{3}", ySet[i].Title.Trim().Replace("\"", ""), answerMap[(int)ySet[i].Id], answerMap[answers[i]], ySet[i].Id == answers[i]);
 			}
			//			var directoryFactory = new LuceneDirectoryFactory(cloudStorageAccount, @"c:\transit\RedditPosts\Indexes");
//			var service = new SubRedditOccurences(repo, index, directoryFactory);
			//			var factory = new RedditIndexFactory(repo, directoryFactory);
			//			var files = new[] {"RS_2010-01.csv", "RS_2011-01.csv", "RS_2012-01.csv", "RS_2013-01.csv", "RS_2014-01.csv", "RS_2015-01.csv", "RS_2016-10.csv", "RS_2016-12.csv"};						
			//			var submissions = LoadFromFiles(files.Select(f => Path.Combine(@"C:\transit\RedditPosts\Input", f)));						
			//			factory.Build(IndexStorageLocation.Local, submissions);

			trx.Commit();
		}
	}
}

void Main()
{
	Run();
}

// Define other methods and classes here