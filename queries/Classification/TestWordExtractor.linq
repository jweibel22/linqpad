<Query Kind="Program">
  <Reference Relative="..\..\..\Identity\Source\Identity.Fetcher\Identity.OAuth\bin\Identity.Infrastructure.dll">C:\git\Identity\Source\Identity.Fetcher\Identity.OAuth\bin\Identity.Infrastructure.dll</Reference>
  <Namespace>Identity.Infrastructure.Services.NLP</Namespace>
</Query>

class Item
{
	public string Title { get; set; }

	public string SubReddit { get; set; }
}


IEnumerable<Item> FetchFile(string folder, string subreddit)
{

	using (StreamReader sr = new StreamReader(Path.Combine(folder, subreddit + ".csv")))
	{
		{
			int i = 0;

			while (sr.Peek() >= 0)
			{
				i++;
				var line = sr.ReadLine().Split(',');
				if (line.Length >= 5)
					yield return new Item { Title = line[4], SubReddit = subreddit };
			}
		}
	}
}

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

	var folder = @"C:\git\reddit-classifier\data";
	var allfiles = new[] { 
		"programming", 
		"technology", 
		"HistoryPorn", 
		"science", 
		"politics",
		"music", 
		"movies", 
		"foodporn", 
		"art", 
		"worldnews", 
		"futurology", 
//		"", 
//		"", 
//		"", 
//		"", 
//		"", 
//		"", 
//		"", 
//		"", 
//		"", 
//		"", 
//		"", 
//		"", 
//		"", 
//		"", 
//		"", 
//		"", 
//		"", 
//		"", 
//		"", 
		
		};
	var items = allfiles.SelectMany(f => FetchFile(folder, f)).Take(1000).ToList();
	var inputs = items.Select(item => item.Title).Distinct().ToDictionary(item => item, item => wordExtractor.GetWords(item));

	foreach (var kv in inputs)
	{
		kv.Key.Dump();
		kv.Value.Dump();
	}
}

// Define other methods and classes here
