using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ShadowedObjects
{
	internal static class ExpressionUtil
	{

			public static PropertyInfo GetProperty<TObject>(Expression<Func<TObject, object>> propertyRefExpr)
			{
				return GetPropertyCore(propertyRefExpr.Body);
			}

			public static PropertyInfo GetPropertyCore(Expression propertyRefExpr)
			{
				if (propertyRefExpr == null)
					throw new ArgumentNullException("propertyRefExpr", "propertyRefExpr is null.");

				MemberExpression memberExpr = propertyRefExpr as MemberExpression;
				if (memberExpr == null)
				{
					UnaryExpression unaryExpr = propertyRefExpr as UnaryExpression;
					if (unaryExpr != null && unaryExpr.NodeType == ExpressionType.Convert)
						memberExpr = unaryExpr.Operand as MemberExpression;
				}

				if (memberExpr != null && memberExpr.Member.MemberType == MemberTypes.Property)
					return memberExpr.Member as PropertyInfo;

				throw new ArgumentException("No property reference expression was found.",
								 "propertyRefExpr");
			}
	}
	
}
