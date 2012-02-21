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
		[Shadowed]
		public class TestLevelA
		{
			public virtual string name { get; set; }

			public TestLevelA()
			{
				name="initial";
			}

			[Shadowed]
			public virtual Collection<TestLevelA> NestedAs { get; set; }

			public virtual Dictionary<string,TestLevelB> dictOfBs { get; set; } 

			public virtual ArrayList UntypedList { get; set; }

			[Shadowed]
			public virtual TestLevelB B { get; set; }

		}

		[Shadowed]
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
		public void StringResetAllPropertiesToOriginalTest()
		{
			var A = ShadowedObject.Create<TestLevelA>();
			A.name = "changed 1";
			A.name = "changed 2";
			A.NestedAs = new Collection<TestLevelA>();
			
			A.ResetToOriginal();
			Assert.IsTrue(A.name == "initial");
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
		public void DictionaryResetToOriginalTest()
		{
			var A = ShadowedObject.Create<TestLevelA>();
			A.dictOfBs = new Dictionary<string, TestLevelB>();

			A.BaselineOriginals();

			A.dictOfBs.Add("asdf", new TestLevelB());

			A.ResetToOriginal(a=>a.dictOfBs);

			Assert.IsTrue(A.dictOfBs.Count < 1);
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
			A.name = "asdf";

			A.BaselineOriginals();

			A.name = "xyz";

			Assert.IsTrue(A.HasChanges());
		}

		[TestMethod]
		public void CheckingForImplicitResetOfValue()
		{
			var A = ShadowedObject.Create<TestLevelA>();
			A.name = "asdf";

			A.BaselineOriginals();

			A.name = "xyz";
			Assert.IsTrue(A.HasChanges());


			A.name = "asdf";
			Assert.IsTrue( ! A.HasChanges() );
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

			Assert.IsTrue( ! A.HasChanges() );

			A.NestedAs.Add(new TestLevelA());
			A.NestedAs.Add(new TestLevelA());

			Assert.IsTrue(A.HasChanges());
		}

		[TestMethod]
		public void RecursiveTwoLevelCheckingForChangedCollection()
		{
			var A = ShadowedObject.Create<TestLevelA>();
			A.NestedAs = new Collection<TestLevelA>();

			A.NestedAs.Add(ShadowedObject.Create<TestLevelA>());
			A.NestedAs.Add(ShadowedObject.Create<TestLevelA>());

			A.BaselineOriginals();

			Assert.IsTrue(!A.HasChanges());

			A.NestedAs[0].name = "xyz";

			Assert.IsTrue(A.HasChanges());
		}

		[TestMethod]
		public void RecursiveTwoLevelCheckingForChangedObject()
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


		[TestMethod]
		public void CreateShadowsFromPocoGraph()
		{
			var A = new TestLevelA();
			A.B = new TestLevelB();
			A.B.C = new TestLevelC();

			var sA = ShadowedObject.CopyInto<TestLevelA>(A);

			sA.BaselineOriginals();

			sA.name = "blah";

			Assert.IsTrue(sA.HasChanges());

			sA.ResetToOriginal(a=>a.name);

			Assert.IsTrue(sA.name == "initial");
			Assert.IsTrue( ! sA.HasChanges());
		}


		[TestMethod]
		public void TrackChangesFromPocoGraphCopiedShadows()
		{
			var A = new TestLevelA();
			A.B = new TestLevelB();
			A.B.C = new TestLevelC();

			var sA = ShadowedObject.CopyInto(A);
			
			sA.B.Bstuff = 3;

			Assert.IsTrue(sA.B.HasChanges());

			sA.B.ResetToOriginal(b=>b.Bstuff);

			Assert.IsTrue( ! sA.B.HasChanges());
		}


		[TestMethod]
		public void TrackChangesFromPocoCollectionCopiedShadows()
		{
			var A = new TestLevelA();
			A.NestedAs = new Collection<TestLevelA>(){new TestLevelA(), new TestLevelA()};

			var sA = ShadowedObject.CopyInto(A);
			
			sA.BaselineOriginals();
			Assert.IsTrue( ! sA.HasChanges());			

			sA.NestedAs[0].name = "xyz";

			Assert.IsTrue(sA.HasChanges());

			sA.NestedAs[0].ResetToOriginal(a => a.name);

			Assert.IsTrue(!sA.HasChanges());
		}
	}
}
