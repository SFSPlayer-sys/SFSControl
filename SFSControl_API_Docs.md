

















# SFSControl API Documentation

**Base URL:** `http://127.0.0.1:27772`

---

## Table of Contents

1. [GET Endpoints](#1-get-endpoints)
2. [POST Endpoints](#2-post-endpoints)
3. [Control Methods](#3-control-methods)
4. [Object Types](#4-object-types)
5. [Cheat Names](#5-cheat-names)
6. [Revert Types](#6-revert-types)
7. [Challenges](#7-challenges)
8. [Examples](#8-examples)

---

## 1. GET Endpoints

### `/version`

Get mod version information.

**Response:**
```json
{
  "name": "SFSControl",
  "version": "1.0.0"
}
```

---

### `/rockets`

Get list of all rockets (save files).

**Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `folder` | string | No | Folder path (default: current save folder) |

**Response:**
```json
{
  "rockets": [
    { "name": "Rocket1", "folder": "Saves/Default" },
    { "name": "My Rocket", "folder": "Saves/Custom" }
  ]
}
```

---

### `/rocket_sim`

Get rocket simplified information.

**Response:**
```json
{
  "name": "Rocket",
  "id": 0,
  "height": 50000,
  "position": { "x": 1000, "y": 500 },
  "throttle": 0.5,
  "orbit": {
    "apoapsis": 80000,
    "periapsis": 50000
  }
}
```

---

### `/planet`

Get current planet information.

**Response:**
```json
{
  "codeName": "Kerbin",
  "displayName": "Kerbin",
  "radius": 600000,
  "mass": 1.2e24,
  "hasAtmospherePhysics": true,
  "hasAtmosphereVisuals": true,
  "hasTerrain": true,
  "hasWater": true,
  "hasRings": false,
  "hasPostProcessing": true,
  "hasOrbit": true,
  "atmosphereHeight": 50000,
  "parentBody": null,
  "landmarks": [...],
  "mapColor": { "r": 1, "g": 1, "b": 1, "a": 1 },
  "orbit": {...},
  "water": {...},
  "rings": null,
  "frontClouds": null
}
```

---

### `/planets`

Get all planets in the solar system.

**Response:** Array of planet objects (same structure as `/planet`).

---

### `/other`

Get miscellaneous information.

**Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `rocketIdOrName` | string | No | Rocket ID or name |

**Response:**
```json
{
  "targetAngle": 90.5,
  "transferWindowDeltaV": 1234.56,
  "worldTime": 12345.67,
  "timewarpSpeed": 4,
  "sceneName": "World"
}
```

---

### `/rocket`

Get detailed rocket save info (requires rocket ID or name).

**Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `idOrName` | string | Yes | Rocket ID or name |

---

### `/debuglog`

Get console log messages.

**Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `maxLines` | int | No | Max lines to return (default: 150) |

**Response:**
```json
{
  "logs": ["[Server] Started", "[Info] Rocket created"]
}
```

---

### `/mission`

Get current mission info and logs.

**Response:**
```json
{
  "missionStatus": { "completed": false, "progress": 50 },
  "missionLog": [
    { "text": "Reached orbit", "reward": 100, "logId": "1" }
  ]
}
```

---

### `/planet_terrain`

Get terrain height profile for a planet.

**Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `planetCode` | string | No | Planet code (default: current planet) |
| `start` | double | No | Start degree (default: 0) |
| `end` | double | No | End degree (default: 360) |
| `count` | int | No | Sample count (default: 360) |
| `clampToWater` | bool | No | Clamp heights to water level (default: true) |

**Response:**
```json
{
  "heights": [600000, 601000, 605000, ...]
}
```

---

### `/screenshot`

Capture game screenshot (requires permission in settings).

**Response:** PNG image binary data.

---

## 2. POST Endpoints

### `/draw`

Draw graphics on screen.

**Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `cmd` | string | Yes | Command: `line`, `circle`, `clear` |
| `start` | array | No | Line start [x, y] or [x, y, z] |
| `end` | array | No | Line end [x, y] or [x, y, z] |
| `center` | array | No | Circle center [x, y] |
| `radius` | double | No | Circle radius |
| `color` | array | No | RGBA [r, g, b, a] (0-1 range) |
| `width` | float | No | Line width |
| `layer` | float | No | Sorting layer |

**Examples:**

Draw line:
```json
{
  "cmd": "line",
  "start": [0, 0],
  "end": [1000, 1000],
  "color": [1, 0, 0, 1],
  "width": 2
}
```

Draw circle:
```json
{
  "cmd": "circle",
  "center": [500000, 500000],
  "radius": 50000,
  "color": [0, 1, 0, 0.5]
}
```

Clear:
```json
{ "cmd": "clear" }
```

---

### `/control`

Execute control methods. See [Control Methods](#3-control-methods) below.

**Body:**
```json
{
  "method": "MethodName",
  "args": ["arg1", "arg2"]
}
```

**Response:**
```json
{ "result": "Success" }
```
or
```json
{ "result": "Error: Something went wrong" }
```

---

### `/cors_settings`

Get or update CORS settings.

**GET Response:**
```json
{
  "enableCORS": true,
  "allowedOrigins": "*",
  "allowedMethods": "GET,POST,PUT,DELETE,OPTIONS",
  "allowedHeaders": "Content-Type,Authorization"
}
```

**POST Body:**
```json
{
  "enableCORS": true,
  "allowedOrigins": "http://localhost:3000"
}
```

---

## 3. Control Methods

### Throttle & RCS

#### `SetThrottle`
Set engine throttle percentage.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `size` | double | Yes | Throttle 0-1 |
| `rocketIdOrName` | string | No | Target rocket |

**Example:** `{"method": "SetThrottle", "args": [0.5, "0"]}`

---

#### `SetRCS`
Toggle RCS thrusters.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `on` | bool | Yes | RCS on/off |
| `rocketIdOrName` | string | No | Target rocket |

**Example:** `{"method": "SetRCS", "args": [true, "0"]}`

---

#### `SetMainEngineOn`
Toggle main engine.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `on` | bool | Yes | Engine on/off |
| `rocketIdOrName` | string | No | Target rocket |

**Example:** `{"method": "SetMainEngineOn", "args": [true]}`

---

### Staging & Launch

#### `Stage`
Activate next stage.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `rocketIdOrName` | string | No | Target rocket |

**Example:** `{"method": "Stage", "args": []}`

---

#### `Launch`
Launch rocket from launchpad.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `rocketIdOrName` | string | No | Target rocket |

**Example:** `{"method": "Launch", "args": []}`

---

### Rotation

#### `Rotate`
Rotate rocket to target direction.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `modeOrAngle` | string/double | No | Direction mode or angle (degrees) |
| `offset` | float | No | Angle offset |
| `rocketIdOrName` | string | No | Target rocket |

**Direction Modes:**
| Mode | Description |
|------|-------------|
| `Prograde` | Along orbit direction |
| `Retrograde` | Against orbit direction |
| `Normal` | Above orbit plane (+Z) |
| `AntiNormal` | Below orbit plane (-Z) |
| `Radial` | Toward planet center |
| `AntiRadial` | Away from planet center |
| `Surface` | Point up from surface |
| `Target` | Point toward nav target |
| `Default` | Reset SAS |
| `None` | Disable SAS |

**Extended Modes:**
- `rocket:<idOrName>` - Point toward another rocket
- `planet:<codeName>` - Point toward planet center
- `coord:<x>,<y>[,<planetCode>]` - Point toward coordinate

**Examples:**
```json
{"method": "Rotate", "args": ["Prograde"]}
{"method": "Rotate", "args": [90]}
{"method": "Rotate", "args": ["rocket:0"]}
{"method": "Rotate", "args": ["coord:1000,500"]}
```

---

#### `StopRotate`
Stop current rotation.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `rocketIdOrName` | string | No | Target rocket |

**Example:** `{"method": "StopRotate", "args": []}`

---

### Navigation

#### `SetTarget`
Set navigation target.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `targetNameOrIndex` | string | Yes | Target name or index |

**Example:** `{"method": "SetTarget", "args": ["Kerbin"]}`

---

#### `ClearTarget`
Clear navigation target.

**Example:** `{"method": "ClearTarget", "args": []}`

---

#### `Track`
Focus map on target.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `nameOrIndex` | string | No | Planet or rocket name/index |

**Example:** `{"method": "Track", "args": ["0"]}`

---

#### `Unfocus`
Clear map focus.

**Example:** `{"method": "Unfocus", "args": []}`

---

### Time Control

#### `TimewarpPlus`
Increase time warp.

**Example:** `{"method": "TimewarpPlus", "args": []}`

---

#### `TimewarpMinus`
Decrease time warp.

**Example:** `{"method": "TimewarpMinus", "args": []}`

---

#### `SetTimewarp`
Set specific time warp speed.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `speed` | double | Yes | Time warp multiplier (0-5) |
| `realtimePhysics` | bool | No | Use realtime physics |

**Example:** `{"method": "SetTimewarp", "args": [4]}`

---

### Rocket Management

#### `SwitchRocket`
Switch to another rocket.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `rocketId` | string | Yes | Rocket ID or name |

**Example:** `{"method": "SwitchRocket", "args": ["1"]}`

---

#### `RenameRocket`
Rename a rocket.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `rocketIdOrName` | string | Yes | Rocket to rename |
| `newName` | string | Yes | New name |

**Example:** `{"method": "RenameRocket", "args": ["0", "New Rocket Name"]}`

---

#### `DeleteRocket`
Delete a rocket.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `idOrName` | string | No | Rocket to delete (default: first non-player rocket) |

**Example:** `{"method": "DeleteRocket", "args": ["1"]}`

---

### Fuel & Resources

#### `TransferFuel`
Transfer fuel between stages.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `direction` | string | Yes | `top` or `bottom` |
| `percentage` | double | No | Transfer percentage (0-1) |
| `rocketIdOrName` | string | No | Target rocket |

**Example:** `{"method": "TransferFuel", "args": ["top", 0.5]}`

---

#### `StopFuelTransfer`
Stop fuel transfer.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `rocketIdOrName` | string | No | Target rocket |

**Example:** `{"method": "StopFuelTransfer", "args": []}`

---

### Wheels

#### `WheelControl`
Control rover wheels.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `enable` | bool | No | Enable/disable wheels |
| `turnAxis` | float | No | Steering -1 to 1 |
| `rocketIdOrName` | string | No | Target rocket |

**Example:** `{"method": "WheelControl", "args": [true, 0.5]}`

---

### Quicksave

#### `QuicksaveManager`
Manage quicksaves.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `operation` | string | No | `save`, `load`, or `delete` |
| `name` | string | No | Save name |

**Example:** `{"method": "QuicksaveManager", "args": ["save", "mySave"]}`

---

### Cheats

#### `SetCheat`
Toggle cheat options.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `cheatName` | string | Yes | Cheat name |
| `enabled` | bool | Yes | Enable/disable |

See [Cheat Names](#5-cheat-names) for valid values.

**Example:** `{"method": "SetCheat", "args": ["infiniteFuel", true]}`

---

### Revert

#### `Revert`
Revert to previous state.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `type` | string | Yes | Revert type |

See [Revert Types](#6-revert-types) for valid values.

**Example:** `{"method": "Revert", "args": ["30s"]}`

---

### Challenges

#### `CompleteChallenge`
Mark a challenge as complete.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `challengeId` | string | Yes | Challenge ID |

See [Challenges](#7-challenges) for valid values.

**Example:** `{"method": "CompleteChallenge", "args": ["Orbit_High"]}`

---

### Orbit

#### `SetOrbit`
Set rocket to specific orbit.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `radius` | double | Yes | Orbital radius (meters) |
| `eccentricity` | double | No | Eccentricity (0-0.99) |
| `trueAnomaly` | double | No | True anomaly in degrees (0 = apoapsis at +X) |
| `counterclockwise` | bool | No | Orbit direction |
| `planetCode` | string | No | Planet code |
| `rocketIdOrName` | string | No | Target rocket |

**Example:** `{"method": "SetOrbit", "args": [700000, 0, 0, true, "Kerbin", "0"]}`

---

### Map

#### `SwitchMapView`
Toggle between map and world view.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `on` | bool | No | `true` = map, `false` = world, `null` = toggle |

**Example:** `{"method": "SwitchMapView", "args": [true]}`

---

#### `SetMapIconColor`
Set rocket icon color on map.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `rgbaValue` | string | Yes | `#RRGGBBAA` or `R,G,B,A` |
| `rocketIdOrName` | string | No | Target rocket |

**Example:** `{"method": "SetMapIconColor", "args": ["#FF0000FF", "0"]}`

---

### Create Objects (DEPRECATED)

**DEPRECATED - Will be removed in future versions.**

Creates objects like astronauts, flags, or explosions.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `objectType` | string | Yes | `astronaut`, `flag`, `explosion` |
| `planetCode` | string | No | Planet code (default: current planet) |
| `x` | double | No | X position |
| `y` | double | No | Y position |
| `objectName` | string | No | Object name |

See [Object Types](#4-object-types) for valid values.

**Example:** `{"method": "CreateObject", "args": ["astronaut", null, 1000, 500]}`

---

### Build

#### `Build`
Load blueprint into build scene.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `blueprintInfo` | string | Yes | Blueprint JSON |

**Example:** `{"method": "Build", "args": ["<blueprint_json>"]}`

---

#### `SwitchToBuild`
Switch to build scene.

**Example:** `{"method": "SwitchToBuild", "args": []}`

---

#### `ClearBlueprint`
Clear current blueprint.

**Example:** `{"method": "ClearBlueprint", "args": []}`

---

### Logging

#### `LogMessage`
Output message to game console.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `level` | string | Yes | `log`, `warning`, or `error` |
| `message` | string | Yes | Message text |

**Example:** `{"method": "LogMessage", "args": ["log", "Hello World"]}`

---

### Other

#### `ShowToast`
Show toast message on screen.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `toast` | string | Yes | Message to display |

**Example:** `{"method": "ShowToast", "args": ["Mission Complete!"]}`

---

#### `CreateRocket`
Create new rocket in world.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `planetCode` | string | No | Planet code |
| `blueprintJson` | string | No | Blueprint JSON |
| `rocketName` | string | No | Rocket name |
| `x` | double | No | X position |
| `y` | double | No | Y position |
| `vx` | double | No | X velocity |
| `vy` | double | No | Y velocity |
| `vr` | double | No | Angular velocity |

**Example:** `{"method": "CreateRocket", "args": ["Kerbin", null, "New Rocket", 1000, 600000, 0, 500, 0]}`

---

## 4. Object Types

For `CreateObject` method:

| Type | Description |
|------|-------------|
| `astronaut` | Create astronaut EVA |
| `eva` | Same as astronaut |
| `flag` | Plant a flag |
| `explosion` | Create explosion effect |
| `explosionparticle` | Create explosion without sound |

---

## 5. Cheat Names

For `SetCheat` method:

| Cheat Name | Description |
|------------|-------------|
| `infiniteFuel` | Unlimited fuel |
| `infiniteBuildArea` | Unlimited build area |
| `noGravity` | Disable gravity |
| `noAtmosphericDrag` | No atmospheric drag |
| `noHeatDamage` | No heat damage |
| `noBurnMarks` | No burn marks on parts |
| `partClipping` | Allow part clipping |
| `unbreakableParts` | Parts cannot break |

---

## 6. Revert Types

For `Revert` method:

| Type | Description |
|------|-------------|
| `launch` | Revert to pre-launch state |
| `30s` | Revert 30 seconds |
| `3min` | Revert 3 minutes |
| `build` | Revert to build scene |

---

## 7. Challenges

For `CompleteChallenge` method:

| Challenge ID | Description |
|--------------|-------------|
| `Liftoff_0` | Liftoff |
| `Reach_10km` | Reach 10km altitude |
| `Reach_30km` | Reach 30km altitude |
| `Reach_Downrange` | Reach 100km downrange |
| `Reach_Orbit` | Reach low Earth orbit |
| `Orbit_High` | Reach high Earth orbit |
| `Moon_Orbit` | Orbit the Moon |
| `Moon_Tour` | Visit 3 Moon landmarks |
| `Asteroid_Crash` | Asteroid crash |
| `Mars_Tour` | Mars tour |
| `Venus_One_Way` | Venus one-way |
| `Venus_Landing` | Venus landing and return |
| `Mercury_One_Way` | Mercury one-way |
| `Mercury_Landing` | Mercury landing and return |

---