<Query Kind="Program">
  <Reference Relative="..\..\..\Identity\Source\Identity.Fetcher\Identity.OAuth\bin\Identity.Infrastructure.dll">C:\git\Identity\Source\Identity.Fetcher\Identity.OAuth\bin\Identity.Infrastructure.dll</Reference>
  <Namespace>Identity.Infrastructure.Reddit</Namespace>
</Query>

void Main()
{
//	XX("Art");
	
	var subreddits = new[] {
		"programming",
		"technology",
		"HistoryPorn",
		"science",
		"politics",
		"music",
		"movies",
		"FoodPorn",
		"art",
		"worldnews",
		"Futurology"};
		
	Other(subreddits);
//
//	foreach (var sr in subreddits)
//	{
//		XX(sr);
//	}
}

void Other(string[] subreddits)
{
	var folder = @"C:\transit\RedditPosts\Input\";
	var files = new[] { "RS_2010-01", "RS_2011-01", "RS_2012-01", "RS_2013-01", "RS_2014-01", "RS_2015-01", "RS_2016-10", "RS_2016-12" }
					.Select(f => Path.Combine(folder, f + ".csv")).ToArray();

	using (var writer = new StreamWriter(Path.Combine(folder, "other.csv")))
	{
		foreach (var post in FetchAll(files).Where(p => !subreddits.Contains(p.SubReddit)).Take(200000))
		{
			writer.WriteLine(String.Format("{0};{1}", post.Title, post.SubReddit));
		}
	}
}

void XX(string subreddit)
{
	var folder = @"C:\transit\RedditPosts\Input\";
	var files = new[] {"RS_2010-01","RS_2011-01","RS_2012-01","RS_2013-01","RS_2014-01","RS_2015-01","RS_2016-10","RS_2016-12"}
					.Select(f => Path.Combine(folder, f + ".csv")).ToArray();

	using (var writer = new StreamWriter(Path.Combine(folder, subreddit + ".csv")))
	{
		foreach (var post in FetchAll(files).Where(p => p.SubReddit == subreddit))
		{
			writer.WriteLine(String.Format("{0};{1}", post.Title, post.SubReddit));
		}
	}
}

IEnumerable<Post> FetchAll(params string[] files)
{
	var reader = new CsvReader();
	foreach (var file in files)
	{
		foreach (var x in reader.Fetch(file))
		{
			yield return x;
		}
	}
}

// Define other methods and classes here