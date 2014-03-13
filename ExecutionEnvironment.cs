using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.IO;
using System;
using UnityEngine;

namespace Kerbulator {
	public class ExecutionEnvironment {
		public JITFunction func;
		Kerbulator kalc;
		List<JITExpression> inputExpressions;

		Exception error;
		bool inError = false;

		List<System.Object> output = null;
		public Rect windowPos = new Rect(0, 0, 200, 100);
		public Vector2 scrollPos = new Vector2(0, 0);
		public bool enabled = true;

		public ExecutionEnvironment(JITFunction func, Kerbulator kalc) {
			this.func = func;
			this.kalc = kalc;
			inputExpressions = new List<JITExpression>(func.Ins.Count);
		}

		public bool InError {
			get { return inError; }
			set { inError = value; if(!value) error = null; }
		}

		public string ErrorString {
			get { return error.Message; }
			protected set { }
		}

		public List<System.Object> Output {
			get { return output; }
			protected set { }
		}

		public List<System.Object> Execute() {
			if(inError) {
				output = null;
				return null;
			}
				
			if(func.InError)
				throw new Exception("Tried to execute a function that is in error state: "+ func.ErrorString);

			try {
				// Evaluate input expressions to yield the input arguments
				List<System.Object> inputArguments = new List<System.Object>(func.Ins.Count);
				foreach(JITExpression e in inputExpressions)
					inputArguments.Add( e.Execute() );

				// Call function using input arguments
				output = func.Execute(inputArguments);
				return output;

			} catch(Exception e) {
				inError = true;
				error = e;
				output = null;
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
