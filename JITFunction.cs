using System;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Kerbulator {
	public class JITFunction {
		private Dictionary<string, Operator> operators;
		private Queue<Token> tokens;

		public JITFunction(string expression, Dictionary<string, Operator> operators) {
			this.operators = operators;

			Tokenizer tok = new Tokenizer();
			tok.Tokenize(expression);
			tokens = tok.tokens;
		}

		public Expression ParseStatement() {
			if(tokens.Peek().type == TokenType.END)
				return null;

			List<string> ids = new List<string>();

			while(true) {
				Token t = Consume(TokenType.IDENTIFIER);
				ids.Add(t.val);
				if(tokens.Peek().val == "=")
					break;
				else
					Consume(",");
			}

			Consume("=");
			Expression expr = ParseExpression();

			if(ids.Count == 1) {
				/* Doesn't work??
				return Expression.Assign(
					Expression.Variable(typeof(double), ids[0]),
					expr
				);
				*/
				return expr;
			} else {
				throw new Exception("Not implemented.");
			}
		}


		public Expression ParseExpression() {
			Stack<Expression> expr = new Stack<Expression>();
			Stack<Operator> ops = new Stack<Operator>();

			bool end = false; // If ever set to true, this is the end of the expression

			while(!end && tokens.Count > 0 && tokens.Peek().type != TokenType.END) {
				Token t = tokens.Peek();
				Kerbulator.DebugLine("Token: "+ Enum.GetName(typeof(TokenType), t.type) +": "+ t.val);

				switch(t.type) {
					case TokenType.NUMBER:
						ParseNumber(expr);
						break;
					case TokenType.OPERATOR:
						ParseOperator(expr, ops);
						break;
					case TokenType.BRACE:
						end = ParseBrace(expr, ops);
						break;
					default:
						Consume();
						break;
				}

			}

			// Handle remaining ops
			while(ops.Count > 0) {
				Operator op = ops.Pop();
				expr.Push( ExecuteOperator(op, expr, ops) );
			}

			/*
			if(expr.Count > 1)
				throw new Exception("Malformed expression");
			*/

			return expr.Pop();
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

		private bool PossiblyValidExpression(Stack<Expression>expr, Stack<Operator> ops) {
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

		private void ParseNumber(Stack<Expression> expr) {
			Token t = tokens.Dequeue();
			Kerbulator.DebugLine("Pushing "+ t.val);
			/*
			expr.Push(
				Expression.Convert(
					Expression.Constant(Double.Parse(t.val, System.Globalization.CultureInfo.InvariantCulture)),
					typeof(Object)
				)
			);
			*/
			expr.Push(
				Expression.Constant(Double.Parse(t.val, System.Globalization.CultureInfo.InvariantCulture))
			);
		}

		private void ParseOperator(Stack<Expression>expr, Stack<Operator>ops) {
			Token t = tokens.Dequeue();
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

			// Handle operators with higher precidence
			while(ops.Count > 0) {
				Operator prevOp = ops.Peek();

				if(op.arity != Arity.BINARY || prevOp.precidence < op.precidence)
					// Leave for later
					break;
				else
					expr.Push( ExecuteOperator(ops.Pop(), expr, ops) );
			}

			// Push current operator on the stack
			Kerbulator.DebugLine("Pushing "+ op.id);
			ops.Push(op);
		}

		private Expression ExecuteOperator(Operator op, Stack<Expression> expr, Stack<Operator> ops) {
			Kerbulator.DebugLine("Executing: "+ op.id);
			if(op.arity == Arity.BINARY && expr.Count < 2)
				throw new Exception("Operator "+ op.id +" expects both a left and a right hand side to operate on.");
			else if(op.arity == Arity.UNARY && expr.Count < 1)
				throw new Exception("Operator "+ op.id +" expects a right hand side to operate on.");
			else if(op.arity == Arity.BOTH)
				throw new Exception("Arity of "+ op.id +" still undefined.");

			Expression a,b;
			switch(op.id) {
				case "+":
					b = expr.Pop(); a = expr.Pop();
					return Expression.Add(a, b);
				case "-":
					b = expr.Pop();
					if(op.arity == Arity.UNARY) 
						return Expression.Negate(b);
					else {
						a = expr.Pop();
						return Expression.Subtract(a, b);
					}
				case "*":
				case "·":
					b = expr.Pop(); a = expr.Pop();
					return Expression.Multiply(a, b);
				case "/":
				case "÷":
					b = expr.Pop(); a = expr.Pop();
					return Expression.Divide(a, b);
				case "%":
					b = expr.Pop(); a = expr.Pop();
					return Expression.Modulo(a, b);
				case "^":
					b = expr.Pop(); a = expr.Pop();
					return Expression.Power(a, b);
				case "√":
					b = expr.Pop();
					if(op.arity == Arity.UNARY) {
						return Expression.Call(
							typeof(Math).GetMethod("Sqrt"),
						   	b
						);
					} else {
						a = expr.Pop();
						return Expression.Power(
							a,
							Expression.Divide(Expression.Constant(1.0), b)
						);
					}
				case "⌊":
					b = expr.Pop();
					return Expression.Call(
						typeof(Math).GetMethod("Floor", new[] {typeof(double)}),
						b
					);
				case "⌈":
					b = expr.Pop();
					return Expression.Call(
						typeof(Math).GetMethod("Ceiling", new[] {typeof(double)}),
						b
					);
				case "|":
					b = expr.Pop();
					return Expression.Call(
						typeof(Math).GetMethod("Abs", new[] {typeof(double)}),
						b
					);
				default:
					throw new Exception("Unknown operator: "+ op.id);
			}
		}

		private bool ParseBrace(Stack<Expression> expr, Stack<Operator> ops) {
			Token t = tokens.Peek();

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
				Expression subexpr = ParseExpression();
				Kerbulator.DebugLine("End of subexpression");
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
						ops.Push(operators[t.val]);
						break;
					case "⌈":
						Consume("⌉");
						ops.Push(operators[t.val]);
						break;
					case "|":
						Consume("|");
						ops.Push(operators[t.val]);
						break;
				}
				return false;
			} else { 
				return true;
			}
		}
	}
}
