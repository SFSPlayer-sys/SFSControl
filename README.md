# SFSControl API Documentation

## Introduction

SFSControl provides programmatic control over Space Flight Simulator rockets through HTTP API requests. This mod allows external applications to control rocket rotation, staging, part usage, and other functions.

## Information Endpoints

These endpoints return state information; all respond with JSON.

- /rocket_sim
  - Description: Simulation snapshot (position, velocity, rotation, orbit, throttle, parentPlanetCode, etc.).
  - Query: rocketIdOrName (optional)
  - HTTP example:
    ```http
    GET /rocket_sim
    ```

- /rocket
  - Description: Rocket save info (structure, parts, location, velocity, RCS, throttlePercent, etc.).
  - Query: rocketIdOrName (optional)
  - HTTP example:
    ```http
    GET /rocket?rocketIdOrName=0
    ```

- /rockets
  - Description: List of all rockets' save info in the current scene.
  - HTTP example:
    ```http
    GET /rockets
    ```

- /planet
  - Description: Planet info (radius, gravity, atmosphere, orbit, landmarks, etc.).
  - Query: codename (optional; defaults to current context)
  - HTTP example:
    ```http
    GET /planet?codename=Earth
    ```

- /planets
  - Description: All planets' info.
  - HTTP example:
    ```http
    GET /planets
    ```

- /other
  - Description: Misc info (targetAngle, quicksaves, navTarget, timewarpSpeed, worldTime, mass, thrust, TWR, etc.).
  - Query: rocketIdOrName (optional)
  - HTTP example:
    ```http
    GET /other
    ```

- /mission
  - Description: Mission/Challenge related info.
  - HTTP example:
    ```http
    GET /mission
    ```

- /debuglog
  - Description: Collected in-game console logs.
  - HTTP example:
    ```http
    GET /debuglog
    ```

- /version
  - Description: SFSControl mod version info.
  - HTTP example:
    ```http
    GET /version
    ```
  - Response example:
    ```json
    {"name":"SFSControl","version":"1.2"}
    ```
  - Python example:
    ```python
    import requests
    print(requests.get("http://127.0.0.1:27772/version").json())
    ```

## Methods

### Rotate

Controls rocket rotation using various modes and custom angles.

**Parameters:**
- `mode` (string): Rotation mode or custom angle
  - `"Prograde"` - Point in velocity direction
  - `"Surface"` - Point towards surface (radial direction)
  - `"Target"` - Point towards selected target
  - `"None"` - Disable rotation control
  - `"Default"` - Disable SAS
  - Custom angle value (e.g., `"90"`, `"180"`) - Rotate to specific angle
- `offset` (float): Angle offset in degrees
- `rocketIdOrName` (string/int, optional): Target rocket ID or name

**Example:**
```json
{"method": "Rotate", "args": ["Prograde", 0, 0]}
{"method": "Rotate", "args": [90, 0, 0]}
{"method": "Rotate", "args": ["Target", 15, 0]}
```

### StopRotate

Stops current rotation and disables rotation control.

**Parameters:**
- `rocketIdOrName` (string/int, optional): Target rocket ID or name
- `stopCoroutine` (bool, optional): Whether to stop rotation coroutines

**Example:**
```json
{"method": "StopRotate", "args": [0]}
```

### SetThrottle

Controls rocket throttle level.

**Parameters:**
- `size` (double): Throttle level (0.0 to 1.0)
- `rocketIdOrName` (string/int, optional): Target rocket ID or name

**Example:**
```json
{"method": "SetThrottle", "args": [0.5, 0]}
```

### SetRCS

Controls RCS (Reaction Control System) on/off.

**Parameters:**
- `on` (bool): True to enable RCS, false to disable
- `rocketIdOrName` (string/int, optional): Target rocket ID or name

**Example:**
```json
{"method": "SetRCS", "args": [true, 0]}
```

### Stage

Activates the next stage of the rocket.

**Parameters:**
- `rocketIdOrName` (string/int, optional): Target rocket ID or name

**Example:**
```json
{"method": "Stage", "args": [0]}
```

### UsePart

Activates a specific part on the rocket.

**Parameters:**
- `partId` (int): Part ID to activate
- `rocketIdOrName` (string/int, optional): Target rocket ID or name

**Example:**
```json
{"method": "UsePart", "args": [1, 0]}
```

### SetMainEngineOn

Controls main engine on/off.

**Parameters:**
- `on` (bool): True to enable main engine, false to disable
- `rocketIdOrName` (string/int, optional): Target rocket ID or name

**Example:**
```json
{"method": "SetMainEngineOn", "args": [true, 0]}
```

### RcsThrust

Controls RCS thrust in specific direction.

**Parameters:**
- `direction` (string): Thrust direction
  - `"up"` - Upward thrust
  - `"down"` - Downward thrust
  - `"left"` - Leftward thrust
  - `"right"` - Rightward thrust
- `seconds` (float): Duration in seconds
- `rocketIdOrName` (string/int, optional): Target rocket ID or name

**Example:**
```json
{"method": "RcsThrust", "args": ["up", 2.0, 0]}
```

### SetRotation

Sets rocket rotation to specific angle.

**Parameters:**
- `angle` (float): Target angle in degrees
- `rocketIdOrName` (string/int, optional): Target rocket ID or name

**Example:**
```json
{"method": "SetRotation", "args": [90, 0]}
```

### SetState

Sets rocket state (position, velocity, angular velocity, blueprint data).

**Parameters:**
- `x` (double, optional): X position
- `y` (double, optional): Y position
- `vx` (double, optional): X velocity
- `vy` (double, optional): Y velocity
- `angularVelocity` (double, optional): Angular velocity
- `blueprintJson` (string, optional): Blueprint JSON data
- `rocketIdOrName` (string/int, optional): Target rocket ID or name

**Example:**
```json
{"method": "SetState", "args": [1000, 2000, 100, 50, 0, null, 0]}
```

### Launch

Launches the rocket.

**Example:**
```json
{"method": "Launch", "args": []}
```

### SwitchRocket

Switches control to specified rocket.

**Parameters:**
- `idOrName` (string/int): Rocket ID or name

**Example:**
```json
{"method": "SwitchRocket", "args": ["Rocket1"]}
```

### RenameRocket

Renames a rocket.

**Parameters:**
- `idOrName` (string/int): Current rocket ID or name
- `newName` (string): New rocket name

**Example:**
```json
{"method": "RenameRocket", "args": ["Rocket1", "NewName"]}
```

### SetTarget

Sets target for rocket.

**Parameters:**
- `nameOrIndex` (string/int): Target name or index

**Example:**
```json
{"method": "SetTarget", "args": ["Earth"]}
```

### ClearTarget

Clears current target.

**Example:**
```json
{"method": "ClearTarget", "args": []}
```

### Build

Builds rocket from blueprint.

**Parameters:**
- `blueprintInfo` (string): Blueprint information

**Example:**
```json
{"method": "Build", "args": ["blueprint_data"]}
```

### ClearBlueprint

Clears current blueprint.

**Example:**
```json
{"method": "ClearBlueprint", "args": []}
```

### SwitchToBuild

Switches to build mode.

**Example:**
```json
{"method": "SwitchToBuild", "args": []}
```

### ClearDebris

Clears all debris from the scene.

**Example:**
```json
{"method": "ClearDebris", "args": []}
```

### AddStage

Adds a new stage to the rocket.

**Parameters:**
- `index` (int): Stage index
- `partIds` (int[]): Array of part IDs
- `rocketIdOrName` (string/int, optional): Target rocket ID or name

**Example:**
```json
{"method": "AddStage", "args": [0, [1, 2, 3], 0]}
```

### RemoveStage

Removes a stage from the rocket.

**Parameters:**
- `index` (int): Stage index to remove
- `rocketIdOrName` (string/int, optional): Target rocket ID or name

**Example:**
```json
{"method": "RemoveStage", "args": [0, 0]}
```

### SetOrbit

Sets rocket orbit parameters.

**Parameters:**
- `radius` (double): Orbit radius
- `eccentricity` (double, optional): Orbit eccentricity
- `trueAnomaly` (double, optional): True anomaly
- `counterclockwise` (bool, optional): Orbit direction
- `planetCode` (string, optional): Planet code
- `rocketIdOrName` (string/int, optional): Target rocket ID or name

**Example:**
```json
{"method": "SetOrbit", "args": [100000, 0.1, 0, true, "Earth", 0]}
```

### DeleteRocket

Deletes specified rocket.

**Parameters:**
- `idOrName` (string/int, optional): Rocket ID or name to delete

**Example:**
```json
{"method": "DeleteRocket", "args": ["Rocket1"]}
```

### CreateRocket

Creates a new rocket.

**Parameters:**
- `planetCode` (string): Planet code
- `blueprintJson` (string): Blueprint JSON data
- `rocketName` (string, optional): Rocket name
- `x` (double, optional): X position
- `y` (double, optional): Y position
- `vx` (double, optional): X velocity
- `vy` (double, optional): Y velocity
- `vr` (double, optional): Angular velocity

**Example:**
```json
{"method": "CreateRocket", "args": ["Earth", "blueprint_data", "NewRocket", 0, 0, 0, 0, 0]}
```

### CreateObject

Creates a new object in the world.

**Parameters:**
- `objectType` (string): Type of object to create
- `planetCode` (string): Planet code
- `x` (double, optional): X position
- `y` (double, optional): Y position
- `objectName` (string, optional): Object name
- `hidden` (bool, optional): Whether object is hidden

**Example:**
```json
{"method": "CreateObject", "args": ["Satellite", "Earth", 1000, 2000, "Sat1", false]}
```

### TransferFuel

Transfers fuel between tanks.

**Parameters:**
- `fromTankId` (int): Source tank ID
- `toTankId` (int): Destination tank ID
- `rocketIdOrName` (string/int, optional): Target rocket ID or name

**Example:**
```json
{"method": "TransferFuel", "args": [1, 2, 0]}
```

### StopFuelTransfer

Stops fuel transfer.

**Parameters:**
- `rocketIdOrName` (string/int, optional): Target rocket ID or name

**Example:**
```json
{"method": "StopFuelTransfer", "args": [0]}
```

### WheelControl

Controls wheel movement.

**Parameters:**
- `enable` (bool, optional): Enable/disable wheel control
- `turnAxis` (float, optional): Turn axis value
- `rocketIdOrName` (string/int, optional): Target rocket ID or name

**Example:**
```json
{"method": "WheelControl", "args": [true, 0.5, 0]}
```

### SetTimewarp

Controls time warp speed.

**Parameters:**
- `speed` (double): Time warp speed multiplier
- `realtimePhysics` (bool, optional): Use realtime physics
- `showMessage` (bool, optional): Show time warp message

**Example:**
```json
{"method": "SetTimewarp", "args": [2.0, false, true]}
```

### TimewarpPlus

Increases time warp speed.

**Example:**
```json
{"method": "TimewarpPlus", "args": []}
```

### TimewarpMinus

Decreases time warp speed.

**Example:**
```json
{"method": "TimewarpMinus", "args": []}
```

### SwitchMapView

Toggles map view.

**Parameters:**
- `on` (bool, optional): True to enable map view, false to disable

**Example:**
```json
{"method": "SwitchMapView", "args": [true]}
```

### Track

Tracks specified object.

**Parameters:**
- `nameOrIndex` (string/int): Object name or index to track

**Example:**
```json
{"method": "Track", "args": ["Earth"]}
```

### Unfocus

Unfocuses current object.

**Example:**
```json
{"method": "Unfocus", "args": []}
```

### SetCheat

Enables/disables cheats.

**Parameters:**
- `cheatName` (string): Name of cheat
  - `"infiniteFuel"` - Infinite fuel
  - `"infiniteBuildArea"` - Infinite build area
  - `"noGravity"` - No gravity
  - `"noAtmosphericDrag"` - No atmospheric drag
  - `"noHeatDamage"` - No heat damage
  - `"noBurnMarks"` - No burn marks
  - `"partClipping"` - Part clipping
  - `"unbreakableParts"` - Unbreakable parts
- `value` (bool): True to enable, false to disable

**Example:**
```json
{"method": "SetCheat", "args": ["unlimitedFuel", true]}
```

### Revert

Reverts to previous state.

**Parameters:**
- `type` (string): Revert type
  - `"launch"` - Revert to launch
  - `"30s"` - Revert 30 seconds
  - `"3min"` - Revert 3 minutes
  - `"build"` - Revert to build

**Example:**
```json
{"method": "Revert", "args": ["launch"]}
```

### CompleteChallenge

Completes a challenge.

**Parameters:**
- `challengeId` (string): Challenge ID to complete

**Example:**
```json
{"method": "CompleteChallenge", "args": ["challenge1"]}
```

### WaitForWindow

Waits for specific window to appear.

**Parameters:**
- `mode` (string, optional): Window mode
  - `"transfer"` - Transfer window (default)
  - `"rendezvous"` - Rendezvous window
- `parameter` (double, optional): Window parameter

**Example:**
```json
{"method": "WaitForWindow", "args": ["transfer", 100]}
```

### ShowToast

Shows toast message.

**Parameters:**
- `toast` (string): Toast message text

**Example:**
```json
{"method": "ShowToast", "args": ["Hello World"]}
```

### LogMessage

Logs a message.

**Parameters:**
- `type` (string): Message type
- `message` (string): Message text

**Example:**
```json
{"method": "LogMessage", "args": ["info", "Test message"]}
```

### QuicksaveManager

Manages quicksave operations.

**Parameters:**
- `operation` (string, optional): Operation type
  - `"save"` - Save quicksave
  - `"load"` - Load quicksave
  - `"delete"` - Delete quicksave
  - `"rename"` - Rename quicksave (format: "oldName:newName")
- `name` (string, optional): Save name

**Examples:**
```json
{"method": "QuicksaveManager", "args": ["save", "quicksave1"]}
{"method": "QuicksaveManager", "args": ["load", "quicksave1"]}
{"method": "QuicksaveManager", "args": ["delete", "quicksave1"]}
{"method": "QuicksaveManager", "args": ["rename", "oldName:newName"]}
```

### SetMapIconColor

Sets map icon color.

**Parameters:**
- `rgbaValue` (string): RGBA color value
- `rocketIdOrName` (string/int, optional): Target rocket ID or name

**Example:**
```json
{"method": "SetMapIconColor", "args": ["255,0,0,255", 0]}
```



## Draw API

Draw simple persistent shapes in world space via HTTP. Shapes remain rendered every frame until cleared.

- Endpoint: `POST /draw`
- Body: JSON
- Coordinates: World space (not screen space). Internally converted using `WorldView` position offset.
- Persistence: Commands are stored and redrawn until `cmd: "clear"` is sent.
- Ordering: Use `sorting` or alias `layer` (float, default 1) to control draw order.
- Color: Array `[r,g,b,a]` with each component in 0-1, default `[1,1,1,1]`.

Supported commands:
- `cmd: "line"` draw a line segment
- `cmd: "circle"` draw a circle outline
- `cmd: "clear"` clear all previously drawn items
- `cmd: "text"` not supported (returns HTTP 400)

### Draw a line

Parameters:
- `start`: `[x,y]` or `[x,y,z]`
- `end`: `[x,y]` or `[x,y,z]`
- `color` (optional): `[r,g,b,a]`
- `width` (optional): float
- `sorting` or `layer` (optional): float

Example:
```json
{ "cmd": "line", "start": [0, 0, 0], "end": [1000, 500, 0], "color": [1, 0, 0, 1], "width": 2.0, "sorting": 1 }
```

### Draw a circle

Parameters:
- `center`: `[x,y]`
- `radius`: float
- `resolution` (optional): integer segments, minimum 8 (default 64)
- `color` (optional): `[r,g,b,a]`
- `sorting` or `layer` (optional): float

Example:
```json
{ "cmd": "circle", "center": [5000, -2000], "radius": 300, "resolution": 64, "color": [0, 1, 0, 0.8], "layer": 2 }
```

### Clear drawings

Example:
```json
{ "cmd": "clear" }
```

Notes:
- `cmd: "text"` is not implemented in the current GL-only renderer and will return `{ "error": "text not supported" }` with HTTP 400.
- All coordinates must be world-space coordinates.

## Usage Notes

- All methods return JSON responses with success/error status
- Rocket ID 0 refers to the currently controlled rocket
- Rotation modes require physics to be running and rocket to be controllable
- Target mode requires a selected target in the game
- Custom angles are specified in degrees (0-360)
- Offset values are added to the base rotation angle


## Acknowledgements

Special thanks to [Smart SAS Mod for SFS](https://github.com/AstroTheRabbit/Smart-SAS-Mod-SFS) for providing rotation control references.

Additional scripts for SFSControl can be found at [SFSControl-_-Scripts](https://github.com/SFSPlayer-sys/SFSControl-_-Scripts).

We provided a Python client [PySFS](https://github.com/SFSPlayer-sys/PySFS).