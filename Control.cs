using SFS;
using SFS.World;
using SFS.Builds;
using SFS.World.Maps;
using SFS.Parts;
using SFS.WorldBase;
using SFS.UI;
using System.IO;
using UnityEngine;
using System.Collections;
using System.Linq;
using System;
using Newtonsoft.Json;
using SFS.Audio;
using System.Collections.Generic;
using SFS.Tutorials;
using SFS.Parts.Modules;

namespace SFSControl
{
    // 处理对火箭和游戏的操作。
    public static class Control
    {
        //归一化角度到-180~180
        private static float NormalizeAngle(float angle)
        {
            angle %= 360f;
            if (angle > 180f) angle -= 360f;
            if (angle < -180f) angle += 360f;
            return angle;
        }

        // 根据编号/名称查找火箭，默认当前控制火箭
        public static Rocket FindRocket(string rocketIdOrName = null)
        {
            if (!string.IsNullOrEmpty(rocketIdOrName))
            {
                if (int.TryParse(rocketIdOrName, out int idx))
                {
                    if (GameManager.main?.rockets != null && idx >= 0 && idx < GameManager.main.rockets.Count)
                        return GameManager.main.rockets[idx];
                }
                else if (GameManager.main?.rockets != null)
                {
                    return GameManager.main.rockets.FirstOrDefault(r => r != null && r.rocketName != null && r.rocketName.Equals(rocketIdOrName, StringComparison.OrdinalIgnoreCase));
                }
            }
            return PlayerController.main?.player?.Value as Rocket;
        }

        // 设置节流阀
        public static string SetThrottle(double size, string rocketIdOrName = null)
        {
            var rocket = FindRocket(rocketIdOrName);
            if (rocket != null && rocket.throttle != null)
            {
                rocket.throttle.throttlePercent.Value = Mathf.Clamp01((float)size);
                return "Success";
            }
            Debug.LogError("[Control] SetThrottle: Not in world scene or rocket/throttle not available");
            return "Error: Not in world scene or rocket/throttle not available";
        }

    // 开关RCS
        public static string SetRCS(bool on, string rocketIdOrName = null)
        {
            var rocket = FindRocket(rocketIdOrName);
            if (rocket != null && rocket.arrowkeys != null)
            {
                rocket.arrowkeys.rcs.Value = on;
                return "Success";
            }
            Debug.LogError("[Control] SetRCS: Not in world scene or rocket/RCS not available");
            return "Error: Not in world scene or rocket/RCS not available";
        }

        // 激活分级
        public static string Stage(string rocketIdOrName = null)
        {
            var rocket = FindRocket(rocketIdOrName);
            if (rocket != null && rocket.staging != null)
            {
                // 取第一个分级
                if (rocket.staging.stages.Count > 0)
                {
                    var stage = rocket.staging.stages[0];
                    // 依次触发分级内所有部件的 onPartUsed
                    foreach (var part in stage.parts.ToArray())
                    {
                        if (part != null && part.onPartUsed != null && part.onPartUsed.GetPersistentEventCount() > 0)
                        {
                            part.onPartUsed.Invoke(new SFS.Parts.UsePartData(new SFS.Parts.UsePartData.SharedData(true), null));
                        }
                    }
                    return "Success";
                }
                Debug.LogError("[Control] Stage: No stage available");
                return "Error: No stage available";
            }
            Debug.LogError("[Control] Stage: Not in world scene or rocket/staging not available");
            return "Error: Not in world scene or rocket/staging not available";
        }
        // 强制停止当前旋转
        public static string StopRotate(string rocketIdOrName = null)
        {
            var rocket = FindRocket(rocketIdOrName);
            if (rocket == null || rocket.arrowkeys == null || rocket.rb2d == null)
                return "Error: Not in world scene or rocket/arrowkeys/rb2d not available";
            rocket.arrowkeys.turnAxis.Value = 0f;      // 停止输入
            rocket.rb2d.angularVelocity = 0f;          // 清零角速度
                return "Success";
            }
        // 合并旋转方法
        public static string Rotate(bool isTarget = false, float angle = 0, string reference = null, string direction = null, string rocketIdOrName = null)
        {
            var rocket = FindRocket(rocketIdOrName);
            if (rocket == null || rocket.arrowkeys == null || rocket.rb2d == null)
                return "Error: Not in world scene or rocket/arrowkeys/rb2d not available";

            // 计算目标角度
            float targetAngle = 0;
            if (isTarget)
            {
                // 参考系处理
                if (reference == "surface")
                {
                    // 表面方向
                    targetAngle = Mathf.Atan2((float)rocket.location.position.Value.y, (float)rocket.location.position.Value.x) * Mathf.Rad2Deg;
                }
                else if (reference == "orbit")
                {
                    // 轨道方向
                    var v = rocket.location.velocity.Value;
                    if (v.magnitude < 0.1)
                        return "Error: Velocity too low for orbit reference";
                    targetAngle = Mathf.Atan2((float)v.y, (float)v.x) * Mathf.Rad2Deg;
                }
                else
                {
                    targetAngle = angle;
                }
            }
            else
            {
                targetAngle = angle;
            }
            // 归一化角度
            targetAngle = NormalizeAngle(targetAngle);

            // 方向处理
                float current = rocket.rb2d.rotation;
                float delta = Mathf.DeltaAngle(current, targetAngle);
            if (direction == "left" && delta < 0) delta += 360f;
            if (direction == "right" && delta > 0) delta -= 360f;

            if (Mathf.Abs(delta) < 1f && Mathf.Abs(rocket.rb2d.angularVelocity) < 0.5f)
            {
                rocket.rb2d.rotation = targetAngle;
                rocket.arrowkeys.turnAxis.Value = 0f;
                rocket.rb2d.angularVelocity = 0f;
                return "Success";
            }

            // 控制转向
            float torque = (float)typeof(Rocket).GetMethod("GetTorque", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(rocket, null);
            float mass = rocket.rb2d.mass;
            if (mass > 200f)
                torque /= Mathf.Pow(mass / 200f, 0.35f);
            float maxAcc = torque * Mathf.Rad2Deg / mass;
            float angVel = rocket.rb2d.angularVelocity;
            float stopTime = Mathf.Abs(angVel / maxAcc);
            float curTime = Mathf.Abs(delta / (angVel == 0 ? 1e-3f : angVel));

            // 判断是否需要减速
            float turnInput;
            if (stopTime > curTime)
            {
                turnInput = Mathf.Sign(angVel); // 继续当前方向
            }
            else
            {
                turnInput = -Mathf.Sign(delta); // 反向减速
            }
            rocket.arrowkeys.turnAxis.Value = turnInput * Mathf.Clamp(Mathf.Abs(delta) / 30f, 0.1f, 1.0f);
            return "Success";
        }

        // 使用部件
        public static string UsePart(int partId, string rocketIdOrName = null)
        {
            //Debug.Log($"[Control] UsePart called, partId={partId}");
            var rocket = FindRocket(rocketIdOrName);
            if (rocket != null && rocket.partHolder?.parts != null)
            {
                if (partId < 0 || partId >= rocket.partHolder.parts.Count)
                {
                    Debug.LogError("[Control] UsePart: Invalid partId");
                    return "Error: Invalid partId";
                }
                var part = rocket.partHolder.parts[partId];
                if (part == null)
                {
                    Debug.LogError("[Control] UsePart: Part not found");
                    return "Error: Part not found";
                }
                if (part.onPartUsed != null && part.onPartUsed.GetPersistentEventCount() > 0)
                {
                    try
                    {
                        part.onPartUsed.Invoke(new SFS.Parts.UsePartData(new SFS.Parts.UsePartData.SharedData(false), null));
                        return "Success";
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[Control] UsePart error: {ex.Message}");
                        return $"Error: {ex.Message}";
                    }
                }
                else
                {
                    Debug.LogError("[Control] UsePart: Part is not controllable");
                    return "Error: Part is not controllable";
                }
            }
            Debug.LogError("[Control] UsePart: Not in world scene or rocket/parts not available");
            return "Error: Not in world scene or rocket/parts not available";
        }

        // 清除碎片
        public static string ClearDebris()
        {
            if (GameManager.main?.rockets != null)
            {
                int count = 0;
                for (int i = GameManager.main.rockets.Count - 1; i >= 0; i--)
                {
                    var rocket = GameManager.main.rockets[i];
                    if (rocket != null && !rocket.hasControl.Value)
                    {
                        SFS.World.RocketManager.DestroyRocket(rocket, SFS.World.DestructionReason.Intentional);
                        count++;
                    }
                }
                return "Success";
            }
            Debug.LogError("[Control] ClearDebris: Not in world scene or rocket list not available");
            return "Error: Not in world scene or rocket list not available";
        }

        // 建造
        public static string Build(string blueprintInfo)
        {
            // 判断是否在建造场景
            bool isBuildScene = SFS.Builds.BuildState.main != null && SFS.Builds.BuildManager.main != null;
            if (!isBuildScene)
                return "Error: Not in build scene";
            if (BuildState.main == null)
                return "Error: BuildState not available";
            try
            {
                var blueprint = SFS.Parsers.Json.JsonWrapper.FromJson<SFS.Builds.Blueprint>(blueprintInfo);
                if (blueprint == null)
                    return "Error: Invalid blueprint JSON";
                BuildState.main.LoadBlueprint(blueprint, Menu.read, false, true, default(Vector2), null);
                return "Success";
            }
            catch (System.Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        // RCS推进
        public static string RcsThrust(string direction, float seconds, string rocketIdOrName = null)
        {
            var rocket = FindRocket(rocketIdOrName);
            if (rocket != null && rocket.arrowkeys != null)
            {
                ControlCoroutineRunner.Instance.StartCoroutine(RcsThrustCoroutine(rocket, direction, seconds));
                return "Success";
            }
            Debug.LogError("[Control] RcsThrust: Not in world scene or rocket/arrowkeys not available");
            return "Error: Not in world scene or rocket/arrowkeys not available";
        }

        private static IEnumerator RcsThrustCoroutine(Rocket rocket, string direction, float seconds)
        {
            float timer = 0f;
            Vector2 dir = Vector2.zero;
            // 只处理平移推进，不再写turnAxis，保证RCS推进期间可以被旋转指令干预
            switch (direction)
            {
                case "up": dir = new Vector2(0, 1); break;
                case "down": dir = new Vector2(0, -1); break;
                case "left": dir = new Vector2(-1, 0); break;
                case "right": dir = new Vector2(1, 0); break;
            }
            while (timer < seconds)
            {
                rocket.arrowkeys.rawArrowkeysAxis.Value = dir;
                rocket.arrowkeys.horizontalAxis.Value = new Double2(dir.x, 0);
                rocket.arrowkeys.verticalAxis.Value = new Double2(0, dir.y);
                timer += Time.deltaTime;
                yield return null;
            }
            rocket.arrowkeys.rawArrowkeysAxis.Value = Vector2.zero;
            rocket.arrowkeys.horizontalAxis.Value = Double2.zero;
            rocket.arrowkeys.verticalAxis.Value = Double2.zero;
        }

        private class ControlCoroutineRunner : MonoBehaviour
        {
            private static ControlCoroutineRunner _instance;
            public static ControlCoroutineRunner Instance
            {
                get
                {
                    if (_instance == null)
                    {
                        var go = new GameObject("ControlCoroutineRunner");
                        GameObject.DontDestroyOnLoad(go);
                        _instance = go.AddComponent<ControlCoroutineRunner>();
                    }
                    return _instance;
                }
            }
        }

        // 切换到建造
        public static string SwitchToBuild()
        {
            //Debug.Log("[Control] SwitchToBuild called");
            bool isBuildScene = SFS.Builds.BuildState.main != null && SFS.Builds.BuildManager.main != null;
            if (isBuildScene)
            {
                Debug.LogError("[Control] SwitchToBuild: Already in build scene");
                return "Error: Already in build scene";
            }
            if (Base.sceneLoader != null)
            {
                try
                {
                    Base.sceneLoader.LoadBuildScene(false);
                    return "Success";
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Control] SwitchToBuild error: {ex.Message}");
                    return $"Error: {ex.Message}";
                }
            }
            Debug.LogError("[Control] SwitchToBuild: Scene loader not available");
            return "Error: Scene loader not available";
        }

        // 清空蓝图
        public static string ClearBlueprint()
        {
            //Debug.Log("[Control] ClearBlueprint called");
            bool isBuildScene = SFS.Builds.BuildState.main != null && SFS.Builds.BuildManager.main != null;
            if (!isBuildScene)
                return "Error: Not in build scene";
            if (BuildState.main != null)
            {
                try
                {
                    BuildState.main.Clear(true);
                    return "Success";
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Control] ClearBlueprint error: {ex.Message}");
                    return $"Error: {ex.Message}";
                }
            }
            Debug.LogError("[Control] ClearBlueprint: BuildState not available");
            return "Error: BuildState not available";
        }

        // 设置角度
        public static string SetRotation(float angle, string rocketIdOrName = null)
        {
            //Debug.Log($"[Control] SetRotation called, angle={angle}");
            var rocket = FindRocket(rocketIdOrName);
            if (rocket != null && rocket.rb2d != null)
            {
                rocket.rb2d.rotation = angle;
                return "Success";
            }
            Debug.LogError("[Control] SetRotation: Not in world scene or rocket/rb2d not available");
            return "Error: Not in world scene or rocket/rb2d not available";
        }

        // 设置火箭状态，参考WorldBuild
        public static string SetState(double? x = null, double? y = null, double? vx = null, double? vy = null, double? angularVelocity = null, string blueprintJson = null, string rocketIdOrName = null)
        {
            var rocket = FindRocket(rocketIdOrName);
            if (rocket == null || rocket.rb2d == null)
            {
                Debug.LogError("[Control] SetState: Not in world scene or rocket/rb2d not available");
                return "Error: Not in world scene or rocket/rb2d not available";
            }
            // 替换零件
            if (!string.IsNullOrEmpty(blueprintJson))
            {
                try
                {
                    var blueprint = SFS.Parsers.Json.JsonWrapper.FromJson<SFS.Builds.Blueprint>(blueprintJson);
                    if (blueprint == null)
                        return "Error: Invalid blueprint JSON";
                    OwnershipState[] ownerships;
                    var parts = SFS.Parts.PartsLoader.CreateParts(blueprint.parts, null, null, OnPartNotOwned.Allow, out ownerships);
                    rocket.SetParts(parts);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Control] SetState blueprint error: {ex}");
                    return $"Error: {ex.Message}";
                }
            }
            // 获取当前Location
            var loc = rocket.location?.Value;
            if (loc == null)
                return "Error: Rocket location not available";
            var planet = loc.planet;
            var position = loc.position;
            var velocity = loc.velocity;
            if (x.HasValue && y.HasValue)
                position = new Double2(x.Value, y.Value);
            if (vx.HasValue && vy.HasValue)
                velocity = new Double2(vx.Value, vy.Value);
            var newLoc = new Location(0, planet, position, velocity);
            rocket.physics.SetLocationAndState(newLoc, false);
            if (angularVelocity.HasValue)
                rocket.rb2d.angularVelocity = (float)angularVelocity.Value;
            return "Success";
        }

        // 发射火箭（仅建造场景）
        public static string Launch(string rocketIdOrName = null)
        {
            var rocket = FindRocket(rocketIdOrName);
            // 这里只能在建造场景发射当前火箭，暂不支持多火箭
            bool isBuildScene = SFS.Builds.BuildState.main != null && SFS.Builds.BuildManager.main != null;
            if (!isBuildScene)
                return "Error: Not in build scene";
            if (BuildState.main == null)
                return "Error: BuildState not available";
            try
            {
                BuildState.main.UpdatePersistent(false);
                Base.sceneLoader.LoadWorldScene(true);
                return "Success";
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Control] Launch error: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }



        // 切换控制火箭

        public static string SwitchRocket(string idOrName)
        {
            var rocket = FindRocket(idOrName);
            if (rocket == null)
                return "Error: Rocket not found";
            try
            {
                PlayerController.main.player.Value = rocket;
                return "Success";
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Control] SwitchRocket error: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        // 重命名火箭
        public static string RenameRocket(string idOrName, string newName)
        {
            var rocket = FindRocket(idOrName);
            if (rocket == null)
                return "Error: Rocket not found";
            try
            {
                rocket.rocketName = newName;
                return "Success";
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Control] RenameRocket error: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        // 锁定导航目标
        public static string SetTarget(string nameOrIndex)
        {
            if (Map.navigation == null)
                return "Error: Navigation system not available";

            // 先查星球
            var planet = Base.planetLoader?.planets?.Values.FirstOrDefault(
                p => p != null && (
                    p.codeName.Equals(nameOrIndex, System.StringComparison.OrdinalIgnoreCase) ||
                    (p.DisplayName != null && p.DisplayName.ToString().Equals(nameOrIndex, System.StringComparison.OrdinalIgnoreCase))
                )
            );
            if (planet != null && planet.mapPlanet != null)
            {
                Map.navigation.SetTarget(planet.mapPlanet);
                return "Success";
            }

            // 再查火箭
            Rocket targetRocket = null;
            if (int.TryParse(nameOrIndex, out int idx))
            {
                if (GameManager.main?.rockets != null && idx >= 0 && idx < GameManager.main.rockets.Count)
                    targetRocket = GameManager.main.rockets[idx];
            }
            else if (GameManager.main?.rockets != null)
            {
                targetRocket = GameManager.main.rockets.FirstOrDefault(r => r != null && r.rocketName != null && r.rocketName.Equals(nameOrIndex, System.StringComparison.OrdinalIgnoreCase));
            }
            if (targetRocket != null && targetRocket.mapPlayer != null)
            {
                Map.navigation.SetTarget(targetRocket.mapPlayer);
                return "Success";
            }

            return "Error: Target not found";
        }

        // 取消导航目标
        public static string ClearTarget()
        {
            if (Map.navigation == null)
                return "Error: Navigation system not available";
            try
            {
                Map.navigation.SetTarget(null);
                return "Success";
            }
            catch (System.Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        // 时间加速
        public static string TimewarpPlus()
        {
            //Debug.Log("[Control] TimewarpPlus called");
            // 只有在世界场景有效
            if (SFS.World.WorldTime.main != null)
            {
                try
                {
                    int idx = SFS.World.WorldTime.main.timewarpIndex + 1;
                    Debug.Log($"[Control] TimewarpPlus: current={SFS.World.WorldTime.main.timewarpIndex}, set={idx}");
                    SFS.World.WorldTime.main.SetTimewarpIndex_ForLoad(idx);
                    Debug.Log($"[Control] TimewarpPlus: after set, now={SFS.World.WorldTime.main.timewarpIndex}");
                    return "Success";
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Control] TimewarpPlus error: {ex.Message}");
                    return $"Error: {ex.Message}";
                }
            }
            Debug.LogError("[Control] TimewarpPlus: WorldTime.main is null");
            return "Error: WorldTime not available";
        }

        // 时间减速
        public static string TimewarpMinus()
        {
            //Debug.Log("[Control] TimewarpMinus called");
            // 只有在世界场景有效
            if (SFS.World.WorldTime.main != null)
            {
                try
                {
                    int idx = SFS.World.WorldTime.main.timewarpIndex - 1;
                    Debug.Log($"[Control] TimewarpMinus: current={SFS.World.WorldTime.main.timewarpIndex}, set={idx}");
                    SFS.World.WorldTime.main.SetTimewarpIndex_ForLoad(idx);
                    Debug.Log($"[Control] TimewarpMinus: after set, now={SFS.World.WorldTime.main.timewarpIndex}");
                    return "Success";
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Control] TimewarpMinus error: {ex.Message}");
                    return $"Error: {ex.Message}";
                }
            }
            Debug.LogError("[Control] TimewarpMinus: WorldTime.main is null");
            return "Error: WorldTime not available";
        }

        // 自动等待到转移窗口、交会窗口或遭遇窗口
        // mode: "transfer"（转移窗口）、"window"（交会窗口）、"encounter"（遭遇窗口）
        public static string WaitForWindow(string mode = "transfer")
        {
            var timewarpTo = UnityEngine.Object.FindObjectOfType(Type.GetType("SFS.World.Maps.TimewarpTo, Assembly-CSharp"));
            if (timewarpTo == null)
                return "Error: TimewarpTo not available";

            var mapNavType = Type.GetType("SFS.World.Maps.MapNavigation, Assembly-CSharp");
            var mapNav = UnityEngine.Object.FindObjectOfType(mapNavType);
            if (mapNav == null)
                return "Error: MapNavigation not available";

            var windowField = mapNavType.GetField("window");
            var window = windowField.GetValue(mapNav);
            var windowType = window.GetType();
            bool hasFutureWindow = (bool)windowType.GetField("Item1").GetValue(window);
            bool planetWindow = (bool)windowType.GetField("Item4").GetValue(window);

            // 遭遇窗口判定：hasFutureWindow==true 且 planetWindow==false
            // 交会窗口判定：hasFutureWindow==true 且 planetWindow==true
            // 转移窗口判定：hasFutureWindow==true 且 planetWindow==false
            if (mode == "encounter")
            {
                if (!hasFutureWindow || planetWindow)
                    return "Error: No encounter window available";
            }
            else if (mode == "window")
            {
                if (!hasFutureWindow || !planetWindow)
                    return "Error: No rendezvous window available";
            }
            else // transfer
            {
                if (!hasFutureWindow || planetWindow)
                    return "Error: No transfer window available";
            }

            try
            {
                var type = timewarpTo.GetType();
                var selectType = type.GetNestedType("Select_TransferWindow", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                var selectInstance = Activator.CreateInstance(selectType);
                type.GetField("selected", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public).SetValue(timewarpTo, selectInstance);
                type.GetMethod("StartTimewarp", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).Invoke(timewarpTo, null);
                return "Success";
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[Control] WaitForWindow error: {ex}");
                return $"Error: {ex.Message}";
            }
        }

        // 控制主发动机开关（总点火开关）
        public static string SetMainEngineOn(bool on, string rocketIdOrName = null)
        {
            var rocket = FindRocket(rocketIdOrName);
            if (rocket != null && rocket.throttle != null)
            {
                rocket.throttle.throttleOn.Value = on;
                return "Success";
            }
            return "Error: Not in world scene or rocket/throttle not available";
        }

        // 通用反射调用方法
        public static string CallMethod(string typeName, string methodName, object[] args)
        {
            try
            {
                // 获取类型
                var type = Type.GetType(typeName);
                if (type == null)
                    return "Error: Type not found";
                // 获取方法（只取第一个匹配的方法，参数类型需完全匹配）
                var methods = type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance)
                    .Where(m => m.Name == methodName && m.GetParameters().Length == (args?.Length ?? 0)).ToArray();
                if (methods.Length == 0)
                    return "Error: Method not found or parameter count mismatch";
                var method = methods[0];
                object instance = null;
                if (!method.IsStatic)
                {
                    // 尝试查找 MonoBehaviour 实例
                    instance = UnityEngine.Object.FindObjectOfType(type);
                    if (instance == null)
                        return "Error: Instance not found";
                }
                // 参数类型转换
                var paramInfos = method.GetParameters();
                object[] realArgs = new object[args.Length];
                for (int i = 0; i < args.Length; i++)
                {
                    realArgs[i] = Convert.ChangeType(args[i], paramInfos[i].ParameterType);
                }
                var result = method.Invoke(instance, realArgs);
                // 返回值序列化
                if (result == null)
                    return "Success";
                return Newtonsoft.Json.JsonConvert.SerializeObject(new { result });
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
        // 显示Toast
        public static string ShowToast(string toast)
        {
            string msg = toast;
            if (MsgDrawer.main == null)
            {
        MsgDrawer.main = GameObject.FindObjectOfType<MsgDrawer>();
        }
            if (MsgDrawer.main != null)
            {
                MsgDrawer.main.Log(msg, false);
                return "Success";
            }
            return "Error: MsgDrawer not available";
        }
        // 添加分级
        public static string AddStage(int index, int[] partIds, string rocketIdOrName = null)
        {
            var rocket = FindRocket(rocketIdOrName);
            if (rocket != null && rocket.staging != null)
            {
                var parts = new List<Part>();
                foreach (var partId in partIds)
                {
                    if (partId >= 0 && partId < rocket.partHolder.parts.Count)
                    {
                        parts.Add(rocket.partHolder.parts[partId]);
                    }
                    else
                    {
                        Debug.LogError($"[Control] AddStage: Invalid partId {partId}");
                        return $"Error: Invalid partId {partId}";
                    }
                }

                if (parts.Count > 0)
                {
                    int stageId = rocket.staging.stages.Count > 0 ? rocket.staging.stages.Max(s => s.stageId) + 1 : 1;
                    if (index < 0 || index > rocket.staging.stages.Count)
                        index = -1;
                    var newStage = new Stage(stageId, parts);
                    try
                    {
                        rocket.staging.InsertStage(newStage, false, index);
                        return "Success";
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[Control] AddStage error: {ex}");
                        return $"Error: {ex}";
                    }
                }
                Debug.LogError("[Control] AddStage: No valid parts to form a stage");
                return "Error: No valid parts to form a stage";
            }
            Debug.LogError("[Control] AddStage: Not in world scene or rocket/staging not available");
            return "Error: Not in world scene or rocket/staging not available";
        }

        public static string RemoveStage(int index, string rocketIdOrName = null)
        {
            var rocket = FindRocket(rocketIdOrName);
            if (rocket != null && rocket.staging != null)
            {
                if (index >= 0 && index < rocket.staging.stages.Count)
                {
                    try
                    {
                        rocket.staging.RemoveStage(rocket.staging.stages[index], false);
                        return "Success";
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[Control] RemoveStage error: {ex}");
                        return $"Error: {ex}";
                    }
                }
                Debug.LogError("[Control] RemoveStage: Invalid stage index");
                return "Error: Invalid stage index";
            }
            Debug.LogError("[Control] RemoveStage: Not in world scene or rocket/staging not available");
            return "Error: Not in world scene or rocket/staging not available";
        }
        // 设置作弊开关
        public static string SetCheat(string cheatName, bool value)
        {
            if (SandboxSettings.main?.settings == null)
            {
                Debug.LogError("[Control] SetCheat: SandboxSettings not available");
                return "Error: SandboxSettings not available";
            }
            var settings = SandboxSettings.main.settings;
            var settingsType = settings.GetType();
            string[] allowed = { "infiniteFuel", "infiniteBuildArea", "noGravity", "noAtmosphericDrag", "noHeatDamage", "noBurnMarks", "partClipping", "unbreakableParts" };
            if (!allowed.Contains(cheatName, StringComparer.OrdinalIgnoreCase))
            {
                Debug.LogError("[Control] SetCheat: Cheat not allowed");
                return "Error: Cheat not allowed";
            }
            string fieldName = cheatName.Equals("noCollisionDamage", StringComparison.OrdinalIgnoreCase) ? "unbreakableParts" : cheatName;
            var field = settingsType.GetField(fieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
            if (field != null)
            {
                bool current = (bool)field.GetValue(settings);
                if (current != value)
                {
                    // 反射调用 ToggleXXX 方法
                    string toggleMethodName = "Toggle" + char.ToUpper(fieldName[0]) + fieldName.Substring(1);
                    var toggleMethod = SandboxSettings.main.GetType().GetMethod(toggleMethodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (toggleMethod != null)
                    {
                        toggleMethod.Invoke(SandboxSettings.main, null);
                        return "Success";
                    }
                    else
                    {
                        Debug.LogError($"[Control] SetCheat: Toggle method {toggleMethodName} not found");
                        return $"Error: Toggle method {toggleMethodName} not found";
                    }
                }
                return "Success";
            }
            Debug.LogError($"[Control] SetCheat: Cheat field {fieldName} not found");
            return $"Error: Cheat field {fieldName} not found";
        }

        // 回溯
        public static string Revert(string type)
        {
            //Debug.Log($"[Control] Revert called, type={type}");
            try
            {
                switch (type.ToLower())
                {
                    case "launch":
                        GameManager.main.RevertToLaunch(true); 
                        return "Success";
                    case "30s":
                        FailureMenu.main.Revert_30_Sec();
                        return "Success";
                    case "3min":
                        FailureMenu.main.Revert_3_Min();
                        return "Success";
                    default:
                        Debug.LogError("[Control] Revert: Unknown revert type");
                        return "Error: Unknown revert type (use 'launch', '30s', or '3min')";
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Control] Revert error: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        // 完成指定挑战
        public static string CompleteChallenge(string challengeId)
        {
            var logManager = SFS.Stats.LogManager.main;
            if (logManager == null)
                return "Error: LogManager not available";
            if (string.IsNullOrEmpty(challengeId))
                return "Error: challengeId required";
            if (!logManager.completeChallenges.Contains(challengeId))
                logManager.completeChallenges.Add(challengeId);
            return "Success";
        }

        // 设置当前火箭到以当前星球为中心、指定半径、偏心率、真近点角的椭圆轨道。
        // 参数：
        //   radius —— 轨道半径（米）
        //   eccentricity —— 偏心率（0~1，可选，默认0）
        //   trueAnomaly —— 真近点角（度，可选，默认0，0=远地点在x正方向）
        //   counterclockwise —— true为逆时针（正），false为顺时针（逆）
        //   planetCode —— 目标星球codeName（可选）
        public static string SetOrbit(double radius, double? eccentricity = null, double? trueAnomaly = null, bool counterclockwise = true, string planetCode = null, string rocketIdOrName = null)
        {
            try
            {
                var rocket = FindRocket(rocketIdOrName);
                if (rocket == null) return "Error: Rocket not found";
                Planet planet = null;
                if (string.IsNullOrEmpty(planetCode))
                {
                    planet = rocket.location.planet.Value;
                }
                else
                {
                    planet = SFS.Base.planetLoader.planets?.Values.FirstOrDefault(p => p != null && p.codeName != null && p.codeName.Equals(planetCode, StringComparison.OrdinalIgnoreCase));
                    if (planet == null)
                    {
                        planet = SFS.Base.planetLoader.planets?.Values.FirstOrDefault(p => p != null && p.DisplayName != null && p.DisplayName.ToString().Equals(planetCode, StringComparison.OrdinalIgnoreCase));
                    }
                    if (planet == null) return $"Error: Planet '{planetCode}' not found";
                }
                if (planet == null) return "Error: Planet not found";

                double ecc = eccentricity ?? 0; 
                if (ecc < 0) ecc = 0;
                if (ecc >= 1) ecc = 0.99;
                double anomaly = trueAnomaly ?? 0; // 默认0度，0=远地点在x正方向
                double anomalyRad = anomaly * Math.PI / 180.0;

                // 计算轨道位置
                double a = radius / (1 - ecc); // 半长轴
                double mu = planet.mass;
                // 轨道上的点（极坐标转直角坐标）
                double r = a * (1 - ecc * ecc) / (1 + ecc * Math.Cos(anomalyRad));
                double x = r * Math.Cos(anomalyRad);
                double y = r * Math.Sin(anomalyRad);
                var position = new Double2(x, y);
                // 速度方向
                double v = Math.Sqrt(mu * (2 / r - 1 / a));
                // 速度方向与径向夹角
                double phi = Math.Acos((a * (1 - ecc * ecc) - r) / (ecc * r));
                if (double.IsNaN(phi)) phi = 0; // 防止圆轨道时NaN
                double vx = -v * Math.Sin(anomalyRad);
                double vy = v * Math.Cos(anomalyRad);
                if (!counterclockwise) vy = -vy;
                var velocity = new Double2(vx, vy);

                var location = new Location(0, planet, position, velocity);

                int idx = GameManager.main.rockets.IndexOf(rocket);
                PlayerController.main.player.Value = null;
                var nullLoc = new Location(0, planet, new Double2(0, 0), new Double2(0, 0));
                GameManager.main.rockets[idx].physics.SetLocationAndState(nullLoc, false);
                GameManager.main.rockets[idx].physics.SetLocationAndState(location, false);
                PlayerController.main.player.Value = GameManager.main.rockets[idx];
                WorldTime.main.StopTimewarp(false);

                WorldView.main.SetViewLocation(location);

                return "Success";
            }
            catch (Exception ex)
            {
                Debug.LogError("[Control] SetOrbit Exception: " + ex);
                return "Error: " + ex.Message;
            }
        }

        // 删除指定火箭，未指定时默认删除当前玩家控制的火箭
        public static string DeleteRocket(string idOrName = null)
        {
            var rocket = FindRocket(idOrName);
            if (rocket == null)
                return "Error: Target rocket is null";
            if (rocket == PlayerController.main?.player?.Value)
                return "Error: Cannot delete the rocket currently controlled by player";
            try
            {
                SFS.World.RocketManager.DestroyRocket(rocket, SFS.World.DestructionReason.Intentional);
                return "Success";
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Control] DeleteRocket error: {ex}");
                return $"Error: {ex.Message}";
            }
        }

        // 输出日志、警告或错误到控制台
        public static string LogMessage(string type, string message)
        {
            switch (type.ToLower())
            {
                case "log":
                    Debug.Log(message);
                    return "Success";
                case "warning":
                    Debug.LogWarning(message);
                    return "Success";
                case "error":
                    Debug.LogError(message);
                    return "Success";
                default:
                    return "Error: Unknown log type (use log/warning/error)";
            }
        }

        // 设置地图焦点（支持火箭名/编号或星球codename/编号）
        public static string Track(string nameOrIndex)
        {
            // 如果未指定，默认聚焦当前控制的火箭
            if (string.IsNullOrEmpty(nameOrIndex))
            {
                var rocket = PlayerController.main?.player?.Value as Rocket;
                if (rocket != null && rocket.mapPlayer != null)
                {
                    var mapView = UnityEngine.Object.FindObjectOfType(Type.GetType("SFS.World.Maps.MapView, Assembly-CSharp"));
                    if (mapView != null)
                    {
                        var method = mapView.GetType().GetMethod("ToggleFocus", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                        method.Invoke(mapView, new object[] { rocket.mapPlayer, 0.8f });
                        return "Success";
                    }
                    return "Error: MapView not available";
                }
                return "Error: No controlled rocket";
            }
            // 查找星球
            var planet = Base.planetLoader?.planets?.Values.FirstOrDefault(
                p => p != null && (
                    p.codeName.Equals(nameOrIndex, StringComparison.OrdinalIgnoreCase) ||
                    (p.DisplayName != null && p.DisplayName.ToString().Equals(nameOrIndex, StringComparison.OrdinalIgnoreCase))
                )
            );
            if (planet != null && planet.mapPlanet != null)
            {
                var mapView = UnityEngine.Object.FindObjectOfType(Type.GetType("SFS.World.Maps.MapView, Assembly-CSharp"));
                if (mapView != null)
                {
                    var method = mapView.GetType().GetMethod("ToggleFocus", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                    method.Invoke(mapView, new object[] { planet.mapPlanet, 0.8f });
                    return "Success";
                }
                return "Error: MapView not available";
            }
            // 查找火箭
            Rocket targetRocket = null;
            if (int.TryParse(nameOrIndex, out int idx))
            {
                if (GameManager.main?.rockets != null && idx >= 0 && idx < GameManager.main.rockets.Count)
                    targetRocket = GameManager.main.rockets[idx];
            }
            else if (GameManager.main?.rockets != null)
            {
                targetRocket = GameManager.main.rockets.FirstOrDefault(r => r != null && r.rocketName != null && r.rocketName.Equals(nameOrIndex, StringComparison.OrdinalIgnoreCase));
            }
            if (targetRocket != null && targetRocket.mapPlayer != null)
            {
                var mapView = UnityEngine.Object.FindObjectOfType(Type.GetType("SFS.World.Maps.MapView, Assembly-CSharp"));
                if (mapView != null)
                {
                    var method = mapView.GetType().GetMethod("ToggleFocus", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                    method.Invoke(mapView, new object[] { targetRocket.mapPlayer, 0.8f });
                    return "Success";
                }
                return "Error: MapView not available";
            }
            return "Error: Target not found";
        }

        // 取消地图焦点
        public static string Unfocus()
        {
            var mapView = UnityEngine.Object.FindObjectOfType(Type.GetType("SFS.World.Maps.MapView, Assembly-CSharp"));
            if (mapView == null)
                return "Error: MapView not available";
            var viewField = mapView.GetType().GetField("view");
            var view = viewField.GetValue(mapView);
            var targetProp = view.GetType().GetField("target");
            var target = targetProp.GetValue(view);
            var valueProp = target.GetType().GetProperty("Value");
            valueProp.SetValue(target, null);
            return "Success";
        }

        // 切换地图/世界视图，on为false切到世界，true切到地图，null为切换
        public static string SwitchMapView(bool? on = null)
        {
            var mapManagerType = Type.GetType("SFS.World.Maps.MapManager, Assembly-CSharp");
            var mapManager = UnityEngine.Object.FindObjectOfType(mapManagerType);
            if (mapManager == null)
                return "Error: MapManager not available";
            var mapModeField = mapManagerType.GetField("mapMode");
            var mapMode = mapModeField.GetValue(mapManager);
            var valueProp = mapMode.GetType().GetProperty("Value");
            bool current = (bool)valueProp.GetValue(mapMode);
            if (on == null)
                valueProp.SetValue(mapMode, !current);
            else
                valueProp.SetValue(mapMode, on.Value);
            return "Success";
        }
    }
}