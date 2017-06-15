<Query Kind="Program">
  <Reference Relative="..\..\algo\infrastructure\Source\BiCoopUpdater\DC.Intraday.Algo.BiCoopUpdater.Domain\bin\Debug\DC.Intraday.Algo.BiCoopUpdater.Domain.dll">C:\git\algo\infrastructure\Source\BiCoopUpdater\DC.Intraday.Algo.BiCoopUpdater.Domain\bin\Debug\DC.Intraday.Algo.BiCoopUpdater.Domain.dll</Reference>
  <NuGetReference>RavenDB.Database</NuGetReference>
  <Namespace>DC.Intraday.Algo.BiCoopUpdater.Domain.BiCoop</Namespace>
  <Namespace>Raven.Client.Embedded</Namespace>
</Query>

void Main()
{
	using (var store = new EmbeddableDocumentStore
	{
		DataDirectory = @"C:\DC\Algo\Data"
	})
	{
		store.Initialize();

		using (var session = store.OpenSession())
		{
			var trades = session.Query<TradedOnGrid>();

			trades.Count().Dump();
		}
	}
}

// Define other methods and classes here