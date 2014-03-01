using System;
using System.Linq;
using System.Collections.Generic;

namespace Kerbulator {
	public class Solver {
		JITFunction func;
		int maxiter, maxfev;
		double xtol, ftol;

		public Solver(JITFunction func) {
			this.func = func;
			maxiter = 0;
			maxfev = 0;
			xtol = 1E-8;
			ftol = 1E-8;
		}

		public Solver(JITFunction func, int maxiter, int maxfev, double xtol, double ftol) {
			this.func = func;
			this.maxiter = maxiter;
			this.maxfev = maxfev;
			this.xtol = xtol;
			this.ftol = ftol;
		}

		// Nelder-Mead algorithm
		public Object Solve(Func<Object> f, string[] vars, string pos) {
			Kerbulator.DebugLine("Entering solver");
			int N = vars.Length;
			if(N == 0)
				return (Object) new Object[] {};

			// Stopping criteria
			int maxiter = this.maxiter == 0 ? 200 * N : this.maxiter;
			int maxfev = this.maxfev == 0 ? 200 * N : this.maxfev;
			int iterations = 1;
			int fcalls = 1;

			// Some parameters
			double rho = 1.0;
			double chi = 2.0;
			double psi = 0.5;
			double sigma = 0.5;

			// Variables that keep track of the current simplex
			double[][] sim = new double[N+1][];
			double[] fsim = new double[N+1];

			// Initialize best vertex of current simplex
			sim[0] = new double[N];
			for(int i=0; i<N; i++) {
				if(func.IsLocalDefined(vars[i])) {
					Object o = func.GetLocal(vars[i], pos);
					if(o.GetType() != typeof(double))
						throw new Exception(pos +"solver cannot optimize variables of type list");
					sim[0][i] = (double) o;
				} else {
					sim[0][i] = 0.0;
				}
			}
			fsim[0] = CallFunc(f, vars, sim[0], pos);
			fcalls ++;

			// Initialize other vertices of current simplex
			double nonzdelt = 1.05;
			double zdelt = 0.00025;

			for(int k=0; k<N; k++) {
				double[] y = (double[]) sim[0].Clone();
				if(y[k] == 0.0)
					y[k] = zdelt;
				else
					y[k] = sim[0][k] * nonzdelt;

				sim[k+1] = y;
				fsim[k+1] = CallFunc(f, vars, y, pos);
				fcalls ++;
			}

			// Sort vertices
			SortVertices(sim, fsim);

			double[] xbar = new double[N];
			double[] xr = new double[N];
			double[] xe = new double[N];
			double[] xc = new double[N];
			double[] xcc = new double[N];

			while(fcalls < maxfev && iterations < maxiter) {
				Kerbulator.DebugLine("Iteration "+ iterations);
				PrintVertices(sim, fsim);

				// Test stopping criterium
				double xmax = 0;
				double fmax = 0;
				for(int i=1; i<N+1; i++) {
					for(int j=1; j<N; j++) {
						double xdiff = Math.Abs(sim[i][j] - sim[0][j]);
						if(xdiff > xmax)
							xmax = xdiff;
					}
					double fdiff = Math.Abs(fsim[i] - fsim[0]);
					if(fdiff > fmax)
						fmax = fdiff;
				}

				if(xmax <= xtol && fmax < ftol) {
					Kerbulator.DebugLine("Stopping criterium reached. ("+ xmax +", "+ fmax +")");
					break;
				} else {
					Kerbulator.DebugLine("Continue. ("+ xmax +", "+ fmax +")");
				}

				for(int i=0; i<N; i++) {
					xbar[i] = 0.0;
					for(int j=0; j<N; j++)
						xbar[i] += sim[j][i];
					xbar[i] /= N;
				}

				for(int i=0; i<N; i++)
					xr[i] = (1 + rho) * xbar[i] - rho * sim[sim.Length-1][i];

				double fxr = CallFunc(f, vars, xr, pos);
				fcalls ++;

				bool doshrink = false;

				if(fxr < fsim[0]) {
					for(int i=0; i<N; i++)
						xe[i] = (1 + rho * chi) * xbar[i] - rho * chi * sim[sim.Length-1][i];

					double fxe = CallFunc(f, vars, xe, pos);
					fcalls ++;

					if(fxe < fxr) {
						for(int i=0; i<N; i++)
							sim[sim.Length-1][i] = xe[i];

						fsim[fsim.Length-1] = fxe;
					} else {
						for(int i=0; i<N; i++)
							sim[sim.Length-1][i] = xr[i];
						fsim[fsim.Length-1] = fxr;
					}
				} else { // fsim[0] <= fxr
					if(fxr < fsim[fsim.Length-2]) {
						for(int i=0; i<N; i++)
							sim[sim.Length-1][i] = xr[i];
						fsim[fsim.Length-1] = fxr;
					} else { // fxr >= fsim[-2]
						// Perform contraction
						if(fxr < fsim[fsim.Length-1]) {
							for(int i=0; i<N; i++)
								xc[i] = (1 + psi * rho) * xbar[i] - psi * rho * sim[sim.Length-1][i];

							double fxc = CallFunc(f, vars, xc, pos);
							fcalls ++;

							if(fxc <= fxr) {
								for(int i=0; i<N; i++)
									sim[sim.Length-1][i] = xc[i];
								fsim[fsim.Length-1] = fxc;
							} else {
								doshrink = true;
							}
						} else {
							// Perform an inside contraction
							for(int i=0; i<N; i++)
								xcc[i] = (1 - psi) * xbar[i] + psi * sim[sim.Length-1][i];

							double fxcc = CallFunc(f, vars, xcc, pos);
							fcalls ++;

							if(fxcc < fsim[fsim.Length-1]) {
								for(int i=0; i<N; i++)
									sim[sim.Length-1][i] = xcc[i];
								fsim[fsim.Length-1] = fxcc;
							} else {
								doshrink = true;
							}
						}

						if(doshrink) {
							for(int j=1; j<N+1; j++) {
								for(int i=0; i<N; i++)
									sim[j][i] = sim[0][i] + sigma * (sim[j][i] - sim[0][i]);
								fsim[j] = CallFunc(f, vars, sim[j], pos);
								fcalls ++;
							}
						}
					}
				}

				SortVertices(sim, fsim);

				iterations ++;
			}
			
			Kerbulator.DebugLine("Leaving solver");
			// Copy the locals of interest to the output
			if(vars.Length == 1)
				return func.GetLocal(vars[0], pos);
			else {
				List<Object> outs = new List<Object>(vars.Length);
				foreach(string id in vars)
					outs.Add(func.GetLocal(id, pos));

				return (Object) outs.ToArray();
			}
		}

		private void SortVertices(double[][] sim, double[] fsim) {
			int[] ind = Enumerable.Range(0, sim.Length).ToArray();
			Array.Sort(fsim, ind);
			Kerbulator.DebugLine("Sorted idx:" + PrintVert(ind));

			double[][] sim2 = new double[sim.Length][];
			for(int i=0; i<sim.Length; i++)
				sim2[i] = sim[ind[i]];

			for(int i=0; i<sim.Length; i++)
				sim[i] = sim2[i];
		}

		private void PrintVertices(double[][] sim, double[] fsim) {
			Kerbulator.DebugLine("Verts:");
			for(int i=0; i<sim.Length; i++) {
				Kerbulator.Debug("\t"+ i +": "+ PrintVert(sim[i]) +" = "+ fsim[i] +"\n");
			}
		}

		private string PrintVert(double[] sim) {
			string r = "";
			r = "[";
			for(int i=0; i<sim.Length-1; i++)
				r += sim[i] +", ";
			r += sim[sim.Length-1] +"]";
			return r;
		}

		private string PrintVert(int[] sim) {
			string r = "";
			r = "[";
			for(int i=0; i<sim.Length-1; i++)
				r += sim[i] +", ";
			r += sim[sim.Length-1] +"]";
			return r;
		}

		private double CallFunc(Func<Object>f, string[] ids, double[] vals, string pos) {
			for(int i=0; i<ids.Length; i++)
				func.SetLocal(ids[i], vals[i]);

			Object r = f();

			if(r.GetType() == typeof(Object[]))
				// For lists, use the magnitude as value to optimize
				try {
					return (double) VectorMath.Mag(r, pos);
				} catch(Exception) {
					throw new Exception(pos +"solver cannot optimize a function that returns a list of lists");
				}
			else {
				double res = Math.Abs((double) r);
				return res;
			}
		}
	}
}
