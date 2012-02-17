using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CastleDynamicProxyPOC.Model;

namespace CastleDynamicProxyPOC
{
	class Program
	{
		static void Main(string[] args)
		{
	
			//intercepted
			var policy2 = ShadowedObject.Create<Policy>();
			policy2.AccountNumber = "987xyz";
			policy2.AccountNumber = "456lmn";
			policy2.touch();

			policy2.ResetToOriginal("AccountNumber");

			policy2.Coverages.Add(ShadowedObject.Create<Coverage>());
			policy2.Coverages[0].name = "ChangedVal";

			policy2.ResetToOriginal("Coverages");

			Console.ReadLine();
		}
	}
}
