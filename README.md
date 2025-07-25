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
| `/other`            | `rocketIdOrName` (string/int, optional)                                    | Get miscellaneous info (window ΔV, fuel bars, nav target, etc.)  | `/other?rocketIdOrName=1`                                      |
| `/rocket`           | `rocketIdOrName` (string/int, optional)                                    | Get the save info of a specific rocket (default: current)        | `/rocket?rocketIdOrName=1`                                     |
| `/debuglog`         | None                                                                       | Get the game console log                                         | `/debuglog`                                                    |
| `/mission`          | None                                                                       | Get current mission status and mission log                       | `/mission`                                                     |
| `/planet_terrain`   | `planetCode` (string, required), `start` (double, optional, deg), `end` (double, optional, deg), `count` (int, optional) | Get an array of terrain heights for the specified planet, sampling from `start` to `end` degrees, with `count` samples. | `/planet_terrain?planetCode=Moon&start=0&end=180&count=100`    |

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
| Rotate            | isTarget (bool), angle (float), reference (str), direction (str), rocketIdOrName (string/int, optional) | Rotate/point                 | false, 90, null, "left", 0    |
| StopRotate        | rocketIdOrName (string/int, optional)                   | Force stop rotation          | 1                             |
| UsePart           | partId (int), rocketIdOrName (string/int, optional)     | Use part                     | 0, 1                          |
| ClearDebris       | none                                                   | Clear debris                 |                               |
| Build             | blueprint (str, JSON)                                   | Build rocket                 | "{...}"                       |
| RcsThrust         | direction (str), seconds (float), rocketIdOrName (string/int, optional) | RCS thrust                   | "up", 5, 0                   |
| SwitchToBuild     | none                                                   | Switch to build scene        |                               |
| ClearBlueprint    | none                                                   | Clear blueprint              |                               |
| SetRotation       | angle (float), rocketIdOrName (string/int, optional)    | Set rotation directly        | 90, 0                         |
| SetState          | x, y, vx, vy, angularVelocity, blueprintJson, rocketIdOrName (string/int, optional) | Set rocket state           | 0,0,0,0,0,null,0              |
| Launch            | rocketIdOrName (string/int, optional)                   | Launch rocket (build scene)  | 0                             |
| SwitchRocket      | idOrName (string/int)                                   | Switch controlled rocket     | 1                             |
| RenameRocket      | idOrName (string/int), newName (str)                    | Rename rocket                | 1, "MyRocket"                 |
| SetTarget         | nameOrIndex (string/int)                                | Set navigation target        | "Earth"/1                    |
| ClearTarget       | none                                                   | Clear navigation target      |                               |
| TimewarpPlus      | none                                                   | Increase timewarp            |                               |
| TimewarpMinus     | none                                                   | Decrease timewarp            |                               |
| Wait              | mode (string: transfer/window/encounter, optional)      | Wait for transfer/rendezvous/encounter window | "encounter"/"window"/"transfer" |
| SetMainEngineOn   | on (bool), rocketIdOrName (string/int, optional)        | Main engine on/off           | true, 0                       |
| SetOrbit          | radius, eccentricity, trueAnomaly, counterclockwise, planetCode, rocketIdOrName (string/int, optional) | Set orbit                | 7000000, 0, 0, true, "Earth", 0   |
| DeleteRocket      | idOrName (string/int)                                   | Delete rocket                | 1                             |
| CompleteChallenge | challengeId (str)                                       | Complete challenge           | "Liftoff_0"                   |
| SetFocus          | nameOrIndex (string/int)                                | Set map focus to rocket or planet | "Moon"/0                  |

---

### Additional/Undocumented HTTP APIs

| Path/Method         | Description                                                                 | Parameters/Body Example                                                                                 |
|---------------------|-----------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------|
| **GET /rocket**     | Get the save info of a specific rocket. If not specified, returns current.  | Query: `rocketIdOrName` (string or int, optional) <br> Example: `/rocket?rocketIdOrName=1`              |
| **POST /rcall**     | Reflective call: invoke any public static method. **Use with caution!**     | JSON body:<br> `{ "type": "Full.Type.Name", "methodName": "Method", "callArgs": [arg1, arg2, ...] }`    |

#### Additional POST /control methods (not in original doc)

| Method           | Description (EN)                                                                 | Example args / Notes                                              |
|------------------|----------------------------------------------------------------------------------|-------------------------------------------------------------------|
| ShowToast        | Show an in-game toast message (popup notification).                              | `['Hello World!']`                                                |
| AddStage         | Add a stage to the rocket.                                                       | `[stageIndex (int), partIds (int[])]`                             |
| RemoveStage      | Remove a stage from the rocket.                                                  | `[stageIndex (int)]`                                              |
| LogMessage       | Write a message to the in-game log.                                              | `[tag (string), message (string)]`                                |
| SetCheat         | Enable or disable a cheat option.                                                | `[cheatName (string), enabled (bool)]`                            |
| Revert           | Revert a previous action (such as undoing a build step).                         | `[actionName (string)]`                                           |

---

### 3. Notes

- **All POST APIs require Content-Type: application/json**.
- All responses are JSON, containing a `result` field or detailed data.
- Some parameters are optional; if omitted, the current player rocket is used.
- `Wait`'s `isEncounter` param: true waits for encounter window, false for transfer window.
- `Rotate` supports multiple references (surface/orbit/custom), direction supports left/right/auto.
- `SetOrbit` param: counterclockwise true=CCW, false=CW.
- `/other`'s `fuelBarGroups` field matches the lower-left UI fuel bars.
- `/other`'s `transferWindowDeltaV` field is the transfer window ΔV, in m/s.

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
