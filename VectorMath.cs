using System;
using System.Collections.Generic;

namespace Kerbulator {
	public class VectorMath {
		private enum DotType {ERROR, UNKNOWN, VECTOR, MATRIX};

		static bool IsVector(Object a) {
			if(a.GetType() != typeof(Object[]))
				return false;

			foreach(Object x in (Object[]) a) {
				if(x.GetType() != typeof(double))
				   return false;	
			}

			return true;
		}

		static bool IsMatrix(Object a) {
			if(a.GetType() != typeof(Object[]))
				return false;

			int ncols = -1;
			foreach(Object x in (Object[]) a) {
				if(x.GetType() != typeof(Object[]))
				   return false;	

				if(ncols == -1)
					ncols = ((Object[])x).Length;
				if(((Object[])x).Length != ncols)
					return false;

				foreach(Object y in (Object[]) x) {
					if(y.GetType() != typeof(double))
					   return false;	
				}
			}

			return true;
		}

		static int NRows(Object a) {
			return ((Object[])a).Length;
		}

		static int NCols(Object a) {
			return ((Object[])((Object[])a)[0]).Length;
		}

		public static Object Len(Object a, string pos) {
			if(a.GetType() != typeof(Object[]))
				throw new Exception(pos +"function len() can only be called with a list as argument");

			return (Object) (double) ((Object[]) a).Length;
		}

		public static Object Dot(Object a, Object b, string pos) {
			DotType type = DotType.UNKNOWN;
			bool convertToVectorAfterwards = false;

			if(IsVector(a) && IsVector(b)) {
				type = DotType.VECTOR;
				if(NRows(a) != NRows(b))
					throw new Exception(pos +"dot(): vector dimension mismatch");
			}
			else if(IsMatrix(a) && IsMatrix(b)) {
				type = DotType.MATRIX;
				if(NRows(a) != NCols(b) && NRows(b) != NCols(a))
					throw new Exception(pos +"dot(): matrix dimension mismatch");
			}
			else if(IsVector(a) && IsMatrix(b)) {
				// Convert a to row vector
				a = new[] {a};
				type = DotType.MATRIX;
				if(NRows(a) != NCols(b) && NRows(b) != NCols(a))
					throw new Exception(pos +"dot(): matrix dimension mismatch");
				convertToVectorAfterwards = true;
			}
			else if(IsMatrix(a) && IsVector(b)) {
				// Convert b to column vector
				Object[] temp = (Object[]) b;
				List<Object[]> matrixB = new List<Object[]>(temp.Length);
				foreach(Object x in temp)
					matrixB.Add(new[] {x});
				b = (Object) matrixB.ToArray();

				type = DotType.MATRIX;
				if(NRows(a) != NCols(b) && NRows(b) != NCols(a))
					throw new Exception(pos +"dot(): matrix dimension mismatch");
				convertToVectorAfterwards = true;
			}
			else
				throw new Exception(pos +"dot() was called with in valid arguments");

			Object[] listA = (Object[]) a;
			Object[] listB = (Object[]) b;

			switch(type) {
				case DotType.VECTOR:
					double dotProduct = 0.0;
					for(int i=0; i<listA.Length; i++)
						dotProduct += ((double)listA[i]) * ((double)listB[i]);
					return (Object) dotProduct;

				case DotType.MATRIX:
					List<Object> matrix = new List<Object>(NRows(a));
					for(int i=0; i<NRows(a); i++) {
						List<Object> row = new List<Object>(NCols(b));
						for(int j=0; j<NCols(b); j++) {
							double res2 = 0.0;
							for(int k=0; k<NCols(a); k++)
								res2 += ((double)((Object[])listA[i])[k]) * ((double)((Object[])listB[k])[j]);
							row.Add((Object)res2);
						}
						matrix.Add((Object)row.ToArray());
					}


					if(convertToVectorAfterwards) {
						List<Object> vector = new List<Object>();

						foreach(Object row in matrix) {
							foreach(Object x in ((Object[])row))
								vector.Add(x);
						}

						return (Object) vector.ToArray();
					} else {
						return (Object) matrix.ToArray();
					}

				default:
					throw new Exception(pos +"dot(): dimension mismatch");
			}
		}

		public static Object Mag(Object a, string pos) {
			if(a.GetType() != typeof(Object[]))
				throw new Exception(pos +"function mag() can only be called with a list as argument");

			Object[] listA = (Object[]) a;

			double mag = 0.0;
			for(int i=0; i<listA.Length; i++) {
				if(listA[i].GetType() != typeof(double))
					throw new Exception(pos +"argument to function mag() must be a list that contains only numbers");
				mag += (double) listA[i] * (double) listA[i];
			}
			mag = Math.Sqrt(mag);

			return (Object) mag;
		}

		public static Object Norm(Object a, string pos) {
			if(a.GetType() != typeof(Object[]))
				throw new Exception(pos +"function norm() can only be called with a list as argument");

			double mag = (double)Mag(a, pos);

			Object[] listA = (Object[]) a;
			Object[] newA = new Object[listA.Length];

			for(int i=0; i<listA.Length; i++) {
				if(listA[i].GetType() != typeof(double))
					throw new Exception(pos +"argument to function norm() must be a list that contains only numbers");
				newA[i] = (double)listA[i] / mag;
			}

			return (Object) newA;
		}

		public static Object Cross(Object a, Object b, string pos) {
			if(a.GetType() != typeof(Object[]) || b.GetType() != typeof(Object[]))
				throw new Exception(pos +"arguments to function cross() must both be lists");

			Object[] x = (Object[]) a;
			Object[] y = (Object[]) b;

			if(x.Length != 3 || y.Length != 3)
				throw new Exception(pos +"function cross() requires two lists of length 3.");

			for(int i=0; i<x.Length; i++) {
				if(x[i].GetType() != typeof(double) || y[i].GetType() != typeof(double))
					throw new Exception(pos +"arguments to function cross() must be lists that contain only numbers");
			}

			Object[] res = new Object[3]{
				(double) x[1] * (double) y[2] - (double) x[2] * (double) y[1],
				(double) x[2] * (double) y[0] - (double) x[0] * (double) y[2],
				(double) x[0] * (double) y[1] - (double) x[1] * (double) y[0]
			};

			return (Object) res;
		}

		public static Object Any(Object a, string pos) {
			Kerbulator.DebugLine("Executing any()");
			if(a.GetType() != typeof(Object[]))
				throw new Exception(pos +"function any() can only be called with a list as argument");

			Object[] listA = (Object[]) a;

			double result = 0.0;
			for(int i=0; i<listA.Length; i++) {
				if(listA[i].GetType() != typeof(double))
					throw new Exception(pos +"argument to function any() must be a list that contains only numbers");
				if(((double) listA[i]) != 0)
					result = 1.0;
			}

			return (Object) result;
		}

		public static Object All(Object a, string pos) {
			Kerbulator.DebugLine("Executing all()");
			if(a.GetType() != typeof(Object[]))
				throw new Exception(pos +"function all() can only be called with a list as argument");

			Object[] listA = (Object[]) a;

			double result = 1.0;
			for(int i=0; i<listA.Length; i++) {
				if(listA[i].GetType() != typeof(double))
					throw new Exception(pos +"argument to function all() must be a list that contains only numbers");
				if(((double) listA[i]) == 0) {
					result = 0.0;
					break;
				}
			}

			return (Object) result;
		}
	}
}
