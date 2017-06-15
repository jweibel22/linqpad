<Query Kind="Statements">
  <NuGetReference Prerelease="true">DC.Foundation.EasyNetQ</NuGetReference>
  <NuGetReference>DC.VPP.Messages</NuGetReference>
  <Namespace>DC.Foundation.EasyNetQ.Configuration</Namespace>
</Query>

var bus = Configure.With().CreateBus("host=rabbitmqlantest").GiveMeTheBus();
var message = new DC.VPP.Messages.System.HeartBeat.V1.HeartBeat();
bus.Publish(message, "aaa");