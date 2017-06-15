<Query Kind="Program">
  <NuGetReference>Dapper</NuGetReference>
  <NuGetReference>Deedle</NuGetReference>
  <Namespace>Dapper</Namespace>
  <Namespace>Deedle</Namespace>
</Query>

class MyRow
{
	public DateTimeOffset DeliveryDateTime { get; set; }
	
	public decimal MaxBuyArmit { get; set; }
	
	public decimal MaxSellArmit { get; set; }
}

class MyRow2
{
	public DateTime DeliveryDateUTC { get; set; }

	public decimal Bid { get; set; }

	public decimal Ask { get; set; }
}


DateTimeOffset StartOfHour(DateTimeOffset time)
{
	DateTimeOffset dateTimeOffset = time;

	return new DateTimeOffset(dateTimeOffset.Year, dateTimeOffset.Month, dateTimeOffset.Day, dateTimeOffset.Hour, 0, 0, time.Offset)
		.ToUniversalTime().ToLocalTime();
}
void Main()
{
	var day = DateTimeOffset.Now.AddDays(-1);
	var flex0 = Frame.FromRecords(GetFlex(day));

	

	var flex = flex0.IndexRows<DateTimeOffset>("DeliveryDateTime").SortRowsByKey();
	
	var groupedFlex = flex.GroupRowsUsing((k,row) => StartOfHour(k))
	
	var imb = Frame.FromRecords(GetImb(day));
	
	flex.Print();
}

private IEnumerable<MyRow> GetFlex(DateTimeOffset day)
{	
	using (var con = new SqlConnection("Data Source=RoboTrader;Initial Catalog=RoboTrader;Integrated Security=SSPI;Connect Timeout=30"))
	{
		var sql = @"SELECT [DeliveryDateTime],[MaxBuyArmit] ,[MaxSellArmit] FROM [Robotrader].[dbo].[BelgiumImbalanceFlexibility] where [DeliveryDateTime] >= @StartDate and [DeliveryDateTime] < @EndDate";
		var results = con.Query<MyRow>(sql,
			new { StartDate = day.LocalDateTime.Date, EndDate = day.LocalDateTime.Date.AddDays(1) }, null, true);

		return results;
	}
}

private IEnumerable<MyRow2> GetImb(DateTimeOffset day)
{
	using (var con = new SqlConnection("Data Source=RoboTrader;Initial Catalog=RoboTrader;Integrated Security=SSPI;Connect Timeout=30"))
	{
		var sql = @"SELECT [DeliveryDateUTC],[Bid],[Ask] FROM [Robotrader].[dbo].[BelgiumImbalance] where DeliveryDateUTC >= @StartDate and DeliveryDateUTC < @EndDate";
		var results = con.Query<MyRow2>(sql,
			new { StartDate = day.LocalDateTime.Date, EndDate = day.LocalDateTime.Date.AddDays(1) }, null, true);

		return results;
	}
}

// Define other methods and classes here
