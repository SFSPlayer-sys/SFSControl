# SFSControl API Documentation

## Introduction

SFSControl provides programmatic control over Space Flight Simulator rockets through HTTP API requests. This mod allows external applications to control rocket rotation, staging, part usage, and other functions.

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
- `direction` (string): Thrust direction ("up", "down", "left", "right", "forward", "backward")
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

Sets rocket state (position, velocity, etc.).

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
- `value` (bool): True to enable, false to disable

**Example:**
```json
{"method": "SetCheat", "args": ["unlimitedFuel", true]}
```

### Revert

Reverts to previous state.

**Parameters:**
- `type` (string): Revert type

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
- `mode` (string, optional): Window mode ("transfer", etc.)
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
- `operation` (string, optional): Operation type ("save", "load", etc.)
- `name` (string, optional): Save name

**Example:**
```json
{"method": "QuicksaveManager", "args": ["save", "quicksave1"]}
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

### GetRocketInfo

Retrieves information about the current or specified rocket.

**Parameters:**
- `rocketIdOrName` (string/int, optional): Target rocket ID or name

**Example:**
```json
{"method": "GetRocketInfo", "args": [0]}
```

### GetRocketList

Returns a list of all available rockets in the current scene.

**Example:**
```json
{"method": "GetRocketList", "args": []}
```

### GetWorldInfo

Retrieves information about the current world state.

**Example:**
```json
{"method": "GetWorldInfo", "args": []}
```

## Usage Notes

- All methods return JSON responses with success/error status
- Rocket ID 0 refers to the currently controlled rocket
- Rotation modes require physics to be running and rocket to be controllable
- Target mode requires a selected target in the game
- Custom angles are specified in degrees (0-360)
- Offset values are added to the base rotation angle