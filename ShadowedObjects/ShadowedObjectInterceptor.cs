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
			//if (IsSetter(invocation.Method))
			//{
			    InterceptSet(invocation);
			//}
			//else if (!IsDictionary(invocation.Method) && IsGenericCollection(invocation.Method))
			//{
			//    InterceptGenericCollection(invocation);
			//}
			//else
            {
                invocation.Proceed();
            }
		}

		//private bool IsDictionary(MethodInfo method)
		//{
		//    return method.ReturnType.Name.Equals(typeof(Dictionary<string, object>).Name);
		//}

		//private bool IsGenericCollection(MethodInfo method)
		//{
		//    return method.ReturnType.IsGenericType && typeof(ICollection).IsAssignableFrom( method.ReturnType);
		//}

		//private bool IsSetter(MethodInfo method)
		//{
		//    return method.IsSpecialName && method.Name.StartsWith("set_", StringComparison.Ordinal);
		//}

		//private void InterceptGenericCollection(IInvocation invocation)
		//{
		//    invocation.Proceed();
		//    var getValue = invocation.ReturnValue;

		//    var strippedName = invocation.MethodInvocationTarget.Name.Replace("set_", "").Replace("get_", "");

		//    var theCollection = (getValue as IShadowCollection);

		//    if (!Originals.ContainsKey(strippedName))
		//    {
		//        if (theCollection == null && getValue != null) //a collection, but not a ShadowCollection
		//        {
		//            Type[] colType = getValue.GetType().GetGenericArguments();
		//            Type GenShadowType = typeof(ShadowCollection<>);
		//            if (colType.Length == 2)
		//            {
		//                GenShadowType = typeof(ShadowDictionary<,>);
		//            }
		//            Type SpecShadowType = GenShadowType.MakeGenericType(colType);

		//            theCollection = Activator.CreateInstance(SpecShadowType, getValue) as IShadowCollection;

		//            Originals[strippedName] = theCollection; //we need a value in originals so the Set_ we are about to invoke doesn't try to do anything.  We remove it right after

		//            var setMethod = invocation.InvocationTarget.GetType().GetMethod("set_" + strippedName);
		//            var setValue = setMethod.Invoke(invocation.InvocationTarget, new object[1] { theCollection });

		//            Originals.Remove(strippedName);

		//            SetCollectionDelegate(strippedName, theCollection);
		//        }
		//        else if (theCollection != null)
		//        {
		//            SetCollectionDelegate(strippedName, theCollection);
		//            //Originals[strippedName] = theCollection == null ? null : theCollection.Clone();
		//        }
		//    }
			
		//}

		//private void SetCollectionDelegate(string strippedName, IShadowCollection theCollection)
		//{
		//    if (!theCollection.isTracked)
		//    {
		//        changedDelegate delg = () =>
		//                                {
		//                                    if (!Originals.ContainsKey(strippedName))
		//                                    {
		//                                        Originals[strippedName] = theCollection.Clone();
		//                                    }
		//                                };

		//        theCollection.changed += delg;
		//    }
		//}

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
