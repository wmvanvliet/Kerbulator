using System;

namespace Kerbulator {
	public class VectorMath {
		public static Object Len(Object a, string pos) {
			if(a.GetType() != typeof(Object[]))
				throw new Exception(pos +"function len() can only be called with a list as argument");

			return (Object) ((Object[]) a).Length;
		}

		public static Object Dot(Object a, Object b, string pos) {
			if(a.GetType() != typeof(Object[]) || b.GetType() != typeof(Object[]))
				throw new Exception(pos +"arguments to function dot() must both be lists");

			Object[] listA = (Object[]) a;
			Object[] listB = (Object[]) b;

			if(listA.Length != listB.Length)
				throw new Exception(pos +"arguments to function dot() must be lists of equal length");

			double res = 0.0;

			for(int i=0; i<listA.Length; i++) {
				if(listA[i].GetType() != typeof(double) || listB[i].GetType() != typeof(double))
					throw new Exception(pos +"arguments to function dot() must be lists that contain only numbers");

				res += (double)listA[i] * (double)listB[i];
			}
			return (Object) res;
		}

		public static Object Mag(Object a, string pos) {
			if(a.GetType() != typeof(Object[]))
				throw new Exception(pos +"function mag() can only be called with a list as argument");

			Object[] listA = (Object[]) a;

			double mag = 0.0;
			for(int i=0; i<listA.Length; i++) {
				if(listA[i].GetType() != typeof(double))
					throw new Exception(pos +"argument to function mag() must be a lists that contains only numbers");
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

			for(int i=0; i<listA.Length; i++)
				newA[i] = (double)listA[i] / mag;

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
	}
}
