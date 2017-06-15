<Query Kind="Program">
  <Reference Relative="..\..\..\Identity\Source\Identity.Fetcher\Identity.OAuth\bin\Identity.Infrastructure.dll">C:\git\Identity\Source\Identity.Fetcher\Identity.OAuth\bin\Identity.Infrastructure.dll</Reference>
  <NuGetReference>Accord</NuGetReference>
  <NuGetReference>Accord.MachineLearning</NuGetReference>
  <NuGetReference>StemmersNet</NuGetReference>
  <Namespace>Accord.MachineLearning.Bayes</Namespace>
  <Namespace>Accord.Statistics.Distributions.Fitting</Namespace>
  <Namespace>Accord.Statistics.Distributions.Univariate</Namespace>
  <Namespace>Identity.Infrastructure.Helpers</Namespace>
  <Namespace>Identity.Infrastructure.Services.NLP</Namespace>
  <Namespace>Iveonik.Stemmers</Namespace>
</Query>

class Item
{
	public string Title { get; set; }

	public string SubReddit { get; set; }
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

IEnumerable<Item> Fetch(string folder, string subreddit)
{
	using (StreamReader sr = new StreamReader(Path.Combine(folder, subreddit+ ".csv")))
	{
		{
			int i = 0;

			while (sr.Peek() >= 0)
			{
				i++;
				Processing(i);
				var line = sr.ReadLine().Split(';');
				if (line.Length == 2)
					yield return new Item { Title = line[0], SubReddit = line[1] };
			}
		}
	}

}


IEnumerable<Item> FetchFile(string folder, string subreddit)
{
	
	using (StreamReader sr = new StreamReader(Path.Combine(folder, subreddit+ ".csv")))
	{
		{
			int i = 0;

			while (sr.Peek() >= 0)
			{
				i++;
				Processing(i);
				var line = sr.ReadLine().Split(',');
				if (line.Length >= 5)
					yield return new Item { Title = line[4], SubReddit = subreddit };
			}
		}
	}

}

//add an extra 'none' class
//Remove low frequency features from the feature set
//add nlp entities as extra features?
//apply advanced feature selection techniques (remove feautures with low mutual information score and X2 score on all classes) from: http://cs229.stanford.edu/proj2012/GillieMalkani-SupervisedMulticlassClassificationOfTweets.pdf

void Main()
{
	var commonWordsFile = @"C:\git\Identity\Source\Identity.Fetcher\Identity.Infrastructure\commonwords-english.txt";
	var commonNounsFile = @"C:\transit\RedditPosts\nouns.csv";

	var options = new WordExtractionOptions
	{
		IgnoreCommonWords = true,
		RemovePunctuation = true,
		Stem = true
	};
	var wordExtractor = new WordExtractor(commonWordsFile, commonNounsFile, options);


	//	var items = Fetch(@"C:\transit\RedditPosts\Input\RS_2011-01.csv")
	//				.Take(200000)
	//				.Where(x => x.SubReddit == "programming" || x.SubReddit == "technology" 
	//							|| x.SubReddit == "HistoryPorn" || x.SubReddit == "science" || x.SubReddit == "politics")
	//				.ToList();

//	var folder = @"C:\git\reddit-classifier\data";
	var folder = @"C:\transit\RedditPosts\Input";
	var allfiles = new[] {
		"programming",
		//"technology",
		//"HistoryPorn",
//		"science",
		"politics",
//		"music",
//		"movies",
		//"foodporn",
		"art",
//		"worldnews",
//		"futurology",
		//"other"
	};
	var items = allfiles.SelectMany(f => Fetch(folder, f).Take(2000)).ToList();
	
	var trainingItems = items.GroupBy(item => item.SubReddit).SelectMany(g => g.Take(1000)).ToList();
	var testItems = items.GroupBy(item => item.SubReddit).SelectMany(g => g.Skip(1000)).ToList(); //items.Except(trainingItems);

	var featureExtractor = new FeatureExtractor(wordExtractor, items.Select(item => item.Title));
	
	var inputsX = trainingItems.Select(item => featureExtractor.GetFeatureVector(item.Title)).ToArray();
	var labelsX = trainingItems.Select(item => item.SubReddit).Distinct().ToList();
	var inputsY = testItems.Select(item => featureExtractor.GetFeatureVector(item.Title)).ToArray();
	var labelsY = testItems.Select(item => item.SubReddit).Distinct().ToList();

	var outputsX = trainingItems.Select(item => labelsX.IndexOf(item.SubReddit)).ToArray();
	var outputsY = testItems.Select(item => labelsY.IndexOf(item.SubReddit)).ToArray();

	var learner = new NaiveBayesLearning<NormalDistribution>();
	learner.Options.InnerOption = new NormalOptions
	{
		Regularization = 1e-5 // to avoid zero variances
	};

	// Estimate the Naive Bayes
	var nb = learner.Learn(inputsX, outputsX);

//	var testInput = File.ReadAllLines(@"C:\git\reddit-classifier\xxx.csv");
//	var testInputVectors = testInput.Select(item => featureExtractor.GetFeatureVector(item)).ToArray();

	// Classify the samples using the model
	int[] answers = nb.Decide(inputsY);

//	var xx = Enumerable.Range(0, answers.Length).Select(i => new Answer { Title = testInput[i], Subreddit = labels[answers[i]] });
//	foreach (var answer in xx.OrderBy(x => x.Subreddit))
//	{
//		Console.WriteLine("{0};{1}", answer.Title, answer.Subreddit);	
//	}

	var score = Enumerable.Range(0, answers.Length).Sum(i => answers[i] == outputsY[i] ? 1.0: 0.0) / (double)answers.Length;
	Console.WriteLine("Score = {0}", score);

//	for(int i = 0; i< answers.Length; i++)
//	{
//		Console.WriteLine("{0};{1}", testInput[i], labels[answers[i]]);
//	}
}

class Answer
{
	public string Title { get; set; }
	
	public string Subreddit { get; set; }
}

// Define other methods and classes here