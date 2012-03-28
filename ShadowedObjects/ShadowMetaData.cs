using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Castle.DynamicProxy;
using log4net;
using System.Reflection;

namespace ShadowedObjects
{
	//delegate bool HasChangesDelegate();


	public class ShadowMetaData : IShadowMetaData
	{
		public object Instance { get; set; }

		protected readonly Dictionary<object, object> Originals = new Dictionary<object, object>();
		//protected readonly Dictionary<object, object> Previous = new Dictionary<object, object>();

		protected readonly Dictionary<object, object> Children = new Dictionary<object, object>();

		protected static readonly ILog logger = LogManager.GetLogger(typeof(ShadowMetaData));

		public void BaselineOriginals()
		{
			Originals.Clear();
			HasDirectChanges = false;

			ICollection<object> keys = Children.Keys;
			foreach (object key in keys)
			{
				var shadowChangeTracker = Children[key] as IShadowChangeTracker;
				if (shadowChangeTracker != null)
				{
					shadowChangeTracker.BaselineOriginals();
				}
			}
		}

		public virtual void ResetToOriginals<T>(T instance, Expression<Func<T, object>> func)
		{
			var prop = ExpressionUtil.GetPropertyCore(func.Body);
			var setMethod = prop.GetSetMethod();

			if (Originals.ContainsKey(prop.Name))
			{
				setMethod.Invoke(instance, new object[1] { Originals[prop.Name] });
			}

			var getMethod = prop.GetGetMethod();
			var getValue = getMethod.Invoke(instance, new object[0] { });
			if (getValue is IShadowObject)
			{
			    getValue.ResetToOriginal();
			}
		}

		public virtual void ResetToOriginals(object instance)
		{
			while (Originals.Keys.Count > 0)
			{
				object key = Originals.Keys.First<object>();
				ResetProperty(instance, key);
			}

			Children.Values.ToList().ForEach(a => { a.ResetToOriginal(); });

		}

		protected virtual void ResetProperty(object instance, object propertyName)
		{
			if (Originals.ContainsKey(propertyName) && propertyName is string)
			{
				var propertyValue = Originals[propertyName];

				var setName = "set_" + propertyName;
				var setMethod = instance.GetType().GetMethod(setName);
				setMethod.Invoke(instance, new object[1] { Originals[propertyName] });
			}

		}

		private void CleanupOriginals(object propName)
		{

			Originals.Remove(propName);

			if (Originals.Count < 1)
			{
				HasDirectChanges = false;
			}
		}

		public bool HasChanges
		{
			get
			{
				return HasDirectChanges || HasChildChanges;
			}
		}

		private bool _hasdirectchanges;
		public bool HasDirectChanges
		{
			get
			{
				return _hasdirectchanges;
			}
			protected set
			{
				_hasdirectchanges = value;
			}
		}

		//private HasChangesDelegate _hasChildrenChangesDelegate = () => { return false; };

		public virtual bool HasChildChanges
		{
			get
			{				
				return Children.Values.ToList().Any( (sh)=> sh.HasChanges() );
			}
		}

        public virtual bool HasPropertyChange<T>(T instance, Expression<Func<T, object>> func)
        {
            var prop = ExpressionUtil.GetPropertyCore(func.Body);
            var setMethod = prop.GetSetMethod();

            if (Originals.ContainsKey(prop.Name))
            {
                return true;
            }

            var getMethod = prop.GetGetMethod();
            var getValue = getMethod.Invoke(instance, new object[0] { });
            if (getValue is IShadowObject)
            {
                return getValue.HasChanges();
            }
            return false;
        }

		public virtual void trackChanges(object propertyName, object getValue, object setValue)
		{
            if (!Originals.ContainsKey(propertyName))
			{
                Originals[propertyName] = getValue;
			}

			#region "logging"
			if (logger.IsInfoEnabled)
			{
				if (getValue is ICollection)
				{
                    // TODO: figure out how to log this when a value is a IDictionary proxy
                    string setValueCount = "cannot determine";
                    if (setValue as ICollection != null)
                    {
                        setValueCount = (setValue as ICollection).Count.ToString();
                    }

                    logger.InfoFormat("Intercepted {0}. Old Length:{1}. New Length:{2}", propertyName, (getValue as ICollection).Count.ToString(), setValueCount);
				}
				else
				{
                    logger.InfoFormat("Intercepted {0}. Old Value:{1}. New Value:{2}", propertyName, getValue, setValue);
				}
			}
			#endregion

			if (setValue is IShadowChangeTracker)
			{
                Children[propertyName] = setValue;
			}
			else
			{
                Children.Remove(propertyName);
			}

            if (setValue == Originals[propertyName] || (setValue != null && setValue.Equals(Originals[propertyName])))
			{
                CleanupOriginals(propertyName);
			}
			else
			{
				HasDirectChanges = true;
			}

			if (setValue is IShadowDeferrer)
			{	SetDeferrerDelegate(propertyName, setValue as IShadowDeferrer);
			}

			//TODO: unsubscribe delegate from old value
		}

		private void SetDeferrerDelegate(object strippedName, IShadowDeferrer deferrer)
		{
			if (!deferrer.isTracked)
			{
				changedDelegate delg = () =>
				{
					if (!Originals.ContainsKey(strippedName))
					{
						Originals[strippedName] = deferrer.Clone();
					}
				};

				deferrer.changed += delg;
			}
		}

		#region GetOriginal
        public object GetOriginal<T>(T instance, Expression<Func<T, object>> func)
        {
            var prop = ExpressionUtil.GetPropertyCore(func.Body);
            var setMethod = prop.GetSetMethod();

            if (Originals.ContainsKey(prop.Name))
            {
                return Originals[prop.Name];
            }
            else
            {
                var getMethod = prop.GetGetMethod();
                return getMethod.Invoke(instance, new object[0] { });
            }
        }

        public T GetOriginal<T>(T instance)
        {
            T originalObject = (T)typeof(T).GetConstructor(new Type[0] { }).Invoke(new object[] {});
            foreach (PropertyInfo pi in typeof(T).GetProperties())
            {
                var originalValue = GetOriginalProperty(instance, pi.Name);

                var setName = "set_" + pi.Name;
                var setMethod = typeof(T).GetMethod(setName);
                setMethod.Invoke(originalObject, new object[1] { originalValue });
            }
            return originalObject;
        }

        public virtual object GetOriginal<T>(T instance, object propertyKey)
        {
            return GetOriginalProperty(instance, propertyKey);
        }

        private object GetOriginalProperty(object instance, object propertyName)
        {
            if (Originals.ContainsKey(propertyName) && propertyName is string)
            {
                return Originals[propertyName];
            }
            else
            {
                var getName = "get_" + propertyName.ToString();
                var getMethod = instance.GetType().GetMethod(getName);
                return getMethod.Invoke(instance, new object[0] { }) ?? "";
            }
        }
		#endregion

		#region Show Changes
		public virtual string ListChanges<T>(T instance)
		{
			StringBuilder changes = new StringBuilder();
			Dictionary<object, object>.KeyCollection keys = Originals.Keys;
			foreach (object key in keys)
			{
				object originalValue = Originals[key] ?? "";

				var getName = "get_" + key.ToString();
				var getMethod = instance.GetType().GetMethod(getName);
				var currentValue = getMethod.Invoke(instance, new object[0] { }) ?? "";


				if (currentValue is IShadowObject)
				{
					changes.AppendLine(string.Format("{0} changed: ", key.ToString()));
					changes.Append(currentValue.ListChanges());
				}
				else
				{
					changes.AppendLine(string.Format("{0} changed from '{1}' to '{2}'", key.ToString(), originalValue.ToString(), currentValue.ToString()));
				}
			}
			// Now go through all the children and list the changes from them.
			Dictionary<object, object>.KeyCollection childKeys = Children.Keys;
			foreach (object childKey in childKeys)
			{
				object childValue = Children[childKey];
				if (childValue is IShadowObject)
				{
					if (childValue.HasChanges())
					{
						changes.AppendLine(string.Format("{0} changed: ", childKey.ToString()));
						changes.Append(childValue.ListChanges());
					}
				}
			}

			return changes.ToString();
		}
		
        public virtual IDictionary<object, ChangeType> GetDictionaryChanges<T>(T instance)
        {
            throw new NotImplementedException();
        }
		#endregion
    }
}
