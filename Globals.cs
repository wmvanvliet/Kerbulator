using KSP.IO; // UNITY
using UnityEngine; // UNITY
using System;
using System.Collections.Generic;

namespace Kalculator {
	public static class Globals {
		public static void Add(Kalculator kalc) {
			// UNITY
			// Planets
			foreach(CelestialBody b in FlightGlobals.Bodies) {
				if(b.name == "Sun")
					Globals.AddCelestialBody(kalc, b, "Kerbol");
				else
					Globals.AddCelestialBody(kalc, b, b.name);
			}
				
			Vessel v = FlightGlobals.ActiveVessel;
			Orbit orbit1 = v.orbit;
			if(v != null) {
				// Current orbit
				Globals.AddOrbit(kalc, orbit1, "Craft");

				// Current time
				double UT = (double)Planetarium.GetUniversalTime();
				Globals.AddDouble(kalc, "UT", UT);

				// Reference body
				Globals.AddCelestialBody(kalc, v.orbit.referenceBody, "Parent");

				// Target
				if(FlightGlobals.fetch.VesselTarget != null) {
					ITargetable target = FlightGlobals.fetch.VesselTarget;
					Orbit orbit2 = target.GetOrbit();

					// Target Orbit
					Globals.AddOrbit(kalc, orbit2, "Target");

					// Intersection with target orbit
					double CD = 0.0;
					double CCD = 0.0;
					double FFp = 0.0;
					double FFs = 0.0;
					double SFp = 0.0;
					double SFs = 0.0;
					int iterationCount = 0;
					Orbit.FindClosestPoints(orbit1, orbit2, ref CD, ref CCD, ref FFp, ref FFs, ref SFp, ref SFs, 0.0, 100, ref iterationCount);
					double T1 = orbit1.GetDTforTrueAnomaly(FFp, 0.0);
					double T2 = orbit1.GetDTforTrueAnomaly(SFp, 0.0);
					Globals.AddDouble(kalc, "Craft.Inter1.dt", T1);
					Globals.AddDouble(kalc, "Craft.Inter1.Δt", T1);
					Globals.AddDouble(kalc, "Craft.Inter1.sep", (orbit1.getPositionAtUT(T1+UT) - orbit2.getPositionAtUT(T1+UT)).magnitude);
					Globals.AddDouble(kalc, "Craft.Inter1.TrueAnomaly", orbit1.TrueAnomalyAtUT(T1+UT));
					Globals.AddDouble(kalc, "Craft.Inter1.θ", orbit1.TrueAnomalyAtUT(T1+UT));
					Globals.AddDouble(kalc, "Craft.Inter2.dt", T2);
					Globals.AddDouble(kalc, "Craft.Inter2.Δt", T2);
					Globals.AddDouble(kalc, "Craft.Inter2.sep", (orbit1.getPositionAtUT(T2+UT) - orbit2.getPositionAtUT(T2+UT)).magnitude);
					Globals.AddDouble(kalc, "Craft.Inter2.TrueAnomaly", orbit2.TrueAnomalyAtUT(T2+UT));
					Globals.AddDouble(kalc, "Craft.Inter2.θ", orbit2.TrueAnomalyAtUT(T2+UT));
				}
			}
			// /UNITY
		}

		// UNITY
		public static void AddOrbit(Kalculator kalc, Orbit orbit, string prefix) {
			if(orbit == null)
				return;

			kalc.AddGlobal(new Variable(prefix +".Ap", VarType.NUMBER, (double)orbit.ApA));
			kalc.AddGlobal(new Variable(prefix +".Pe", VarType.NUMBER, (double)orbit.PeA));
			//kalc.AddGlobal(new Variable(prefix +".Inc", VarType.NUMBER, (double)orbit.inclination));
			kalc.AddGlobal(new Variable(prefix +".Alt", VarType.NUMBER, (double)orbit.altitude));
			kalc.AddGlobal(new Variable(prefix +".ArgPe", VarType.NUMBER, (double)orbit.argumentOfPeriapsis));
			kalc.AddGlobal(new Variable(prefix +".ω", VarType.NUMBER, (double)orbit.argumentOfPeriapsis));
			kalc.AddGlobal(new Variable(prefix +".TimeToAp", VarType.NUMBER, (double)orbit.timeToAp));
			kalc.AddGlobal(new Variable(prefix +".TimeToPe", VarType.NUMBER, (double)orbit.timeToPe));
			kalc.AddGlobal(new Variable(prefix +".Vel", VarType.NUMBER, (double)orbit.vel.magnitude));
			kalc.AddGlobal(new Variable(prefix +".TrueAnomaly", VarType.NUMBER, (double)orbit.trueAnomaly));
			kalc.AddGlobal(new Variable(prefix +".θ", VarType.NUMBER, (double)orbit.trueAnomaly));

			if(orbit.UTsoi > 0) {
				kalc.AddGlobal(new Variable(prefix +".SOI.dt", VarType.NUMBER, (double)orbit.UTsoi-Planetarium.GetUniversalTime()));
				kalc.AddGlobal(new Variable(prefix +".SOI.TrueAnomaly", VarType.NUMBER, (double)orbit.TrueAnomalyAtUT(orbit.UTsoi)));
				kalc.AddGlobal(new Variable(prefix +".SOI.θ", VarType.NUMBER, (double)orbit.TrueAnomalyAtUT(orbit.UTsoi)));
			}
		}

		public static void AddCelestialBody(Kalculator kalc, CelestialBody body) {
			AddCelestialBody(kalc, body, body.name);
		}

		public static void AddCelestialBody(Kalculator kalc, CelestialBody body, string prefix) {
			if(body == null)
				return;

			try {
				if(body.orbit != null) {
					AddOrbit(kalc, body.orbit, prefix);
				}
			} catch(Exception) {
				// Somehow, testing body.orbit != null is not enough. I don't know why...
				// Leave a pull request or file an issue if you can help me figure this out!
			}

			kalc.AddGlobal(new Variable(prefix +".R", VarType.NUMBER, (double)body.Radius));
			kalc.AddGlobal(new Variable(prefix +".M", VarType.NUMBER, (double)body.Mass));
			kalc.AddGlobal(new Variable(prefix +".mu", VarType.NUMBER, (double)body.gravParameter));
			kalc.AddGlobal(new Variable(prefix +".µ", VarType.NUMBER, (double)body.gravParameter));
			kalc.AddGlobal(new Variable(prefix +".day", VarType.NUMBER, (double)body.rotationPeriod));
			kalc.AddGlobal(new Variable(prefix +".SOI", VarType.NUMBER, (double)body.sphereOfInfluence));
			kalc.AddGlobal(new Variable(prefix +".AtmosHeight", VarType.NUMBER, (double)body.maxAtmosphereAltitude));
			// TODO: atmospheric pressure at sea level... how to take it from some internal variable?
			AddAtmosPress(kalc, prefix);
		}

		public static void AddDouble(Kalculator kalc, string id, double v) {
			Variable g = new Variable(id, VarType.NUMBER, v);
			kalc.AddGlobal(g);
		}

		public static void AddBool(Kalculator kalc, string id, bool v) {
			double val = v ? 1.0 : 0.0;
			Variable g = new Variable(id, VarType.NUMBER, val);
			kalc.AddGlobal(g);
		}

		public static void AddVector3d(Kalculator kalc, string id, Vector3d v) {
			Variable x = new Variable("x", VarType.NUMBER, v.x);
			Variable y = new Variable("y", VarType.NUMBER, v.y);
			Variable z = new Variable("z", VarType.NUMBER, v.z);

			List<Variable> elements = new List<Variable>(3);
			elements.Add(x);
			elements.Add(y);
			elements.Add(z);

			Variable g = new Variable(id, VarType.LIST, elements);
			kalc.AddGlobal(g);
		}

		public static void AddVector3(Kalculator kalc, string id, Vector3 v) {
			Variable x = new Variable("x", VarType.NUMBER, (double)v.x);
			Variable y = new Variable("y", VarType.NUMBER, (double)v.y);
			Variable z = new Variable("z", VarType.NUMBER, (double)v.z);

			List<Variable> elements = new List<Variable>(3);
			elements.Add(x);
			elements.Add(y);
			elements.Add(z);

			Variable g = new Variable(id, VarType.LIST, elements);
			kalc.AddGlobal(g);
		}

		public static void AddAtmosPress(Kalculator kalc, string name) {
			// Taken from http://wiki.kerbalspaceprogram.com
			switch(name) {
				case "Kerbal":
					AddDouble(kalc, name +".AtmosPress", 0.0);
					break;
				case "Moho":
					AddDouble(kalc, name +".AtmosPress", 0.0);
					break;
				case "Eve":
					AddDouble(kalc, name +".AtmosPress", 506625.0);
					break;
				case "Gilly":
					AddDouble(kalc, name +".AtmosPress", 0.0);
					break;
				case "Kerbin":
					AddDouble(kalc, name +".AtmosPress", 101325.0);
					break;
				case "Mun":
					AddDouble(kalc, name +".AtmosPress", 0.0);
					break;
				case "Minmus":
					AddDouble(kalc, name +".AtmosPress", 0.0);
					break;
				case "Duna":
					AddDouble(kalc, name +".AtmosPress", 20265.0);
					break;
				case "Ike":
					AddDouble(kalc, name +".AtmosPress", 0.0);
					break;
				case "Dres":
					AddDouble(kalc, name +".AtmosPress", 0.0);
					break;
				case "Jool":
					AddDouble(kalc, name +".AtmosPress", 1519880.0);
					break;
				case "Laythe":
					AddDouble(kalc, name +".AtmosPress", 81060.0);
					break;
				case "Vall":
					AddDouble(kalc, name +".AtmosPress", 0.0);
					break;
				case "Tylo":
					AddDouble(kalc, name +".AtmosPress", 0.0);
					break;
				case "Bob":
					AddDouble(kalc, name +".AtmosPress", 0.0);
					break;
				case "Pol":
					AddDouble(kalc, name +".AtmosPress", 0.0);
					break;
				case "Eeloo":
					AddDouble(kalc, name +".AtmosPress", 0.0);
					break;
				default:
					break;
			}
		}

		// /UNITY
	}
}
