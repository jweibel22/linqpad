<Query Kind="Program">
  <Reference Relative="..\..\Identity\Source\Identity.Fetcher\Identity.OAuth\bin\Identity.Domain.dll">C:\git\Identity\Source\Identity.Fetcher\Identity.OAuth\bin\Identity.Domain.dll</Reference>
  <Reference Relative="..\..\Identity\Source\Identity.Fetcher\Identity.OAuth\bin\Identity.Infrastructure.dll">C:\git\Identity\Source\Identity.Fetcher\Identity.OAuth\bin\Identity.Infrastructure.dll</Reference>
  <NuGetReference>Lucene.Net.Store.Azure</NuGetReference>
  <NuGetReference>NetMQ</NuGetReference>
  <NuGetReference>WindowsAzure.Storage</NuGetReference>
  <Namespace>Identity.Domain.RedditIndexes</Namespace>
  <Namespace>Identity.Infrastructure.Helpers</Namespace>
  <Namespace>Identity.Infrastructure.Reddit</Namespace>
  <Namespace>Microsoft.WindowsAzure.Storage</Namespace>
  <Namespace>NetMQ.Sockets</Namespace>
  <Namespace>NetMQ</Namespace>
</Query>


		
void Main()
{	
	string topic = "";
	Console.WriteLine("Subscriber started for Topic : {0}", topic);

	using (var subSocket = new SubscriberSocket())
	{
		subSocket.Options.ReceiveHighWatermark = 1000;
		subSocket.Connect("tcp://localhost:55077");
		subSocket.Subscribe(topic);
		Console.WriteLine("Subscriber socket connecting...");
		while (true)
		{		
			var msg = subSocket.ReceiveFrameBytes(); // .ReceiveFrameString();
			var msgStr = Encoding.UTF8.GetString(msg);
			Console.WriteLine(msgStr);
		}
	}

}

// Define other methods and classes here
