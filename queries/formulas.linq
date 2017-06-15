<Query Kind="Program" />



class Formula
{
	static Random r = new Random();
	
	static double Rnd()
	{
		return Math.Round(r.NextDouble() * 50, 1);
	}

	public string Filename { get; set; }
	
	public object[] Args { get; set; }

	public string AsString
	{
		get
		{
			return String.Format(File.ReadAllText(Filename), Args);
		}
	}
	
	public static Formula CiMinus(double d,double t, double s,double n1, double n2)
	{
		return new Formula
		{
			Args = new object[] { d, t, s, n1, n2, d - (t * s * Math.Sqrt(1.0 / n1 + 1.0 / n2)) },
			Filename = @"c:\transit\ciminus.xml"
		};
	}
	public static Formula CiPlus(double d, double t, double s, double n1, double n2)
	{
		return new Formula
		{
			Args = new object[] { d, t, s, n1, n2, d + (t * s * Math.Sqrt(1.0 / n1 + 1.0 / n2)) },
			Filename = @"c:\transit\ciplus.xml"
		};
	}

	public static Formula TTest(double d, double s, double n1, double n2)
	{
		return new Formula
		{
			Args = new object[] { d, s, n1, n2, Math.Abs(d / (s* Math.Sqrt((1/n1) + (1/n2)))) },
			Filename = @"c:\transit\ttest.xml"
		};
	}

	public static Formula SD1(double s1, double s2, double n1, double n2)
	{
		return new Formula
		{		
			Args = new object[] { n1, s1, n2, s2, n1, n2, Math.Sqrt( ((n1-1)*s1*s1 + (n2-1)*s2*s2)/(n1+n2-2)) },
			Filename = @"c:\transit\sd1.xml"
		};
	}


	public static Formula CiMinus()
	{
		return CiMinus(Rnd(), Rnd(), Rnd(), Rnd(), Rnd());
	}

	public static Formula CiPlus()
	{
		return CiPlus(Rnd(), Rnd(), Rnd(), Rnd(), Rnd());
	}
	
	public static Formula TTest()
	{		
		return TTest(Rnd(),Rnd(),Rnd(),Rnd());
	}
	
	public static Formula SD1()
	{
		return SD1(Rnd(),Rnd(),Rnd(),Rnd());
	}

}




void Main()
{

	var rnd = new Random();

	var formulas = Enumerable.Range(1, 40).Select(i =>
		 {
		 	var r = rnd.NextDouble() * 4;
			 if (r < 1)
			 {
			 	return Formula.CiMinus();
			 }
			 else if (r < 2)
			 {
				return Formula.CiPlus();
			 }
			 else if (r < 3)
			 {
				return Formula.TTest();
			 }
			 else
			 {
				return Formula.SD1();
			 }					
		 }
	
	).ToList();
	
	var s = String.Join("\r\n",formulas.Select(f => f.AsString)).Dump();
}

// Define other methods and classes here
