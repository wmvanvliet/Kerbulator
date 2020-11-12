Changelog
---------
### 0.5
The mechanics for creating maneuvers and alarms have been overhauled. See the
manual and language reference for the new syntax. Thanks to Thorulf Neustrup!

 - Language features:
   - New notation for defining maneuvers
   - New notation for defining alarms
   - Added ability to create multiple maneuvers and alarms in a single function

### 0.46
Bugfixes.
 - GUI features:
   - Fix clickthrough prevention logic not releasing the lock when closing a repeating-function output window
   - Fix toolbar button not being removed when exiting the VHB or Hangar

### 0.45
Add support for KSP version 1.9

### 0.44
Add support for KSP version 1.4.4

 - GUI features
   - Kerbulator now makes use of the clickthrough prevention logic

### 0.43
Recompile for KSP 1.3

### 0.42
Recompile for KSP 1.2

### 0.41
Bugfixes.

 - Globals
   - Added Inf and ∞ globals to denote infinity
   - Fixed some globals that were in radians instead of degrees (e.g. θ)

 - GUI features
   - Fix function name validation in GUI
   - Fix column indicator in error messages
   - Fix missing expression error message
   - Fix behavior when running a repeating function that contains an error
   - Fix a bug where the output of the previously run function was shown
   - Fix delta-V in normal direction when placing maneuver node
   
### 0.4
Some welcome additions to the language.

 - Language features
   - Add boolean operators: < > <= ≤ >= ≥ == != ≠ ¬ ! ∧ and ∨ or
   - Add support for piecewise functions (a.k.a. if-statements)
   - Add support for specifying pre- and postfixes for output variables

 - GUI features
   - Add support for showing pre- and postfixes for output variables
   - An error message is shown when the user tries to save a function with an invalid name

### 0.36
Add support for KSP version 1.1.3

 - GUI features
   - Remember window positions

### 0.35
Add support for KSP version 1.1.2

 - Language features
   - Added global `Sun`

### 0.34
More globals. Thanks to Emanuele Bardelli.
 - Language features
   - Fixed `{Body}.AtmosHeight` global
   - Fixed `{Body}.AtmosPress` global
   - Added `Craft.Rel.AN` global
   - Added `Craft.Rel.DN` global
   - Added `Craft.Rel.Inc` global

### 0.33
Small maintenance update. Thanks to Emanuele Bardelli.

- GUI features
  - Fixed support for blizzy78 toolbar
 
- Language features
  - Added build-in function `atan2`
  - Added build-in function `atan2_rad`

!! Breaking backwards compatability
  - Kerbulator used to store its files (.math) outside the main Kerbulator folder,
    which is a violation of the guidelines laid out by Squad. Function files now go
    in the `PluginData/Kerbulator` folder.

### 0.32
Kerbal Alarm Clock integration

- GUI features
  - maneuver node button hides when not in flight
  - Added button to add alarm

- Language features
  - Added ability to add alarm when KAC is installed

### 0.31
KSP 1.0 Compatibility

- GUI features
  - Added support for the stock application toolbar
  - Kerbulator now available in all scenes, not just in flight

- Globals
  - *.AtmosHeight and *.AtmosPress currently disabled as KSP 1.0 changed things

### 0.30
JIT Compiling baby!

- GUI features
  - Added GUI for calling functions that require inputs
  - Added ability to run functions continuously and pin the output to the HUD
  - Windows can be resized

- Language features
  - Functions are now JIT-compiled and run at native .NET speed.
  - Added Nelder-Mead solver for numeric approximation
  - Added build-in function `mag`
  - Added build-in function `cosh`
  - Added build-in function `sinh`
  - Added build-in function `tanh`
  - Geometric build-in functions now work in degrees by default
  - *_rad function added that work in radians
  - Build-in function `dot` can now also perform matrix multiplication

- Globals
  - Craft.Inter1.TrueAnomaly is now in degrees instead of radians
  - Craft.Inter1.θ is now in degrees instead of radians
  - Craft.Inter2.TrueAnomaly is now in degrees instead of radians
  - Craft.Inter2.θ is now in degrees instead of radians

!! Breaking backwards compatability
  - Geometric build-in functions now work in degrees instead of radians.
    Use the `*_rad` functions to get the versions that work in radians.

### 0.23
Bugfix. Thanks to Bas Cornelissen to patiently work it out with me.

- Language features:
  - Properly deal with `\t` and `\r`

### 0.22
Better errors and more globals.

- Globals ([description of all globals](doc/globals.mkd)):
  - Added `{body}.Inc`
  - Added `{body}.LAN`
  - Added `{body}.Ω`
  - Added `{body}.SOI.Δt`
  - Added `Navball.Heading`
  - Added `Navball.Pitch`
  - Added `Navball.Roll`
  - Added `Navball.OrbitalVelocity`
  - Added `Navball.SurfaceVelocity`
  - Added `Navball.VerticalVelocity`
  - Renamed `Craft.Inter1.sep` to `Craft.Inter1.Sep`
  - Renamed `Craft.Inter2.sep` to `Craft.Inter2.Sep`

- GUI features:
  - Better error messages. Hunt down the bugs in your code with more ease.

### 0.21
Bugfixes. Thanks to Teseracto for finding them.

- GUI features:
  - Fixed losing changes on window switch
  - Closing main window no longer breaks toolbar button
- Language features:
  - Operator precidence fixed. (Added some unit tests for these cases)

### 0.2
Renamed the entire project Kerbulator, since Kalculator is already an
excellent mod by agises.

- Globals:
  - Fixed μ globals
- GUI features:
  - Added support for blizzy78 toolbar
  - Icons for some buttons

### 0.11
Fixed bug where sometimes the run button did not work.

### 0.1
Initial version.

- Language features:
  - Functions
  - Expressions
  - Lists
  - List unpacking
- Globals:
  - All celestial bodies
  - Current orbit
  - Orbit of target
  - Target intercept information
- GUI features:
  - Function list
  - Description of input and outputs
  - Very basic support for error reporting
  - Editor with keyboard
  - Re-scan function on window focus
  - Add maneuver nodes
