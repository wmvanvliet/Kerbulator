using System;
using System.IO;
using System.Collections.Generic;

namespace Kalculator {

	public class VarException: Exception {
		public VarException() {}
		public VarException(string msg): base(msg) {}
		public VarException(string msg, Exception inner): base(msg, inner) {}
	}

	public enum VarType {NUMBER, LIST, FUNCTION, USER_FUNCTION};
	public class Variable {
		public string id;
		public VarType type;
		public double val;
		public int numArgs;
		public List<Variable> elements;

		public Variable(string id): this(id, VarType.NUMBER, 0) {}
		public Variable(VarType type, object val): this(null, type, val) {}
		public Variable(string id, VarType type, object val) {
			this.id = id;
			this.type = type;

			switch(type) {
				case VarType.NUMBER:
					if(val.GetType() != typeof(double))
						throw new VarException("Value must be of type double, but is "+ val.GetType());
					this.val = (double)val;
					break;
				case VarType.LIST:
					if(val.GetType() != typeof(List<Variable>))
						throw new VarException("Value must be a list of variables");
					this.elements = (List<Variable>)val;
					if(this.elements.Count == 0)
						throw new VarException("Empty lists are not allowed.");
					else
						this.val = this.elements[0].val;
					break;
				case VarType.FUNCTION:
					if(val.GetType() != typeof(int))
						throw new VarException("Number of arguments must be specified as an int");
					this.numArgs = (int)val;
					break;
				case VarType.USER_FUNCTION:
					break;
			}
		}

		public Variable Copy() {
			return Copy(id);
		}

		public Variable Copy(string newid) {
			switch(type) {
				case VarType.NUMBER:
					return new Variable(newid, type, val);

				case VarType.LIST:
					List<Variable> newElements = new List<Variable>(elements.Count);
					for(int i=0; i<elements.Count; i++)
						newElements.Add(elements[i].Copy());
					return new Variable(newid, type, newElements);
			
				case VarType.FUNCTION:
					return new Variable(newid, type, numArgs);

				case VarType.USER_FUNCTION:
					return new Variable(newid, type, null);
			}

			throw new VarException("Unknown var type.");
		}

		override public string ToString() {
			switch(type) {
				case VarType.NUMBER:
					return val.ToString();
				case VarType.LIST:
					string str = "[";
					for(int i=0; i<elements.Count-1; i++)
						str += elements[i].ToString() +", ";
					str += elements[elements.Count-1].ToString() + "]";
					return str;
				case VarType.FUNCTION:
					return "<function "+ id +">";
				case VarType.USER_FUNCTION:
					return "<user function "+ id +">";
			}

			return "unknown";
		}
	}

	public enum Arity {UNARY, BINARY, BOTH};
	public class Operator {
		public string id;
		public int precidence;
		public Arity arity;

		public Operator(string id, int precidence, Arity arity) {
			this.id = id;
			this.precidence = precidence;
			this.arity = arity;
		}
	}
}
