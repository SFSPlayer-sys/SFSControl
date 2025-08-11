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
| `/other`            | `rocketIdOrName` (string/int, optional)                                    | Get miscellaneous info (window ΔV, fuel bars, nav target, mass, thrust, TWR, etc.)  | `/other?rocketIdOrName=1`                                      |
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
| Rotate            | isTarget (bool), angle (float), reference (str), direction (str), rocketIdOrName (string/int, optional) | Rotate/point to target angle | false, 90, null, "left", 0    |
| StopRotate        | rocketIdOrName (string/int, optional)                   | Force stop rotation          | 1                             |
| UsePart           | partId (int), rocketIdOrName (string/int, optional)     | Use part                     | 0, 1                          |
| ClearDebris       | none                                                   | Clear debris                 |                               |
| Build             | blueprint (str, JSON)                                   | Build rocket                 | "{...}"                       |
| RcsThrust         | direction (str), seconds (float), rocketIdOrName (string/int, optional) | RCS thrust                   | "up", 5, 0                   |
| SwitchToBuild     | none                                                   | Switch to build scene        |                               |
| ClearBlueprint    | none                                                   | Clear blueprint              |                               |
| SetRotation       | angle (float), rocketIdOrName (string/int, optional)    | Set rotation directly        | 90, 0                         |
| SetState          | x, y, vx, vy, angularVelocity, blueprintJson, rocketIdOrName (string/int, optional) | Set rocket state           | 0,0,0,0,0,null,0              |
| Launch            | -                                                    | Launch rocket from build scene | -                           |
| SwitchRocket      | idOrName (string/int)                                   | Switch controlled rocket     | 1                             |
| RenameRocket      | idOrName (string/int), newName (str)                    | Rename rocket                | 1, "MyRocket"                 |
| SetTarget         | nameOrIndex (string/int)                                | Set navigation target        | "Earth"/1                    |
| ClearTarget       | none                                                   | Clear navigation target      |                               |
| TimewarpPlus      | none                                                   | Increase timewarp            |                               |
| TimewarpMinus     | none                                                   | Decrease timewarp            |                               |
| SetTimewarp       | speed (double), realtimePhysics (bool, optional), showMessage (bool, optional)          | Set timewarp speed directly  | 1000.0, false, true                  |
| Wait              | mode (string: transfer/rendezvous, optional)      | Wait for transfer/rendezvous window | "rendezvous"/"transfer" |
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
| WheelControl      | enable (bool, optional), turnAxis (float, required), rocketIdOrName (string/int, optional) | Control rover wheel direction | true, 0.5, null |
| ShowToast         | toast (str)                                             | Show in-game toast message   | "Hello World!"                |
| AddStage          | index (int), partIds (int[]), rocketIdOrName (string/int, optional) | Add stage to rocket      | 1, [0,1,2], 0                 |
| RemoveStage       | index (int), rocketIdOrName (string/int, optional)      | Remove stage from rocket     | 1, 0                          |
| LogMessage        | type (str), message (str)                               | Write to game log            | "log", "Debug info"           |
| SetCheat          | cheatName (str), enabled (bool)                         | Enable/disable cheat         | "infiniteFuel", true          |
| Revert            | type (str)                                              | Revert action                | "launch"/"30s"/"3min"/"build" |

---

### 3. Notes

- **All POST APIs require Content-Type: application/json**.
- All responses are JSON, containing a `result` field or detailed data.
- Some parameters are optional; if omitted, the current player rocket is used.
- `Wait`'s mode param: "rendezvous" waits for rendezvous window, "transfer" (default) waits for transfer window.
- `Rotate` supports multiple references:
  - `"surface"`: 相对于行星表面的参考系（指向行星中心）
  - `"orbit"`: 相对于轨道速度方向的参考系
  - `null` or other: 默认使用提供的角度值
  - Direction supports left/right/auto.
- `SetOrbit` param: counterclockwise true=CCW, false=CW.
- `/other`'s `fuelBarGroups` field matches the lower-left UI fuel bars.
- `/other`'s `transferWindowDeltaV` field is the transfer window ΔV, in m/s.
- `/other`'s `timewarpSpeed` field is the current timewarp speed multiplier (e.g., 1.0 = real-time, 1000.0 = 1000x speed).
- `/other`'s `mass` field is the total rocket mass, in tons.
- `/other`'s `thrust` field is the current total thrust, in tons.
- `/other`'s `TWR` field is the thrust-to-weight ratio.
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
  - `enable` (optional): Enable/disable all wheels on the rocket (null = keep current state)
  - `turnAxis` (required): Set wheel turn axis (-1.0 to 1.0, where -1 = left, 0 = straight, 1 = right)
  - `rocketIdOrName` (optional): Rocket ID or name to control (default: current rocket)

---

## Example: Call Rotate API

```bash
curl -X POST http://127.0.0.1:27772/control \
  -H "Content-Type: application/json" \
  -d '{"method":"Rotate","args":[false, 90, null, "left"]}'
```



## Example: Get Transfer Window ΔV

```bash
curl http://127.0.0.1:27772/other
```

---

## License & Open Source

- This project is licensed under GPL-3.0, see [LICENSE](LICENSE)
- More info and source: [https://github.com/SFSPlayer-sys/SFSControl](https://github.com/SFSPlayer-sys/SFSControl)