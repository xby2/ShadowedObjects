using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Castle.DynamicProxy;
using log4net;

namespace ShadowedObjects
{
	
	public class ShadowedCollectionInterceptor<T> : IShadowIntercept<T>, IInterceptor
	{

		//protected static readonly ILog logger = LogManager.GetLogger(typeof(ShadowedCollectionInterceptor<>));

		public IShadowMetaData Instance { get; set; }

		public void Intercept(IInvocation invocation)
		{
			Instance.trackChanges("","","");

			invocation.Proceed();            
		}

	}


	public class ShadowedCollectionProxyGenerationHook : IProxyGenerationHook
	{
		public void MethodsInspected()
		{
		}

		public void NonProxyableMemberNotification(Type type, System.Reflection.MemberInfo memberInfo)
		{
		}

		private static readonly string[] methodNamesToIntercept = new[] { "Add", "Remove", "set_Item", "InsertItem", "SetItem", "RemoveItem", "ClearItems" };

		public bool ShouldInterceptMethod(Type type, System.Reflection.MethodInfo methodInfo)
		{
			if (methodNamesToIntercept.Contains(methodInfo.Name))
			{ return true; }

			return false;
		}
	}
}
