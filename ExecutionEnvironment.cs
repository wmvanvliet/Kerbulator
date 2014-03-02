using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;

namespace Kerbulator {
	public class ExecutionEnvironment {
		JITFunction func;
		Kerbulator kalc;
		List<JITExpression> inputExpressions;

		Exception error;
		bool inError = false;

		public ExecutionEnvironment(JITFunction func, Kerbulator kalc) {
			this.func = func;
			this.kalc = kalc;
			inputExpressions = new List<JITExpression>(func.Ins.Count);
		}

		public JITFunction Function {
			get { return func; }
			protected set { }
		}

		public bool InError {
			get { return inError; }
			set { inError = value; if(!value) error = null; }
		}

		public string ErrorString {
			get { return error.Message; }
			protected set { }
		}

		public List<Object> Execute() {
			if(inError)
				return null;
				
			if(func.InError)
				throw new Exception("Tried to execute a function that is in error state: "+ func.ErrorString);

			try {
				// Evaluate input expressions to yield the input arguments
				List<Object> inputArguments = new List<Object>(func.Ins.Count);
				foreach(JITExpression e in inputExpressions)
					inputArguments.Add( e.Execute() );

				// Call function using input arguments
				List<Object> r = func.Execute(inputArguments);
				return r;

			} catch(Exception e) {
				inError = true;
				error = e;
				return null;
			}
		}

		public void SetArguments(List<string> args) {
			if(args.Count != func.Ins.Count) {
				inError = true;
				error = new Exception("Function must be called with "+ func.Ins.Count +" arguments");
				return;
			}

			inputExpressions = new List<JITExpression>(args.Count);
			for(int i=0; i<args.Count; i++) {
				try {
					inputExpressions.Add(new JITExpression(args[i], kalc));
				} catch(Exception e) {
					inError = true;
					error = new Exception("Argument "+ func.Ins[i] +": "+ e.Message);
					return;
				}
			}
		}
	}
}
