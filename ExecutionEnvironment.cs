using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;

namespace Kerbulator {
	public class ExecutionEnvironment {
		JITFunction func;
		Kerbulator kalc;
		List<JITExpression> inputExpressions;

		public ExecutionEnvironment(JITFunction func, Kerbulator kalc) {
			this.func = func;
			this.kalc = kalc;
			inputExpressions = new List<JITExpression>(func.Ins.Count);
		}

		public List<Object> Execute() {
			if(func.InError)
				throw new Exception(func.ErrorString);

			// Evaluate input expressions to yield the input arguments
			List<Object> inputArguments = new List<Object>(func.Ins.Count);
			foreach(JITExpression e in inputExpressions)
				inputArguments.Add( e.Execute() );

			// Call function using input arguments
			List<Object> r = func.Execute(inputArguments);

			if(func.InError)
				throw new Exception(func.ErrorString);

			return r;
		}

		public void SetArguments(List<string> args) {
			inputExpressions = new List<JITExpression>(args.Count);
			foreach(string arg in args)
				inputExpressions.Add(new JITExpression(arg, kalc));
		}
	}
}
