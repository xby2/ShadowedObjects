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

			public virtual TestLevelC C { get; set; }

			public TestLevelB(){ Bstuff = 1; }
		}

		public class TestLevelC
		{
			public virtual string CStuff { get; set; }
		}

		#region Resetting Values

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
		public void ChildObjectResetToOriginalTest()
		{
			var A = ShadowedObject.Create<TestLevelA>();
			A.NestedAs = new Collection<TestLevelA>();
			A.B = ShadowedObject.Create<TestLevelB>();
			A.B.Bstuff = 2;

			A.BaselineOriginals();
			A.B.BaselineOriginals();

			
			A.B = ShadowedObject.Create<TestLevelB>();
			A.B.Bstuff = 3;

			//This works without recursion because the entire B object, along with the BStuff Property is reset to the B with a BStuff of 2.
			A.ResetToOriginal(a=>a.B);

			Assert.IsTrue(A.B.Bstuff == 2);
		}


		[TestMethod]
		public void RecursionResetToOriginalTest()
		{
			var A = ShadowedObject.Create<TestLevelA>();
			A.NestedAs = new Collection<TestLevelA>();
			A.B = ShadowedObject.Create<TestLevelB>();
			A.B.Bstuff = 2;

			A.BaselineOriginals();
			A.B.BaselineOriginals();

			A.B.Bstuff = 3;

			//This WON'T work without recursion because the B here still is the original B, nothing to reset.
			A.ResetToOriginal(a => a.B);

			Assert.IsTrue(A.B.Bstuff == 2);
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

	#endregion

		#region Checking for Changes flag
		[TestMethod]
		public void CheckingForChangedObject()
		{
			var A = ShadowedObject.Create<TestLevelA>();
			A.name = "asfd";

			A.BaselineOriginals();

			A.name = "xyz";

			Assert.IsTrue(A.HasChanges());
		}

		[TestMethod]
		public void RecursiveCheckingForChangedObject()
		{
			var A = ShadowedObject.Create<TestLevelA>();
			A.B = ShadowedObject.Create<TestLevelB>();

			A.BaselineOriginals();
			A.B.BaselineOriginals();

			A.B.Bstuff = 2;

			Assert.IsTrue(A.HasChanges());
		}

		[TestMethod]
		public void RecursiveCheckingForChangedCollection()
		{
			var A = ShadowedObject.Create<TestLevelA>();
			A.NestedAs = new Collection<TestLevelA>();

			A.BaselineOriginals();

			A.NestedAs.Add(new TestLevelA());
			A.NestedAs.Add(new TestLevelA());

			A.ResetToOriginal(a => a.NestedAs);

			Assert.IsTrue(A.HasChanges());
		}

		[TestMethod]
		public void RecursiveTwoLevelsCheckingForChangedObject()
		{
			var A = ShadowedObject.Create<TestLevelA>();
			A.B = ShadowedObject.Create<TestLevelB>();
			A.B.C = ShadowedObject.Create<TestLevelC>();

			A.BaselineOriginals();
			A.B.BaselineOriginals();

			A.B.C.CStuff = "asdf";

			Assert.IsTrue(A.HasChanges());
		}


		[TestMethod]
		public void RecursiveCheckingForChangedObjectAfterReset()
		{
			var A = ShadowedObject.Create<TestLevelA>();
			A.B = ShadowedObject.Create<TestLevelB>();
			A.B.C = ShadowedObject.Create<TestLevelC>();

			A.BaselineOriginals();
			A.B.BaselineOriginals();

			A.B.C.CStuff = "asdf";

			A.B.C.ResetToOriginal(c=>c.CStuff);

			Assert.IsFalse(A.B.C.HasChanges());
		}		

		#endregion
	}
}
