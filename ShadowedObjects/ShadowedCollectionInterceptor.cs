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

	//public delegate bool HasChangesDelegate();

	public class ShadowedCollectionSelector : IInterceptorSelector
	{
		private static readonly string[] methodNamesToIntercept = new[] { "Add", "Remove", "set_Item", "InsertItem", "SetItem", "RemoveItem", "ClearItems" };

		public IInterceptor[] SelectInterceptors(Type type, MethodInfo method, IInterceptor[] interceptors)
		{
			if (methodNamesToIntercept.Contains(method.Name))
			{
				Type[] colType = type.GetGenericArguments();
				Type GenShadowType = typeof(ShadowedCollectionInterceptor<>);
				Type SpecShadowType = GenShadowType.MakeGenericType(colType);
				ConstructorInfo constructor = SpecShadowType.GetConstructor(new Type[0]);
				var newObject1 = constructor.Invoke(new object[0]);
				var newObject = newObject1 as IInterceptor;
				if (interceptors.Length == 1)
				{
					return interceptors;
				}
				else
				{
					return new IInterceptor[] { newObject };
				}
			}

			return new IInterceptor[] { };
		}
	}
    

}
