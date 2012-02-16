using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.DynamicProxy;

namespace CastleDynamicProxyPOC
{
	public static class ShadowedObject
	{
		private static readonly ProxyGenerator _generator = new ProxyGenerator();

		public static T Create<T>() where T: class
		{
			var shadowCeptor = new ShadowedObjectInterceptor();
			var options = new ProxyGenerationOptions(new ShadowedObjectProxyGenerationHook());
			var theShadow = _generator.CreateClassProxy(typeof(T), options, shadowCeptor);

			GetIShadow(theShadow).BaselineOriginals();
			
			return theShadow as T;
		}

		public static void ResetToOriginal(this object shadowed, string propName="")
		{
			var ishadow = GetIShadow(shadowed);

			ishadow.ResetToOriginal(shadowed, propName);
		}

		private static IShadowObject GetIShadow(object shadowed)
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
			
			return hack.GetInterceptors().FirstOrDefault(i => i is IShadowObject) as IShadowObject ;
		}
	}

	public interface IShadowObject
	{
		void BaselineOriginals();
		void ResetToOriginal(object instance, string propName);
	}

	public class ShadowedObjectInterceptor : IInterceptor, IShadowObject
	{
		private readonly Dictionary<string, object> Originals = new Dictionary<string, object>();
		private readonly Dictionary<string, object> Previous = new Dictionary<string, object>(); 

		public void BaselineOriginals()
		{
			Originals.Clear();
		}

		public void ResetToOriginal(object instance, string propName = "")
		{
			var setName = "set_" + propName;
			var setMethod = instance.GetType().GetMethod(setName);
			var getValue = setMethod.Invoke(instance, new object[1] { Originals[propName] });
		}

		public void Intercept(IInvocation invocation)
		{
			var strippedName = invocation.MethodInvocationTarget.Name.Replace("set_", "");
			var getName = "get_" + strippedName;
			var getMethod = invocation.InvocationTarget.GetType().GetMethod(getName);
			var getValue = getMethod.Invoke(invocation.InvocationTarget, new object[0]);

			Previous[strippedName] = getValue;

			if (!Originals.ContainsKey(strippedName))
			{
				Originals[strippedName] = getValue;
			}

			Console.WriteLine(String.Format("Intercepted {0}. Old Value:{1}. New Value:{2}",invocation.MethodInvocationTarget.Name, getValue, invocation.GetArgumentValue(0) ) );

			invocation.Proceed();
		}
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
			return methodInfo.Name.StartsWith("set_", StringComparison.Ordinal);

			return true;
		}
	}
}
