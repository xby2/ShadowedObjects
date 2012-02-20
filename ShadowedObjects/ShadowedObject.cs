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
	
	public static class ShadowedObject
	{
		private static readonly ProxyGenerator _generator = new ProxyGenerator();

		public static T Create<T>() where T: class
		{
			var setCeptor = new ShadowedInterceptor<T>();
			var options = new ProxyGenerationOptions(new ShadowedObjectProxyGenerationHook());
			var theShadow = _generator.CreateClassProxy(typeof(T), new Type[]{ typeof(IShadowObject) }, options, setCeptor);

			GetIShadow<T>((T)theShadow).BaselineOriginals();
			
			return theShadow as T;
		}

		public static void ResetToOriginal<T>(this T shadowed, Expression<Func<T, object>> property )
		{
			var ishadow = GetIShadow<T>((T)shadowed);
			ishadow.ResetToOriginals((T)shadowed, property);
		}

		public static void BaselineOriginals<T>(this T shadowed)
		{
			BaselineOriginalsEx<T>((T)shadowed);
		}

		public static void BaselineOriginalsEx<T>(T shadowed)
		{
			var ishadow = GetIShadow<T>(shadowed);

			ishadow.BaselineOriginals();
		}

		public static bool HasChanges<T>(this T shadowed)
		{
			var ishadow = GetIShadow(shadowed);

			return ishadow.HasChanges;
		}

		private static IShadowIntercept<T> GetIShadow<T>(T shadowed)
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

			return hack.GetInterceptors().FirstOrDefault(i => i is IShadowIntercept<T>) as IShadowIntercept<T>;
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
			//var err = System.Text.UTF8Encoding.UTF8.GetBytes("Can't Proxy: " + memberInfo.Name + "\n");
			//Console.OpenStandardError().Write(err, 0, err.Length);
		}

		public bool ShouldInterceptMethod(Type type, System.Reflection.MethodInfo methodInfo)
		{	
			//JDB: technically I think this should use the Attribute that the compiler puts on the IL, but this will do for now
			if (methodInfo.Name.StartsWith("set_", StringComparison.Ordinal))
			{ return true;}

			if (methodInfo.ReturnType.IsGenericType && typeof(ICollection).IsAssignableFrom( methodInfo.ReturnType))
			{	return true; }

			return false;
		}
	}

	//public class ShadowedObjectsInterceptorSelector : IInterceptorSelector
	//{

	//    public IInterceptor[] SelectInterceptors(Type type, System.Reflection.MethodInfo method, IInterceptor[] interceptors)
	//    {
	//        if ( IsSetter(method) )
	//        {
	//            return interceptors.Where( i=>i.GetType().GetGenericTypeDefinition() == typeof( ShadowedSetInterceptor<> ) ).ToArray();
	//        }
	//        else if ( IsGenericCollection( method ) )
	//        {
	//            return interceptors.Where(i => i.GetType().GetGenericTypeDefinition() == typeof(ShadowedGenericCollectionInterceptor<>)).ToArray();
	//        }

	//        return new IInterceptor[0];
	//    }

	//    private bool IsGenericCollection(MethodInfo method)
	//    {
	//        return method.ReturnType.IsGenericType && typeof(ICollection).IsAssignableFrom( method.ReturnType);
	//    }

	//    private bool IsSetter(MethodInfo method)
	//    {
	//        return method.IsSpecialName && method.Name.StartsWith("set_", StringComparison.Ordinal);
	//    }
	//}
}
