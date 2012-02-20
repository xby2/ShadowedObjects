using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Collections;

namespace ShadowedObjects
{
	public delegate	void changedDelegate();

	public interface IShadowCollection : IShadowChangeTracker
	{
		event changedDelegate changed;
		bool isTracked { get; }
		IShadowCollection Clone();
	}

	public class ShadowCollection<T> : Collection<T> , IShadowCollection
	{
		public bool isChanged;
		
		public event changedDelegate changed;

		public bool isTracked
		{
			get
			{
				return changed.GetInvocationList().Length > 1;
			}
		}

		public ShadowCollection(IEnumerable<T> collection ) : base(collection.ToList())
		{
			changed = () =>
			          	{
			          		HasDirectChanges = true;
			          	};
		}

		//public ShadowCollection(ICollection collection)
		//{
		//    foreach (var v in collection)
		//    {
		//        this.Add(v as T);
		//    }			
		//}

		public ShadowCollection() : base()
		{
		}

		public IShadowCollection Clone()
		{
			//TODO: see about a better disconnected clone implementation
			return new ShadowCollection<T>((this as Collection<T>).ToArray().ToList<T>());
		}

		protected override void ClearItems()
		{
			changed();
			isChanged = true;
			base.ClearItems();
		}

		protected override void InsertItem(int index, T item)
		{
			changed();
			isChanged = true;
			base.InsertItem(index, item);
		}

		protected override void SetItem(int index, T item)
		{
			changed();
			isChanged = true;
			base.SetItem(index, item);

		}

		protected override void RemoveItem(int index)
		{
			changed();
			isChanged = true;
			base.RemoveItem(index);
		}

		public bool HasChildChanges
		{
			get
			{
				//return this.ToList().Where(t=>t is IShadowObject).Any(t=>(t as IShadowObject).HasChanges());

				return this.ToList().Where(t => t is IShadowObject).Any(shad => 
				{ 
					if (shad is IShadowObject)
					{
						return ShadowedObject.GetIShadow(shad as IShadowObject).HasChanges;
					}
					else if (shad is IShadowCollection)
					{
						return (shad as IShadowCollection).HasChanges;
					}
					return false;
				} );

			}
		}

		public bool HasDirectChanges
		{
			get;private set;
		}

		public bool HasChanges
		{
			get
			{
				return HasDirectChanges || HasChildChanges;
			}
		}
	}
}
