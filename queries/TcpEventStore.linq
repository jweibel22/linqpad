<Query Kind="Program">
  <NuGetReference>EventStore.Client</NuGetReference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>EventStore.ClientAPI</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
</Query>

public class EventStoreClient
{
	private IEventStoreConnection connection;
	private byte[] metadata;

	public EventStoreClient(string baseUrl)
	{
		connection = EventStoreConnection.Create(new Uri(baseUrl));
		connection.ConnectAsync().Wait();
		metadata = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { Tags = new[] { "EpexData" } }));
	}

	public void Write(string streamId, string typeName, string data)
	{
		var eventData = new EventData(Guid.NewGuid(), typeName, true, Encoding.UTF8.GetBytes(data), metadata);

		connection.AppendToStreamAsync(streamId, ExpectedVersion.Any, eventData).Wait();
	}

	public void Write(string streamId, string typeName, IEnumerable<string> data)
	{
		var eventData = data.Select(d => new EventData(Guid.NewGuid(), typeName, true, Encoding.UTF8.GetBytes(d), metadata));

		connection.AppendToStreamAsync(streamId, ExpectedVersion.Any, eventData).Wait();
	}


	public string Read(string streamId, int idx)
	{
		var events = connection.ReadStreamEventsForwardAsync(streamId, idx, 1, false).Result;
		if (events.Events.Length == 1)
		{
			return Encoding.UTF8.GetString(events.Events[0].Event.Data);
		}
		return null;
	}

	public IEnumerable<string> Read(string streamId, int idx, int count)
	{
		var events = connection.ReadStreamEventsForwardAsync(streamId, idx, count, false).Result;
		return events.Events.Select(e => Encoding.UTF8.GetString(e.Event.Data));
	}

}

public class ConnectionBroker
{
	private IEventStoreConnection connection;

	private void Create()
	{
		connection = EventStoreConnection.Create("ConnectTo=tcp://admin:changeit@localhost:1113;MaxReconnections=10");
		connection.Disconnected += (sender, args) => connection = null;		
		connection.Closed += (sender, args) => connection = null;		
		connection.ConnectAsync().Wait();
	}

	public IEventStoreConnection Get()
	{
		if (connection == null)
			Create();
			
		return connection;
	}
		
}

void Main()
{
//	var metadata = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { Tags = new[] { "EpexData" } }));
//	var settings = ConnectionSettings.Create().KeepReconnecting();
//	var sss = ConnectionSettings.Create(). .KeepReconnecting().Build();
	
	
//	var broker = new ConnectionBroker();
	var connection = EventStoreConnection.Create("ConnectTo=tcp://admin:changeit@srvt00083:1113");
//	var connection = EventStoreConnection.Create(new Uri("tcp://admin:changeit@localhost:1113"));
////	connection.Settings.Dump();
	connection.ConnectAsync().Wait();
//
//	using (var writer = new StreamWriter(@"c:\transit\blocksniper\allevents.json"))
//	{
//		for (var page = 0; page < 1; page++)
//		{
//			foreach (var e in connection.ReadStreamEventsForwardAsync("OmsEvents", 24096391, 1000, false).Result.Events)
//			{
//				//writer.WriteLine(Encoding.UTF8.GetString(e.Event.Data));
//			}
//		}
//	}
//	
//	var data = connection.ReadStreamEventsForwardAsync("OmsEvents", 24095391, 24100000-24095391, false).Result.Events;
//	
//	Encoding.UTF8.GetString(data.First().Event.Data)
	
//	var data = connection.ReadStreamEventsForwardAsync("OmsEvents", 989159, 2150, false).Result.Events;
	
//	
//	while (true)
//	{
//		try
//		{
//			var eventData = new EventData(Guid.NewGuid(), "jwe-type", true, Encoding.UTF8.GetBytes("aaaaa"), metadata);
//			var connection = broker.Get();
//			connection.AppendToStreamAsync("jwe-test", ExpectedVersion.Any, eventData).Wait();
//		}
//		catch (Exception ex)
//		{
//			ex.Message.Dump();
//		}
//		
//		System.Threading.Thread.Sleep(5000);
//	}
	

//	var client = new EventStoreClient("tcp://admin:changeit@localhost:1113");	
//	client.Write("jwe-test", "jwe-type", "aaaaa");

	var metadata = StreamMetadata.Create(4000000); 
	connection.SetStreamMetadataAsync("OmsEvents",ExpectedVersion.Any, metadata).Wait();
	connection.Close();
}

// Define other methods and classes here