using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CastleDynamicProxyPOC.Model
{
	public class Policy
	{
		public Policy()
		{
			AccountNumber = "ORIGINAL ACCOUNT";
			Coverages = new List<Coverage>();	
		}

		
		public virtual String AccountNumber { get; set; }

		public virtual List<Coverage> Coverages { get; set; }

		public void touch()
		{
			Console.WriteLine("touched.");
		}
	}
}
