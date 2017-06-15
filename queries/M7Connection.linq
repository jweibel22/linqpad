<Query Kind="Program">
  <Reference Relative="..\..\dc.intraday.ate\DC.Intraday.ATE.MessageApi.M7\bin\Debug\DC.Intraday.ATE.MessageApi.M7.dll">C:\git\dc.intraday.ate\DC.Intraday.ATE.MessageApi.M7\bin\Debug\DC.Intraday.ATE.MessageApi.M7.dll</Reference>
  <NuGetReference Prerelease="true">DC.Foundation.EasyNetQ</NuGetReference>
  <Namespace>DC.Foundation.EasyNetQ</Namespace>
  <Namespace>DC.Intraday.ATE.MessageApi.M7</Namespace>
  <Namespace>EasyNetQ</Namespace>
  <Namespace>EasyNetQ.Topology</Namespace>
</Query>

void Main()
{
	
	var bus = DC.Foundation.EasyNetQ.Configuration.Configure.With().CreateBus("host=advsimu1.epex.m7.deutsche-boerse.com").GiveMeTheBus();

	var username = "CXDVNY04";

	var corrId = Guid.NewGuid().ToString();
	var queueName = String.Format("m7.private.responseQueue.{0}.{1}", username, Guid.NewGuid().ToString());

	
	var login = new LoginReq
	{
		disconnectAction = disconnectActType.DEACT_USER_ORDRS,
		force = true,
		user = username,
		StandardHeader = new StandardHeaderType
		{
			marketId = "EPEX"			
		}
	};
	
	var responseQueue = bus.Advanced.QueueDeclare(queueName, false, false, true, true);

	var messageProperties = new MessageProperties
	{
		AppId = "DJEKLM_0",
		ContentType = "x-comxerv/request; version=42",
		CorrelationId = corrId,
		Expiration = "20000",
		UserId = username,
		ReplyTo = queueName,
		DeliveryMode = 1,
		Priority = 9
	};

	UserRprt loginDetails;

	bus.Advanced.Consume(responseQueue, (bytes, prop, info) =>
	{
		if (prop.CorrelationId == corrId)
		{
			var response = login.DeserializeResp(bytes);

			if (response.IsExpected)
			{
				loginDetails = response.ExpectedResponse;
			}
			else
			{
				response.RawErr.Dump();
			}
		}
	});
	
	bus.Advanced.Publish(new Exchange("comxerv.requestExchange."+username), login.RoutingKey, true, messageProperties, login.SerializeToBytes());
	
	
	Thread.Sleep(30000)	;
	
	bus.Dispose();

//	var response = bus.Request<LoginReq, LogoutReq>(new LoginReq
//	{
//		user
//	}
//	);
	
	
}

// Define other methods and classes here
