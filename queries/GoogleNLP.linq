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
  <NuGetReference>morelinq</NuGetReference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <NuGetReference>RestSharp</NuGetReference>
  <Namespace>MoreLinq</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>RestSharp</Namespace>
</Query>

public class Text
{
	public string Content { get; set; }
}

    public class Entities
    {
        public IDictionary<Text, List<string>> Articles { get; set; }

        public Entities(IList<Text> texts)
        {            			
			Articles = texts.ToDictionary(t => t, t => new List<string>());						
        }

        public void Tag(Text article, string tag)
        {
            Articles[article].Add(tag);
        }
    }

public class GoogleNLPClient
{
	class Body
	{
		public Document document { get; set; }
		public string encodingType { get; set; }

		public static Body New(string content)
		{
			return new Body
			{
				encodingType = "UTF16",
				document = new Document
				{
					type = "PLAIN_TEXT",
					content = content
				}
			};
		}
	}

	class Document
	{
		public string type { get; set; }
		public string content { get; set; }
	}

	class ApiResponse
	{
		public IList<Entity> entities { get; set; }
		public string language { get; set; }
	}

	class Entity
	{
		public string name { get; set; }
		public string type { get; set; }
		public MetaData metadata { get; set; }
		public double salience { get; set; }
		public IList<Mention> mentions { get; set; }
	}

	class MetaData
	{
		public string mid { get; set; }
		public string wikipedia_url { get; set; }
	}

	class Mention
	{
		public string type { get; set; }
		public MentionText text { get; set; }
	}

	class MentionText
	{
		public string content { get; set; }
		public int beginOffset { get; set; }
	}


	private int IndexAtOffset(IList<Text> articles, int offset)
	{
		var separatorLength = 3;
		int idx = 0;
		int currentOffset = 0;

		try
		{
			while (idx < articles.Count && currentOffset + articles[idx].Content.Length + separatorLength <= offset)
			{				
				currentOffset += articles[idx].Content.Length + separatorLength;
				idx++;
			}
		}
		catch (Exception ex)
		{
			ex.Dump();
		}
		return idx;
	}

	private readonly string url;
	private readonly string apiKey;
	private readonly string[] commonEnglishWords;
	private readonly IRestClient client;

	public GoogleNLPClient(string[] commonEnglishWords, string apiKey, string url)
	{
		this.commonEnglishWords = commonEnglishWords;
		this.apiKey = apiKey;
		this.url = url;
		this.client = new RestClient("https://language.googleapis.com/v1/documents:analyzeEntities");
	}

	public Entities Get(IList<Text> texts)
	{
		var request = new RestRequest("", Method.POST);
		request.AddQueryParameter("key", apiKey);
		request.RequestFormat = DataFormat.Json;

		var content = String.Join(" | ", texts.Select(a => a.Content));
		var body = Body.New(content);
		request.AddBody(body);

		//content.Dump();		
		var response = client.Execute(request);
		var r = JsonConvert.DeserializeObject<ApiResponse>(response.Content);
		//var r = JsonConvert.DeserializeObject<ApiResponse>(File.ReadAllText(@"c:\transit\nlp1.json"));
		//r.Dump();
		
		var result = new Entities(texts);

		foreach (var x in r.entities)
		{
			foreach (var mention in x.mentions)
			{
				if (commonEnglishWords.Contains(mention.text.content.ToLower().Trim()))
				{
					continue;
				}

				var idx = IndexAtOffset(texts, mention.text.beginOffset);
				if (idx < texts.Count)
				{
					result.Tag(texts[idx], mention.text.content);
				}
			}
		}

		return result;
	}
}

class Document
{
	public long Id { get; set; }
	public string Title { get; set; }
	public string Description { get; set; }
}

IEnumerable<Document> FetchArticles(long[] cids)
{
	var from = new DateTime(2017, 1, 6, 0, 0, 0);
	var to = new DateTime(2017, 1, 6, 13, 48, 0);

	return ChannelItems
		.Join(Posts, ci => ci.PostId, p => p.Id, (ci, p) => new { ci, p })
		.Where(x => cids.Contains(x.ci.ChannelId) && x.ci.Created < to && x.ci.Created > from)
		.OrderByDescending(x => x.ci.Created)
		.Select(x => new Document { Id = x.p.Id, Title = x.p.Title.Trim(), Description = x.p.Description })
		.DistinctBy(x => x.Id)
		.ToList();
}

void Main()
{
	//System.Net.GlobalProxySelection.Select = new System.Net.WebProxy("127.0.0.1", 8888);

	var commonWordsFile = "commonwords-english.txt";
	var commonWords = System.IO.File.ReadAllLines(@"C:\git\Identity\Source\Identity.Fetcher\Identity.Infrastructure\" + commonWordsFile).Select(w => w.Trim().ToLower()).ToArray();

	var client = new GoogleNLPClient(commonWords,"AIzaSyBAPJ3LgmXm-DrmG6CiZ6AHslkMl8C999U", "https://language.googleapis.com/v1/documents:analyzeEntities");

	var articles = FetchArticles(new[] { 118L }).Select(d => new Text { Content = d.Title}).ToList();
	
	var result = client.Get(articles);

	foreach (var a in articles)
	{
		a.Content.Dump();
		result.Articles[a].Dump();		
	}
	
	
	//result.Articles.Dump();
}

// Define other methods and classes here