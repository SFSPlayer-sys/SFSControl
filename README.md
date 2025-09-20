# SFSControl Mod Server API Documentation

This project is an automation/remote control/information API server for SFS (Spaceflight Simulator), supporting features like transfer window ΔV, fuel, attitude, staging, and more.

---

## Getting Started

1. Build and run this mod, ensuring SFS is loaded.
2. The server listens by default at `http://127.0.0.1:27772/` (port can be customized).

---

## HTTP GET Information API List

| Path                | Parameters                                                                 | Description                                                      | Example                                                        |
|---------------------|----------------------------------------------------------------------------|------------------------------------------------------------------|----------------------------------------------------------------|
| `/rocket_sim`       | `rocketIdOrName` (string/int, optional)                                    | Get detailed info of the specified rocket (default: current)     | `/rocket_sim?rocketIdOrName=1`                                 |
| `/rockets`          | None                                                                       | Get a list of all rockets in the scene (save info)               | `/rockets`                                                     |
| `/planet`           | `codename` (string, optional)                                              | Get detailed info of the specified planet (default: current)     | `/planet?codename=Moon`                                        |
| `/planets`          | None                                                                       | Get detailed info for all planets                                | `/planets`                                                     |
| `/other`            | `rocketIdOrName` (string/int, optional)                                    | Get miscellaneous info (window ΔV, fuel bars, nav target, mass, thrust, maxThrust, TWR, inertia, etc.)  | `/other?rocketIdOrName=1`                                      |
| `/rocket`           | `rocketIdOrName` (string/int, optional)                                    | Get the save info of a specific rocket (default: current)        | `/rocket?rocketIdOrName=1`                                     |
| `/debuglog`         | None                                                                       | Get the game console log                                         | `/debuglog`                                                    |
| `/mission`          | None                                                                       | Get current mission status and mission log                       | `/mission`                                                     |
| `/planet_terrain`   | `planetCode` (string, required), `start` (double, optional, deg), `end` (double, optional, deg), `count` (int, optional) | Get an array of terrain heights for the specified planet, sampling from `start` to `end` degrees, with `count` samples. | `/planet_terrain?planetCode=Moon&start=0&end=180&count=100`    |
| `/screenshot`       | None                                                                       | Get a screenshot of the SFS game window (PNG format). Requires `allowScreenshot` to be enabled in settings. | `/screenshot`                                                 |

---

## HTTP POST Control API List

All control APIs use `POST /control` with a JSON body:
```json
{
  "method": "MethodName",
  "args": [param1, param2, ...]
}
```

| Method            | Parameters (order/type)                                 | Description                  | Example Params                |
|-------------------|--------------------------------------------------------|------------------------------|-------------------------------|
| SetThrottle       | size (float), rocketIdOrName (string/int, optional)     | Set throttle                 | 0.5, 0                        |
| SetRCS            | on (bool), rocketIdOrName (string/int, optional)        | Toggle RCS                   | true, 1                       |
| Stage             | rocketIdOrName (string/int, optional)                   | Activate staging             | 0                             |
| Rotate            | isTarget (bool), angle (float), reference (str), direction (str), rocketIdOrName (string/int, optional) | Rotate/point to target angle | false, 90, "", "left", 0    |
| StopRotate        | rocketIdOrName (string/int, optional), stopCoroutine (bool, optional) | Force stop rotation (with optional coroutine control) | 1, true                       |
| UsePart           | partId (int), rocketIdOrName (string/int, optional)     | Use part                     | 0, 1                          |
| ClearDebris       | none                                                   | Clear debris                 |                               |
| Build             | blueprint (str, JSON)                                   | Build rocket                 | "{...}"                       |
| RcsThrust         | direction (str), seconds (float), rocketIdOrName (string/int, optional) | RCS thrust                   | "up", 5, 0                   |
| SwitchToBuild     | none                                                   | Switch to build scene        |                               |
| ClearBlueprint    | none                                                   | Clear blueprint              |                               |
| SetRotation       | angle (float), rocketIdOrName (string/int, optional)    | Set rotation directly        | 90, 0                         |
| SetState          | x, y, vx, vy, angularVelocity, blueprintJson, rocketIdOrName (string/int, optional) | Set rocket state           | 0,0,0,0,0,"",0              |
| Launch            | -                                                    | Launch rocket from build scene | -                           |
| SwitchRocket      | idOrName (string/int)                                   | Switch controlled rocket     | 1                             |
| RenameRocket      | idOrName (string/int), newName (str)                    | Rename rocket                | 1, "MyRocket"                 |
| SetTarget         | nameOrIndex (string/int)                                | Set navigation target        | "Earth"/1                    |
| ClearTarget       | none                                                   | Clear navigation target      |                               |
| TimewarpPlus      | none                                                   | Increase timewarp            |                               |
| TimewarpMinus     | none                                                   | Decrease timewarp            |                               |
| SetTimewarp       | speed (double), realtimePhysics (bool, optional), showMessage (bool, optional)          | Set timewarp speed directly  | 1000.0, false, true                  |
| Wait              | mode (string, optional), parameter (double, optional) | Wait for transfer/rendezvous window or specific angle | "transfer", null / "angle", 90.0 |
| SetMainEngineOn   | on (bool), rocketIdOrName (string/int, optional)        | Main engine on/off           | true, 0                       |
| SetOrbit          | radius, eccentricity, trueAnomaly, counterclockwise, planetCode, rocketIdOrName (string/int, optional) | Set orbit                | 7000000, 0, 0, true, "Earth", 0   |
| DeleteRocket      | idOrName (string/int)                                   | Delete rocket                | 1                             |
| CompleteChallenge | challengeId (str)                                       | Complete challenge           | "Liftoff_0"                   |
| Track             | nameOrIndex (string/int)                                 | Set map focus to rocket or planet | "Moon"/0                  |
| SwitchMapView     | on (bool, optional)                                      | Switch between map and world view. true=map, false=world, omit to toggle | true/false/空 |
| Unfocus           | none                                                   | Unfocus map view (clear current focus)   |                               |
| TransferFuel      | fromTankId (int), toTankId (int), rocketIdOrName (string/int, optional) | Transfer fuel between tanks      | 0, 1, 0                       |
| StopFuelTransfer  | rocketIdOrName (string/int, optional)                   | Stop all fuel transfers         | 0                             |
| QuicksaveManager  | operation (str, optional), name (str)                    | Manage quicksaves (save/load/delete/rename)  | "save", "MySave"              |
| WheelControl      | enable (bool, optional), turnAxis (float, required), rocketIdOrName (string/int, optional) | Control rover wheel direction | true, 0.5, "" |
| SetMapIconColor   | rgbaValue (string), rocketIdOrName (string/int, optional) | Set rocket map icon color | "#FF0000", 0 |
| SetEngineGimbal   | partId (int), gimbalAngle (float), rocketIdOrName (string/int, optional) | Set engine gimbal angle (-1 to 1) | 0, 0.5, 0 |
| SetEngineGimbalOn | partId (int), gimbalOn (bool), rocketIdOrName (string/int, optional) | Enable/disable engine gimbal | 0, true, 0 |
| GetEngineGimbalInfo | partId (int), rocketIdOrName (string/int, optional) | Get engine gimbal information | 0, 0 |
| CreateRocket      | planetCode (string), blueprintJson (string), rocketName (string, optional), x (double, optional), y (double, optional), vx (double, optional), vy (double, optional), vr (double, optional) | Create rocket from blueprint at specified location | "Earth", "{...}", "MyRocket", 0, 0, 0, 0, 0 |
| CreateObject      | objectType (string), planetCode (string), x (double, optional), y (double, optional), objectName (string, optional), hidden (bool, optional), explosionSize (float, optional), createSound (bool, optional), createShake (bool, optional), rotation (float, optional), angularVelocity (float, optional), ragdoll (bool, optional), fuelPercent (double, optional), temperature (float, optional), flagDirection (int, optional), showFlagAnimation (bool, optional), createDamage (bool, optional) | Create various objects with full parameter control | "astronaut", "Earth", 0, 0, "MyAstronaut", false, 2.0, true, true, 0, 0, false, 1.0, 293.15, 1, true, true |
| ShowToast         | toast (str)                                             | Show in-game toast message   | "Hello World!"                |
| AddStage          | index (int), partIds (int[]), rocketIdOrName (string/int, optional) | Add stage to rocket      | 1, [0,1,2], 0                 |
| RemoveStage       | index (int), rocketIdOrName (string/int, optional)      | Remove stage from rocket     | 1, 0                          |
| LogMessage        | type (str), message (str)                               | Write to game log            | "log", "Debug info"           |
| SetCheat          | cheatName (str), enabled (bool)                         | Enable/disable cheat         | "infiniteFuel", true          |
| Revert            | type (str)                                              | Revert action                | "launch"/"30s"/"3min" |

---

### 3. Notes

- **All POST APIs require Content-Type: application/json**.
- All responses are JSON, containing a `result` field or detailed data.
- Some parameters are optional; if omitted, the current player rocket is used.
- **Important Note: When a parameter doesn't need a value, use empty string `""` instead of `null`!**
- `Wait` method supports multiple modes:
  - `"transfer"` (default): Wait for transfer window
  - `"rendezvous"`: Wait for rendezvous window  
  - Examples: `["transfer", null]`, `["rendezvous", null]`
  - **Note**: Requires navigation target to be set first using `SetTarget`
- `Rotate` method has been improved with more realistic physics:
  - Uses SFS's original `GetStopRotationTurnAxis` logic for precise control
  - Supports multiple references:
    - `"surface"`: Relative to planet surface normal angle (terrain normal)
    - `"orbit"`: Relative to orbital velocity direction reference frame
    - `""` (empty string) or other: Default to using the provided angle value
  - Direction supports left/right/auto.
  - Improved control strategy: proportional control for large errors, precise stop logic for small errors
- `SetOrbit` param: counterclockwise true=CCW, false=CW.
- `StopRotate` now supports optional coroutine control:
  - `stopCoroutine` (optional): true = stop rotation and coroutines (default), false = stop rotation only
  - Useful for temporary stops while keeping rotation coroutines running
- `/other`'s `fuelBarGroups` field matches the lower-left UI fuel bars.
- `/other`'s `transferWindowDeltaV` field is the transfer window ΔV, in m/s.
- `/other`'s `timewarpSpeed` field is the current timewarp speed multiplier (e.g., 1.0 = real-time, 1000.0 = 1000x speed).
- `/other`'s `mass` field is the total rocket mass, in tons.
- `/other`'s `thrust` field is the current total thrust, in tons.
- `/other`'s `maxThrust` field is the maximum possible thrust (at 100% throttle), in tons.
- `/other`'s `TWR` field is the thrust-to-weight ratio.
- `/other`'s `inertia` field contains detailed inertia information:
  - `inertia`: Moment of inertia
  - `angularVelocity`: Current angular velocity
  - `angularDrag`: Angular drag coefficient
  - `rotation`: Current rotation angle
  - `centerOfMass`: Center of mass coordinates (x, y)
- `/other` includes orbital information:
  - `distToApoapsis`: Distance to apoapsis in meters
  - `distToPeriapsis`: Distance to periapsis in meters
  - `timeToApoapsis`: Time to reach apoapsis in seconds
  - `timeToPeriapsis`: Time to reach periapsis in seconds
- `/planet` and `/planets` include detailed information:
  - `landmarks`: Landmark information with name, displayName, angles, etc.
  - `mapColor`: Planet map color with RGB values and hex code
  - `orbit`: Orbital information including:
    - `eccentricity`: Orbital eccentricity
    - `semiMajorAxis`: Semi-major axis in meters
    - `argumentOfPeriapsis`: Argument of periapsis in degrees
    - `direction`: Orbital direction (1 for prograde, -1 for retrograde)
    - `currentTrueAnomaly`: Current true anomaly in degrees
    - `currentPositionAngle`: Current position angle in degrees
    - `currentVelocityAngle`: Current velocity angle in degrees
    - `currentRadius`: Current distance from parent body in meters
    - `currentVelocity`: Current orbital velocity in m/s
- `QuicksaveManager` operations:
  - `"save"` (default): Save current game state with given name
  - `"load"`: Load quicksave with given name
  - `"delete"`: Delete quicksave with given name
  - `"rename"`: Rename quicksave, format: `"oldName:newName"`
- `SetTimewarp` parameters:
  - `realtimePhysics` (optional): true = real-time physics simulation, false = rail simulation (default: false)
  - `showMessage` (optional): show in-game message (default: true)
  - Auto logic: If `realtimePhysics` is not specified (default false), 0-5x speed defaults to real-time, others default to rail simulation
- `WheelControl` parameters:
  - `enable` (optional): Enable/disable all wheels on the rocket ("" = keep current state)
  - `turnAxis` (required): Set wheel turn axis (-1.0 to 1.0, where -1 = left, 0 = straight, 1 = right)
  - `rocketIdOrName` (optional): Rocket ID or name to control (default: current rocket)
- `SetMapIconColor` parameters:
  - `rgbaValue` (required): Color value in hex (#RRGGBB, #RRGGBBAA) or comma-separated (R,G,B,A) format
  - `rocketIdOrName` (optional): Rocket ID or name to control (default: current rocket)
  - Supported formats:
    - Hex: "#FF0000" (red), "#00FF0080" (green with 50% alpha)
    - RGBA: "1.0,0.0,0.0,0.5" (red with 50% alpha), "0,1,0" (green, full alpha)
- Engine Gimbal Control:
  - `SetEngineGimbal`: Set engine gimbal angle (-1.0 to 1.0, where -1 = max left, 0 = center, 1 = max right)
  - `SetEngineGimbalOn`: Enable/disable engine gimbal functionality
  - `GetEngineGimbalInfo`: Get detailed engine gimbal information including current angle, status, thrust, etc.
  - All gimbal methods require `partId` (engine part index) and optional `rocketIdOrName`
- `CreateRocket` parameters:
  - `planetCode` (required): Planet codename where rocket will be created (e.g., "Earth", "Moon")
  - `blueprintJson` (required): JSON string containing rocket blueprint data
  - `rocketName` (optional): Custom name for the rocket (default: auto-generated name like "Rocket_0")
  - `x`, `y` (optional): Position coordinates relative to planet center (default: 0, 0)
  - `vx`, `vy` (optional): Initial velocity components (default: 0, 0)
  - `vr` (optional): Initial angular velocity (default: 0)
  - Blueprint format: JSON with parts array, stages array, and other rocket configuration data
- `CreateObject` parameters:
  - `objectType` (required): Type of object to create (see supported types below)
  - `planetCode` (required): Planet codename where object will be created
  - `x`, `y` (optional): Position coordinates relative to planet center (default: 0, 0)
  - `objectName` (optional): Custom name for the object (default: auto-generated name)
  - `hidden` (optional): Whether to create the object as hidden/inactive (default: false)
  - `explosionSize` (optional): Size/strength of explosion effect (default: 2.0, only for explosion type). **This now controls REAL part destruction radius - explosionSize × 5 = damage radius**
- `createSound` (optional): Whether to create explosion sound (default: true, only for explosion type)
  - `createShake` (optional): Whether to create camera shake effect (default: true, only for explosion type)
  - `createDamage` (optional): Whether to cause actual part damage (default: true, only for explosion type). **Set to false for visual-only explosions**
  - `rotation` (optional): Initial rotation in degrees (default: 0, mainly for astronaut)
  - `angularVelocity` (optional): Initial angular velocity (default: 0, mainly for astronaut)
  - `ragdoll` (optional): Whether astronaut starts in ragdoll state (default: false, only for astronaut)
  - `fuelPercent` (optional): Astronaut fuel percentage 0.0-1.0 (default: 1.0, only for astronaut)
  - `temperature` (optional): Astronaut temperature in Kelvin (default: 293.15K, only for astronaut)
  - `flagDirection` (optional): Flag direction -1 (left) or 1 (right) (default: 1, only for flag)
  - `showFlagAnimation` (optional): Whether to show flag planting animation (default: true, only for flag)
  - Supported object types:
    - `"astronaut"`, `"eva"`: Astronaut in EVA state with full physics control
    - `"flag"`: Flag objects with direction and animation control
    - `"explosion"`, `"explosionparticle"`: **Configurable explosions** - Creates visual effects and optionally destroys/disconnects rocket parts within the explosion radius. Use `createDamage` parameter to control whether parts are actually damaged (true) or just visual effects (false).
- `Revert` parameters:
  - `"launch"`: Revert to launch state
  - `"30s"`: Revert to 30 seconds ago
  - `"3min"`: Revert to 3 minutes ago

---

## Example: Call Rotate API

```bash
curl -X POST http://127.0.0.1:27772/control \
  -H "Content-Type: application/json" \
  -d '{"method":"Rotate","args":[false, 90, "", "left"]}'
```



## Example: Get Transfer Window ΔV

```bash
curl http://127.0.0.1:27772/other
```

## Example: Wait for Specific Angle

```bash
# Wait for 90 degrees
curl -X POST http://127.0.0.1:27772/control \
  -H "Content-Type: application/json" \
  -d '{"method":"Wait","args":["angle", 90.0]}'

# Wait for transfer window
curl -X POST http://127.0.0.1:27772/control \
  -H "Content-Type: application/json" \
  -d '{"method":"Wait","args":["transfer", null]}'
```

---

## License & Open Source

- This project is licensed under GPL-3.0, see [LICENSE](LICENSE)
- More info and source: [https://github.com/SFSPlayer-sys/SFSControl](https://github.com/SFSPlayer-sys/SFSControl)