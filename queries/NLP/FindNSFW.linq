<Query Kind="Program">
  <Reference Relative="..\..\..\Identity\Source\Identity.Fetcher\Identity.OAuth\bin\Identity.Infrastructure.dll">C:\git\Identity\Source\Identity.Fetcher\Identity.OAuth\bin\Identity.Infrastructure.dll</Reference>
  <NuGetReference>Accord</NuGetReference>
  <NuGetReference>Accord.MachineLearning</NuGetReference>
  <NuGetReference>MathNet.Numerics</NuGetReference>
  <NuGetReference>MathNet.Numerics.Data.Text</NuGetReference>
  <NuGetReference>StemmersNet</NuGetReference>
  <Namespace>Accord</Namespace>
  <Namespace>Accord.MachineLearning</Namespace>
  <Namespace>Accord.MachineLearning.VectorMachines</Namespace>
  <Namespace>Accord.MachineLearning.VectorMachines.Learning</Namespace>
  <Namespace>Accord.Math</Namespace>
  <Namespace>Accord.Statistics.Kernels</Namespace>
  <Namespace>Identity.Infrastructure.Helpers</Namespace>
  <Namespace>Iveonik.Stemmers</Namespace>
  <Namespace>MathNet.Numerics</Namespace>
  <Namespace>MathNet.Numerics.Data.Text</Namespace>
</Query>

void Main()
{
	var stemmer = new EnglishStemmer();
	var commonWordsFile = @"C:\git\Identity\Source\Identity.Fetcher\Identity.Infrastructure\commonwords-english.txt";
	var commonNounsFile = @"C:\transit\RedditPosts\nouns.csv";
	var lang = new EnglishLanguage(commonWordsFile, commonNounsFile);

	CreateInput(stemmer, lang);
//	Train(stemmer, lang);

}

class InputRow
{
	public string SubReddit { get; set; }
	
	public double[] Words { get; set; }
	
	public int Label { get; set; }
}


void WriteInputs(string filename, string[] SubReddits, double[][] Values)
{
	using (var writer = new StreamWriter(filename))
	{
		for (int i = 0; i < SubReddits.Length; i++)
		{
			writer.WriteLine(String.Format("{0};{1}", SubReddits[i], Values[i].Join(",")));
		}
	}
}


IEnumerable<Tuple<string, double[]>> ReadInputs(string filename)
{
	using (var sr = new StreamReader(filename))
	{
		while (sr.Peek() >= 0)
		{
			var line = sr.ReadLine().Split(';');
			yield return new Tuple<string, double[]>(line[0], line[1].Split(',').Select(s => Double.Parse(s)).ToArray());
		}
	}
}

IEnumerable<int> ReadLabels(string filename)
{
	using (var sr = new StreamReader(filename))
	{
		while (sr.Peek() >= 0)
		{
			var line = sr.ReadLine().Split(';');
			yield return line[1] == "1" ? 1 : 0;
		}
	}
}

IEnumerable<InputRow> LoadFromCsv(string folder)
{
	var inputs = ReadInputs(folder + "input.csv").ToList();
	var labels = ReadLabels(folder + "labels.csv").ToList();

	return inputs.Zip(labels, (x, y) => new InputRow
	{
		SubReddit = x.Item1,
		Words = x.Item2,
		Label = y
	});
}


void Train(EnglishStemmer stemmer, EnglishLanguage lang)
{
	var folder = @"c:\transit\RedditPosts\svm\";	
	var vocabulary = GetVocabulary(stemmer, lang, Fetch(@"C:\transit\RedditPosts\Input\RS_2011-01.csv").ToList());
	var all = LoadFromCsv(folder);
	var trainingData = all.OrderByDescending(r => r.Label).Take(100);
	var testData = all.Except(trainingData).ToList();


	var teacher = new SequentialMinimalOptimization<Gaussian>()
	{
		UseComplexityHeuristic = true,
		UseKernelEstimation = true // Estimate the kernel from the data
	};

	SupportVectorMachine<Gaussian> svm = teacher.Learn(trainingData.Select(r => r.Words).ToArray(), trainingData.Select(r => r.Label).ToArray());

	
	var topWeights = svm.Weights.Select((w, i) => new { w, i}).OrderBy(wi => wi.w).Take(100);

	foreach (var w in topWeights)
	{
		Console.WriteLine("{0} = {1}", vocabulary[w.i], w.w);
	}


	bool[] answers = svm.Decide(testData.Select(r => r.Words).ToArray());

	for (int i = 0; i < answers.Length; i++)
	{
		Console.WriteLine("{0};{1}", testData[i].SubReddit, answers[i]);
	}
}

List<string> GetVocabulary(EnglishStemmer stemmer, EnglishLanguage lang, List<Item> items)
{
	return items.SelectMany(item => lang.GetWords(item.Title)).Select(word => stemmer.Stem(word)).Distinct().ToList();
}

static IList<string> CreateVocabList(IEnumerable<string[]> dataSet)
{
	return dataSet.SelectMany(l => l).Distinct().ToList();
}

static IEnumerable<double> GetWordVector(IEnumerable<string> vocabList, string[] text)
{
	return vocabList.Select(word => text.Contains(word) ? 1.0 : 0.0);
}


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

IEnumerable<Item> Fetch(string filename)
{
	using (StreamReader sr = new StreamReader(filename))
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



void CreateInput(EnglishStemmer stemmer, EnglishLanguage lang)
{
	var items = Fetch(@"C:\transit\RedditPosts\Input\RS_2011-01.csv").Take(200000).ToList();		
	var all = new Dictionary<string, MathNet.Numerics.LinearAlgebra.Vector<double>>();	
	var vocabulary = GetVocabulary(stemmer, lang, items);

	var map = new Dictionary<string, List<string>>
			{
				{"other", new List<string>()},
				{"programming", new List<string>()},
				{"politics", new List<string>()},
				{"technology", new List<string>()},
				{"science", new List<string>()},
				{"history", new List<string>()},
				{"art", new List<string>()},
			};

	var xx = items
		.Where(i => map.ContainsKey(i.SubReddit.ToLower()))
		.GroupBy(i => i.SubReddit)
		.ToDictionary(g => g.Key, g => g.Take(2000).Select(f => f.Title));
		

	


	//foreach (var item in items) //.Where(sr => sr.Key == "sex")
//	{

		
//		var text = subreddit.SelectMany(item => lang.GetWords(item.Title)).Distinct().ToDictionary(t => t, t => 1);;
		
//		Console.WriteLine("\r\n{0}", subreddit.Key);
		
//		var allTitles = subreddit.SelectMany(item => lang.GetWords(item.Title));
		//var text = subreddit.SelectMany(item => lang.GetWords(item.Title)).Select(word => stemmer.Stem(word)).GroupBy(w => w).ToDictionary(t => t.Key, t => t.Count());;
//
//		foreach (var x in text.Where(kv => kv.Value > 2).OrderByDescending(kv => kv.Value).Take(5))
//		{
//			Console.WriteLine("{0} = {1}", x.Key, x.Value);
//		}
		
		//all[subreddit.Key] = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.SparseOfEnumerable(vocabulary.Select(word => text.ContainsKey(word) ? text[word] : 0.0));
	}

			
	//WriteInputs(@"c:\transit\RedditPosts\svm\input.csv", all.Keys.ToArray(), all.Values.Select(v => v.ToArray()).ToArray());

}