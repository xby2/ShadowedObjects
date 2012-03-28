using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Castle.DynamicProxy;
using log4net;

namespace ShadowedObjects
{
 
    public enum ChangeType
    {
        Add, Remove, Edit
    }

	public static class ShadowedObject
	{
		private static readonly ProxyGenerator _generator = new ProxyGenerator();

		#region "Create" Extensions
		public static T Create<T>() where T: class
		{
			var setCeptor = new ShadowedInterceptor<T>();
			var options = new ProxyGenerationOptions(new ShadowedObjectProxyGenerationHook());
			var meta = new ShadowMetaData();
			
			options.AddMixinInstance(meta);			
			setCeptor.Instance = meta;

			var theShadow = _generator.CreateClassProxy(typeof(T), new Type[]{ typeof(IShadowObject) }, options, setCeptor);

			meta.Instance = theShadow;
			setCeptor.Instance = (theShadow as IShadowMetaData); //reset Instance to the wrapping proxy class

			((T)theShadow).BaselineOriginals();
			
			return theShadow as T;
		}

        public static IDictionary<TKey, TValue> CreateDictionary<TKey, TValue>()
        {
            var setCeptor = new ShadowedDictionaryInterceptor<TKey, TValue>();
            var options = new ProxyGenerationOptions ( new ShadowedDictionaryProxyGenerationHook() );
            var meta = new ShadowDictionaryMetaData<TKey, TValue>();
            options.AddMixinInstance(meta);
            setCeptor.Instance = meta;

            var target = new Dictionary<TKey, TValue>();
			var theShadow = _generator.CreateInterfaceProxyWithTarget(typeof(IDictionary<TKey, TValue>), new Type[] { typeof(IShadowObject) }, target, options, setCeptor);

			meta.Instance = theShadow;
			setCeptor.Instance = (theShadow as IShadowMetaData);

            ((IDictionary<TKey, TValue>)theShadow).BaselineOriginals();

            return theShadow as IDictionary<TKey, TValue>;
        }
		
		public static IList<T> CreateCollection<T>()
		{
			var setCeptor = new ShadowedCollectionInterceptor<T>();
			var options = new ProxyGenerationOptions( new ShadowedCollectionProxyGenerationHook() );
			var meta = new ShadowCollectionMetaData<T>();

			options.AddMixinInstance(meta);
			setCeptor.Instance = meta;

			var target = new Collection<T>();
			var theShadow = _generator.CreateInterfaceProxyWithTarget(typeof(IList<T>), new Type[] { typeof(IShadowObject) }, target, options, setCeptor);

			meta.Instance = theShadow;
			setCeptor.Instance = (theShadow as IShadowMetaData); //reset Instance to the wrapping proxy class

			meta.BaselineOriginals();

			return theShadow as IList<T>;
		}
		#endregion "Create"

		#region "CopyInto Extensions"
		public static IDictionary<TKey, TValue> CopyIntoDictionary<TKey, TValue>(IDictionary<TKey, TValue> baseDictionary)
		{
			var shadowDictionary = CreateDictionary<TKey, TValue>();

			foreach (TKey key in baseDictionary.Keys)
			{
				TValue theValue = baseDictionary[key];
				if (theValue.GetType().GetCustomAttributes(typeof(ShadowedAttribute), true).Length > 0
					&& !(theValue is IShadowObject))
				{
					theValue = (TValue)CopyIntoSomething(theValue);
				}
				shadowDictionary.Add(key, theValue);

			}
			return shadowDictionary;
		}
		
		public static IList<T> CopyIntoCollection<T>(IList<T> baseCollection)
		{
			var shadCollection = CreateCollection<T>();

			for ( int i=0; i < baseCollection.Count; i++)
			{
				var theValue = baseCollection[i];
				if (theValue.GetType().GetCustomAttributes(typeof(ShadowedAttribute), true).Length > 0
					&& !(theValue is IShadowObject))
				{
					theValue = (T)CopyIntoSomething(theValue);
				}

				shadCollection.Add(theValue);				
			}

			return shadCollection;
		}

		public static T CopyInto<T>(T baseInstance) where T: class
		{
			var shadInstance = Create<T>();

            foreach (PropertyInfo pi in typeof(T).GetProperties())
			{
				var theValue = pi.GetGetMethod().Invoke(baseInstance, new object[0]);

                if (theValue == null) continue;

                if (pi.GetCustomAttributes(typeof(ShadowedAttribute), true).Length > 0)
                {
                    theValue = CopyIntoSomething(theValue);
                }

				pi.GetSetMethod().Invoke(shadInstance, new object[1]{ theValue });
			}
            shadInstance.BaselineOriginals();
			return shadInstance;
		}

		//Main switching and recursing method
		private static object CopyIntoSomething(object theValue) 
		{
			var valType = theValue.GetType();

			if (valType.IsGenericType && theValue is IList)
			{
				Type[] colType = valType.GetGenericArguments();
				Type shadowedObjectType = typeof (ShadowedObject);
				var genMethod = shadowedObjectType.GetMethod("CopyIntoCollection");
				var specMethod = genMethod.MakeGenericMethod(colType);
				theValue = specMethod.Invoke(null, new[] {theValue});
			}
			else if (valType.GetCustomAttributes(typeof (ShadowedAttribute), true).Length > 0
			         && !(theValue is IShadowObject))
			{
				//This is gross, there are definitely better ways to accomplish this 
				var selfRef = typeof (ShadowedObject).GetMethod("CopyInto",
				                                                System.Reflection.BindingFlags.Static | BindingFlags.Public);
				var genericRef = selfRef.MakeGenericMethod(valType);
				theValue = genericRef.Invoke(null, new[] {theValue});
			}
			else if (IsDictionary(valType))
			{				
				Type[] colType = valType.GetGenericArguments();
				Type shadowedObjectType = typeof (ShadowedObject);
				var genMethod = shadowedObjectType.GetMethod("CopyIntoDictionary");
				var specMethod = genMethod.MakeGenericMethod(colType);
				theValue = specMethod.Invoke(null, new[] {theValue});
			}
			return theValue;
		}
		#endregion

		#region "ResetToOriginal" Extensions
		public static void ResetToOriginal<T>(this T shadowed, Expression<Func<T, object>> property )
		{
			//var ishadow = GetIShadow((IShadowObject)shadowed);
			(shadowed as IShadowChangeTracker).ResetToOriginals((T)shadowed, property);
		}

		public static void ResetToOriginal(this object shadowed)
		{
            //var ishadow = GetIShadow((IShadowObject)shadowed);
            (shadowed as IShadowChangeTracker).ResetToOriginals(shadowed);
		}
		#endregion

		public static void BaselineOriginals(this object shadowed) 
		{
			(shadowed as IShadowChangeTracker).BaselineOriginals();
		}

		#region "HasChanges" Extensions
		public static bool HasChanges<T>(this T shadowed)
		{
			return (shadowed as IShadowChangeTracker).HasChanges;
		}

		public static bool HasChanges<T>(this T shadowed, Expression<Func<T, object>> property)
		{
			//var ishadow = GetIShadow((IShadowObject)shadowed);
			return (shadowed as IShadowChangeTracker).HasPropertyChange((T)shadowed, property);
		}
		#endregion

        public static string ListChanges<T>(this T shadowed)
        {
            return (shadowed as IShadowChangeTracker).ListChanges((T)shadowed);
        }

        public static IDictionary<object, ChangeType> GetDictionaryChanges<T>(this T shadowed)
        {
            return (shadowed as IShadowChangeTracker).GetDictionaryChanges((T)shadowed);
        }

		#region "GetOriginals Extensions"

        public static T GetOriginal<T>(this T shadowed)
        {
            //var ishadow = GetIShadow((IShadowObject)shadowed);
            return (shadowed as IShadowChangeTracker).GetOriginal((T)shadowed);
        }

        public static object GetOriginal<T>(this T shadowed, Expression<Func<T, object>> property)
        {
            //var ishadow = GetIShadow((IShadowObject)shadowed);
            return (shadowed as IShadowChangeTracker).GetOriginal((T)shadowed, property);
        }

        public static object GetOriginal<T>(this T shadowed, object propertyKey)
        {
            //var ishadow = GetIShadow((IShadowObject)shadowed);
            return (shadowed as IShadowChangeTracker).GetOriginal((T)shadowed, propertyKey);
        }
		#endregion

		private static bool IsDictionary(Type checkType)
		{
			Type[] colType = checkType.GetGenericArguments();
			if (colType.Count() == 2)
			{
				Type GenShadowType = typeof(IDictionary<,>);
				Type SpecShadowType = GenShadowType.MakeGenericType(colType);
				return SpecShadowType.IsAssignableFrom(checkType);
			}
			return false;
		}

		//This is not used....but um, I like having it around to remind me how to get the Interceptor reference...bad coding, bad.
		internal static IShadowIntercept GetIShadow(object shadowed)
		{
			if (shadowed == null)
			{
				return null;
			}
			var hack = shadowed as IProxyTargetAccessor;

			if (hack == null)
			{
				throw new ArgumentException("Object is not a Proxy");
			}

			return hack.GetInterceptors().FirstOrDefault(i => i is IShadowIntercept) as IShadowIntercept;
		}

	}

	public interface IShadowObject
	{
		
	}

	public class ShadowedObjectProxyGenerationHook : IProxyGenerationHook
	{
		public void MethodsInspected()
		{
		}

		public void NonProxyableMemberNotification(Type type, System.Reflection.MemberInfo memberInfo)
		{
		}

		public bool ShouldInterceptMethod(Type type, System.Reflection.MethodInfo methodInfo)
		{	
			//JDB: technically I think this should use the Attribute that the compiler puts on the IL, but this will do for now
			if (methodInfo.Name.StartsWith("set_", StringComparison.Ordinal))
			{ return true;}
			
			return false;
		}
	}
}
