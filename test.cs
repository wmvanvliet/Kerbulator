using System.Linq.Expressions;
using System;
using System.Reflection;

namespace test {
	public class test {
		public static void Main(string[] args) {
			ParameterExpression result = Expression.Parameter(typeof(int), "result");
			Expression e = Expression.Invoke(result, new[] {Expression.Constant(1)});
			Console.WriteLine("Hello, world!");
			Console.WriteLine(e.ToString());
		}
	}
}
