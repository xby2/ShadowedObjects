using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShadowedObjects;

namespace ShadowedObjectsTests
{
	[TestClass]
	public class ShadowedObjectTests
	{
		public class TestLevelA
		{
			public virtual string name { get; set; }

			public TestLevelA()
			{
				name="initial";
			}

			public virtual Collection<TestLevelA> NestedAs { get; set; }

			public virtual ArrayList UntypedList { get; set; }

			public virtual TestLevelB B { get; set; }
		}

		public class TestLevelB
		{
			public virtual int Bstuff { get; set; }

			public TestLevelB(){ Bstuff = 1; }
		}

		[TestMethod]
		public void StringResetToOriginalTest()
		{			
			var A = ShadowedObject.Create<TestLevelA>();
			A.name = "changed 1";
			A.name = "changed 2";
			A.ResetToOriginal(a=>a.name);
			Assert.IsTrue(A.name == "initial");
		}

		[TestMethod]
		public void CollectionResetToOriginalTest()
		{
			var A = ShadowedObject.Create<TestLevelA>();
			A.NestedAs = new Collection<TestLevelA>();

			A.BaselineOriginals();
			
			A.NestedAs.Add(new TestLevelA());
			A.NestedAs.Add(new TestLevelA());

			A.ResetToOriginal(a => a.NestedAs);

			Assert.IsTrue(A.NestedAs.Count < 1);			
		}


		[TestMethod]
		public void NonGenericCollectionResetToOriginalTest()
		{
			var A = ShadowedObject.Create<TestLevelA>();
			A.UntypedList = new ArrayList();

			A.BaselineOriginals();

			A.UntypedList.Add(new TestLevelA());
			A.UntypedList.Add(new TestLevelA());

			A.ResetToOriginal(a => a.UntypedList);

			Assert.IsTrue(A.UntypedList.Count < 1);
		}

		[TestMethod]
		public void CollectionResetAndRefillTest()
		{
			var A = ShadowedObject.Create<TestLevelA>();
			A.NestedAs = new Collection<TestLevelA>();

			A.BaselineOriginals();

			A.NestedAs.Add(new TestLevelA());
			A.NestedAs.Add(new TestLevelA());

			A.ResetToOriginal(a=>a.NestedAs);

			A.NestedAs.Add(new TestLevelA());
			A.NestedAs.Add(new TestLevelA());
			A.NestedAs.Add(new TestLevelA());
			A.NestedAs.Add(new TestLevelA());

			Assert.IsTrue(A.NestedAs.Count == 4);
		}

		[TestMethod]
		public void ResetsOnMultipleObjects()
		{
			var A = ShadowedObject.Create<TestLevelA>();
			A.NestedAs = new Collection<TestLevelA>();
			A.B = ShadowedObject.Create<TestLevelB>();
			A.B.Bstuff = 2;

			A.B.BaselineOriginals();
			A.BaselineOriginals();

			A.NestedAs.Add(new TestLevelA());
			A.NestedAs.Add(new TestLevelA());

			A.B.Bstuff = 3;

			A.ResetToOriginal(a => a.NestedAs);
			A.B.ResetToOriginal(b => b.Bstuff);

			Assert.IsTrue(A.B.Bstuff == 2);
			Assert.IsTrue(A.NestedAs.Count == 0);
		}
	}
}
