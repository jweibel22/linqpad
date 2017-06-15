<Query Kind="Program">
  <Reference Relative="..\..\..\Identity\Source\Identity.Fetcher\Identity.OAuth\bin\Identity.Domain.dll">C:\git\Identity\Source\Identity.Fetcher\Identity.OAuth\bin\Identity.Domain.dll</Reference>
  <Reference Relative="..\..\..\Identity\Source\Identity.Fetcher\Identity.OAuth\bin\Identity.Infrastructure.dll">C:\git\Identity\Source\Identity.Fetcher\Identity.OAuth\bin\Identity.Infrastructure.dll</Reference>
  <Namespace>Identity.Infrastructure.Reddit</Namespace>
  <Namespace>Identity.Infrastructure.Services.NLP</Namespace>
  <Namespace>Identity.Infrastructure.Services.AutoTagger</Namespace>
</Query>


class PostWithTitle
{
	public long PostId { get; set; }

	public string Title { get; set; }
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

void Main()
{

	var inputChannelMap = new Dictionary<long, int>
			{
				{30141, 0},
				{30133, 1},
				{30136, 2},
				{30122, 3},
				{30118, 4},
				{30140, 5},
			};

	var trainingPosts = File
					.ReadAllLines(@"C:\transit\RedditPosts\TrainingPosts.csv")
					.Select(Parse)
					.Where(x => x != null)
					.Select(p => new PostAndId { PostId = p.PostId, Title = p.Title, Id = inputChannelMap[p.Id] })
					.ToList();

	var texts = File
				   .ReadAllLines(@"C:\transit\RedditPosts\Texts.csv")
				   .Select(line => line.Split(';'))
				   .Select(line => new PostWithTitle { PostId = Int64.Parse(line[0]), Title = line[1].Trim() })
				   .ToList();
	var textsInPosts = File
					   .ReadAllLines(@"C:\transit\RedditPosts\TextsInPosts.csv")
					   .Select(line => line.Split(';'))
					   .Select(line => new PostAndId { PostId = Int64.Parse(line[0]), Id = Int64.Parse(line[1]) })
					   .ToList();

	var xxx = from tip in textsInPosts
			  join t in texts on tip.Id equals t.PostId
			  join p in trainingPosts on tip.PostId equals p.PostId
			  select new Tuple<long, string>(p.PostId, t.Title);


	var answerMap = new Dictionary<int, string>
			{
				{0, "other"},
				{1, "programming"},
				{2, "politics"},
				{3, "technology"},
				{4, "science"},
				{5, "history"}
			};


	var IndexId = 1;
	var commonWordsFile = @"C:\git\Identity\Source\Identity.Fetcher\Identity.Infrastructure\commonwords-english.txt";
	var commonNounsFile = @"C:\transit\RedditPosts\nouns.csv";	
	
		var options = new WordExtractionOptions
		{
			IgnoreCommonWords = true,
			RemovePunctuation = true,
			Stem = false
		};
		var wordExtractor = new WordExtractor(commonWordsFile, commonNounsFile, options);

	var bigSubReddits = File.ReadAllLines(@"C:\transit\RedditPosts\Indexes\1\BigSubReddits2.csv").Select(l => Int64.Parse(l)).Take(200).ToDictionary(l => l, l => l);

	using (SqlConnection con = new SqlConnection(@"Data Source=.\SQLEXPRESS;Initial Catalog=Reddit;Trusted_Connection=True;"))
	{
		con.Open();

		using (var trx = con.BeginTransaction())
		{
			var repo = new RedditIndexRepository(trx);
			var index = repo.GetFullRedditIndex(IndexId);
			var textExtractor = new TextExtractor(wordExtractor, xxx.ToList(),index);
			var featureExtractor = new Identity.Infrastructure.Services.AutoTagger.FeatureExtractor(index, o => bigSubReddits.ContainsKey(o.SubRedditId), textExtractor);

			//var s = "I used to work at a retirement home and I found some papers and pictures one of the residents that served in WWII saved";
			var s = "An Infographic: How Bad Is U.S. Health Care?";
            var allTexts = repo.GetTexts(IndexId,wordExtractor.GetWords(s).Distinct());			

			featureExtractor.DebugInfo(allTexts.Select(t => t.Id).ToList()).Dump();
//			
//			var nb = PredictionModel.Load(new PredictionModelRepository(trx).GetModel(7), featureExtractor, repo);
//			var answers = nb.Decide(allTexts.Select(t => t.Id).ToList());
//			answerMap[answers[0]].Dump();
		}
	}
	
}
