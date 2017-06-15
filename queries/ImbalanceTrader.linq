<Query Kind="Program">
  <Reference Relative="..\..\algo\infrastructure\Source\BiCoopUpdater\DC.Intraday.Algo.BiCoopUpdater.Domain\bin\Debug\DC.Algo.Imbalance.Domain.dll">C:\git\algo\infrastructure\Source\BiCoopUpdater\DC.Intraday.Algo.BiCoopUpdater.Domain\bin\Debug\DC.Algo.Imbalance.Domain.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Configuration.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Security.dll</Reference>
  <NuGetReference Prerelease="true">DC.Foundation.EasyNetQ</NuGetReference>
  <Namespace>DC.Foundation.EasyNetQ.Configuration</Namespace>
  <Namespace>System.Configuration</Namespace>
  <Namespace>DC.Algo.Imbalance.Domain</Namespace>
</Query>

void Main()
{
	var bus = Configure
				.With()
				.Logging(x => x.None())
				.UsingConventions(new DcConventions { JebusRpcCompatibilityModeEnabled = true, UseDetoxicator = true })
				.CreateBus("host=rabbitmqreliable")
				.GiveMeTheBus();

	var msg = new EpexTrade
	{
		Algo = Algo.BEBAL,
		TradeTime = new DateTime(2017,5,19,17,0,0),
		DeliveryStart = new DateTime(2017,5,19,18,0,0),
		DeliveryEnd = new DateTime(2017,5,19,19,0,0),
		Direction = TradeDirection.Buy,
		Price = 31.4M,
		Volume = 1.0M
	};
	
	//bus.Publish(msg);
}

// Define other methods and classes here
