using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace Kerbulator {
	/// <summary>Glue code to smooth over differences when plugin is loaded in
	/// the Unity editor versus when it is loaded in the actual game.</summary>
	public interface IGlue {
		void PlaceNode(List<Variable> output);
		void AddGlobals(Kerbulator kalc);
		Texture2D GetTexture(string id);
		void ChangeState(bool open);
	}

	public class KerbulatorGUI {
		// Selecting functions and getting info
		private Function selectedFunction = null;
		private string functionDescription = "";
		private float functionDescriptionHeight = 0;

		// Editing functions
		private Function editFunction = null;
		private string editFunctionContent = "";
		private string editFunctionName = "maneuver";
		private string functionFile = "maneuver.math";
		private string maneuverTemplate = "out: Δv_r\nout: Δv_n\nout: Δv_p\nout: Δt\n\nΔv_r = 0\nΔv_n = 0\nΔv_p = 0\nΔt = 0";
		
		// Running functions
		private Function runFunction = null;
		private string functionOutput = "";

		// For dragging windows
		private Rect titleBarRect = new Rect(0,0, 10000, 20);

		// Different scrollbars
		private Vector2 mainScrollPos = new Vector2(0, 0);
		private Vector2 editorScrollPos = new Vector2(0, 0);
		private Vector2 runScrollPos = new Vector2(0, 0);

		// Main Kerbulator instance
		private Kerbulator kalc;

		// Main button position
		private bool drawMainButton = false;
		private Rect mainButtonPos = new Rect(190, 0, 32, 32);

		// Window positions
		private Rect mainWindowPos = new Rect(0, 60, 280, 400);
		private bool mainWindowEnabled = false;
		private Rect editWindowPos = new Rect(280, 60, 350, 300);
		private bool editWindowEnabled = false;
		private Rect runWindowPos = new Rect(0, 470, 200, 200);
		private bool runWindowEnabled = false;

		// Dictionary containing all available functions
		Dictionary<string, Function> functions;
		string functionDir = "";

		// Math symbols
        string[] greekLetters = new[] {"α","β","γ","δ","ε","ζ","η","θ","ι","κ","λ","μ","ν","ξ","ο","π","ρ","σ","τ","υ","φ","χ","ψ","ω"};
		string[] greekUCLetters = new[] {"Α","Β","Γ","Δ","Ε","Ζ","Η","Θ","Ι","Κ","Λ","Μ","Ν","Ξ","Ο","Π","Ρ","Σ","Τ","Υ","Φ","Χ","Ψ","Ω"};
		string[] symbols = new[] {"=","+","-","*","×","·","/","÷","%", "√","^","(",")","[","]","{","}","⌊","⌋","⌈","⌉"};

		// GUI styles in use
		private bool stylesInitiated = false;
		GUIStyle keyboard;
		GUIStyle defaultButton;

		private IGlue glue;
		private bool inEditor = false;

		// Icons
		Texture2D kerbulatorIcon;
		Texture2D editIcon;
		Texture2D runIcon;
		Texture2D saveIcon;
		Texture2D deleteIcon;
		Texture2D nodeIcon;

		public KerbulatorGUI(IGlue glue, bool inEditor, bool drawMainButton) {
			this.glue = glue;
			this.inEditor = inEditor;
			this.drawMainButton = drawMainButton;
			ChangeState(false);

			functionDir = Application.persistentDataPath + "/Kerbulator";
			editFunctionContent = maneuverTemplate;

			if(!Directory.Exists(functionDir)) {
				Directory.CreateDirectory(functionDir);
			}

			// Load icons
			kerbulatorIcon = glue.GetTexture("kerbulator");
			editIcon = glue.GetTexture("edit");
			runIcon = glue.GetTexture("run");
			nodeIcon = glue.GetTexture("node");
			saveIcon = glue.GetTexture("save");
			deleteIcon = glue.GetTexture("delete");

			Scan();
			kalc = new Kerbulator(functionDir);
		}

        public void ChangeState(bool open) {
            mainWindowEnabled = open;
            glue.ChangeState(open);
        }

		/// <summary>Draws the GUI</summary>
		public void OnGUI() {
			// Initiate styles
			if(!stylesInitiated) {
				keyboard = new GUIStyle(GUI.skin.GetStyle("button"));
				keyboard.padding = new RectOffset(0,0,2,2);

				defaultButton = new GUIStyle(GUI.skin.GetStyle("button"));
				defaultButton.padding = new RectOffset(4,4,4,4);
			}

			if(drawMainButton) {
				// Draw the main button
				if(GUI.Button(mainButtonPos, kerbulatorIcon, defaultButton)) {
					ChangeState(!mainWindowEnabled);
				}
			}

			// Draw the windows (if enabled)
			if(mainWindowEnabled) {
				mainWindowPos = GUILayout.Window(93841, mainWindowPos, DrawMainWindow, "Kerbulator", GUILayout.ExpandHeight(false));
				//mainWindowPos.height = 0;
			}

			if(editWindowEnabled) {
				editWindowPos = GUILayout.Window(93842, editWindowPos, DrawEditWindow, "Function Editor", GUILayout.ExpandHeight(false));
				//editWindowPos.height = 0;
			}

			if(runWindowEnabled) {
				runWindowPos = GUILayout.Window(93843, runWindowPos, DrawRunWindow, "Run "+ runFunction.Id, GUILayout.ExpandHeight(false));
				//runWindowPos.height = 0;
			}
		}

		/// <summary>Draws the main window that displays a list of available functions</summary>
		/// <param name="id">An unique number indentifying the window</param>
		public void DrawMainWindow(int id) {
			// Close button at the top right corner
			ChangeState(!GUI.Toggle(new Rect(mainWindowPos.width - 25, 0, 20, 20), !mainWindowEnabled, ""));

			GUILayout.Label("Available functions:");

			mainScrollPos = GUILayout.BeginScrollView(mainScrollPos, false, true, GUILayout.Height(300));

			foreach(KeyValuePair<string, Function> f in functions) {
				GUILayout.BeginHorizontal();

				if(GUILayout.Button(f.Key, GUILayout.Height(24))) { 
					selectedFunction = f.Value;
					functionDescription = FunctionDescription(f.Value);
					functionDescriptionHeight = GUI.skin.GetStyle("label").CalcHeight(new GUIContent(functionDescription), 225);
				}

				if(GUILayout.Button(editIcon, defaultButton, GUILayout.Width(24), GUILayout.Height(24))) {
					// Load the function to be edited
					editFunction = f.Value;
					functionFile = functionDir +"/"+ f.Key +".math";
					editFunctionName = f.Key;
					editFunctionContent = System.IO.File.ReadAllText(functionFile);
					editWindowEnabled = true;
				}

				if(GUILayout.Button(runIcon, defaultButton, GUILayout.Width(24), GUILayout.Height(24))) {
					// Load the function to be run
					functionOutput = "";
					runFunction = f.Value;
					runWindowEnabled = true;

					// Run it
					List<Variable> output = Run();
					functionOutput = FormatOutput(runFunction, output);
				}

				GUILayout.EndHorizontal();

				// When a function is selected, display some info
				if(selectedFunction == f.Value) {
					GUILayout.Label("Function info:");
					GUILayout.Label(functionDescription, GUILayout.Width(225), GUILayout.Height(functionDescriptionHeight)); 
				}
			}

			GUILayout.EndScrollView();

			GUILayout.Space(20);	

			GUILayout.BeginHorizontal();
			if(GUILayout.Button("New function")) {
				editFunction = null;

				// Load template for an empty function
				functionFile = functionDir +"/unnamed.math";
				editFunctionName = "unnamed";
				editFunctionContent = "";
				editWindowEnabled = true;
			}

			if(GUILayout.Button("New maneuver")) {
				editFunction = null;

				// Load template for a function controlling a maneuver node
				functionFile = functionDir +"/maneuver.math";
				editFunctionName = "maneuver";
				editFunctionContent = maneuverTemplate;
				editWindowEnabled = true;
			}

			GUILayout.EndHorizontal();

			GUI.DragWindow(titleBarRect);
		}
		
		/// <summary>Draws the edit window that allows basic text editing.</summary>
		/// <param name="id">An unique number indentifying the window</param>
		public void DrawEditWindow(int id) {
			// Close button
			editWindowEnabled = !GUI.Toggle(new Rect(editWindowPos.width - 25, 0, 20, 20), !editWindowEnabled, "");

			GUILayout.BeginHorizontal();
			
			if(GUILayout.Button(deleteIcon, defaultButton, GUILayout.Width(25), GUILayout.Height(24))) {
				Delete();
			}

			editFunctionName = GUILayout.TextField(editFunctionName, GUILayout.Height(24));
			
			if(GUILayout.Button(saveIcon, defaultButton, GUILayout.Width(24), GUILayout.Height(24))) {
				Save();

				if(editFunction == null)
					editFunction = functions[editFunctionName];
			}

			if(GUILayout.Button(runIcon, defaultButton, GUILayout.Width(24), GUILayout.Height(24))) {
				// Save it
				Save();

				// Load the function to be run
				functionOutput = "";
				runFunction = editFunction;
				runWindowEnabled = true;

				// Run it
				List<Variable> output = Run();
				functionOutput = FormatOutput(runFunction, output);
			}

			GUILayout.EndHorizontal();

			editorScrollPos = GUILayout.BeginScrollView(editorScrollPos, false, true, GUILayout.Height(200), GUILayout.Width(460));
			editFunctionContent = GUILayout.TextArea(editFunctionContent, GUILayout.ExpandWidth(true));
			TextEditor editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
			GUILayout.EndScrollView();

			GUILayout.BeginHorizontal();
			foreach(string s in greekLetters) {
				if(GUILayout.Button(s, keyboard, GUILayout.Width(15))) {
					editFunctionContent = editFunctionContent.Insert(editor.pos, s);
					editor.pos ++;
					editor.selectPos ++;
				}
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			foreach(string s in greekUCLetters) {
				if(GUILayout.Button(s, keyboard, GUILayout.Width(15))) {
					editFunctionContent = editFunctionContent.Insert(editor.pos, s);
					editor.pos ++;
					editor.selectPos ++;
				}
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			foreach(string s in symbols) {
				if(GUILayout.Button(s, keyboard, GUILayout.Width(15))) {
					editFunctionContent = editFunctionContent.Insert(editor.pos, s);
					editor.pos ++;
					editor.selectPos ++;
				}
			}
			GUILayout.EndHorizontal();

			GUI.DragWindow(titleBarRect);
		}

		/// <summary>Draws the run window that allows execution of a function.</summary>
		/// <param name="id">An unique number indentifying the window</param>
		public void DrawRunWindow(int id) {
			// Close button
			runWindowEnabled = !GUI.Toggle(new Rect(runWindowPos.width - 25, 0, 20, 20), !runWindowEnabled, "");

			if(runFunction == null) {
				GUILayout.Label("ERROR: no function selected.");
			} else if(runFunction.InError) {
				GUILayout.Label("ERROR: "+ runFunction.ErrorString);
			} else {
				/*
				if(runFunction.Ins.Count == 0) {
					GUILayout.Label("Inputs: none.");
				} else {
					GUILayout.Label("Inputs:");
					for(int i=0; i<runFunction.Ins.Count; i++) {
						GUILayout.BeginHorizontal();
						GUILayout.Label(runFunction.Ins[i]);
						GUILayout.TextField("0");
						GUILayout.Label(runFunction.InDescriptions[i]);
						GUILayout.EndHorizontal();
					}

				}
				*/

				GUILayout.BeginHorizontal();

				if(GUILayout.Button(runIcon, defaultButton, GUILayout.Height(32))) {
					List<Variable> output = Run();
					functionOutput = FormatOutput(runFunction, output);
				}
				
				if(GUILayout.Button(nodeIcon, defaultButton, GUILayout.Height(32))) {
					List<Variable> output = Run();
					glue.PlaceNode(output);
					functionOutput = FormatOutput(runFunction, output);
				}

				GUILayout.EndHorizontal();

				runScrollPos = GUILayout.BeginScrollView(runScrollPos, GUILayout.Height(150));
				GUILayout.Label(functionOutput);
				GUILayout.EndScrollView();
			}

			GUI.DragWindow(titleBarRect);
		}

		/// <summary>Run a function.</summary>
		/// <param name="f">The function to run</param>
		public List<Variable> Run() {
			if(runFunction == editFunction)
				Save();

			glue.AddGlobals(kalc);
			return kalc.Run(runFunction);
		}

		/// <summary>Save the current function being edited.</summary>
		public void Save() {
			if(editFunction != null) {
				string oldFunctionFile = functionDir +"/"+ editFunction.Id +".math";
				Debug.Log(oldFunctionFile);
				if(System.IO.File.Exists(oldFunctionFile))
					System.IO.File.Delete(oldFunctionFile);
			}

			functionFile = functionDir +"/"+ editFunctionName +".math";
			System.IO.File.WriteAllText(functionFile, editFunctionContent);

			Scan();
            editFunction = functions[editFunctionName];
		}

		/// <summary>Delete the current function being edited.</summary>
		public void Delete() {
			if(editFunction != null) {
				string oldFunctionFile = functionDir +"/"+ editFunction.Id +".math";
				if(System.IO.File.Exists(oldFunctionFile))
					System.IO.File.Delete(oldFunctionFile);
			}

			Scan();
			editWindowEnabled = false;
		}

		/// <summary>Obtain some info of a function.</summary>
		/// <param name="f">The function to obtain the info of</param>
		public string FunctionDescription(Function f) {
			string desc = "";

			if(f.Ins.Count == 0) {
				desc += "Inputs: none.";
			} else {
				desc += "Inputs:\n";
				for(int i=0; i<f.Ins.Count; i++) {
					desc += f.Ins[i];
					if(i < f.InDescriptions.Count)
						desc += ": "+ f.InDescriptions[i];
					desc += "\n";
				}
			}

			if(f.Outs.Count == 0) {
				desc += "\nOutputs: none.";
			} else {
				desc += "\nOutputs:\n";
				for(int i=0; i<f.Outs.Count; i++) {
					desc += f.Outs[i];
					if(i < f.OutDescriptions.Count)
						desc += ": "+ f.OutDescriptions[i];
					desc += "\n";
				}
			}

			if(f.InError)
				desc += "\nERROR: "+ f.ErrorString;

			return desc;
		}

		/// <summary>Provide a string representation of the output resulting from executing a function.</summary>
		/// <param name="f">The function that was executed</param>
		/// <param name="output">The variables resuting from the execution</param>
		public string FormatOutput(Function f, List<Variable> output) {
			string desc = "Outputs:\n";
			if(output.Count == 0) {
				desc += "None.";
			} else {
				foreach(Variable v in output)
					desc += v.id +" = "+ v.ToString() +"\n";
			}

			if(f.InError)
				desc += "\nERROR: "+ f.ErrorString;

			return desc;
		}

		/// <summary>Called by Unity when the game gains or loses focus.</summary>
		/// <param name="focused">True when the game gained focues, otherwise false</summary>
		public void OnApplicationFocus(bool focused) {
			if(focused) {
				// Rebuild the list of functions. It could be that they were edited outside of KSP
				functions = Function.Scan(functionDir);

				// Reload the function being edited
				if(editFunction != null)
					editFunctionContent = System.IO.File.ReadAllText(functionFile);
			} else {
				Save();
			}
		}

		/// <summary>Scan for available functions and update funtion references if needed.</summary>
		public void Scan() {
			functions = Function.Scan(functionDir);

			if(selectedFunction != null) {
				if(functions.ContainsKey(selectedFunction.Id)) {
					selectedFunction = functions[selectedFunction.Id];
					functionDescription = FunctionDescription(selectedFunction);
				} else {
					selectedFunction = null;
					functionDescription = "";
				}

				functionDescriptionHeight = GUI.skin.GetStyle("label").CalcHeight(new GUIContent(functionDescription), 225);
			}

			if(editFunction != null) {
				if(functions.ContainsKey(editFunction.Id))
					editFunction = functions[editFunction.Id];
				else {
					editFunction = null;
					editWindowEnabled = false;
				}
			}

			if(runFunction != null) {
				if(functions.ContainsKey(runFunction.Id))
					runFunction = functions[runFunction.Id];
				else {
					runFunction = null;
					runWindowEnabled = false;
				}
				functionOutput = "";
			}
		}
	}
}
