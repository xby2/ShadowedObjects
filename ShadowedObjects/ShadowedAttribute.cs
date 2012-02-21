using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShadowedObjects
{
	public class ShadowedAttribute : Attribute
	{
		public string ChangedStringTemplate { get; set; }
		
	}
}
