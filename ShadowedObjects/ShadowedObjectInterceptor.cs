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
	
	public class ShadowedInterceptor<T> : IShadowIntercept<T>, IInterceptor
	{

		protected static readonly ILog logger = LogManager.GetLogger(typeof(ShadowedInterceptor<>));

		public IShadowMetaData Instance { get; set; }

		public void Intercept(IInvocation invocation)
		{
			InterceptSet(invocation);

		}

		private void InterceptSet(IInvocation invocation)
		{
			var strippedName = invocation.MethodInvocationTarget.Name.Replace("set_", "");
			var getName = "get_" + strippedName;
			var getMethod = invocation.InvocationTarget.GetType().GetMethod(getName);
			var getValue = getMethod.Invoke(invocation.InvocationTarget, new object[0]);

			var setValue = invocation.GetArgumentValue(0);

			Instance.trackChanges(strippedName, getValue, setValue);

			invocation.Proceed();
		}
	}

}
