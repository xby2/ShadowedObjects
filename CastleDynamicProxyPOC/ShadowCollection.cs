using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Collections;

namespace CastleDynamicProxyPOC
{
	public delegate	void changedDelegate();

	public interface IShadowCollection
	{
		event changedDelegate changed;
		bool isTracked { get; }
		IShadowCollection Clone();
	}

	public class ShadowCollection<T> : Collection<T> , IShadowCollection
	{
		public bool isChanged;
		
		public event changedDelegate changed = () => {};

		public bool isTracked { get{ return changed.GetInvocationList().Length > 1; } }

		public ShadowCollection(IList<T> collection ) : base(collection)
		{
		}

		public ShadowCollection() : base()
		{
		}

		public IShadowCollection Clone()
		{
			//TODO: see about a better disconnected clone implementation
			return new ShadowCollection<T>((this as Collection<T>).ToArray());
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
	}
}
