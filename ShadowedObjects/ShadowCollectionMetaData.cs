using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Collections;
using System.Linq.Expressions;

namespace ShadowedObjects
{
    public class ShadowCollectionMetaData<T> : ShadowMetaData , IShadowDeferrer
    {		
		#region "IShadowDeferrer
		
		public event changedDelegate changed;

		public bool isTracked
		{
			get
			{
				return changed.GetInvocationList().Length > 1;
			}
		}
		#endregion

    	public ShadowCollectionMetaData()
    	{
			changed = () => HasDirectChanges = true;
    	}

        protected override void ResetProperty(object instance, object propertyName)
        {
            throw new NotImplementedException();
        }

		public override void trackChanges(object propertyName, object getValue, object setValue)
		{
			var clone = Clone();			
			changed();			
		}

    	public IShadowObject Clone()
    	{
    		return ShadowedObject.CopyIntoCollection(((Instance as IList<T>).ToArray().ToList<T>())) as IShadowObject;
    	}

    	public override bool HasChildChanges
		{
			get
			{
				return (Instance as IList<T>).Where(t => t is IShadowChangeTracker).ToList().Any((sh) => sh.HasChanges());
			}
		}

		public override void ResetToOriginals(object instance)
		{

			(Instance as IList<T>).Where(t => t is IShadowChangeTracker).ToList().ForEach(a => { a.ResetToOriginal(); });
		}

		public override void ResetToOriginals<T>(T instance, System.Linq.Expressions.Expression<Func<T, object>> func)
		{
			throw new NotImplementedException();
		}

        public virtual bool HasPropertyChange<T>(T instance, Expression<Func<T, object>> func)
        {
            throw new NotImplementedException();
        }
    }
}
