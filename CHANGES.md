Changelog
---------
### 0.30
JIT Compiling baby!

- GUI features
  - Added GUI for calling functions that require inputs
  - Added ability to run functions continuously and pin the output to the HUD

- Language features
  - Functions are now JIT-compiled and run at native .NET speed.
  - Added Nelder-Mead solver for numeric approximation
  - Added build-in function `mag`
  - Added build-in function `cosh`
  - Added build-in function `sinh`
  - Added build-in function `tanh`
  - Geometric functions now work in degrees by default
  - *_rad function added that work in radians
  - Build-in function `dot` can now also perform matrix multiplication

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
