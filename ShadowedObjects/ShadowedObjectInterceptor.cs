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
	public interface IShadowIntercept<T>
	{
		void BaselineOriginals();
		void ResetToOriginals(T instance, Expression<Func<T, object>> func);
	}

	
	public class ShadowedInterceptor<T> : IShadowIntercept<T>, IInterceptor
	{

		protected readonly Dictionary<string, object> Originals = new Dictionary<string, object>();
		protected readonly Dictionary<string, object> Previous = new Dictionary<string, object>();

		protected static readonly ILog logger = LogManager.GetLogger(typeof(ShadowedInterceptor<T>));

		public void BaselineOriginals()
		{
			Originals.Clear();
		}

		public void ResetToOriginals(T instance, Expression<Func<T, object>> func)
		{
			var prop = ExpressionUtil.GetPropertyCore(func.Body);
			prop.GetSetMethod().Invoke(instance, new object[1] { Originals[prop.Name] });
		}
			
		public void Intercept(IInvocation invocation)
		{
			if (IsSetter(invocation.Method))
			{
				InterceptSet(invocation);
			}
			else if (IsGenericCollection(invocation.Method))
			{
				InterceptGenericCollection(invocation);
			}
		}
		

		private bool IsGenericCollection(MethodInfo method)
		{
			return method.ReturnType.IsGenericType && typeof(ICollection).IsAssignableFrom( method.ReturnType);
		}

		private bool IsSetter(MethodInfo method)
		{
			return method.IsSpecialName && method.Name.StartsWith("set_", StringComparison.Ordinal);
		}

		private void InterceptGenericCollection(IInvocation invocation)
		{
			invocation.Proceed();
			var getValue = invocation.ReturnValue;

			var strippedName = invocation.MethodInvocationTarget.Name.Replace("set_", "").Replace("get_", "");

			var theCollection = (getValue as IShadowCollection);

			if (!Originals.ContainsKey(strippedName))
			{
				if (theCollection == null && getValue != null) //a collection, but not a ShadowCollection
				{
					Type[] colType = getValue.GetType().GetGenericArguments();
					Type GenShadowType = typeof(ShadowCollection<>);
					Type SpecShadowType = GenShadowType.MakeGenericType(colType);

					var ShadowList = Activator.CreateInstance(SpecShadowType, getValue);

					Originals[strippedName] = ShadowList;
				}
				else
				{
					Originals[strippedName] = theCollection == null ? null : theCollection.Clone();
				}
			}

			//if (theCollection != null)
			//{
			//    if ( ! theCollection.isTracked)
			//    {
			//        changedDelegate delg = ()=>
			//        {
			//            if (!Originals.ContainsKey(strippedName))
			//            {
			//                Originals[strippedName] = theCollection.Clone();
			//            }
			//        };

			//        theCollection.changed += delg;				
			//    }
			//}

		}

		private void InterceptSet(IInvocation invocation)
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

			if (logger.IsInfoEnabled)
			{
				if (getValue is ICollection)
				{
					logger.InfoFormat("Intercepted {0}. Old Length:{1}. New Length:{2}", invocation.MethodInvocationTarget.Name, (getValue as ICollection).Count, (invocation.GetArgumentValue(0) as ICollection).Count);
				}
				else
				{
					logger.InfoFormat("Intercepted {0}. Old Value:{1}. New Value:{2}", invocation.MethodInvocationTarget.Name, getValue, invocation.GetArgumentValue(0));
				}
			}

			invocation.Proceed();
		}
	}

}
