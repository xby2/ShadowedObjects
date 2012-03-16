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
 	public class ShadowedDictionaryInterceptor<TKey, TValue> : IShadowIntercept<IDictionary<TKey, TValue>>, IInterceptor
	{

		public IShadowMetaData Instance { get; set; }	

		public void Intercept(IInvocation invocation)
		{
			InterceptDictionary(invocation);
		}

        private void InterceptDictionary(IInvocation invocation)
        {
			if (invocation.Method.Name.Equals("Remove"))
			{
                Instance.trackChanges(invocation.GetArgumentValue(0), (invocation.InvocationTarget as IDictionary)[invocation.GetArgumentValue(0)], null);
			}
			else if (invocation.Method.Name.Equals("Add"))
			{
				Instance.trackChanges(invocation.GetArgumentValue(0), null, invocation.GetArgumentValue(1));			
			}
            else if (invocation.Method.Name.Equals("set_Item"))
            {
                object getValue = null;
                if ((invocation.InvocationTarget as IDictionary).Contains(invocation.GetArgumentValue(0)))
                {
                    getValue = (invocation.InvocationTarget as IDictionary)[invocation.GetArgumentValue(0)];
                }

                Instance.trackChanges(invocation.GetArgumentValue(0), getValue, invocation.GetArgumentValue(1));
            }
            
            invocation.Proceed();            
        }
    }

	public class ShadowedDictionaryProxyGenerationHook : IProxyGenerationHook
	{
		public void MethodsInspected()
		{
		}

		public void NonProxyableMemberNotification(Type type, System.Reflection.MemberInfo memberInfo)
		{
		}

		private static readonly string[] methodNamesToIntercept = new[] { "Add", "Remove", "set_Item" }; 

		public bool ShouldInterceptMethod(Type type, System.Reflection.MethodInfo methodInfo)
		{
			if (methodNamesToIntercept.Contains(methodInfo.Name))
			{ return true; }

			return false;
		}
	}
}
