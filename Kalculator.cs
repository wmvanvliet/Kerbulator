// File encoding should remain in UTF-8!

using System;
using System.IO;
using System.Collections.Generic;

namespace Kalculator {
	public class Kalculator {
		public static bool DEBUG = false;
		public static void Debug(string s) {
			if(DEBUG)
				Console.Write(s);
		}
		public static void DebugLine(string s) {
			if(DEBUG)
				Console.WriteLine(s);
		}

		private Dictionary<string, Operator>operators;
		private Dictionary<string, Variable> globals;
		private string functionDir;

		public Kalculator(string functionDir) {
			this.functionDir = functionDir;

			operators = new Dictionary<string, Operator>();
			operators.Add("=", new Operator("=", 1, Arity.BINARY)); // Assignment
			operators.Add("-", new Operator("-", 1, Arity.BOTH)); // Substraction or negation
			operators.Add("+", new Operator("+", 1, Arity.BINARY)); // Addition
			operators.Add("/", new Operator("/", 2, Arity.BINARY)); // Division
			operators.Add("÷", new Operator("÷", 2, Arity.BINARY)); // Division
			operators.Add("√", new Operator("√", 2, Arity.BOTH)); // Square Root or ^(1/n)
			operators.Add("%", new Operator("%", 2, Arity.BINARY)); // Modulo
			operators.Add("*", new Operator("*", 2, Arity.BINARY)); // Multiplication
			operators.Add("·", new Operator("·", 2, Arity.BINARY)); // Multiplication
			operators.Add("×", new Operator("×", 2, Arity.BINARY)); // Multiplication
			operators.Add("^", new Operator("^", 3, Arity.BINARY)); // Multiplication
			operators.Add("|", new Operator("|", 3, Arity.UNARY)); // Absolute
			operators.Add("⌊", new Operator("⌊", 3, Arity.UNARY)); // Floor
			operators.Add("⌈", new Operator("⌈", 3, Arity.UNARY)); // Ceiling
			operators.Add("func", new Operator("func", 2, Arity.BINARY)); // Execute function as unary operator

			globals = new Dictionary<string, Variable>();
			globals.Add("abs", new Variable("abs", VarType.FUNCTION, 1));
			globals.Add("acos", new Variable("acos", VarType.FUNCTION, 1));
			globals.Add("asin", new Variable("asin", VarType.FUNCTION, 1));
			globals.Add("atan", new Variable("atan", VarType.FUNCTION, 1));
			globals.Add("ceil", new Variable("ceil", VarType.FUNCTION, 1));
			globals.Add("cos", new Variable("cos", VarType.FUNCTION, 1));
			globals.Add("exp", new Variable("exp", VarType.FUNCTION, 1));
			globals.Add("floor", new Variable("floor", VarType.FUNCTION, 1));
			globals.Add("ln", new Variable("ln", VarType.FUNCTION, 1));
			globals.Add("log", new Variable("log", VarType.FUNCTION, 1));
			globals.Add("log10", new Variable("log10", VarType.FUNCTION, 1));
			globals.Add("max", new Variable("max", VarType.FUNCTION, 2));
			globals.Add("min", new Variable("min", VarType.FUNCTION, 2));
			globals.Add("pow", new Variable("pow", VarType.FUNCTION, 2));
			globals.Add("round", new Variable("round", VarType.FUNCTION, 2));
			globals.Add("sign", new Variable("sign", VarType.FUNCTION, 1));
			globals.Add("sin", new Variable("sin", VarType.FUNCTION, 1));
			globals.Add("sqrt", new Variable("sqrt", VarType.FUNCTION, 1));
			globals.Add("tan", new Variable("tan", VarType.FUNCTION, 1));
			globals.Add("len", new Variable("len", VarType.FUNCTION, 1));
			globals.Add("norm", new Variable("norm", VarType.FUNCTION, 1));
			globals.Add("dot", new Variable("dot", VarType.FUNCTION, 2));
			globals.Add("cross", new Variable("cross", VarType.FUNCTION, 2));

			globals.Add("pi", new Variable("pi", VarType.NUMBER, Math.PI));
			globals.Add("π", new Variable("π", VarType.NUMBER, Math.PI));
			globals.Add("e", new Variable("e", VarType.NUMBER, Math.E));

			// Add KSP globals
			Globals.Add(this);
		}

		public void AddGlobal(Variable v) {
			if(globals.ContainsKey(v.id))
				globals[v.id] = v;
			else
				globals.Add(v.id, v);
		}

		public List<Variable> Run(string functionId) {
			Dictionary<string, Function> functions = Function.Scan(functionDir);
			return functions[functionId].Execute(new List<Variable>(), operators, globals, functions);
		}

		public List<Variable> Run(Function f) {
			Dictionary<string, Function> functions = Function.Scan(functionDir);
			return f.Execute(new List<Variable>(), operators, globals, functions);
		}

		public static void Main(string[] args) {
			Kalculator k = new Kalculator("./tests");
			Kalculator.DEBUG = true;
			List<Variable> result = k.Run(args[0]);
			foreach(Variable v in result)
				Debug(v.id +" = "+ v.ToString() +", ");
			Debug("\n");
		}
	}
}
