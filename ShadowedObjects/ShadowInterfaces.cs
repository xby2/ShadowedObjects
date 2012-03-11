using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace ShadowedObjects
{

	public interface IShadowChangeTracker
	{
		bool HasChildChanges { get; }
		bool HasDirectChanges { get; }
		bool HasChanges { get; }
        bool HasPropertyChange<T>(T instance, Expression<Func<T, object>> func);
		void BaselineOriginals();
		void ResetToOriginals(object instance);

		void ResetToOriginals<T>(T instance, Expression<Func<T, object>> func);

        string ListChanges<T>(T instance);
        IDictionary<object, ChangeType> GetDictionaryChanges<T>(T p);

        T GetOriginal<T>(T instance);
        object GetOriginal<T>(T instance, Expression<Func<T, object>> property);
        object GetOriginal<T>(T instance, object propertyKey);

        
    }

	public interface IShadowMetaData : IShadowChangeTracker
	{
		void trackChanges(object strippedName, object getValue, object setValue);
	}

	public interface IShadowDeferrer : IShadowMetaData
	{
		bool isTracked { get;}
		
		IShadowObject Clone();

		event changedDelegate changed;
	}

	public interface IShadowIntercept
	{
		IShadowMetaData Instance { get; set; }
	}

	public interface IShadowIntercept<T> : IShadowIntercept
	{
	}

	public delegate void changedDelegate();

}
