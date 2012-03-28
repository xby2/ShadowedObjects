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

		#region Test Objects
		[Shadowed]
		public class TestLevelA
		{
			[Shadowed]
			public virtual string name { get; set; }

			public TestLevelA()
			{
				name = "initial";
			}

			[Shadowed]
			public virtual IList<TestLevelA> NestedAs { get; set; }

			[Shadowed]
			public virtual IDictionary<string, TestLevelB> dictOfBs { get; set; }

			[Shadowed]
			public virtual IDictionary<int, TestLevelC> dictOfCs { get; set; }

			[Shadowed]
			public virtual ArrayList UntypedList { get; set; }

			[Shadowed]
			public virtual TestLevelB B { get; set; }

		}

		[Shadowed]
		public class TestLevelB
		{
			public virtual int Bstuff { get; set; }

			public virtual TestLevelC C { get; set; }

			public TestLevelB() { Bstuff = 1; }
		}

		[Shadowed]
		public class TestLevelC
		{
			public virtual string CStuff { get; set; }

			public TestLevelC() { CStuff = "start"; }
		}
		#endregion

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
			A.ResetToOriginal(a => a.name);
			Assert.IsTrue(A.name == "initial");
		}

		[TestMethod]
		public void ChildObjectResetToOriginalTest()
		{
			TestLevelA A = ShadowedObject.Create<TestLevelA>();
			A.NestedAs = new Collection<TestLevelA>();
			A.B = ShadowedObject.Create<TestLevelB>();
			A.B.Bstuff = 2;

			A.BaselineOriginals();
			A.B.BaselineOriginals();


			A.B = ShadowedObject.Create<TestLevelB>();
			A.B.Bstuff = 3;

			//This works without recursion because the entire B object, along with the BStuff Property is reset to the B with a BStuff of 2.
			A.ResetToOriginal(a => a.B);

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
			//A.ResetToOriginal(a => a.B);
			A.ResetToOriginal();

			Assert.IsTrue(A.B.Bstuff == 2);
		}


		[TestMethod]
		public void CollectionResetToOriginalTest()
		{
			var A = new TestLevelA();
			A.NestedAs = new Collection<TestLevelA>();

			A = ShadowedObject.CopyInto(A);

			A.BaselineOriginals();

			A.NestedAs.Add(new TestLevelA());
			A.NestedAs.Add(new TestLevelA());

			A.ResetToOriginal(a => a.NestedAs[0]);

			Assert.IsTrue(A.NestedAs.Count < 1);
		}



		[TestMethod]
		public void DictionaryResetToOriginalUndoAddTest()
		{
			var A = ShadowedObject.Create<TestLevelA>();
			//A.dictOfBs = ShadowedObject.CreateDictionary<string, TestLevelB>();
			var stuff = ShadowedObject.CreateDictionary<string, TestLevelB>();
			A.dictOfBs = stuff;

			A.BaselineOriginals();
			Assert.IsTrue(!A.HasChanges());
			Assert.IsTrue(!A.dictOfBs.HasChanges());

			A.dictOfBs.Add("asdf", ShadowedObject.Create<TestLevelB>());

			Assert.IsTrue(A.HasChanges());
			Assert.IsTrue(A.dictOfBs.HasChanges());
			Assert.IsTrue(!A.dictOfBs["asdf"].HasChanges());
			Assert.IsTrue(A.dictOfBs.Count == 1);

			A.ResetToOriginal(a => a.dictOfBs);
			//A.dictOfBs.ResetToOriginal();
			//A.ResetToOriginal();

			Assert.IsTrue(A.dictOfBs.Count < 1);
			Assert.IsTrue(!A.HasChanges());
			Assert.IsTrue(!A.dictOfBs.HasChanges());
		}

		[TestMethod]
		public void DictionaryResetToOriginalUndoRemoveTest()
		{
			var A = ShadowedObject.Create<TestLevelA>();
			//A.dictOfBs = ShadowedObject.CreateDictionary<string, TestLevelB>();
			var stuff = ShadowedObject.CreateDictionary<string, TestLevelB>();
			A.dictOfBs = stuff;

			A.dictOfBs.Add("asdf", ShadowedObject.Create<TestLevelB>());
			A.BaselineOriginals();
			A.dictOfBs.BaselineOriginals();

			Assert.IsTrue(A.dictOfBs.Count == 1);
			Assert.IsTrue(!A.HasChanges());
			Assert.IsTrue(!A.dictOfBs.HasChanges());
			Assert.IsTrue(!A.dictOfBs["asdf"].HasChanges());

			A.dictOfBs.Remove("asdf");
			Assert.IsTrue(A.dictOfBs.Count == 0);
			Assert.IsTrue(A.HasChanges());
			Assert.IsTrue(A.dictOfBs.HasChanges());

			A.ResetToOriginal(a => a.dictOfBs);
			//A.dictOfBs.ResetToOriginal();

			Assert.IsTrue(A.dictOfBs.Count == 1);
			Assert.IsTrue(!A.HasChanges());
			Assert.IsTrue(!A.dictOfBs.HasChanges());
			Assert.IsTrue(!A.dictOfBs["asdf"].HasChanges());
		}

		[TestMethod]
		public void DictionaryResetToOriginalUndoItemEditTest()
		{
			var A = ShadowedObject.Create<TestLevelA>();
			//A.dictOfBs = ShadowedObject.CreateDictionary<string, TestLevelB>();
			var stuff = ShadowedObject.CreateDictionary<string, TestLevelB>();
			A.dictOfBs = stuff;

			A.dictOfBs.Add("asdf", ShadowedObject.Create<TestLevelB>());
			A.dictOfBs["asdf"].Bstuff = 8;
			A.BaselineOriginals();
			A.dictOfBs.BaselineOriginals();

			Assert.IsTrue(A.dictOfBs.Count == 1);
			Assert.AreEqual(8, A.dictOfBs["asdf"].Bstuff);
			Assert.IsTrue(!A.HasChanges());
			Assert.IsTrue(!A.dictOfBs.HasChanges());
			Assert.IsTrue(!A.dictOfBs["asdf"].HasChanges());

			A.dictOfBs["asdf"].Bstuff = 13;
			Assert.AreEqual(13, A.dictOfBs["asdf"].Bstuff);
			Assert.IsTrue(A.dictOfBs["asdf"].HasChanges());
			Assert.IsTrue(A.dictOfBs.HasChanges());
			Assert.IsTrue(A.HasChanges());



			A.ResetToOriginal(a => a.dictOfBs);
			//A.dictOfBs.ResetToOriginal();

			Assert.AreEqual(8, A.dictOfBs["asdf"].Bstuff);
			Assert.IsTrue(!A.HasChanges());
			Assert.IsTrue(!A.dictOfBs.HasChanges());
			Assert.IsTrue(!A.dictOfBs["asdf"].HasChanges());
		}

		[TestMethod]
		public void DictionaryResetToOriginalUndoUpdateItem()
		{
			var A = ShadowedObject.Create<TestLevelA>();
			//A.dictOfBs = ShadowedObject.CreateDictionary<string, TestLevelB>();
			var stuff = ShadowedObject.CreateDictionary<string, TestLevelB>();
			A.dictOfBs = stuff;

			A.dictOfBs.Add("asdf", ShadowedObject.Create<TestLevelB>());
			A.dictOfBs["asdf"].Bstuff = 8;
			A.BaselineOriginals();
			A.dictOfBs.BaselineOriginals();

			Assert.IsTrue(A.dictOfBs.Count == 1);
			Assert.AreEqual(8, A.dictOfBs["asdf"].Bstuff);
			Assert.IsTrue(!A.HasChanges());
			Assert.IsTrue(!A.dictOfBs.HasChanges());
			Assert.IsTrue(!A.dictOfBs["asdf"].HasChanges());

			A.dictOfBs["asdf"] = ShadowedObject.Create<TestLevelB>();
			Assert.AreEqual(1, A.dictOfBs["asdf"].Bstuff);
			Assert.IsTrue(!A.dictOfBs["asdf"].HasChanges());
			Assert.IsTrue(A.dictOfBs.HasChanges());
			Assert.IsTrue(A.HasChanges());



			//A.ResetToOriginal(a=>a.dictOfBs);
			A.dictOfBs.ResetToOriginal();

			Assert.AreEqual(8, A.dictOfBs["asdf"].Bstuff);
			Assert.IsTrue(!A.HasChanges());
			Assert.IsTrue(!A.dictOfBs.HasChanges());
			Assert.IsTrue(!A.dictOfBs["asdf"].HasChanges());
		}


		[TestMethod]
		public void NonGenericCollectionResetToOriginalTest()
		{
			var A = new TestLevelA();
			A.UntypedList = new ArrayList();

			A = ShadowedObject.CopyInto(A);

			A.BaselineOriginals();

			A.UntypedList.Add(new TestLevelA());
			A.UntypedList.Add(new TestLevelA());

			A.ResetToOriginal(a => a.UntypedList);

			Assert.IsTrue(A.UntypedList.Count < 1);
		}

		[TestMethod]
		public void CollectionResetAndRefillTest()
		{
			var A = new TestLevelA();
			A.NestedAs = new Collection<TestLevelA>();

			A = ShadowedObject.CopyInto(A);

			A.BaselineOriginals();

			A.NestedAs.Add(new TestLevelA());
			A.NestedAs.Add(new TestLevelA());

			A.ResetToOriginal(a => a.NestedAs);

			A.NestedAs.Add(new TestLevelA());
			A.NestedAs.Add(new TestLevelA());
			A.NestedAs.Add(new TestLevelA());
			A.NestedAs.Add(new TestLevelA());

			Assert.IsTrue(A.NestedAs.Count == 4);
		}

		[TestMethod]
		public void ResetsOnMultipleObjects()
		{
			var A = new TestLevelA();
			A.NestedAs = new Collection<TestLevelA>();
			A.B = new TestLevelB();
			A.B.Bstuff = 2;

			A = ShadowedObject.CopyInto(A);

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
			Assert.IsTrue(!A.HasChanges());
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
			var A = new TestLevelA();
			A.NestedAs = new Collection<TestLevelA>();

			A = ShadowedObject.CopyInto(A);

			A.BaselineOriginals();

			Assert.IsTrue(!A.HasChanges());

			A.NestedAs.Add(new TestLevelA());
			A.NestedAs.Add(new TestLevelA());

			Assert.IsTrue(A.HasChanges());
		}

		[TestMethod]
		public void RecursiveCheckingForChangedDictionary()
		{
			var A = ShadowedObject.Create<TestLevelA>();
			A.B = ShadowedObject.Create<TestLevelB>();
			A.dictOfBs = ShadowedObject.CreateDictionary<string, TestLevelB>();
			A.B.C = ShadowedObject.Create<TestLevelC>();

			A.BaselineOriginals();

			Assert.IsFalse(A.HasChanges());

			A.dictOfBs.Add("asdf", ShadowedObject.Create<TestLevelB>());
			A.dictOfBs.Add("qwerty", ShadowedObject.Create<TestLevelB>());

			Assert.IsTrue(A.HasChanges());
		}

		[TestMethod]
		public void RecursiveTwoLevelCheckingForChangedCollection()
		{
			var A = new TestLevelA();
			A.NestedAs = new Collection<TestLevelA>();

			A.NestedAs.Add(ShadowedObject.Create<TestLevelA>());
			A.NestedAs.Add(ShadowedObject.Create<TestLevelA>());

			A = ShadowedObject.CopyInto(A);

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

			A.B.C.ResetToOriginal(c => c.CStuff);

			Assert.IsFalse(A.B.C.HasChanges());
		}

		[TestMethod]
		public void CheckForSpecificPropertyChange()
		{
			var A = ShadowedObject.Create<TestLevelA>();
			A.B = ShadowedObject.Create<TestLevelB>();
			A.B.C = ShadowedObject.Create<TestLevelC>();

			A.BaselineOriginals();
			A.B.BaselineOriginals();

			A.B.C.CStuff = "asdf";

			Assert.IsTrue(A.B.HasChanges());
			Assert.IsFalse(A.B.HasChanges(b => b.Bstuff));

			A.B.Bstuff = 9;
			Assert.IsTrue(A.B.HasChanges());
			Assert.IsTrue(A.B.HasChanges(b => b.Bstuff));
		}

		[TestMethod]
		public void CheckForSpecificObjectPropertyChange()
		{
			var A = ShadowedObject.Create<TestLevelA>();
			A.B = ShadowedObject.Create<TestLevelB>();
			A.B.C = ShadowedObject.Create<TestLevelC>();

			A.BaselineOriginals();
			A.B.BaselineOriginals();

			A.B.Bstuff = 21;

			Assert.IsTrue(A.B.HasChanges());
			Assert.IsFalse(A.B.HasChanges(b => b.C));

			A.B.C.CStuff = "asdf";
			Assert.IsTrue(A.B.HasChanges());
			Assert.IsTrue(A.B.HasChanges(b => b.C));
		}
		
		[TestMethod]
		public void AllowDictionariesWithNonStringKeys()
		{
			var A = new TestLevelA();
			A.B = new TestLevelB();
			A.B.C = new TestLevelC();
			A.dictOfCs = new Dictionary<int, TestLevelC>();
			A.dictOfCs.Add(5, new TestLevelC());
			A.dictOfCs[5].CStuff = "qwerty";
			A.dictOfCs.Add(6, new TestLevelC());
			A.dictOfCs[6].CStuff = "Bo";

			var sA = ShadowedObject.CopyInto<TestLevelA>(A);

			sA.BaselineOriginals();

			sA.dictOfCs[5].CStuff = "asdf";

			Assert.IsTrue(sA.HasChanges());
			Assert.AreEqual("asdf", sA.dictOfCs[5].CStuff);

			sA.ResetToOriginal(a => a.dictOfCs);

			Assert.AreEqual("qwerty", sA.dictOfCs[5].CStuff);
			Assert.IsTrue(!sA.HasChanges());

			sA.dictOfCs[6] = ShadowedObject.Create<TestLevelC>();

			Assert.IsTrue(sA.HasChanges());
			Assert.AreEqual("start", sA.dictOfCs[6].CStuff);

			sA.BaselineOriginals();

			Assert.IsFalse(sA.HasChanges());
			sA.dictOfCs.Remove(6);
			Assert.IsTrue(sA.HasChanges());
		}

		#endregion

		#region copyInto tests
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

			sA.ResetToOriginal(a => a.name);

			Assert.IsTrue(sA.name == "initial");
			Assert.IsTrue(!sA.HasChanges());
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

			sA.B.ResetToOriginal(b => b.Bstuff);

			Assert.IsTrue(!sA.B.HasChanges());
		}


		[TestMethod]
		public void TrackChangesFromPocoCollectionCopiedShadows()
		{
			var A = new TestLevelA();
			A.NestedAs = new Collection<TestLevelA>() { new TestLevelA(), new TestLevelA() };

			var sA = ShadowedObject.CopyInto(A);

			sA.BaselineOriginals();
			Assert.IsTrue(!sA.HasChanges());


			sA.NestedAs[0].name = "xyz";

			Assert.IsTrue(sA.HasChanges());

			sA.NestedAs[0].ResetToOriginal(a => a.name);

			Assert.IsTrue(!sA.HasChanges());
		}

		[TestMethod]
		public void CreateDictionaryShadowsFromPocoGraph()
		{
			var A = new TestLevelA();
			A.B = new TestLevelB();
			A.B.C = new TestLevelC();
			A.dictOfBs = new Dictionary<string, TestLevelB>();

			var sA = ShadowedObject.CopyInto<TestLevelA>(A);

			sA.BaselineOriginals();

			sA.dictOfBs.Add("asdf", new TestLevelB());

			Assert.IsTrue(sA.HasChanges());
			Assert.AreEqual(1, sA.dictOfBs.Count);

			sA.ResetToOriginal(a => a.dictOfBs);

			Assert.AreEqual(0, sA.dictOfBs.Count);
			Assert.IsTrue(!sA.HasChanges());
		}

		[TestMethod]
		public void CreateDictionaryWithItemShadowsFromPocoGraph()
		{
			var A = new TestLevelA();
			A.B = new TestLevelB();
			A.B.C = new TestLevelC();
			A.dictOfBs = new Dictionary<string, TestLevelB>();
			A.dictOfBs.Add("asdf", new TestLevelB());
			A.dictOfBs["asdf"].Bstuff = 3;

			var sA = ShadowedObject.CopyInto<TestLevelA>(A);

			sA.BaselineOriginals();

			sA.dictOfBs["asdf"].Bstuff = 8;

			Assert.IsTrue(sA.HasChanges());
			Assert.AreEqual(8, sA.dictOfBs["asdf"].Bstuff);

			sA.ResetToOriginal(a => a.dictOfBs);

			Assert.AreEqual(3, sA.dictOfBs["asdf"].Bstuff);
			Assert.IsTrue(!sA.HasChanges());
		}

		#endregion
		
		#region Show Changes

		[TestMethod]
		public void ShowChangesTest()
		{
			var A = ShadowedObject.Create<TestLevelA>();
			A.B = ShadowedObject.Create<TestLevelB>();
			A.B.C = ShadowedObject.Create<TestLevelC>();

			A.BaselineOriginals();

			Assert.IsFalse(A.HasChanges());
			Assert.AreEqual(0, A.ListChanges().Length);

			A.B.C.CStuff = "asdf";

			Assert.IsTrue(A.HasChanges());
			Assert.IsTrue(A.ListChanges().Length > 0);
		}

		[TestMethod]
		public void ShowChangesToDictionaryTest()
		{
			var A = ShadowedObject.Create<TestLevelA>();
			A.B = ShadowedObject.Create<TestLevelB>();
			A.dictOfBs = ShadowedObject.CreateDictionary<string, TestLevelB>();
			A.B.C = ShadowedObject.Create<TestLevelC>();

			A.BaselineOriginals();

			Assert.IsFalse(A.HasChanges());
			Assert.AreEqual(0, A.ListChanges().Length);

			A.dictOfBs.Add("asdf", ShadowedObject.Create<TestLevelB>());
			A.dictOfBs.Add("qwerty", ShadowedObject.Create<TestLevelB>());

			Assert.IsTrue(A.HasChanges());
			Assert.IsTrue(A.ListChanges().Length > 0);
		}

		[TestMethod]
		public void GetDictionaryChangesTest()
		{
			var A = ShadowedObject.Create<TestLevelA>();
			A.B = ShadowedObject.Create<TestLevelB>();
			A.dictOfBs = ShadowedObject.CreateDictionary<string, TestLevelB>();
			A.dictOfBs.Add("1234", ShadowedObject.Create<TestLevelB>());
			A.dictOfBs.Add("5678", ShadowedObject.Create<TestLevelB>());
			A.B.C = ShadowedObject.Create<TestLevelC>();

			A.BaselineOriginals();

			Assert.IsFalse(A.HasChanges());

			A.dictOfBs.Add("asdf", ShadowedObject.Create<TestLevelB>());

			Assert.IsTrue(A.HasChanges());
			IDictionary<object, ChangeType> changes = A.dictOfBs.GetDictionaryChanges();
			Assert.AreEqual(1, changes.Count);
			Assert.IsTrue(changes["asdf"] == ChangeType.Add);

			A.dictOfBs.Remove("1234");
			changes = A.dictOfBs.GetDictionaryChanges();
			Assert.AreEqual(2, changes.Count);
			Assert.IsTrue(changes["asdf"] == ChangeType.Add);
			Assert.IsTrue(changes["1234"] == ChangeType.Remove);

			A.dictOfBs["5678"].Bstuff = 23;
			changes = A.dictOfBs.GetDictionaryChanges();
			Assert.AreEqual(3, changes.Count);
			Assert.IsTrue(changes["asdf"] == ChangeType.Add);
			Assert.IsTrue(changes["1234"] == ChangeType.Remove);
			Assert.IsTrue(changes["5678"] == ChangeType.Edit);

			A.BaselineOriginals();
			changes = A.dictOfBs.GetDictionaryChanges();
			Assert.AreEqual(0, changes.Count);

			A.dictOfBs["asdf"] = ShadowedObject.Create<TestLevelB>();
			changes = A.dictOfBs.GetDictionaryChanges();
			Assert.AreEqual(1, changes.Count);
			Assert.IsTrue(changes["asdf"] == ChangeType.Edit);
		}

		#endregion

		#region GetOriginal Tests

		[TestMethod]
		public void GetOriginalOfChangedObjectTest()
		{
			var A = ShadowedObject.Create<TestLevelA>();
			A.B = ShadowedObject.Create<TestLevelB>();
			A.B.C = ShadowedObject.Create<TestLevelC>();
			A.B.C.CStuff = "qwerty";

			A.BaselineOriginals();

			A.B.C.CStuff = "asdf";

			TestLevelC originalC = A.B.C.GetOriginal();

			Assert.AreEqual("qwerty", originalC.CStuff);
		}

		[TestMethod]
		public void GetOriginalOfUnchangedObjectTest()
		{
			var A = ShadowedObject.Create<TestLevelA>();
			A.B = ShadowedObject.Create<TestLevelB>();
			A.B.C = ShadowedObject.Create<TestLevelC>();
			A.B.C.CStuff = "qwerty";

			A.BaselineOriginals();

			TestLevelC originalC = A.B.C.GetOriginal();

			Assert.AreEqual("qwerty", originalC.CStuff);
		}

		[TestMethod]
		public void GetOriginalOfChangedPropertyTest()
		{
			var A = ShadowedObject.Create<TestLevelA>();
			A.B = ShadowedObject.Create<TestLevelB>();
			A.B.C = ShadowedObject.Create<TestLevelC>();
			A.B.C.CStuff = "qwerty";

			A.BaselineOriginals();

			A.B.C.CStuff = "asdf";

			string originalCStuff = (string)A.B.C.GetOriginal(c => c.CStuff);

			Assert.AreEqual("qwerty", originalCStuff);
		}

		[TestMethod]
		public void GetOriginalOfUnchangedPropertyTest()
		{
			var A = ShadowedObject.Create<TestLevelA>();
			A.B = ShadowedObject.Create<TestLevelB>();
			A.B.C = ShadowedObject.Create<TestLevelC>();
			A.B.C.CStuff = "qwerty";

			A.BaselineOriginals();

			string originalCStuff = (string)A.B.C.GetOriginal(c => c.CStuff);

			Assert.AreEqual("qwerty", originalCStuff);
		}

		[TestMethod]
		public void GetOriginalOfChangedPropertyByNameTest()
		{
			var A = ShadowedObject.Create<TestLevelA>();
			A.B = ShadowedObject.Create<TestLevelB>();
			A.B.C = ShadowedObject.Create<TestLevelC>();
			A.B.C.CStuff = "qwerty";

			A.BaselineOriginals();

			A.B.C.CStuff = "asdf";

			string originalCStuff = (string)A.B.C.GetOriginal("CStuff");

			Assert.AreEqual("qwerty", originalCStuff);
		}

		[TestMethod]
		public void GetOriginalOfUnchangedPropertyByNameTest()
		{
			var A = ShadowedObject.Create<TestLevelA>();
			A.B = ShadowedObject.Create<TestLevelB>();
			A.B.C = ShadowedObject.Create<TestLevelC>();
			A.B.C.CStuff = "qwerty";

			A.BaselineOriginals();

			string originalCStuff = (string)A.B.C.GetOriginal("CStuff");

			Assert.AreEqual("qwerty", originalCStuff);
		}

		[TestMethod]
		public void GetOriginalOfChangedDictionaryPropertyByNameTest()
		{
			var A = ShadowedObject.Create<TestLevelA>();
			A.B = ShadowedObject.Create<TestLevelB>();
			A.B.C = ShadowedObject.Create<TestLevelC>();
			A.dictOfBs = ShadowedObject.CreateDictionary<string, TestLevelB>();
			A.dictOfBs["asdf"] = ShadowedObject.Create<TestLevelB>();
			A.dictOfBs["asdf"].Bstuff = 7;

			A.BaselineOriginals();

			//A.dictOfBs["asdf"].Bstuff = 103;
			A.dictOfBs.Remove("asdf");

			int originalBStuff = ((TestLevelB)A.dictOfBs.GetOriginal("asdf")).Bstuff;

			Assert.AreEqual(7, originalBStuff);
		}

		[TestMethod]
		public void GetOriginalOfUnchangedDictionaryPropertyByNameTest()
		{
			var A = ShadowedObject.Create<TestLevelA>();
			A.B = ShadowedObject.Create<TestLevelB>();
			A.B.C = ShadowedObject.Create<TestLevelC>();
			A.dictOfBs = ShadowedObject.CreateDictionary<string, TestLevelB>();
			A.dictOfBs["asdf"] = ShadowedObject.Create<TestLevelB>();
			A.dictOfBs["asdf"].Bstuff = 7;

			A.BaselineOriginals();

			int originalBStuff = ((TestLevelB)A.dictOfBs.GetOriginal("asdf")).Bstuff;

			Assert.AreEqual(7, originalBStuff);
		}

		#endregion
	}
}
