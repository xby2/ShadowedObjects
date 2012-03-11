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
    /*
	public interface IShadowChangeTracker
	{
		bool HasChildChanges { get; }
		bool HasDirectChanges { get; }
		bool HasChanges{ get; }
        void BaselineOriginals();
	}

	public interface IShadowIntercept<T> : IShadowChangeTracker
	{
		void ResetToOriginals(T instance, Expression<Func<T, object>> func);
	}*/
	
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

			//    TKey key = (TKey)invocation.GetArgumentValue(0);
			//    if (key != null && !Originals.ContainsKey(key))
			//    {
			//        Originals[key] = ((Dictionary<TKey, TValue>)invocation.InvocationTarget)[key];
			//        _hasdirectchanges = true;
			//    }
			//    if (key != null && Children.ContainsKey(key))
			//    {
			//        Children.Remove(key);
			//    }                    
			}
			else if (invocation.Method.Name.Equals("Add"))
			{
				Instance.trackChanges(invocation.GetArgumentValue(0), null, invocation.GetArgumentValue(1));
			//    TKey key = (TKey)invocation.GetArgumentValue(0);
			//    if (key != null && !Originals.ContainsKey(key))
			//    {
			//        Originals[key] = default(TValue);
			//        _hasdirectchanges = true;
			//    }
			//    if (key != null && !Children.ContainsKey(key))
			//    {
			//        TValue addValue = (TValue)invocation.GetArgumentValue(1);
			//        Children.Add(key, addValue);
			//    }                    
			}
            else if (invocation.Method.Name.Equals("set_Item"))
            {
                object getValue = null;
                if ((invocation.InvocationTarget as IDictionary).Contains(invocation.GetArgumentValue(0)))
                {
                    getValue = (invocation.InvocationTarget as IDictionary)[invocation.GetArgumentValue(0)];
                }

                Instance.trackChanges(invocation.GetArgumentValue(0), getValue, invocation.GetArgumentValue(1));
                //TKey key = (TKey)invocation.GetArgumentValue(0);
                //if (key != null && !Originals.ContainsKey(key))
                //{
                //    if (((Dictionary<TKey, TValue>)invocation.InvocationTarget).ContainsKey(key))
                //    {
                //        Originals[key] = ((Dictionary<TKey, TValue>)invocation.InvocationTarget)[key];
                //    }
                //    else
                //    {
                //        Originals[key] = default(TValue);
                //    }
                //    _hasdirectchanges = true;
                //}
                //if (key != null)
                //{
                //    TValue addValue = (TValue)invocation.GetArgumentValue(1);
                //    Children[key] = addValue;
                //    ChildrenChanges[key] = () => { return ShadowedObject.GetIShadow(addValue as IShadowObject).HasChanges; };
                //}
            }
            
			

            invocation.Proceed();
            
        }
    }

	//public delegate bool HasChangesDelegate();

    public class ShadowedDictionarySelector : IInterceptorSelector
    {
        private static readonly string[] methodNamesToIntercept = new[] { "Add", "Remove", "set_Item" }; 

        public IInterceptor[] SelectInterceptors(Type type, MethodInfo method, IInterceptor[] interceptors)
        {
            if (methodNamesToIntercept.Contains(method.Name))
            {
                Type[] colType = type.GetGenericArguments();
                Type GenShadowType = typeof(ShadowedDictionaryInterceptor<,>);
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
