<Query Kind="Program">
  <Reference Relative="..\..\..\Identity\Source\Identity.Fetcher\Identity.OAuth\bin\Identity.Domain.dll">C:\git\Identity\Source\Identity.Fetcher\Identity.OAuth\bin\Identity.Domain.dll</Reference>
  <Reference Relative="..\..\..\Identity\Source\Identity.Fetcher\Identity.OAuth\bin\Identity.Infrastructure.dll">C:\git\Identity\Source\Identity.Fetcher\Identity.OAuth\bin\Identity.Infrastructure.dll</Reference>
  <Namespace>Identity.Domain</Namespace>
  <Namespace>Identity.Domain.Events</Namespace>
  <Namespace>Identity.Infrastructure.Feeders</Namespace>
  <Namespace>Identity.Infrastructure.Helpers</Namespace>
  <Namespace>Identity.Infrastructure.Repositories</Namespace>
  <Namespace>Identity.Infrastructure.Services.NLP</Namespace>
</Query>

void Main()
{
	XX();
}

class MyLogger : ILogger
{
	public void Info(string message) { Console.WriteLine(message); }
	public void Error(string message, Exception ex) { Console.WriteLine(message + " " + ex.Message); }
}

public class ChannelLinkEventListener : IChannelLinkEventListener
{	
	public void Add(IChannelLinkEvent e)
	{

	}
}

IEnumerable<FeedItem> FetchItems(string subreddit)
{
	return File
						.ReadAllLines(String.Format(@"C:\transit\RedditPosts\Input\{0}.csv", subreddit))
						.Select(line => line.Split(';')[0])						
						.Select(title => new FeedItem
						{
							Title = title.Length > 255 ? title.Substring(0,255) : title,
							CreatedAt = DateTimeOffset.Now,
							Content = "",
							Links = new[] { new Uri(String.Format("http://{0}.com", Guid.NewGuid())) },
							Tags = new[] { subreddit },
							UpdatedAt = DateTimeOffset.Now
						});
}


void XX()
{
	var commonWordsFile = @"C:\git\Identity\Source\Identity.Fetcher\Identity.Infrastructure\commonwords-english.txt";
	var commonNounsFile = @"C:\transit\RedditPosts\nouns.csv";
	var helper = new EnglishLanguage(commonWordsFile, commonNounsFile);
	var nlpClient = new GoogleNLPClient("AIzaSyBAPJ3LgmXm-DrmG6CiZ6AHslkMl8C999U", "https://language.googleapis.com/v1/documents:analyzeEntities", helper);


	using (SqlConnection con = new SqlConnection(@"Data Source=v5kf9wt87u.database.windows.net;Initial Catalog=Identity;Persist Security Info=True;User ID=jweibel;Password=MiInPw01;Connect Timeout=30"))
	{
		con.Open();

		using (var trx = con.BeginTransaction())
		{
			var nlpRepo = new NLPEntityRepository(trx);
			var postRepo = new PostRepository(trx);
			var userRepo = new UserRepository(trx);
			var channelRepo = new ChannelRepository(trx);			
			var postAnalyzer = new PostNlpAnalyzer(nlpRepo, helper, nlpClient);
			var feedProcessor = new FeedProcessor(new MyLogger(), channelRepo, userRepo, postRepo, postAnalyzer);
			var rssFeederUser = userRepo.FindByName("rssfeeder");
			
			var items = FetchItems("other").Skip(1000).Take(1000).ToList();

			var feed = new Identity.Domain.Feed
			{
				ChannelId = 30115,
				Id = 1000,
				LastFetch = DateTime.Now,
				Type = Identity.Domain.FeedType.Rss,
				Url = ""
			};
			
			feedProcessor.ProcessFeed(rssFeederUser, feed, new ChannelLinkEventListener(), items);

			trx.Commit();
		}
	}
}

// Define other methods and classes here
