# SFSControl Mod Server API Documentation

This project is an automation/remote control/information API server for SFS (Spaceflight Simulator), supporting features like transfer window ΔV, fuel, attitude, staging, and more.

---

## Getting Started

1. Build and run this mod, ensuring SFS is loaded.
2. The server listens by default at `http://127.0.0.1:27772/` (port can be customized).

---

## HTTP API List

### 1. Rocket & Scene Information

- `GET /rocket_sim?rocketIdOrName=xxx`  
  Get detailed info of the specified rocket (defaults to the current player rocket if not specified).

- `GET /rockets`  
  Get a list of all rockets in the scene (save info).

- `GET /planet?codename=xxx`  
  Get detailed info of the specified planet (defaults to the current player's planet if not specified).

- `GET /planets`  
  Get detailed info for all planets.

- `GET /other?rocketIdOrName=xxx`  
  Get miscellaneous info (transfer window ΔV, fuel bars, navigation target, timewarp, scene name, etc).

- `GET /mission`  
  Get current mission status and mission log.

- `GET /debuglog`  
  Get the game console log.

---

### 2. Control APIs (POST /control)

All control APIs use `POST /control` with a JSON body:
```json
{
  "method": "MethodName",
  "args": [param1, param2, ...]
}
```

#### Common API List

| Method            | Parameters (order/type)                                 | Description                  | Example Params                |
|-------------------|--------------------------------------------------------|------------------------------|-------------------------------|
| SetThrottle       | size (float), rocketIdOrName (optional)                 | Set throttle                 | 0.5                           |
| SetRCS            | on (bool), rocketIdOrName (optional)                    | Toggle RCS                   | true                          |
| Stage             | rocketIdOrName (optional)                               | Activate staging             |                               |
| Rotate            | isTarget (bool), angle (float), reference (str), direction (str), rocketIdOrName (str) | Rotate/point                 | false, 90, null, "left"       |
| StopRotate        | rocketIdOrName (optional)                               | Force stop rotation          |                               |
| UsePart           | partId (int), rocketIdOrName (optional)                 | Use part                     | 0                             |
| ClearDebris       | none                                                   | Clear debris                 |                               |
| Build             | blueprint (str, JSON)                                   | Build rocket                 | "{...}"                       |
| RcsThrust         | direction (str), seconds (float)                        | RCS thrust                   | "up", 5                       |
| SwitchToBuild     | none                                                   | Switch to build scene        |                               |
| ClearBlueprint    | none                                                   | Clear blueprint              |                               |
| SetRotation       | angle (float), rocketIdOrName (optional)                | Set rotation directly        | 90                            |
| SetState          | x, y, vx, vy, angularVelocity, blueprintJson, rocketIdOrName | Set rocket state           | 0,0,0,0,0,null                |
| Launch            | none                                                   | Launch rocket (build scene)  |                               |
| SwitchRocket      | idOrName (str/int)                                      | Switch controlled rocket     | "1"                           |
| RenameRocket      | idOrName (str/int), newName (str)                       | Rename rocket                | "1", "MyRocket"               |
| SetTarget         | nameOrIndex (str/int)                                   | Set navigation target        | "Earth"/1                     |
| ClearTarget       | none                                                   | Clear navigation target      |                               |
| TimewarpPlus      | none                                                   | Increase timewarp            |                               |
| TimewarpMinus     | none                                                   | Decrease timewarp            |                               |
| Wait              | isEncounter (bool, optional)                            | Wait for transfer/encounter window | true/false             |
| SetMainEngineOn   | on (bool), rocketIdOrName (optional)                    | Main engine on/off           | true                           |
| SetOrbit          | radius, eccentricity, trueAnomaly, counterclockwise, planetCode | Set orbit                | 7000000, 0, 0, true, "Earth"   |
| DeleteRocket      | idOrName (str/int)                                      | Delete rocket                | "1"                           |
| CompleteChallenge | challengeId (str)                                       | Complete challenge           | "Liftoff_0"                   |

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
