// File encoding should remain in UTF-8!

using System;
using System.IO;
using System.Collections.Generic;

namespace Kerbulator {
	public class Function {
		private string id = "";

		private Queue<Token> tokens;
		private Dictionary<string, Operator>operators;
		private Dictionary<string, Variable> globals;
		private Dictionary<string, Variable> locals;
		private Dictionary<string, Function> functions;

		List<string> ins;
		List<string> outs;
		List<string> inDescriptions;
		List<string> outDescriptions;
		private bool inError = false;
		private string errorString = "";

		public Function(string id, string function) {
			this.id = id;
			this.ins = new List<string>();
			this.outs = new List<string>();
			this.inDescriptions = new List<string>();
			this.outDescriptions = new List<string>();

			try {
				Tokenizer t = new Tokenizer();
				t.Tokenize(function +"\n");
				tokens = t.tokens;

				Parse();
			} catch(Exception e) {
				inError = true;
				errorString = e.Message;
			}
		}
	   
		public static Function FromFile(string filename) {
			StreamReader file = File.OpenText(filename);
            string contents = file.ReadToEnd();
            file.Close();
			return new Function(Path.GetFileNameWithoutExtension(filename), contents);
		}

		public static Dictionary<string, Function> Scan(string dir) {
			Dictionary<string, Function> functions = new Dictionary<string, Function>();

			foreach(string filename in Directory.GetFiles(dir, "*.math")) {
				Function f = FromFile(filename);
				functions.Add(f.Id, f);
			}

			return functions;
		}

		public string Id {
			get { return id; }
			protected set {}
		}

		public List<string> Ins {
			get { return ins; }
			protected set {}
		}

		public List<string> InDescriptions {
			get { return inDescriptions; }
			protected set {}
		}

		public List<string> Outs {
			get { return outs; }
			protected set {}
		}

		public List<string> OutDescriptions {
			get { return outDescriptions; }
			protected set {}
		}

		public bool InError {
			get { return inError; }
			set { inError = value; if(!value) errorString = ""; }
		}

		public string ErrorString {
			get { return errorString; }
			set { errorString = (string)value; Kerbulator.DebugLine(value); inError = true; }
		}

		private Token Consume() {
			if(tokens.Count == 0)
				throw new Exception("Unexpected end of expression.");

			return tokens.Dequeue();
		}

		private Token Consume(string val) {
			if(tokens.Count == 0)
				throw new Exception("Expected: "+ val);

			Token t = tokens.Dequeue();
			if(!String.Equals(t.val, val))
				throw new Exception("Expected: "+ val);

			return t;
		}

		private Token Consume(TokenType type) {
			if(tokens.Count == 0)
				throw new Exception("Expected "+ Enum.GetName(typeof(TokenType), type));

			Token t = tokens.Dequeue();
			if(t.type != type)
				throw new Exception("Expected "+ Enum.GetName(typeof(TokenType), type));

			return t;
		}

		private void Parse() {
			while(tokens.Peek().type == TokenType.END)
				Consume();

			while(tokens.Peek().type == TokenType.IN) {
				Consume();
				Token id = Consume(TokenType.IDENTIFIER);

				if(tokens.Peek().type == TokenType.TEXT) {
					inDescriptions.Add( Consume(TokenType.TEXT).val );
				}

				Consume(TokenType.END);
				ins.Add(id.val);
				Kerbulator.DebugLine("Found IN statement for "+ id.val);
			}

			while(tokens.Peek().type == TokenType.END)
				Consume();

			while(tokens.Peek().type == TokenType.OUT) {
				Consume();
				Token id = Consume(TokenType.IDENTIFIER);

				if(tokens.Peek().type == TokenType.TEXT) {
					outDescriptions.Add( Consume(TokenType.TEXT).val );
				}

				Consume(TokenType.END);
				outs.Add(id.val);
				Kerbulator.DebugLine("Found OUT statement for "+ id.val);
			}

			Kerbulator.DebugLine("");
		}

		public List<Variable> Execute(List<Variable> arguments, Dictionary<string, Operator> operators, Dictionary<string, Variable> globals, Dictionary<string, Function> functions) {
			List<Variable> result = new List<Variable>();
			Queue<Token> oldTokens = new Queue<Token>(tokens);

			try {
				this.InError = false;

				this.locals = new Dictionary<string, Variable>();
				this.operators = operators;
				this.globals = globals;
				this.functions = functions;

				if(ins.Count != arguments.Count)
					throw new Exception("Function takes "+ ins.Count +" arguments, but "+ arguments.Count +" were supplied.");

				for(int i=0; i<ins.Count; i++)
					locals.Add(ins[i], arguments[i].Copy(ins[i]));

				Variable lastVal = new Variable(VarType.NUMBER, 0.0);
				while(tokens.Count > 0) {
					Variable val = ExecuteStatement();
					if(val != null)
						lastVal = val;
					Consume(TokenType.END);
				}

				if(outs.Count > 0) {
					foreach(string id in outs) {
						if(!locals.ContainsKey(id))
							throw new Exception("Output variable "+ id +" is not defined by function.");
						result.Add(locals[id]);
					}
				} else
					result.Add(lastVal);
			} catch(Exception e) {
				this.InError = true;
				this.ErrorString = e.Message;
			}

			tokens = oldTokens;
			return result;
		}

		private Variable ExecuteStatement() {
			if(tokens.Peek().type == TokenType.END)
				return null;

			List<string> ids = new List<string>();

			while(true) {
				ids.Add( Consume(TokenType.IDENTIFIER).val );
				if(tokens.Peek().val == "=")
					break;
				else
					Consume(",");
			}

			Consume("=");
			Variable val = ExecuteExpression();
			Variable copy;

			if(ids.Count == 1) {
				copy = val.Copy(ids[0]);
				if(locals.ContainsKey(ids[0]))
					locals[ids[0]] = copy;
				else
					locals.Add(ids[0], copy);
				Kerbulator.DebugLine(ids[0] +" = "+ val.ToString());
				return copy;
			} else {
				List<Variable> elements = new List<Variable>(ids.Count);
				if(val.type != VarType.LIST)
					throw new Exception("Expression needed to yield "+ ids.Count +" values, but yielded 1.");
				if(ids.Count != val.elements.Count)
					throw new Exception("Expression needed to yield "+ ids.Count +" values, but yielded "+ val.elements.Count);	

				for(int i=0; i<ids.Count; i++) {
					copy = val.elements[i].Copy(ids[i]);
					if(locals.ContainsKey(ids[i]))
						locals[ids[i]] = copy;
					else
						locals.Add(ids[i], copy);

					Kerbulator.Debug(ids[i] +" = "+ val.elements[i].ToString() +", ");
					elements.Add(copy);
				}

				Kerbulator.DebugLine("");
				return new Variable(VarType.LIST, elements);
			}
		}

		private bool PossiblyValidExpression(Stack<Variable>expr, Stack<Operator> ops) {
			if(expr.Count == 0 && ops.Count == 0)
				return false;


			int required = 0;
			int supplied = expr.Count;

			foreach(Operator op in ops) {
				Kerbulator.DebugLine(op.id);
				supplied ++;
				if(op.arity == Arity.BINARY)
					required += 2;
				else
					required += 1;
			}

			Kerbulator.DebugLine("required: "+ required +", supplied: "+ supplied);
			return supplied == required + 1;
		}

		private Variable ExecuteExpression() {
			Stack<Variable> expr = new Stack<Variable>();
			Stack<Operator> ops = new Stack<Operator>();

			while(tokens.Count > 0 && tokens.Peek().type != TokenType.END) {
				Token t = tokens.Peek();
				Kerbulator.DebugLine("Token: "+ Enum.GetName(typeof(TokenType), t.type) +": "+ t.val);

				if(t.type == TokenType.BRACE) {
					// Determine whether it's a left or right brace
					bool isLeft = false;

					switch(t.val) {
						case "(":
						case "{":
						case "⌊":
						case "⌈":
							isLeft = true;
							break;
						case "|":
							isLeft = !PossiblyValidExpression(expr, ops);

							if(isLeft)
								Kerbulator.DebugLine("| is left brace");
							else {
								Kerbulator.DebugLine("| is right brace");
							}
							break;
					} 

					// If it's a left brace, start a sub-expression
					if(isLeft) {
						Consume();

						// Execute sub-expression
						Kerbulator.DebugLine("Starting subexpression");
						Variable subexpr = ExecuteExpression();
						Kerbulator.DebugLine("Answer of subexpression: "+ subexpr.ToString());
						expr.Push(subexpr);

						// Consume right brace. Execute operation if any
						switch(t.val) {
							case "(":
								Consume(")");
								break;
							case "{":
								Consume("}");
								break;
							case "⌊":
								Consume("⌋");
								while(ops.Count > 0 && ops.Peek().precidence > operators[t.val].precidence)
									expr.Push( ExecuteOperator(ops.Pop(), expr, ops) );
								ops.Push(operators[t.val]);
								break;
							case "⌈":
								Consume("⌉");
								while(ops.Count > 0 && ops.Peek().precidence > operators[t.val].precidence)
									expr.Push( ExecuteOperator(ops.Pop(), expr, ops) );
								ops.Push(operators[t.val]);
								break;
							case "|":
								Consume("|");
								while(ops.Count > 0 && ops.Peek().precidence > operators[t.val].precidence)
									expr.Push( ExecuteOperator(ops.Pop(), expr, ops) );
								ops.Push(operators[t.val]);
								break;
						}
					} else {
						// It's a right brace, just end the sub-expression
						break;
					}
				}

				else if(t.type == TokenType.NUMBER) {
					expr.Push( new Variable(VarType.NUMBER, Double.Parse(t.val, System.Globalization.CultureInfo.InvariantCulture)) );
					Consume();
				}

				else if(t.type == TokenType.OPERATOR) {
					Operator op = operators[t.val];

					// Handle ambiguous cases of arity
					if(op.arity == Arity.BOTH) {	
						if(PossiblyValidExpression(expr, ops) ) {
							op = new Operator(op.id, op.precidence, Arity.BINARY);
							Kerbulator.DebugLine(op.id +" is binary.");
						} else {
							op = new Operator(op.id, 3, Arity.UNARY);
							Kerbulator.DebugLine(op.id +" is unary.");
						}
					} 

					while(ops.Count > 0 && ops.Peek().precidence > op.precidence)
						expr.Push( ExecuteOperator(ops.Pop(), expr, ops) );
					ops.Push(op);
					Consume();
				}

				else if(t.type == TokenType.IDENTIFIER) {
					Variable var;
				   
					if(locals.ContainsKey(t.val))
						var = locals[t.val];
					else if(functions.ContainsKey(t.val))
						var = new Variable(t.val, VarType.USER_FUNCTION, null);
					else if(globals.ContainsKey(t.val))
						var = globals[t.val];
					else
						throw new Exception("Undefined variable or function: "+ t.val);

					Consume();

					if(var.type == VarType.FUNCTION || var.type == VarType.USER_FUNCTION) {
						List<Variable> arguments = new List<Variable>();

						// Check for argument list
						if(tokens.Peek().val == "(") {
							Consume();
							Kerbulator.DebugLine("Arguments specified");
							while(tokens.Peek().val != ")") {
								Kerbulator.DebugLine("Starting subexpression");
								Variable subexpr = ExecuteExpression();
								Kerbulator.DebugLine("Answer of subexpression: "+ subexpr.ToString());
								arguments.Add(subexpr);
								
								if(tokens.Peek().val != ")")
									Consume(TokenType.COMMA);
							}
							Consume(")");

							// Execute function right now with the given argument list
							Variable result = ExecuteFunction(var, arguments);
							Kerbulator.DebugLine("Result of function: "+ result.ToString());
							expr.Push(result);
						} else {
							int numArgs = 0;
							if(var.type == VarType.FUNCTION)
								numArgs = globals[t.val].numArgs;
							else
								numArgs = functions[t.val].Ins.Count;

							if(numArgs == 0) {
								// Function doesn't take arguments, execute right now with empty argument list
								Variable result = ExecuteFunction(var, new List<Variable>());
								Kerbulator.DebugLine("Result of function: "+ result.ToString());
								expr.Push(result);
							} else {
								// Push the execution of the function onto the stack
								Kerbulator.DebugLine("No arguments specified");
								expr.Push(var);
								ops.Push(operators["func"]);
							}
						}
					} else {	
						expr.Push(var);
					}

				} else if(t.type == TokenType.LIST) {
					if(t.val == "]")
						break; // Right brace ends list

					List<Variable> elements = new List<Variable>();
					Consume();
					while(tokens.Peek().val != "]") {
						Kerbulator.DebugLine("Starting subexpression");
						Variable subexpr = ExecuteExpression();
						Kerbulator.DebugLine("Answer of subexpression: "+ subexpr.ToString());
						elements.Add(subexpr);
						
						if(tokens.Peek().val != "]")
							Consume(TokenType.COMMA);
					}
					Consume("]");
					expr.Push(new Variable("", VarType.LIST, elements));
				} else
					break;
			}
			
			// Handle remaining ops
			while(ops.Count > 0) {
				Operator op = ops.Pop();
				expr.Push( ExecuteOperator(op, expr, ops) );
			}

			if(expr.Count > 1)
				throw new Exception("Malformed expression");
		
			return expr.Pop();
		}

		private Variable ExecuteOperator(Operator op, Stack<Variable> expr, Stack<Operator> ops) {
			Kerbulator.DebugLine("Executing: "+ op.id);
			if(op.arity == Arity.BINARY && expr.Count < 2)
				throw new Exception("Malformed expression");
			else if(op.arity == Arity.UNARY && expr.Count < 1)
				throw new Exception("Malformed expression");
			else if(op.arity == Arity.BOTH)
				throw new Exception("Arity of "+ op.id +" still undefined.");

			Variable a,b;

			switch(op.id) {
				case "+":
					b = expr.Pop(); a = expr.Pop();
					return ApplyBinaryFunction(a, b, delegate(double c, double d) { return c + d; });

				case "-":
					b = expr.Pop();
					if(op.arity == Arity.UNARY)
						return new Variable(VarType.NUMBER, -b.val);
					else {
						a = expr.Pop();
						return new Variable(VarType.NUMBER, a.val - b.val);
					}

				case "*":
				case "·":
				case "×":
					b = expr.Pop(); a = expr.Pop();
					return new Variable(VarType.NUMBER, a.val * b.val);

				case "/":
				case "÷":
					b = expr.Pop(); a = expr.Pop();
					return new Variable(VarType.NUMBER, a.val / b.val);

				case "%":
					b = expr.Pop(); a = expr.Pop();
					return new Variable(VarType.NUMBER, a.val % b.val);

				case "^":
					b = expr.Pop(); a = expr.Pop();
					return new Variable(VarType.NUMBER, Math.Pow(a.val, b.val));

				case "√":
					b = expr.Pop();
					if(op.arity == Arity.UNARY)
						return new Variable(VarType.NUMBER, Math.Sqrt(b.val));
					else {
						a = expr.Pop();
						return new Variable(VarType.NUMBER, Math.Pow(a.val, 1/b.val));
					}

				case "⌊":
					return new Variable(VarType.NUMBER, Math.Floor(expr.Pop().val));

				case "⌈":
					return new Variable(VarType.NUMBER, Math.Ceiling(expr.Pop().val));

				case "|":
					return new Variable(VarType.NUMBER, Math.Abs(expr.Pop().val));

				case "func":
					b = expr.Pop();

					Variable func = expr.Pop();
					List<Variable> arguments = new List<Variable>();
					arguments.Add(b);

					return ExecuteFunction(func, arguments);

				default:
					throw new Exception("Unknown operator: "+ op.id);
			}
		}

		private Variable ExecuteFunction(Variable func, List<Variable> arguments) {
			Kerbulator.DebugLine("Executing function: "+ func.id);

			if(func.type == VarType.FUNCTION) {
				if(arguments.Count != func.numArgs)
					throw new Exception("Function "+ func.id +" takes "+ func.numArgs +" arguments, but "+ arguments.Count +" were supplied.");

				double result = 0;

				switch(func.id) {
					case "abs":
						return ApplyUnaryFunction(arguments[0], Math.Abs);

					case "acos":
						return ApplyUnaryFunction(arguments[0], Math.Acos);

					case "asin":
						return ApplyUnaryFunction(arguments[0], Math.Asin);

					case "atan":
						return ApplyUnaryFunction(arguments[0], Math.Atan);

					case "ceil":
						return ApplyUnaryFunction(arguments[0], Math.Ceiling);

					case "cos":
						return ApplyUnaryFunction(arguments[0], Math.Cos);

					case "exp":
						return ApplyUnaryFunction(arguments[0], Math.Exp);

					case "floor":
						return ApplyUnaryFunction(arguments[0], Math.Floor);

					case "ln":
					case "log":
						return ApplyUnaryFunction(arguments[0], Math.Log);

					case "log10":
						return ApplyUnaryFunction(arguments[0], Math.Log10);

					case "max":
						return new Variable(VarType.NUMBER, Math.Max(arguments[0].val, arguments[1].val));

					case "min":
						return new Variable(VarType.NUMBER, Math.Min(arguments[0].val, arguments[1].val));

					case "pow":
						return new Variable(VarType.NUMBER, Math.Pow(arguments[0].val, arguments[1].val));

					case "round":
						if(arguments[1].type != VarType.NUMBER)
							throw new Exception("Function round takes a number as second parameter.");

						return ApplyUnaryFunction(arguments[0], delegate(double val) { return Math.Round(val, (int)arguments[1].val); });

					case "sign":
						return ApplyUnaryFunction(arguments[0], delegate(double val) { return (double)Math.Sign(val); });

					case "sin":
						return ApplyUnaryFunction(arguments[0], Math.Sin);

					case "sqrt":
						return ApplyUnaryFunction(arguments[0], Math.Sqrt);

					case "tan":
						return ApplyUnaryFunction(arguments[0], Math.Tan);

					case "len":
						if(arguments[0].type != VarType.LIST)
							throw new Exception("Function len expects a list as input.");

						return new Variable(VarType.NUMBER, (double)arguments[0].elements.Count);

					case "norm":
						if(arguments[0].type != VarType.LIST)
							throw new Exception("Function norm expects a list as input.");

						double length = 0;
						foreach(Variable e in arguments[0].elements) {
							if(e.type != VarType.NUMBER)
								throw new Exception("Function norm cannot handle nested lists.");
							length += e.val * e.val;
						}

						length = Math.Sqrt(length);

						List<Variable> elements = new List<Variable>(arguments[0].elements.Count);
						foreach(Variable e in arguments[0].elements)
							elements.Add(new Variable(e.id, VarType.NUMBER, e.val/length));
						
						return new Variable(VarType.LIST, elements);

					case "dot":
						if(arguments[0].type != VarType.LIST || arguments[1].type != VarType.LIST)
							throw new Exception("Function dot expects two lists as input.");
						if(arguments[0].elements.Count != arguments[1].elements.Count)
							throw new Exception("Function dot requires two lists of the same size.");

						result = 0;
						for(int i=0; i<arguments[0].elements.Count; i++) {
							Variable a = arguments[0].elements[i];
							Variable b = arguments[1].elements[i];

							if(a.type != VarType.NUMBER || b.type != VarType.NUMBER)
								throw new Exception("Function dot cannot handle nested lists.");

							result += a.val * b.val;
						}
						
						return new Variable(VarType.NUMBER, result);

					case "cross":
						if(arguments[0].type != VarType.LIST || arguments[1].type != VarType.LIST)
							throw new Exception("Function cross expects two lists as input.");

						if(arguments[0].elements.Count != 3 || arguments[1].elements.Count != 3)
							throw new Exception("Function cross requires two lists of length 3.");

						List<Variable> x = arguments[0].elements;
						List<Variable> y = arguments[1].elements;

						for(int i=0; i<x.Count; i++) {
							if(x[i].type != VarType.NUMBER || y[i].type != VarType.NUMBER)
								throw new Exception("Function cross cannot handle nested lists.");
						}

						List<Variable>z = new List<Variable>(new[]{
							new Variable(VarType.NUMBER, x[1].val * y[2].val - x[2].val * y[1].val),
							new Variable(VarType.NUMBER, x[2].val * y[0].val - x[0].val * y[2].val),
							new Variable(VarType.NUMBER, x[0].val * y[1].val - x[1].val * y[0].val)
						});

						return new Variable(VarType.LIST, z);

					default:
						throw new Exception("Unknown function: "+ func.id);
				}
			} else {
				// User function
				Kerbulator.DebugLine("Executing "+ func.id);
				List<Variable> result = functions[func.id].Execute(arguments, operators, globals, functions);

				if(result.Count == 1)
					return result[0];
				else {
					return new Variable(VarType.LIST, result);
				}
			}
		}

		public delegate double UnaryFunction(double x); 
		public delegate double BinaryFunction(double x, double y); 

		private Variable ApplyUnaryFunction(Variable v, UnaryFunction action) {
			if(v.type == VarType.NUMBER)
				return new Variable(v.id, v.type, action(v.val));
			else if(v.type == VarType.LIST) {
				List<Variable> newElements = new List<Variable>(v.elements.Count);
				foreach(Variable e in v.elements)
					newElements.Add(ApplyUnaryFunction(e, action));
				return new Variable(v.id, v.type, newElements);
			}

			throw new Exception("Trying to perform an operation on invalid type.");
		}

		private Variable ApplyBinaryFunction(Variable a, Variable b, BinaryFunction action) {
			if(a.type == VarType.NUMBER && b.type == VarType.NUMBER)
				return new Variable("", VarType.NUMBER, action(a.val, b.val));

			else if(a.type == VarType.NUMBER && b.type == VarType.LIST) {
				List<Variable> newElements = new List<Variable>(b.elements.Count);
				foreach(Variable e in b.elements)
					newElements.Add(ApplyBinaryFunction(a, e, action));
				return new Variable("", VarType.LIST, newElements);
			}

			else if(a.type == VarType.LIST && b.type == VarType.NUMBER) {
				List<Variable> newElements = new List<Variable>(a.elements.Count);
				foreach(Variable e in a.elements)
					newElements.Add(ApplyBinaryFunction(e, b, action));
				return new Variable("", VarType.LIST, newElements);
			}

			else if(a.type == VarType.LIST && b.type == VarType.LIST) {
				if(a.elements.Count != b.elements.Count)
					throw new Exception("Trying to perform a binary operation on lists of unequal size.");

				List<Variable> newElements = new List<Variable>(a.elements.Count);
				for(int i=0; i<a.elements.Count; i++)
					newElements.Add(ApplyBinaryFunction(a.elements[i], b.elements[i], action));
				return new Variable("", VarType.LIST, newElements);
			}

			throw new Exception("Trying to perform an operation on invalid types.");
		}

	}
}
