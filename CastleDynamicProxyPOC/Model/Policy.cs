﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace CastleDynamicProxyPOC.Model
{
	public class Policy
	{
		public Policy()
		{
			AccountNumber = "ORIGINAL ACCOUNT";
			Coverages = new ShadowCollection<Coverage>();	
		}

		
		public virtual String AccountNumber { get; set; }

		public virtual Collection<Coverage> Coverages { get; set; }

		public void touch()
		{
			Console.WriteLine("touched.");
		}
	}
}
