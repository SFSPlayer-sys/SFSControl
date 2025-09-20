using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using SFS.World;
using SFS.Parts;
using SFS;
using SFS.WorldBase;
using SFS.Parts.Modules;
using Newtonsoft.Json;
using SFS.Builds;
using System.IO;
using System;
using SFS.IO;
using ModLoader.IO;

namespace SFSControl
{
    public static class Info
    {
        // 获取当前火箭的详细信息
        public static Dictionary<string, object> GetRocketInfo(string rocketIdOrName = null)
        {
            Rocket rocket = null;
            if (!string.IsNullOrEmpty(rocketIdOrName))
            {
                if (int.TryParse(rocketIdOrName, out int idx))
                {
                    if (GameManager.main?.rockets != null && idx >= 0 && idx < GameManager.main.rockets.Count)
                        rocket = GameManager.main.rockets[idx];
                }
                else if (GameManager.main?.rockets != null)
                {
                    rocket = GameManager.main.rockets.FirstOrDefault(r => r != null && r.rocketName != null && r.rocketName.Equals(rocketIdOrName, StringComparison.OrdinalIgnoreCase));
                }
            }
            if (rocket == null && PlayerController.main?.player?.Value is Rocket curRocket)
                rocket = curRocket;
            if (rocket != null)
            {
                return GetRocketInfoDict(rocket);
            }
            return new Dictionary<string, object> { { "error", "Player is not controlling a rocket or rocket data is not available" } };
        }

        // 获取所有在场景中的火箭的详细信息列表
        public static List<Dictionary<string, object>> GetAllRocketsInfo()
        {
            var allRocketsInfo = new List<Dictionary<string, object>>();
            if (GameManager.main?.rockets == null)
            {
                return allRocketsInfo;
            }

            foreach (var rocket in GameManager.main.rockets)
            {
                allRocketsInfo.Add(GetRocketInfoDict(rocket));
            }
            return allRocketsInfo;
        }

        private static Dictionary<string, object> GetRocketInfoDict(Rocket rocket)
        {
            if (rocket == null)
                return new Dictionary<string, object> { { "error", "Rocket data is null" } };

            var parts = rocket.partHolder?.parts;
            var partIdMap = new Dictionary<Part, int>();
            if (parts != null)
            {
                for (int i = 0; i < parts.Count; i++)
                    if (parts[i] != null)
                        partIdMap[parts[i]] = i;
            }

            // 分级信息
            var stagingInfo = new List<Dictionary<string, object>>();
            if (rocket.staging?.stages != null)
            {
                foreach (var stage in rocket.staging.stages)
                {
                    if (stage == null) continue;
                    stagingInfo.Add(new Dictionary<string, object>
                    {
                        { "stageId", stage.stageId },
                        { "partIds", stage.parts?.Where(p => p != null && partIdMap.ContainsKey(p)).Select(p => partIdMap[p]).ToList() ?? new List<int>() }
                    });
                }
            }

            // 部件温度列表
            var partTemperatures = new List<Dictionary<string, object>>();
            if (parts != null)
            {
                for (int i = 0; i < parts.Count; i++)
                {
                    var part = parts[i];
                    if (part != null && !float.IsNegativeInfinity(part.temperature))
                    {
                        partTemperatures.Add(new Dictionary<string, object>
                        {
                            { "id", i },
                            { "temperature", part.temperature }
                        });
                    }
                }
            }

            var location = rocket.location?.Value;
            var planet = location?.planet;
            SFS.World.Orbit orbit = null;
            bool orbitSuccess = false;
            if (location != null && location.velocity.magnitude > 0.1)
            {
                try { orbit = SFS.World.Orbit.TryCreateOrbit(location, true, false, out orbitSuccess); }
                catch { orbitSuccess = false; }
            }

            // 获取母星codename
            string parentPlanetCode = planet?.parentBody?.codeName;


            return new Dictionary<string, object>
            {
                { "name", rocket.rocketName ?? "N/A" },
                { "id", GameManager.main.rockets != null ? GameManager.main.rockets.IndexOf(rocket) : -1 },
                { "height", location?.Height ?? 0 },
                { "position", rocket.rb2d != null ? new { x = rocket.rb2d.position.x, y = rocket.rb2d.position.y } : null },
                { "angularVelocity", rocket.rb2d?.angularVelocity ?? 0 },
                { "rotation", rocket.rb2d?.rotation ?? 0 },
                { "staging", stagingInfo },
                { "throttle", rocket.throttle?.throttlePercent?.Value ?? 0 },
                { "rcs", rocket.arrowkeys?.rcs?.Value ?? false },
                { "partTemperatures", partTemperatures },
                { "orbit", (orbit != null && orbitSuccess) ? new {
                    apoapsis = orbit.apoapsis,
                    periapsis = orbit.periapsis,
                    period = orbit.period,
                    trueAnomaly = orbit.GetTrueAnomaly(SFS.World.WorldTime.main.worldTime) * 180.0 / Math.PI
                } : null },
                { "parentPlanetCode", parentPlanetCode }
            };
        }

        // 获取当前所处星球的详细信息
        public static Dictionary<string, object> GetCurrentPlanetInfo(string codename = null)
        {
            Planet planet = null;
            if (!string.IsNullOrEmpty(codename) && Base.planetLoader?.planets != null)
            {
                planet = Base.planetLoader.planets.Values.FirstOrDefault(p => p != null && 
                    (p.codeName.Equals(codename, StringComparison.OrdinalIgnoreCase) || 
                     (p.DisplayName != null && p.DisplayName.ToString().Equals(codename, StringComparison.OrdinalIgnoreCase))));
            }
            if (planet == null)
            {
                if (PlayerController.main?.player?.Value != null)
                    planet = PlayerController.main.player.Value.location?.Value?.planet;
                else if (WorldView.main?.ViewLocation != null)
                    planet = WorldView.main.ViewLocation.planet;
            }
            if (planet == null)
                return new Dictionary<string, object> { { "error", "Could not determine current planet" } };
            return GetPlanetInfoDict(planet);
        }

        // 获取所有星球的信息列表
        public static List<Dictionary<string, object>> GetAllPlanetsInfo()
        {
            var list = new List<Dictionary<string, object>>();
            if (Base.planetLoader?.planets != null)
            {
                foreach (var planet in Base.planetLoader.planets.Values)
                {
                    if (planet == null) continue;
                    list.Add(GetPlanetInfoDict(planet));
                }
            }
            return list;
        }

        private static Dictionary<string, object> GetPlanetInfoDict(Planet planet)
        {
            if (planet == null) return null;
            
            // 获取地标信息
            List<Dictionary<string, object>> landmarks = null;
            try {
                if (planet.landmarks != null && planet.landmarks.Length > 0)
                {
                    landmarks = new List<Dictionary<string, object>>();
                    foreach (var landmark in planet.landmarks)
                    {
                        if (landmark != null && landmark.data != null)
                        {
                            landmarks.Add(new Dictionary<string, object>
                            {
                                { "name", landmark.data.name },
                                { "displayName", landmark.displayName?.ToString() },
                                { "angle", landmark.data.angle },
                                { "startAngle", landmark.data.startAngle },
                                { "endAngle", landmark.data.endAngle },
                                { "center", landmark.data.Center },
                                { "angularWidth", landmark.data.AngularWidth }
                            });
                        }
                    }
                }
            } catch { landmarks = null; }

            // 获取地图颜色
            Dictionary<string, object> mapColor = null;
            try {
                if (planet.data?.basics?.mapColor != null)
                {
                    var color = planet.data.basics.mapColor;
                    mapColor = new Dictionary<string, object>
                    {
                        { "r", color.r },
                        { "g", color.g },
                        { "b", color.b },
                        { "a", color.a },
                        { "hex", $"#{ColorUtility.ToHtmlStringRGB(color)}" }
                    };
                }
            } catch { mapColor = null; }

            // 获取轨道信息（偏心率、位置角度等）
            Dictionary<string, object> orbitInfo = null;
            try {
                if (planet.orbit != null)
                {
                    var currentTime = SFS.World.WorldTime.main?.worldTime ?? 0;
                    var currentLocation = planet.GetLocation(currentTime);
                    var trueAnomaly = planet.orbit.GetTrueAnomaly(currentTime);
                    
                    orbitInfo = new Dictionary<string, object>
                    {
                        { "eccentricity", planet.data?.orbit?.eccentricity ?? 0 },
                        { "semiMajorAxis", planet.data?.orbit?.semiMajorAxis ?? 0 },
                        { "argumentOfPeriapsis", planet.data?.orbit?.argumentOfPeriapsis ?? 0 },
                        { "direction", planet.data?.orbit?.direction ?? 1 },
                        { "currentTrueAnomaly", trueAnomaly * 180.0 / Math.PI }, // 转换为度
                        { "currentPositionAngle", currentLocation.position.AngleDegrees },
                        { "currentVelocityAngle", currentLocation.velocity.AngleDegrees },
                        { "currentRadius", currentLocation.position.magnitude },
                        { "currentVelocity", currentLocation.velocity.magnitude }
                    };
                }
            } catch { orbitInfo = null; }
            
            return new Dictionary<string, object>
            {
                { "codeName", planet.codeName },
                { "displayName", planet.DisplayName?.ToString() },
                { "radius", planet.Radius },
                { "gravity", planet.data?.basics?.gravity ?? 0 },
                { "maxTerrainHeight", planet.maxTerrainHeight },
                { "SOI", planet.SOI },
                { "hasAtmosphere", planet.HasAtmospherePhysics },
                { "atmosphereHeight", planet.HasAtmospherePhysics ? planet.AtmosphereHeightPhysics : 0 },
                { "parent", planet.parentBody?.codeName },
                { "satellites", planet.satellites?.Where(s => s != null).Select(s => s.codeName).ToArray() },
                { "landmarks", landmarks },
                { "mapColor", mapColor },
                { "orbit", orbitInfo }
            };
        }

        // 获取其它杂项信息（当前角度、快速保存、导航目标、时间加速、场景名、转移窗口deltaV等）
        public static Dictionary<string, object> GetOtherInfo(string rocketIdOrName = null)
        {
            Rocket rocket = null;
            if (!string.IsNullOrEmpty(rocketIdOrName))
            {
                if (int.TryParse(rocketIdOrName, out int idx))
                {
                    if (GameManager.main?.rockets != null && idx >= 0 && idx < GameManager.main.rockets.Count)
                        rocket = GameManager.main.rockets[idx];
                }
                else if (GameManager.main?.rockets != null)
                {
                    rocket = GameManager.main.rockets.FirstOrDefault(r => r != null && r.rocketName != null && r.rocketName.Equals(rocketIdOrName, StringComparison.OrdinalIgnoreCase));
                }
            }
            if (rocket == null && PlayerController.main?.player?.Value is Rocket curRocket)
                rocket = curRocket;

            // 获取当前角度
            double? targetAngle = null;
            try {
                if (LocationDrawer.main?.currentAngleInfo != null)
                    targetAngle = LocationDrawer.main.currentAngleInfo.targetAngle;
            } catch { targetAngle = null; }

            // 获取所有快速保存
            var quicksaves = (object)null;
            try {
                if (Base.worldBase?.paths != null)
                {
                    var quicksaveNames = Base.worldBase.paths.GetQuicksavesFileList().GetOrder();
                    var list = new List<object>();
                    foreach (var name in quicksaveNames)
                    {
                        var path = Base.worldBase.paths.GetQuicksavePath(name);
                        var timestamp = File.GetLastWriteTimeUtc(path.ToString()).ToString("o"); 
                        list.Add(new {
                            name = name,
                            time = timestamp
                        });
                    }
                    quicksaves = list;
                }
            } catch { quicksaves = null; }

            // 获取当前导航目标信息
            Dictionary<string, object> targetInfo = null;
            try {
                var navTarget = SFS.World.Maps.Map.navigation?.target;
                if (navTarget != null)
                {
                    string type = "unknown";
                    string name = null;
                    string codeName = null;
                    int? id = null;
                    string soiPlanet = null; 
                    var planet = navTarget as SFS.World.Maps.MapPlanet;
                    var mapRocket = navTarget as SFS.World.Maps.MapRocket;
                    if (planet != null)
                    {
                        type = "planet";
                        name = planet.planet.DisplayName?.ToString();
                        codeName = planet.planet.codeName;
                    }
                    else if (mapRocket != null)
                    {
                        type = "rocket";
                        name = mapRocket.rocket?.rocketName;
                        id = GameManager.main?.rockets?.IndexOf(mapRocket.rocket) ?? -1;
                        var locPlanet = mapRocket.rocket?.location?.Value?.planet;
                        if (locPlanet != null)
                            soiPlanet = locPlanet.DisplayName?.ToString();
                    }
                    targetInfo = new Dictionary<string, object>
                    {
                        { "type", type },
                        { "name", name },
                        { "codeName", codeName },
                        { "id", id },
                        { "soiPlanet", soiPlanet } 
                    };
                }
            } catch { targetInfo = null; }

            // 获取当前时间加速倍率
            double? timewarpSpeed = null;
            try {
                if (SFS.World.WorldTime.main != null)
                {
                    timewarpSpeed = SFS.World.WorldTime.main.timewarpSpeed;
                }
            } catch { timewarpSpeed = null; }

            // 获取当前世界时间
            double? worldTime = null;
            try {
                if (SFS.World.WorldTime.main != null)
                {
                    worldTime = SFS.World.WorldTime.main.worldTime;
                }
            } catch { worldTime = null; }

            // 获取当前场景名字
            string sceneName = null;
            try {
                sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            } catch { sceneName = null; }

            // 获取窗口ΔV（使用原版逻辑）
            double? transferWindowDeltaV = null;
            try {
                Planet targetPlanet = null;
                Location targetLocation = null;
                var navTarget = SFS.World.Maps.Map.navigation?.target;
                if (navTarget != null)
                {
                    var type = navTarget.GetType();
                    // 尝试取星球
                    var planetField = type.GetField("planet");
                    if (planetField != null)
                        targetPlanet = planetField.GetValue(navTarget) as Planet;
                    // 如果目标是火箭，自动取其当前星球和location
                    if (targetPlanet == null)
                    {
                        var rocketField = type.GetField("rocket");
                        if (rocketField != null)
                        {
                            var targetRocket = rocketField.GetValue(navTarget) as Rocket;
                            targetPlanet = targetRocket?.location?.Value?.planet;
                            targetLocation = targetRocket?.location?.Value;
                        }
                    }
                }
                var currentPlanet = rocket?.location?.Value?.planet;
                var location = rocket?.location?.Value;
                if (currentPlanet != null && targetPlanet != null && location != null)
                {
                    var fromOrbit = currentPlanet.orbit;
                    var targetOrbit = targetPlanet.orbit;
                    double tolerance = 1000;
                    bool crossing;
                    
                    var hohmann = SFS.Navigation.Basic.GetHohmanTransfer(targetLocation ?? location, fromOrbit, targetOrbit, tolerance, out crossing);
                    if (hohmann != null)
                    {
                        // 获取霍曼转移轨道的出发位置
                        var departureLocation = hohmann.GetLocation(hohmann.orbitStartTime);
                        
                        // 计算当前速度
                        var currentVelocity = (targetLocation ?? location).velocity.magnitude;
                        
                        // 计算出发速度
                        var departureVelocity = departureLocation.velocity.magnitude;
                        
                        // 计算ΔV
                        transferWindowDeltaV = departureVelocity - currentVelocity;
                    }
                }
            } catch { transferWindowDeltaV = null; }

            // 获取当前任务状态
            Dictionary<string, object> missionStatus = null;
            try {
                var navTarget = SFS.World.Maps.Map.navigation?.target;
                if (navTarget != null)
                {
                    // 通过反射获取Select_CanEndMission和Select_EndMissionText属性
                    var type = navTarget.GetType();
                    var canEndMissionProp = type.GetProperty("Select_CanEndMission");
                    var endMissionTextProp = type.GetProperty("Select_EndMissionText");
                    bool? canEndMission = canEndMissionProp != null ? (bool?)canEndMissionProp.GetValue(navTarget) : null;
                    string endMissionText = endMissionTextProp != null ? (string)endMissionTextProp.GetValue(navTarget) : null;
                    missionStatus = new Dictionary<string, object>
                    {
                        { "canEndMission", canEndMission },
                        { "endMissionText", endMissionText }
                    };
                }
            } catch { missionStatus = null; }
            // 获取燃料组数据
            List<Dictionary<string, object>> fuelBarGroups = null;
            try {
                if (rocket != null)
                    fuelBarGroups = GetFuelBarGroups(rocket);
            } catch { fuelBarGroups = null; }

            // 获取火箭质量（吨）
            double? mass = null;
            try {
                if (rocket?.rb2d != null)
                    mass = rocket.rb2d.mass;
            } catch { mass = null; }

            // 获取当前总推力（吨）
            double? thrust = null;
            try {
                if (rocket?.partHolder != null)
                {
                    float thrustValue = rocket.partHolder.GetModules<SFS.Parts.Modules.EngineModule>().Sum(a => a.thrust.Value * a.throttle_Out.Value) 
                                      + rocket.partHolder.GetModules<SFS.Parts.Modules.BoosterModule>().Sum(b => b.thrustVector.Value.magnitude * b.throttle_Out.Value);
                    thrust = thrustValue;
                }
            } catch { thrust = null; }

            // 获取最大推力（吨）
            double? maxThrust = null;
            try {
                if (rocket?.partHolder != null)
                {
                    float maxThrustValue = rocket.partHolder.GetModules<SFS.Parts.Modules.EngineModule>().Sum(a => a.thrust.Value) 
                                         + rocket.partHolder.GetModules<SFS.Parts.Modules.BoosterModule>().Sum(b => b.thrustVector.Value.magnitude);
                    maxThrust = maxThrustValue;
                }
            } catch { maxThrust = null; }

            // 计算推重比(TWR)
            double? twr = null;
            try {
                if (mass.HasValue && thrust.HasValue && mass.Value > 0)
                    twr = thrust.Value / mass.Value;
            } catch { twr = null; }

            // 获取轨道远点近点信息
            double? distToApoapsis = null, distToPeriapsis = null;
            double? timeToApoapsis = null, timeToPeriapsis = null;
            try {
                var location = rocket?.location?.Value;
                if (location != null && location.velocity.magnitude > 0.1)
                {
                    var orbit = SFS.World.Orbit.TryCreateOrbit(location, true, false, out bool orbitSuccess);
                    if (orbit != null && orbitSuccess)
                    {
                        double curRadius = location.Radius;
                        distToApoapsis = Math.Abs(orbit.apoapsis - curRadius);
                        distToPeriapsis = Math.Abs(orbit.periapsis - curRadius);
                        var currentTime = SFS.World.WorldTime.main?.worldTime ?? 0;
                        // 远点对应的真近点角是π（180度）
                        timeToApoapsis = orbit.GetNextTrueAnomalyPassTime(currentTime, Math.PI) - currentTime;
                        // 近点对应的真近点角是0度
                        timeToPeriapsis = orbit.GetNextTrueAnomalyPassTime(currentTime, 0) - currentTime;
                    }
                }
            } catch { 
                distToApoapsis = null; 
                distToPeriapsis = null; 
                timeToApoapsis = null; 
                timeToPeriapsis = null; 
            }

            // 获取转动惯量相关信息
            Dictionary<string, object> inertiaInfo = null;
            try {
                if (rocket?.rb2d != null)
                {
                    var rb2d = rocket.rb2d;
                    inertiaInfo = new Dictionary<string, object>
                    {
                        { "inertia", rb2d.inertia },
                        { "angularVelocity", rb2d.angularVelocity },
                        { "angularDrag", rb2d.angularDrag },
                        { "rotation", rb2d.rotation },
                        { "centerOfMass", new { x = rb2d.centerOfMass.x, y = rb2d.centerOfMass.y } }
                    };
                }
            } catch { inertiaInfo = null; }

            return new Dictionary<string, object>
            {
                { "targetAngle", targetAngle },
                { "quicksaves", quicksaves },
                { "navTarget", targetInfo },
                { "timewarpSpeed", timewarpSpeed }, 
                { "worldTime", worldTime },
                { "sceneName", sceneName },
                { "transferWindowDeltaV", transferWindowDeltaV },
                { "missionStatus", missionStatus },
                { "fuelBarGroups", fuelBarGroups },
                { "mass", mass },
                { "thrust", thrust },
                { "maxThrust", maxThrust },
                { "TWR", twr },
                { "distToApoapsis", distToApoapsis },
                { "distToPeriapsis", distToPeriapsis },
                { "timeToApoapsis", timeToApoapsis },
                { "timeToPeriapsis", timeToPeriapsis },
                { "inertia", inertiaInfo }
            };
        }

        // 获取当前任务状态和任务日志
        public static Dictionary<string, object> GetMissionInfo()
        {
            var result = new Dictionary<string, object>();
            // 任务状态复用GetOtherInfo的missionStatus
            var other = GetOtherInfo();
            result["missionStatus"] = other.ContainsKey("missionStatus") ? other["missionStatus"] : null;

            // 任务日志
            List<Dictionary<string, object>> logs = null;
            try {
                var navTarget = SFS.World.Maps.Map.navigation?.target;
                // 只对火箭和宇航员目标尝试获取日志
                if (navTarget != null)
                {
                    var type = navTarget.GetType();
                    // 火箭
                    var rocketField = type.GetField("rocket");
                    if (rocketField != null)
                    {
                        var rocket = rocketField.GetValue(navTarget) as SFS.World.Rocket;
                        if (rocket != null && rocket.stats != null)
                        {
                            var location = rocket.location?.Value;

                            var endMissionMenuType = typeof(SFS.World.EndMissionMenu);
                            var replayMission = endMissionMenuType.GetMethod("ReplayMission", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                            object[] methodArgs = new object[] { rocket.stats.branch, location, null, null, null };
                            var logTuples = replayMission.Invoke(null, methodArgs) as System.Collections.IEnumerable;
                            logs = new List<Dictionary<string, object>>();
                            foreach (var tuple in logTuples)
                            {
                                var t = tuple.GetType().GetFields();
                                logs.Add(new Dictionary<string, object>
                                {
                                    { "text", t[0].GetValue(tuple) },
                                    { "reward", t[1].GetValue(tuple) },
                                    { "logId", t[2].GetValue(tuple)?.ToString() }
                                });
                            }
                        }
                    }
                }
            } catch { logs = null; }
            result["missionLog"] = logs;
            return result;
        }

        // 获取当前玩家控制的火箭的保存信息（RocketSave对象）
        public static RocketSave GetRocketSaveInfo(string rocketIdOrName = null)
        {
            Rocket rocket = null;
            if (!string.IsNullOrEmpty(rocketIdOrName))
            {
                if (int.TryParse(rocketIdOrName, out int idx))
                {
                    if (GameManager.main?.rockets != null && idx >= 0 && idx < GameManager.main.rockets.Count)
                        rocket = GameManager.main.rockets[idx];
                }
                else if (GameManager.main?.rockets != null)
                {
                    rocket = GameManager.main.rockets.FirstOrDefault(r => r != null && r.rocketName != null && r.rocketName.Equals(rocketIdOrName, StringComparison.OrdinalIgnoreCase));
                }
            }
            if (rocket == null && PlayerController.main?.player?.Value is Rocket curRocket)
                rocket = curRocket;
            if (rocket != null)
            {
                return new RocketSave(rocket);
            }
            return null;
        }

        // 获取当前建造场景的蓝图信息（JSON）
        public static string GetCurrentBlueprint()
        {
            // 只有在建造场景下才有效
            bool isBuildScene = SFS.Builds.BuildState.main != null && SFS.Builds.BuildManager.main != null;
            if (!isBuildScene)
                return "Error: Not in build scene";
            if (SFS.Builds.BuildState.main == null)
                return "Error: BuildState not available";
            try
            {
                var blueprint = SFS.Builds.BuildState.main.GetBlueprint(false);
                return SFS.Parsers.Json.JsonWrapper.ToJson(blueprint, true);
            }
            catch (System.Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        // 获取每一级的所有资源类型剩余量
        public static List<Dictionary<string, object>> GetStagesFuel(Rocket rocket)
        {
            var result = new List<Dictionary<string, object>>();
            if (rocket?.staging?.stages == null) return result;

            foreach (var stage in rocket.staging.stages)
            {
                var resourceDict = new Dictionary<string, double>();
                foreach (var part in stage.parts)
                {
                    if (part == null) continue;
                    var resourceModules = part.GetModules<ResourceModule>();
                    foreach (var res in resourceModules)
                    {
                        string type = res.resourceType.name;
                        double amount = res.ResourceAmount;
                        if (resourceDict.ContainsKey(type))
                            resourceDict[type] += amount;
                        else
                            resourceDict[type] = amount;
                    }
                }
                result.Add(new Dictionary<string, object>
                {
                    { "stageId", stage.stageId },
                    { "resources", resourceDict }
                });
            }
            return result;
        }

        // 获取和燃料条的燃料
        public static List<Dictionary<string, object>> GetFuelBarGroups(Rocket rocket)
        {
            var result = new List<Dictionary<string, object>>();
            if (rocket?.resources?.localGroups == null) return result;

            foreach (var group in rocket.resources.localGroups)
            {
                result.Add(new Dictionary<string, object>
                {
                    { "type", group.resourceType.name },
                    { "current", group.ResourceAmount },
                    { "max", group.TotalResourceCapacity },
                    { "percent", group.resourcePercent.Value }
                });
            }
            return result;
        }

        // 获取星球指定经度范围的地形高度数组
        public static double[] GetTerrainProfile(string planetCode, double startDegree, double endDegree, int sampleCount = 360)
        {
            var planet = SFS.Base.planetLoader.planets.Values.FirstOrDefault(
                p => p.codeName.Equals(planetCode, StringComparison.OrdinalIgnoreCase));
            if (planet == null) return null;
            
            if (sampleCount <= 0)
            {
                return null;
            }
            
            double[] heights = new double[sampleCount];
            double startRad = startDegree * Math.PI / 180.0;
            double endRad = endDegree * Math.PI / 180.0;
            double delta = (endRad - startRad) / (sampleCount - 1);
            for (int i = 0; i < sampleCount; i++)
            {
                double angle = startRad + delta * i;
                heights[i] = planet.GetTerrainHeightAtAngle(angle);
            }
            return heights;
        }


        // 获取游戏控制台日志内容
        public static List<string> GetConsoleLog(int maxLines = 150)
        {
            var queueField = typeof(ModLoader.IO.Console).GetField("queue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var queue = queueField?.GetValue(ModLoader.IO.Console.main) as Queue<string>;
            if (queue == null)
                return new List<string> { "No console log available" };
            return queue.Reverse().Take(maxLines).Reverse().ToList();
        }

        // 获取SFS窗口的截图
        public static byte[] CaptureSFSWindow()
        {
            try
            {
                // 确保在主线程中执行
                if (!UnityEngine.Application.isPlaying)
                {
                    UnityEngine.Debug.LogError("[SFSControl] Cannot capture screenshot: Application is not playing");
                    return null;
                }

                // 获取主相机
                var camera = UnityEngine.Camera.main;
                if (camera == null)
                {
                    UnityEngine.Debug.LogError("[SFSControl] No main camera found");
                    return null;
                }

                // 创建RenderTexture
                var renderTexture = new UnityEngine.RenderTexture(Screen.width, Screen.height, 24);
                var originalTarget = camera.targetTexture;
                
                try
                {
                    // 设置相机渲染到RenderTexture
                    camera.targetTexture = renderTexture;
                    camera.Render();
                    
                    // 使用SFS自带的ImageTools.RenderTextureToPng方法
                    byte[] pngData = SFS.UI.ImageTools.RenderTextureToPng(renderTexture, Screen.width, Screen.height);
                    
                    // 恢复相机设置
                    camera.targetTexture = originalTarget;
                    UnityEngine.Object.DestroyImmediate(renderTexture);
                    
                    if (pngData != null && pngData.Length > 0)
                    {
                        return pngData;
                    }
                    else
                    {
                        UnityEngine.Debug.LogError("[SFSControl] Failed to capture screenshot using ImageTools");
                        return null;
                    }
                }
                catch (System.Exception ex)
                {
                    // 恢复相机设置
                    camera.targetTexture = originalTarget;
                    if (renderTexture != null)
                        UnityEngine.Object.DestroyImmediate(renderTexture);
                    
                    UnityEngine.Debug.LogError($"[SFSControl] Screenshot capture failed: {ex.Message}");
                    return null;
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[SFSControl] Screenshot capture failed: {ex.Message}");
                return null;
            }
        }


    }
}