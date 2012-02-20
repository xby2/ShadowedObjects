﻿using System;
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
	public interface IShadowIntercept
	{
		bool HasChildChanges { get; }
		bool HasDirectChanges { get; }
		bool HasChanges{ get; }
	}

	public interface IShadowIntercept<T> : IShadowIntercept
	{
		void BaselineOriginals();
		void ResetToOriginals(T instance, Expression<Func<T, object>> func);

	}
	
	public class ShadowedInterceptor<T> : IShadowIntercept<T>, IShadowIntercept, IInterceptor
	{

		protected readonly Dictionary<string, object> Originals = new Dictionary<string, object>();
		protected readonly Dictionary<string, object> Previous = new Dictionary<string, object>();

		protected readonly Dictionary<string, IShadowObject> Children = new Dictionary<string, IShadowObject>();

		protected static readonly ILog logger = LogManager.GetLogger(typeof(ShadowedInterceptor<T>));

		public void BaselineOriginals()
		{
			Originals.Clear();
			HasDirectChanges = false;
		}

		public void ResetToOriginals(T instance, Expression<Func<T, object>> func)
		{
			var prop = ExpressionUtil.GetPropertyCore(func.Body);

			if ( ! Originals.ContainsKey(prop.Name))
			{	return;}

			prop.GetSetMethod().Invoke(instance, new object[1] { Originals[prop.Name] });

			Originals.Remove(prop.Name);
			
			if ( Originals.Count < 1 )
			{
				HasDirectChanges = false;
			}
		}

		public bool HasChanges 
		{ 
			get
			{
				return HasDirectChanges || HasChildChanges;
			} 
		}

		public bool HasDirectChanges { get; private set; }

		private HasChangesDelegate _hasChildrenChangesDelegate = () => {return false;};

		public bool HasChildChanges 
		{ 
			get
			{
				if (_hasChildrenChangesDelegate.GetInvocationList().Cast<HasChangesDelegate>().Any(delg => delg()))
				{
					return true;
				}
				return false;
			}
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

			if ( ! Originals.ContainsKey(strippedName))
			{
				if (theCollection == null && getValue != null) //a collection, but not a ShadowCollection
				{
					Type[] colType = getValue.GetType().GetGenericArguments();
					Type GenShadowType = typeof(ShadowCollection<>);
					Type SpecShadowType = GenShadowType.MakeGenericType(colType);

					theCollection = Activator.CreateInstance(SpecShadowType, getValue) as IShadowCollection;

					Originals[strippedName] = theCollection; //we need a value in originals so the Set_ we are about to invoke doesn't try to do anything.  We remove it right after

					var setMethod = invocation.InvocationTarget.GetType().GetMethod("set_" + strippedName);
					var setValue = setMethod.Invoke(invocation.InvocationTarget, new object[1] { theCollection } );
					
					Originals.Remove(strippedName);

					SetCollectionDelegate(strippedName, theCollection);
				}
				else if (theCollection != null)
				{
					SetCollectionDelegate(strippedName, theCollection);
					//Originals[strippedName] = theCollection == null ? null : theCollection.Clone();
				}
			}
			
		}

		private void SetCollectionDelegate(string strippedName, IShadowCollection theCollection)
		{
			if (!theCollection.isTracked)
			{
				changedDelegate delg = () =>
				                       	{
				                       		if (!Originals.ContainsKey(strippedName))
				                       		{
				                       			Originals[strippedName] = theCollection.Clone();
				                       		}
				                       	};

				theCollection.changed += delg;
			}
		}

		private void InterceptSet(IInvocation invocation)
		{
			var strippedName = invocation.MethodInvocationTarget.Name.Replace("set_", "");
			var getName = "get_" + strippedName;
			var getMethod = invocation.InvocationTarget.GetType().GetMethod(getName);
			var getValue = getMethod.Invoke(invocation.InvocationTarget, new object[0]);

			var setValue = invocation.GetArgumentValue(0);

			Previous[strippedName] = getValue;

			if (!Originals.ContainsKey(strippedName))
			{
				Originals[strippedName] = getValue;
			}

			if (logger.IsInfoEnabled)
			{
				if (getValue is ICollection)
				{
					logger.InfoFormat("Intercepted {0}. Old Length:{1}. New Length:{2}", invocation.MethodInvocationTarget.Name, (getValue as ICollection).Count, (setValue as ICollection).Count);
				}
				else
				{
					logger.InfoFormat("Intercepted {0}. Old Value:{1}. New Value:{2}", invocation.MethodInvocationTarget.Name, getValue, setValue);
				}
			}

			if ( setValue is IShadowObject )
			{
				_hasChildrenChangesDelegate += ()=>{ return ShadowedObject.GetIShadow(setValue as IShadowObject).HasChanges; };
			}
			else
			{
				Children.Remove(strippedName);
			}

			invocation.Proceed();

			HasDirectChanges = true;
		}
	}

	public delegate bool HasChangesDelegate();
}