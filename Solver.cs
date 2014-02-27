using System;
using System.Collections.Generic;

namespace Kerbulator {
	// Nelder-Mead algorithm
	public class Solver {
		public static Object[] Solve(Func<Object> f, string[] vars, Dictionary<string, System.Object> locals) {
			Console.WriteLine("Solver baby!");
			int maxiter = 200 * vars.Length;
			int maxfev = 200 * vars.Length;

			Console.WriteLine("all locals:");
			foreach(string v in locals.Keys)
				Console.WriteLine(v);
			
			// Copy the locals of interest to the output
			Console.WriteLine("all vars of interest:");
			List<Object> outs = new List<Object>(vars.Length);
			foreach(string id in vars) {
				Console.WriteLine(id);
				outs.Add(locals[id]);
			}

			return outs.ToArray();
		}
	}
}
