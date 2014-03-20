using KSP.IO;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace Kerbulator {
	public static class Globals {
		public static void Add(Kerbulator kalc) {
			// Planets
			foreach(CelestialBody b in FlightGlobals.Bodies) {
				if(b.name == "Sun")
					AddCelestialBody(kalc, b, "Kerbol");
				else
					AddCelestialBody(kalc, b, b.name);
			}
				
			Vessel v = FlightGlobals.ActiveVessel;
			Orbit orbit1 = v.orbit;
			if(v != null) {
				// Current orbit
				AddOrbit(kalc, orbit1, "Craft");

				// Current position in carthesian coordinates
				AddVector3d(kalc, "Craft.Pos", v.GetWorldPos3D());
				
				// Navball (thank you MechJeb source)
				Vector3d CoM = v.findWorldCenterOfMass();
				Vector3d up = (CoM - v.mainBody.position).normalized;
				Vector3d north = Vector3d.Exclude(up, (v.mainBody.position + v.mainBody.transform.up * (float)v.mainBody.Radius) - CoM).normalized;
				Quaternion rotationSurface = Quaternion.LookRotation(north, up);
				Quaternion rotationVesselSurface = Quaternion.Inverse(Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(v.GetTransform().rotation) * rotationSurface);
            	Vector3d velocityVesselOrbit = v.orbit.GetVel();
				Vector3d velocityVesselSurface = velocityVesselOrbit - v.mainBody.getRFrmVel(CoM);

            	AddDouble(kalc, "Navball.Heading", rotationVesselSurface.eulerAngles.y);
            	AddDouble(kalc, "Navball.Pitch",  (rotationVesselSurface.eulerAngles.x > 180) ? (360.0 - rotationVesselSurface.eulerAngles.x) : -rotationVesselSurface.eulerAngles.x);
            	AddDouble(kalc, "Navball.Roll", (rotationVesselSurface.eulerAngles.z > 180) ? (rotationVesselSurface.eulerAngles.z - 360.0) : rotationVesselSurface.eulerAngles.z);
            	AddDouble(kalc, "Navball.OrbitalVelocity", velocityVesselOrbit.magnitude);
            	AddDouble(kalc, "Navball.SurfaceVelocity", velocityVesselSurface.magnitude);
            	AddDouble(kalc, "Navball.VerticalVelocity", Vector3d.Dot(velocityVesselSurface, up));

				// Current time
				double UT = (double)Planetarium.GetUniversalTime();
				AddDouble(kalc, "UT", UT);

				// Reference body
				AddCelestialBody(kalc, v.orbit.referenceBody, "Parent");

				// Target
				if(FlightGlobals.fetch.VesselTarget != null) {
					ITargetable target = FlightGlobals.fetch.VesselTarget;
					Orbit orbit2 = target.GetOrbit();

					// Target Orbit
					AddOrbit(kalc, orbit2, "Target");

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
					AddDouble(kalc, "Craft.Inter1.dt", T1);
					AddDouble(kalc, "Craft.Inter1.Δt", T1);
					AddDouble(kalc, "Craft.Inter1.Sep", (orbit1.getPositionAtUT(T1+UT) - orbit2.getPositionAtUT(T1+UT)).magnitude);
					AddDouble(kalc, "Craft.Inter1.TrueAnomaly", orbit1.TrueAnomalyAtUT(T1+UT));
					AddDouble(kalc, "Craft.Inter1.θ", orbit1.TrueAnomalyAtUT(T1+UT));
					AddDouble(kalc, "Craft.Inter2.dt", T2);
					AddDouble(kalc, "Craft.Inter2.Δt", T2);
					AddDouble(kalc, "Craft.Inter2.Sep", (orbit1.getPositionAtUT(T2+UT) - orbit2.getPositionAtUT(T2+UT)).magnitude);
					AddDouble(kalc, "Craft.Inter2.TrueAnomaly", orbit2.TrueAnomalyAtUT(T2+UT));
					AddDouble(kalc, "Craft.Inter2.θ", orbit2.TrueAnomalyAtUT(T2+UT));
				}
			}
		}

		// UNITY
		public static void AddOrbit(Kerbulator kalc, Orbit orbit, string prefix) {
			if(orbit == null)
				return;

			AddDouble(kalc, prefix +".Ap", (double)orbit.ApA);
			AddDouble(kalc, prefix +".Pe", (double)orbit.PeA);
			AddDouble(kalc, prefix +".Inc", (double)orbit.inclination);
			AddDouble(kalc, prefix +".Alt", (double)orbit.altitude);
			AddDouble(kalc, prefix +".ArgPe", (double)orbit.argumentOfPeriapsis);
			AddDouble(kalc, prefix +".ω", (double)orbit.argumentOfPeriapsis);
			AddDouble(kalc, prefix +".LAN", (double)orbit.LAN);
			AddDouble(kalc, prefix +".Ω", (double)orbit.LAN);
			AddDouble(kalc, prefix +".TimeToAp", (double)orbit.timeToAp);
			AddDouble(kalc, prefix +".TimeToPe", (double)orbit.timeToPe);
			AddDouble(kalc, prefix +".Vel", (double)orbit.vel.magnitude);
			AddDouble(kalc, prefix +".TrueAnomaly", (double)orbit.trueAnomaly);
			AddDouble(kalc, prefix +".θ", (double)orbit.trueAnomaly);

			if(orbit.UTsoi > 0) {
				AddDouble(kalc, prefix +".SOI.dt", (double)orbit.UTsoi-Planetarium.GetUniversalTime());
				AddDouble(kalc, prefix +".SOI.Δt", (double)orbit.UTsoi-Planetarium.GetUniversalTime());
				AddDouble(kalc, prefix +".SOI.TrueAnomaly", (double)orbit.TrueAnomalyAtUT(orbit.UTsoi));
				AddDouble(kalc, prefix +".SOI.θ", (double)orbit.TrueAnomalyAtUT(orbit.UTsoi));
			}
		}

		public static void AddCelestialBody(Kerbulator kalc, CelestialBody body) {
			AddCelestialBody(kalc, body, body.name);
		}

		public static void AddCelestialBody(Kerbulator kalc, CelestialBody body, string prefix) {
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

			AddDouble(kalc, prefix +".R", (double)body.Radius);
			AddDouble(kalc, prefix +".M", (double)body.Mass);
			AddDouble(kalc, prefix +".mu", (double)body.gravParameter);
            AddDouble(kalc, prefix +".μ", (double)body.gravParameter);
			AddDouble(kalc, prefix +".day", (double)body.rotationPeriod);
			AddDouble(kalc, prefix +".SOI", (double)body.sphereOfInfluence);
			AddDouble(kalc, prefix +".AtmosHeight", (double)body.maxAtmosphereAltitude);
			AddDouble(kalc, prefix +".AtmosPress", (double)body.atmosphereMultiplier * 101325.0);
		}

		public static void AddDouble(Kerbulator kalc, string id, double v) {
			if(kalc.Globals.ContainsKey(id))
				kalc.Globals[id] = (System.Object) v; 
			else
				kalc.Globals.Add(id, (System.Object) v);
		}

		public static void AddBool(Kerbulator kalc, string id, bool v) {
			double val = v ? 1.0 : 0.0;
			AddDouble(kalc, id, val);
		}

		public static void AddVector3d(Kerbulator kalc, string id, Vector3d v) {
			Variable x = new Variable(id +".x", VarType.NUMBER, v.x);
			Variable y = new Variable(id +".y", VarType.NUMBER, v.y);
			Variable z = new Variable(id +".z", VarType.NUMBER, v.z);

			List<Variable> elements = new List<Variable>(3);
			elements.Add(x);
			elements.Add(y);
			elements.Add(z);

			if(kalc.Globals.ContainsKey(id))
				kalc.Globals[id] = (System.Object) elements.ToArray();
			else
				kalc.Globals.Add(id, (System.Object) elements.ToArray());
		}

		public static void AddVector3(Kerbulator kalc, string id, Vector3 v) {
			Variable x = new Variable(id +".x", VarType.NUMBER, (double)v.x);
			Variable y = new Variable(id +".y", VarType.NUMBER, (double)v.y);
			Variable z = new Variable(id +".z", VarType.NUMBER, (double)v.z);

			List<Variable> elements = new List<Variable>(3);
			elements.Add(x);
			elements.Add(y);
			elements.Add(z);

			kalc.Globals.Add(id, (System.Object) elements.ToArray());
		}
	}
}
