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
using System.Reflection;

namespace SFSControl
{
    // 方向模式枚举
    public enum DirectionMode
    {
        Prograde,
        Target,
        Surface,
        None,
        Default
    }

    public class SASComponent : MonoBehaviour
    {
        public DirectionMode Direction = DirectionMode.Default;
        public float Offset = 0f;
        public float TargetAngle = 0f;
        public string ModeDescription = "";

        void FixedUpdate()
        {
            ApplySASControl();
        }

        void Update()
        {
            ApplySASControl();
        }

        void LateUpdate()
        {
            ApplySASControl();
        }

        void ApplySASControl()
        {
            if (!WorldTime.main.realtimePhysics.Value)
                return;

            var rocket = GetComponent<Rocket>();
            if (rocket == null || rocket.arrowkeys == null || rocket.rb2d == null || !rocket.hasControl.Value)
                return;

            // 设置角阻力   
            rocket.rb2d.angularDrag = 0.05f;

            float angularVelocity = rocket.rb2d.angularVelocity;
            float currentRotation = NormalizeAngle(rocket.GetRotation());

            float TargetRotationToTorque(float targetAngle)
            {
                targetAngle -= Offset;
                float deltaAngle = NormalizeAngle(targetAngle - currentRotation);

                float torque = (float)typeof(Rocket).GetMethod("GetTorque", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(rocket, null);
                float mass = rocket.rb2d.mass;
                if (mass > 200f)
                    torque /= Mathf.Pow(mass / 200f, 0.35f);
                
                float maxAcceleration = torque * Mathf.Rad2Deg / mass;
                float stoppingTime = Mathf.Abs(angularVelocity / maxAcceleration);
                float currentTime = Mathf.Abs(deltaAngle / angularVelocity);
                
                if (stoppingTime > currentTime)
                {
                    return Mathf.Sign(angularVelocity);
                }
                else
                {
                    return -Mathf.Sign(deltaAngle);
                }
            }

            float result = 0f;
            switch (Direction)
            {
                case DirectionMode.Default:
                    return;

                case DirectionMode.Prograde:
                    Double2 offset = rocket.location.velocity.Value;
                    if (offset.magnitude <= 3)
                        return;
                    result = TargetRotationToTorque((float)Math.Atan2(offset.y, offset.x) * Mathf.Rad2Deg);
                    break;

                case DirectionMode.Surface:
                    float targetRotation = NormalizeAngle((float)Math.Atan2(rocket.location.position.Value.y, rocket.location.position.Value.x) * Mathf.Rad2Deg);
                    result = TargetRotationToTorque(targetRotation);
                    break;

                case DirectionMode.Target:
                    var target = SFS.World.Maps.Map.navigation?.target;
                    if (target == null)
                        return;
                    
                    Double2 targetPosition = target.Location.position;
                    Double2 rocketPosition = rocket.location.position.Value;
                    Double2 directionToTarget = targetPosition - rocketPosition;
                    
                    if (directionToTarget.magnitude <= 0.1)
                        return;
                    
                    float targetAngle = NormalizeAngle((float)Math.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg);
                    result = TargetRotationToTorque(targetAngle);
                    break;

                case DirectionMode.None:
                    rocket.rb2d.angularDrag = 0;
                    result = 0;
                    break;
            }

            rocket.arrowkeys.turnAxis.Value = result;
        }

        // 计算转向输入
        private float CalculateTurnInput(Rocket rocket, float targetAngle)
        {
            float angularVelocity = rocket.rb2d.angularVelocity;
            float currentRotation = NormalizeAngle(rocket.GetRotation());
            float deltaAngle = NormalizeAngle(targetAngle - currentRotation);

            float torque = (float)typeof(Rocket).GetMethod("GetTorque", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(rocket, null);
            float mass = rocket.rb2d.mass;
            if (mass > 200f)
                torque /= Mathf.Pow(mass / 200f, 0.35f);
            
            float maxAcceleration = torque * Mathf.Rad2Deg / mass;
            float stoppingTime = Mathf.Abs(angularVelocity / maxAcceleration);
            float currentTime = Mathf.Abs(deltaAngle / angularVelocity);
            
            if (stoppingTime > currentTime)
            {
                return Mathf.Sign(angularVelocity);
            }
            else
            {
                return -Mathf.Sign(deltaAngle);
            }
        }

        // 归一化角度到-180~180
        private float NormalizeAngle(float input)
        {
            float m = (input + 180f) % 360f;
            return m < 0 ? m + 180f : m - 180f;
        }
    }

    // 处理对火箭和游戏的操作。
    public static class Control
    {

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
        
        // 停止当前旋转
        public static string StopRotate(string rocketIdOrName = null, bool stopCoroutine = true)
        {
            var rocket = FindRocket(rocketIdOrName);
            if (rocket == null || rocket.arrowkeys == null || rocket.rb2d == null)
                return "Error: Not in world scene or rocket/arrowkeys/rb2d not available";
            
            // 禁用SAS组件
            var sasComponent = rocket.GetComponent<SASComponent>();
            if (sasComponent != null)
            {
                sasComponent.Direction = DirectionMode.Default;
            }
            
            rocket.arrowkeys.turnAxis.Value = 0f;      // 停止输入
            rocket.rb2d.angularVelocity = 0f;          // 清零角速度
            
            // 停止旋转协程
            if (stopCoroutine)
            {
                ControlCoroutineRunner.Instance.StopAllCoroutines();
            }
            
            return "Success";
        }
        
        // 旋转方法
        public static string Rotate(object modeOrAngle = null, float offset = 0f, string rocketIdOrName = null)
        {
            var rocket = FindRocket(rocketIdOrName);
            if (rocket == null || rocket.arrowkeys == null || rocket.rb2d == null)
                return "Error: Not in world scene or rocket/arrowkeys/rb2d not available";

            if (!WorldTime.main.realtimePhysics.Value || !rocket.hasControl.Value)
                return "Error: Physics not running or rocket not controllable";

            // 设置角阻力
            rocket.rb2d.angularDrag = 0.05f;

            if (modeOrAngle == null)
            {
                // 无参数
                rocket.arrowkeys.turnAxis.Value = 0f;
                return "Success";
            }
            
            // 获取或创建SAS组件
            var sasComponent = rocket.GetOrAddComponent<SASComponent>();
            
            // 设置SAS参数
            if (float.TryParse(modeOrAngle.ToString(), out float directAngle))
            {
                sasComponent.Direction = DirectionMode.Default;
                sasComponent.TargetAngle = directAngle + offset;
                sasComponent.ModeDescription = $"{directAngle}°";
            }
            else if (Enum.TryParse<DirectionMode>(modeOrAngle.ToString(), true, out DirectionMode directionMode))
            {
                sasComponent.Direction = directionMode;
                sasComponent.Offset = offset;
                sasComponent.ModeDescription = directionMode.ToString();
                
                if (directionMode == DirectionMode.Default || directionMode == DirectionMode.None)
                {
                    rocket.arrowkeys.turnAxis.Value = 0f;
                    if (directionMode == DirectionMode.None)
                        rocket.rb2d.angularDrag = 0;
                    return $"Success";
                }
            }
            else
            {
                return "Error: Invalid mode or angle. Use: Prograde, Target, Surface, None, Default, or a number for angle";
            }
            
            string offsetText = offset != 0 ? $" with {offset}° offset" : "";
            return $"Success";
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
        // 协程管理器
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
        public static string Launch()
        {
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

        // 自动等待到转移窗口、交会窗口或遭遇窗口
        // mode: "transfer"（转移窗口）、"rendezvous"（交会窗口）
        public static string WaitForWindow(string mode = "transfer", double? parameter = null)
        {
            var timewarpTo = UnityEngine.Object.FindObjectOfType(Type.GetType("SFS.World.Maps.TimewarpTo, Assembly-CSharp"));
            if (timewarpTo == null)
                return "Error: TimewarpTo not available";

            var mapNavType = Type.GetType("SFS.World.Maps.MapNavigation, Assembly-CSharp");
            var mapNav = UnityEngine.Object.FindObjectOfType(mapNavType);
            if (mapNav == null)
                return "Error: MapNavigation not available";

            // 检查是否有导航目标
            var navTarget = SFS.World.Maps.Map.navigation?.target;
            if (navTarget == null)
                return "Error: No navigation target set. Use SetTarget first.";

            var windowField = mapNavType.GetField("window");
            var window = windowField.GetValue(mapNav);
            var windowType = window.GetType();
            bool hasFutureWindow = (bool)windowType.GetField("Item1").GetValue(window);
            Location windowLocation = (Location)windowType.GetField("Item2").GetValue(window);
            bool planetWindow = (bool)windowType.GetField("Item4").GetValue(window);

            // 处理不同的等待模式
            if (mode == "rendezvous")
            {
                // 交会窗口判定：hasFutureWindow==true 且 planetWindow==true
                if (!hasFutureWindow || !planetWindow)
                    return "Error: No rendezvous window available";
            }
            else // transfer (默认)
            {
                // 转移窗口判定：hasFutureWindow==true 且 planetWindow==false
                if (!hasFutureWindow || planetWindow)
                    return "Error: No transfer window available";
            }

            try
            {
                // 获取转移窗口的时间
                double windowTime = windowLocation.time;
                double currentTime = SFS.World.WorldTime.main.worldTime;
                double timeToWait = windowTime - currentTime;
                
                if (timeToWait <= 0)
                {
                    return "Success: Transfer window is already available";
                }

                var type = timewarpTo.GetType();
                var selectType = type.GetNestedType("Select_TransferWindow", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                var selectInstance = Activator.CreateInstance(selectType);
                
                // 设置目标
                var targetField = selectType.GetField("target", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (targetField != null)
                {
                    var currentNavTarget = SFS.World.Maps.Map.navigation?.target;
                    if (currentNavTarget != null)
                    {
                        targetField.SetValue(selectInstance, currentNavTarget);
                    }
                }
                
                // 设置选择的对象
                type.GetField("selected", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public).SetValue(timewarpTo, selectInstance);
                
                // 开始时间加速
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
        // 删除指定序号的分级
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
                    case "build":
                        if (Base.sceneLoader != null)
                        {
                            Base.sceneLoader.LoadBuildScene(false);
                            return "Success";
                        }
                        return "Error: Scene loader not available";
                    default:
                        Debug.LogError("[Control] Revert: Unknown revert type");
                        return "Error: Unknown revert type (use 'launch', '30s', '3min','build' )";
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
            // 获取日志管理器
            var logManager = SFS.Stats.LogManager.main;
            if (logManager == null)
                return "Error: LogManager not available";
            if (string.IsNullOrEmpty(challengeId))
                return "Error: challengeId required";
            if (!logManager.completeChallenges.Contains(challengeId))
                logManager.completeChallenges.Add(challengeId);
            return "Success";
        }

        // 设置当前火箭轨道
        public static string SetOrbit(double radius, double? eccentricity = null, double? trueAnomaly = null, bool counterclockwise = true, string planetCode = null, string rocketIdOrName = null)
        {
            try
            {
                // 获取火箭
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
                // 偏心率
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

        // 时间加速
        public static string TimewarpPlus()
        {
            if (SFS.World.WorldTime.main != null)
            {
                try
                {
                    // 获取最大索引
                    var worldTimeType = typeof(SFS.World.WorldTime);
                    var maxIndexProperty = worldTimeType.GetProperty("MaxIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    int maxIndex = (int)maxIndexProperty.GetValue(null);
                    
                    int currentIndex = SFS.World.WorldTime.main.timewarpIndex;
                    if (currentIndex >= maxIndex)
                    {
                        return "Error: Already at maximum timewarp speed";
                    }
                    
                    int newIndex = currentIndex + 1;
                    SFS.World.WorldTime.main.SetTimewarpIndex_ForLoad(newIndex);
                    return "Success";
                }
                catch (System.Exception ex)
                {
                    return $"Error: {ex.Message}";
                }
            }
            return "Error: WorldTime not available";
        }

        // 时间减速
        public static string TimewarpMinus()
        {
            if (SFS.World.WorldTime.main != null)
            {
                try
                {
                    int currentIndex = SFS.World.WorldTime.main.timewarpIndex;
                    if (currentIndex <= 0)
                    {
                        return "Error: Already at minimum timewarp speed";
                    }
                    
                    int newIndex = currentIndex - 1;
                    SFS.World.WorldTime.main.SetTimewarpIndex_ForLoad(newIndex);
                    return "Success";
                }
                catch (System.Exception ex)
                {
                    return $"Error: {ex.Message}";
                }
            }
            return "Error: WorldTime not available";
        }

        // 设置时间加速倍率
        public static string SetTimewarp(double speed, bool realtimePhysics = false, bool showMessage = true)
        {
            if (SFS.World.WorldTime.main != null)
            {
                try
                {
                    // 如果realtimePhysics参数为默认值false，则根据速度自动判断
                    // 0-5倍速默认实时，其余不实时
                    if (!realtimePhysics)
                    {
                        if (speed >= 0 && speed <= 5)
                        {
                            realtimePhysics = true;
                    }
                        else
                        {
                            realtimePhysics = false;
                        }
                    }
                    
                    // 调用SetState方法设置时间加速
                    SFS.World.WorldTime.main.SetState(speed, realtimePhysics, showMessage);
                    
                    return "Success";
                }
                catch (System.Exception ex)
                {
                    return $"Error: {ex.Message}";
                }
            }
            return "Error: WorldTime not available";
        }

        // 燃料转移
        public static string TransferFuel(int fromTankId, int toTankId, string rocketIdOrName = null)
        {
            var rocket = FindRocket(rocketIdOrName);
            if (rocket == null || rocket.resources == null)
                return "Error: Rocket or resources not available";

            try
            {
                // 获取源油箱和目标油箱
                Part fromTank = null;
                Part toTank = null;
                ResourceModule fromGroup = null;
                ResourceModule toGroup = null;

                // 查找源油箱
                if (fromTankId >= 0 && fromTankId < rocket.partHolder.parts.Count)
                {
                    fromTank = rocket.partHolder.parts[fromTankId];
                    if (fromTank != null)
                    {
                        var fromModules = fromTank.GetModules<ResourceModule>();
                        if (fromModules != null && fromModules.Length > 0)
                        {
                            fromGroup = fromModules[0].parent;
                        }
                    }
                }

                // 查找目标油箱
                if (toTankId >= 0 && toTankId < rocket.partHolder.parts.Count)
                {
                    toTank = rocket.partHolder.parts[toTankId];
                    if (toTank != null)
                    {
                        var toModules = toTank.GetModules<ResourceModule>();
                        if (toModules != null && toModules.Length > 0)
                        {
                            toGroup = toModules[0].parent;
                        }
                    }
                }

                // 验证油箱和燃料组
                if (fromTank == null || fromGroup == null)
                    return "Error: Source tank not found or invalid";
                if (toTank == null || toGroup == null)
                    return "Error: Target tank not found or invalid";
                if (fromGroup == toGroup)
                    return "Error: Cannot transfer fuel to the same tank";
                if (fromGroup.resourceType != toGroup.resourceType)
                    return "Error: Cannot transfer different fuel types";

                // 清除现有的转移
                rocket.resources.transfers.Clear();

                // 添加新的转移
                rocket.resources.transfers.Add(new SFS.World.Resources.Transfer(fromTank, fromGroup));
                rocket.resources.transfers.Add(new SFS.World.Resources.Transfer(toTank, toGroup));

                return "Success";
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Control] TransferFuel error: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        // 停止燃料转移
        public static string StopFuelTransfer(string rocketIdOrName = null)
        {
            var rocket = FindRocket(rocketIdOrName);
            if (rocket == null || rocket.resources == null)
                return "Error: Rocket or resources not available";

            try
            {
                rocket.resources.transfers.Clear();
                return "Success";
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Control] StopFuelTransfer error: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        // 快速保存管理
        public static string QuicksaveManager(string operation = "save", string name = null)
        {
            try
            {
                if (Base.worldBase?.paths == null)
                    return "Error: WorldBase or paths not available";

                switch (operation.ToLower())
                {
                    case "save":
                        if (string.IsNullOrEmpty(name))
                            return "Error: Save name cannot be empty";
                        
                        // 创建快速保存
                        var savePath = Base.worldBase.paths.GetQuicksavePath(name);
                        if (savePath == null)
                            return "Error: Could not create quicksave path";
                        
                        // 使用反射调用private方法CreateWorldSave
                        var createWorldSaveMethod = typeof(GameManager).GetMethod("CreateWorldSave", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (createWorldSaveMethod == null)
                            return "Error: Could not find CreateWorldSave method";
                        
                        var worldSave = createWorldSaveMethod.Invoke(GameManager.main, null) as WorldSave;
                        if (worldSave == null)
                            return "Error: Failed to create world save";
                        
                        // 保存到快速保存路径
                        WorldSave.Save(savePath, true, worldSave, Base.worldBase.IsCareer);
                        return "Success";
                        
                    case "load":
                        if (string.IsNullOrEmpty(name))
                            return "Error: Load name cannot be empty";
                        
                        // 检查快速保存是否存在
                        var loadPath = Base.worldBase.paths.GetQuicksavePath(name);
                        if (loadPath == null || !loadPath.FolderExists())
                            return $"Error: Quicksave '{name}' not found";
                        
                        // 加载快速保存
                        MsgCollector logger = new MsgCollector();
                        WorldSave worldSaveToLoad;
                        if (WorldSave.TryLoad(loadPath, true, logger, out worldSaveToLoad))
                        {
                            // 使用反射调用private方法ClearWorld
                            var clearWorldMethod = typeof(GameManager).GetMethod("ClearWorld", 
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (clearWorldMethod == null)
                                return "Error: Could not find ClearWorld method";
                            
                            clearWorldMethod.Invoke(GameManager.main, null);
                            
                            // 加载保存的世界
                            GameManager.main.LoadSave(worldSaveToLoad, false, logger);
                            return "Success";
                        }
                        else
                        {
                            return $"Error: Failed to load quicksave '{name}': {logger.msg}";
                        }
                        
                    case "delete":
                        if (string.IsNullOrEmpty(name))
                            return "Error: Delete name cannot be empty";
                        
                        // 检查快速保存是否存在
                        var deletePath = Base.worldBase.paths.GetQuicksavePath(name);
                        if (deletePath == null || !deletePath.FolderExists())
                            return $"Error: Quicksave '{name}' not found";
                        
                        try
                        {
                            // 删除快速保存文件夹
                            deletePath.DeleteFolder();
                            return "Success";
                        }
                        catch (System.Exception ex)
                        {
                            return $"Error: Failed to delete quicksave '{name}': {ex.Message}";
                        }
                        
                    case "rename":
                        if (string.IsNullOrEmpty(name))
                            return "Error: Rename parameters cannot be empty";
                        
                        // 解析重命名参数：格式为 "oldName:newName"
                        var renameParts = name.Split(':');
                        if (renameParts.Length != 2)
                            return "Error: Rename format should be 'oldName:newName'";
                        
                        string oldName = renameParts[0].Trim();
                        string newName = renameParts[1].Trim();
                        
                        if (string.IsNullOrEmpty(oldName) || string.IsNullOrEmpty(newName))
                            return "Error: Old name and new name cannot be empty";
                        
                        // 检查源快速保存是否存在
                        var oldPath = Base.worldBase.paths.GetQuicksavePath(oldName);
                        if (oldPath == null || !oldPath.FolderExists())
                            return $"Error: Source quicksave '{oldName}' not found";
                        
                        // 检查目标名称是否已存在
                        var newPath = Base.worldBase.paths.GetQuicksavePath(newName);
                        if (newPath != null && newPath.FolderExists())
                            return $"Error: Target quicksave '{newName}' already exists";
                        
                        try
                        {
                            // 重命名文件夹
                            oldPath.Move(newPath);
                            return "Success";
                        }
                        catch (System.Exception ex)
                        {
                            return $"Error: Failed to rename quicksave '{oldName}' to '{newName}': {ex.Message}";
                        }
                        
                    default:
                        return "Error: Invalid operation. Use 'save', 'load', 'delete', or 'rename'";
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Control] QuicksaveManager error: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        // 车轮控制
        public static string WheelControl(bool? enable = null, float turnAxis = 0f, string rocketIdOrName = null)
        {
            try
            {
                var rocket = FindRocket(rocketIdOrName);
                if (rocket == null || rocket.partHolder?.parts == null)
                {
                    Debug.LogError("[Control] WheelControl: Rocket not found or parts not available");
                    return "Error: Rocket not found or parts not available";
                }

                // 查找所有车轮模块
                var wheelModules = new List<WheelModule>();
                foreach (var part in rocket.partHolder.parts)
                {
                    if (part != null)
                    {
                        var wheels = part.GetModules<WheelModule>();
                        if (wheels != null && wheels.Length > 0)
                        {
                            wheelModules.AddRange(wheels);
                        }
                    }
                }

                if (wheelModules.Count == 0)
                {
                    Debug.LogError("[Control] WheelControl: No wheel modules found on rocket");
                    return "Error: No wheel modules found on rocket";
                }

                int modifiedCount = 0;
                foreach (var wheel in wheelModules)
                {
                    if (wheel == null) continue;

                    // 控制车轮开关状态（可选）
                    if (enable.HasValue && wheel.on != null)
                    {
                        wheel.on.Value = enable.Value;
                        modifiedCount++;
                    }

                    // 控制转向轴（必填）
                    wheel.TurnAxis = Mathf.Clamp(turnAxis, -1f, 1f);
                    modifiedCount++;
                }

                if (modifiedCount > 0)
                {
                    return $"Success: Modified {modifiedCount} wheel properties on {wheelModules.Count} wheel modules";
                }
                else
                {
                    return "Success: No changes made";
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Control] WheelControl error: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        // 创建火箭
        public static string CreateRocket(string planetCode, string blueprintJson, string rocketName = "", double x = 0, double y = 0, double vx = 0, double vy = 0, double vr = 0)
        {
            try
            {
                // 检查是否在世界场景中
                if (GameManager.main?.rockets == null)
                {
                    Debug.LogError("[Control] CreateRocket: Not in world scene");
                    return "Error: Not in world scene";
                }

                // 验证必需参数
                if (string.IsNullOrEmpty(planetCode))
                {
                    Debug.LogError("[Control] CreateRocket: Planet code cannot be null or empty");
                    return "Error: Planet code cannot be null or empty";
                }

                if (string.IsNullOrEmpty(blueprintJson))
                {
                    Debug.LogError("[Control] CreateRocket: Blueprint JSON cannot be null or empty");
                    return "Error: Blueprint JSON cannot be null or empty";
                }

                // 查找目标星球
                var planet = Base.planetLoader?.planets?.Values?.FirstOrDefault(p => p?.codeName != null && p.codeName.Equals(planetCode, StringComparison.OrdinalIgnoreCase));
                if (planet == null)
                {
                    Debug.LogError($"[Control] CreateRocket: Planet '{planetCode}' not found");
                    return $"Error: Planet '{planetCode}' not found";
                }

                // 解析蓝图JSON
                Blueprint blueprint;
                try
                {
                    blueprint = SFS.Parsers.Json.JsonWrapper.FromJson<Blueprint>(blueprintJson);
                    if (blueprint == null)
                    {
                        Debug.LogError("[Control] CreateRocket: Failed to parse blueprint JSON");
                        return "Error: Failed to parse blueprint JSON";
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Control] CreateRocket: Invalid blueprint JSON format: {ex.Message}");
                    return $"Error: Invalid blueprint JSON format: {ex.Message}";
                }

                // 验证蓝图数据
                if (blueprint.parts == null || blueprint.parts.Length == 0)
                {
                    Debug.LogError("[Control] CreateRocket: Blueprint has no parts");
                    return "Error: Blueprint has no parts";
                }

                // 创建部件
                OwnershipState[] ownershipStates;
                Part[] parts = PartsLoader.CreateParts(blueprint.parts, null, null, OnPartNotOwned.Allow, out ownershipStates);
                
                if (parts == null || parts.Length == 0)
                {
                    Debug.LogError("[Control] CreateRocket: Failed to create parts from blueprint");
                    return "Error: Failed to create parts from blueprint";
                }

                // 检查是否有未拥有的部件
                bool hasNonOwnedParts = ownershipStates.Any(state => state != OwnershipState.OwnedAndUnlocked);
                if (hasNonOwnedParts)
                {
                    Debug.LogWarning("[Control] CreateRocket: Some parts are not owned, using placeholders");
                }

                // 创建关节组
                var jointGroup = new JointGroup(new List<PartJoint>(), parts.ToList());

                // 计算火箭位置（相对于星球中心）
                Vector2 planetPosition = new Vector2((float)x, (float)y);
                Vector2 globalPosition = (Vector2)planet.transform.position + planetPosition;
                
                // 创建位置对象
                Location location = new Location(planet, new Double2(x, y), new Double2(vx, vy));

                // 创建火箭 - 使用反射访问私有方法
                var createRocketMethod = typeof(RocketManager).GetMethod("CreateRocket", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (createRocketMethod == null)
                {
                    Debug.LogError("[Control] CreateRocket: CreateRocket method not found");
                    return "Error: CreateRocket method not found";
                }
                
                // 创建委托对象
                var locationDelegate = new Func<Rocket, Location>((Rocket r) => location);
                Rocket rocket = (Rocket)createRocketMethod.Invoke(null, new object[] { jointGroup, "", false, 0.5f, false, 0f, (float)vr, locationDelegate, false });
                
                if (rocket == null)
                {
                    Debug.LogError("[Control] CreateRocket: Failed to create rocket");
                    return "Error: Failed to create rocket";
                }

                // 设置火箭名称
                if (!string.IsNullOrEmpty(rocketName))
                {
                    rocket.rocketName = rocketName;
                }
                else
                {
                    rocket.rocketName = ""; 
                }

                // 加载分级信息
                if (blueprint.stages != null && blueprint.stages.Length > 0)
                {
                    rocket.staging.Load(blueprint.stages, rocket.partHolder.GetArray(), false);
                }

                // 设置火箭的物理状态
                rocket.physics.SetLocationAndState(location, false);
                rocket.rb2d.angularVelocity = (float)vr;

                // 初始化火箭的统计信息和任务日志系统
                if (rocket.stats != null)
                {
                    try
                    {
                        // 为火箭创建新的日志分支
                        int newBranch;
                        SFS.Stats.LogManager.main.CreateRoot(out newBranch);
                        
                        // 初始化统计记录器
                        rocket.stats.Load(newBranch);
                        
                        Debug.Log($"[Control] CreateRocket: Initialized rocket stats with branch {newBranch}");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[Control] CreateRocket: Failed to initialize rocket stats: {ex.Message}");
                    }
                }

                //Debug.Log($"[Control] CreateRocket: Successfully created rocket '{rocket.rocketName}' at planet '{planetCode}' position ({x}, {y}) with velocity ({vx}, {vy}) and angular velocity {vr}");
                return $"Success: Created rocket '{rocket.rocketName}' with ID {GameManager.main.rockets.Count - 1}";
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Control] CreateRocket error: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        // 创建各种对象（宇航员、旗子、爆炸效果、地图图标等）
        public static string CreateObject(string objectType, string planetCode, double x = 0, double y = 0, string objectName = "", bool hidden = false, 
            float explosionSize = 2.0f, bool createSound = true, bool createShake = true,
            float rotation = 0f, float angularVelocity = 0f, bool ragdoll = false, double fuelPercent = 1.0, float temperature = 293.15f,
            int flagDirection = 1, bool showFlagAnimation = true, bool createDamage = true)
        {
            try
            {
                // 检查是否在世界场景中
                if (GameManager.main?.rockets == null)
                {
                    Debug.LogError("[Control] CreateObject: Not in world scene");
                    return "Error: Not in world scene";
                }

                // 验证必需参数
                if (string.IsNullOrEmpty(objectType))
                {
                    Debug.LogError("[Control] CreateObject: Object type cannot be null or empty");
                    return "Error: Object type cannot be null or empty";
                }

                if (string.IsNullOrEmpty(planetCode))
                {
                    Debug.LogError("[Control] CreateObject: Planet code cannot be null or empty");
                    return "Error: Planet code cannot be null or empty";
                }

                // 查找目标星球
                var planet = Base.planetLoader?.planets?.Values?.FirstOrDefault(p => p?.codeName != null && p.codeName.Equals(planetCode, StringComparison.OrdinalIgnoreCase));
                if (planet == null)
                {
                    Debug.LogError($"[Control] CreateObject: Planet '{planetCode}' not found");
                    return $"Error: Planet '{planetCode}' not found";
                }

                GameObject createdObject = null;
                string resultMessage = "";

                // 根据对象类型创建不同的对象
                switch (objectType.ToLower())
                {
                    case "astronaut":
                    case "eva":
                        // 创建宇航员EVA
                        if (AstronautManager.main?.astronautPrefab != null)
                        {
                            var astronaut = AstronautManager.main.SpawnEVA(
                                string.IsNullOrEmpty(objectName) ? "Astronaut" : objectName,
                                new Location(planet, new Double2(x, y), Double2.zero),
                                rotation, angularVelocity, ragdoll, fuelPercent, temperature
                            );
                            
                            // 初始化宇航员的统计信息，避免任务日志显示异常
                            if (astronaut.stats != null)
                            {
                                try
                                {
                                    // 为宇航员创建新的日志分支
                                    int newBranch;
                                    SFS.Stats.LogManager.main.CreateRoot(out newBranch);
                                    
                                    // 初始化统计记录器
                                    astronaut.stats.Load(newBranch);
                                }
                                catch (System.Exception ex)
                                {
                                    Debug.LogWarning($"[Control] CreateObject: Failed to initialize astronaut stats: {ex.Message}");
                                }
                            }
                            
                            // 确保宇航员处于物理模式并与地形碰撞
                            if (astronaut.physics != null)
                            {
                                // 设置为物理模式
                                astronaut.physics.PhysicsMode = true;
                                
                                // 确保Rigidbody2D启用
                                if (astronaut.rb2d != null)
                                {
                                    astronaut.rb2d.simulated = true;
                                    astronaut.rb2d.velocity = Vector2.zero;
                                    astronaut.rb2d.angularVelocity = angularVelocity;
                                }
                                
                                // 确保碰撞器启用
                                Collider2D[] colliders = astronaut.GetComponentsInChildren<Collider2D>();
                                foreach (var collider in colliders)
                                {
                                    collider.enabled = true;
                                }
                                
                                // 设置正确的层级以与地形碰撞
                                astronaut.gameObject.layer = LayerMask.NameToLayer("Astronaut");
                                
                                // 确保宇航员在正确的位置
                                Vector2 planetPosition = new Vector2((float)x, (float)y);
                                Vector3 globalPosition = planet.transform.position + new Vector3(planetPosition.x, planetPosition.y, 0f);
                                astronaut.transform.position = globalPosition;
                                
                                // 强制更新物理状态
                                astronaut.physics.SetLocationAndState(
                                    new Location(planet, new Double2(x, y), Double2.zero), 
                                    true  // 设置为物理模式
                                );
                            }
                            
                            createdObject = astronaut.gameObject;
                            resultMessage = $"astronaut EVA (rotation: {rotation}, angularVel: {angularVelocity}, ragdoll: {ragdoll}, fuel: {fuelPercent:P0}, temp: {temperature}K)";
                        }
                        else
                        {
                            Debug.LogError("[Control] CreateObject: Astronaut prefab not available");
                            return "Error: Astronaut prefab not available";
                        }
                        break;

                    case "flag":
                        // 创建旗子
                        if (AstronautManager.main?.flagPrefab != null)
                        {
                            var flag = AstronautManager.main.SpawnFlag(
                                new Location(planet, new Double2(x, y), Double2.zero),
                                flagDirection
                            );
                            
                            // 设置旗子方向
                            flag.direction = flagDirection;
                            
                            // 如果不需要动画，直接设置最终状态
                            if (!showFlagAnimation)
                            {
                                flag.holder.localScale = new Vector2((float)flagDirection, 1f);
                                flag.holder.rotation = Quaternion.Euler(0f, 0f, (float)flag.location.Value.position.AngleDegrees - 90f);
                                flag.mapIcon.SetRotation(flag.holder.rotation.eulerAngles.z + 90f);
                            }
                            else
                            {
                                // 显示种植动画
                                flag.ShowPlantAnimation();
                            }
                            
                            createdObject = flag.gameObject;
                            resultMessage = $"flag (direction: {flagDirection}, animation: {showFlagAnimation})";
                        }
                        else
                        {
                            Debug.LogError("[Control] CreateObject: Flag prefab not available");
                            return "Error: Flag prefab not available";
                        }
                        break;

                    case "explosion":
                    case "explosionparticle":
                        // 直接执行真正的零件爆炸，不创建临时对象
                        // 输入：火箭的SFS坐标 (x, y)
                        // 需要转换为Unity世界坐标
                        Vector3 explosionPosition = new Vector3((float)x, (float)y, 0f);
                        Vector3 explosionGlobalPosition = planet.transform.position + explosionPosition; // 星球位置 + 火箭相对坐标 = Unity世界坐标
                        
                        // 创建爆炸视觉效果
                        if (!createSound)
                        {
                            // 使用反射访问EffectManager.main.explosionPrefab
                            var effectManagerType = typeof(EffectManager);
                            var mainField = effectManagerType.GetField("main", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                            var mainInstance = mainField?.GetValue(null) as EffectManager;
                            
                            if (mainInstance != null && mainInstance.explosionPrefab != null)
                            {
                                var explosionEffect = EffectManager.CreateEffect(mainInstance.explosionPrefab, explosionGlobalPosition, 8f);
                                explosionEffect.localScale = Vector3.one * explosionSize;
                            }
                            else
                            {
                                Debug.LogError("[Control] CreateObject: Explosion prefab not available");
                                return "Error: Explosion prefab not available";
                            }
                        }
                        else
                        {
                            EffectManager.CreateExplosion(explosionGlobalPosition, explosionSize);
                        }
                        
                        // 根据参数决定是否执行零件爆炸逻辑
                        string explosionResult = "";
                        if (createDamage)
                        {
                            explosionResult = ExecutePartExplosion(explosionGlobalPosition, explosionSize, createShake);
                        }
                        else
                        {
                            explosionResult = "Visual effect only - no damage";
                        }
                        
                        // 创建一个简单的标记对象来表示爆炸已发生
                        createdObject = new GameObject(string.IsNullOrEmpty(objectName) ? "ExplosionMarker" : objectName);
                        createdObject.transform.position = explosionGlobalPosition;
                        
                        resultMessage = $"explosion (size: {explosionSize}, sound: {createSound}, shake: {createShake}, damage: {createDamage}) - {explosionResult}";
                        break;

                    default:
                        Debug.LogError($"[Control] CreateObject: Unknown object type '{objectType}'");
                        return $"Error: Unknown object type '{objectType}'. Supported types: astronaut/eva, flag, explosion/explosionparticle";
                }

                if (createdObject == null)
                {
                    Debug.LogError("[Control] CreateObject: Failed to create object");
                    return "Error: Failed to create object";
                }

                // 设置对象位置
                Vector2 objectPlanetPosition = new Vector2((float)x, (float)y);
                Vector3 objectGlobalPosition = planet.transform.position + new Vector3(objectPlanetPosition.x, objectPlanetPosition.y, 0f);
                createdObject.transform.position = objectGlobalPosition;

                // 设置对象名称
                if (!string.IsNullOrEmpty(objectName))
                {
                    createdObject.name = objectName;
                }

                // 设置隐藏状态
                if (hidden)
                {
                    createdObject.SetActive(false);
                }

                // 将对象设置为星球的子对象
                if (planet.transform != null)
                {
                    createdObject.transform.SetParent(planet.transform);
                }


                return $"Success: Created {resultMessage} '{createdObject.name}' with ID {createdObject.GetInstanceID()}";
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Control] CreateObject error: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        private static string ExecutePartExplosion(Vector3 explosionPosition, float explosionSize, bool createShake)
        {
            try
            {
                if (GameManager.main?.rockets == null)
                {
                    Debug.LogWarning("[Control] ExecutePartExplosion: No rockets available for explosion effects");
                    return "No rockets available";
                }

                // 计算爆炸影响范围（基于爆炸大小）
                float explosionRadius = explosionSize * 5f; // 爆炸大小 * 5 = 影响半径
                


                // 遍历所有火箭，检查是否有零件在爆炸范围内
                var rocketsToProcess = new List<SFS.World.Rocket>(GameManager.main.rockets);
                
                int rocketsAffected = 0;
                int partsDestroyed = 0;
                int partsDisconnected = 0;
                
                foreach (var rocket in rocketsToProcess)
                {
                    if (rocket == null || rocket.partHolder?.parts == null) continue;

                    // 检查火箭是否在爆炸范围内
                    float distanceToExplosion = Vector3.Distance(rocket.transform.position, explosionPosition);
                    
                    if (distanceToExplosion <= explosionRadius)
                    {

                        
                        // 处理火箭的零件爆炸
                        var result = ProcessRocketExplosion(rocket, explosionPosition, explosionRadius, distanceToExplosion);
                        rocketsAffected++;
                        partsDestroyed += result.partsDestroyed;
                        partsDisconnected += result.partsDisconnected;
                    }
                }
                
                return $"Affected {rocketsAffected} rockets, destroyed {partsDestroyed} parts, disconnected {partsDisconnected} parts";
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Control] ExecutePartExplosion error: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        // 处理火箭的零件爆炸
        private static (int partsDestroyed, int partsDisconnected) ProcessRocketExplosion(SFS.World.Rocket rocket, Vector3 explosionPosition, float explosionRadius, float distanceToExplosion)
        {
            try
            {
                if (rocket?.partHolder?.parts == null) return (0, 0);

                var partsToDestroy = new List<SFS.Parts.Part>();
                var partsToDisconnect = new List<SFS.Parts.Part>();

                // 遍历火箭的所有零件
                foreach (var part in rocket.partHolder.parts)
                {
                    if (part == null) continue;

                    // 计算零件到爆炸中心的距离
                    float partDistance = Vector3.Distance(part.transform.position, explosionPosition);
                    
                    if (partDistance <= explosionRadius)
                    {
                        // 根据距离和爆炸大小计算破坏概率
                        float damageProbability = CalculateExplosionDamage(partDistance, explosionRadius, part.mass.Value);
                        
                        if (UnityEngine.Random.Range(0f, 1f) < damageProbability)
                        {
                            // 零件将被摧毁
                            partsToDestroy.Add(part);
                            //Debug.Log($"[Control] ProcessRocketExplosion: Part '{part.name}' will be destroyed (distance: {partDistance:F2}, probability: {damageProbability:F2})");
                        }
                        else if (damageProbability > 0.3f) // 中等伤害概率
                        {
                            // 零件将被断开连接
                            partsToDisconnect.Add(part);
                            //Debug.Log($"[Control] ProcessRocketExplosion: Part '{part.name}' will be disconnected (distance: {partDistance:F2}, probability: {damageProbability:F2})");
                        }
                    }
                }

                // 执行零件摧毁
                foreach (var part in partsToDestroy)
                {
                    try
                    {
                        // 在零件位置创建额外的爆炸效果
                        Vector3 partExplosionPos = part.transform.position;
                        float partExplosionSize = Mathf.Max(0.5f, part.mass.Value * 0.5f);
                        
                        // 创建零件爆炸效果
                        EffectManager.CreateExplosion(partExplosionPos, partExplosionSize);
                        
                        // 摧毁零件
                        part.DestroyPart(true, true, SFS.World.DestructionReason.TerrainCollision);
                        

                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[Control] ProcessRocketExplosion: Failed to destroy part '{part.name}': {ex.Message}");
                    }
                }

                // 执行零件断开连接
                foreach (var part in partsToDisconnect)
                {
                    try
                    {
                        // 尝试断开零件的连接
                        var connectedJoints = rocket.jointsGroup.GetConnectedJoints(part);
                        
                        if (connectedJoints.Count > 0)
                        {
                            // 断开第一个连接
                            bool split;
                            SFS.World.Rocket newRocket;
                            SFS.World.JointGroup.DestroyJoint(connectedJoints[0], rocket, out split, out newRocket);
                            
                            if (split && newRocket != null)
                            {
                                // 启用碰撞免疫，防止立即碰撞
                                rocket.EnableCollisionImmunity(1.5f);
                                newRocket.EnableCollisionImmunity(1.5f);
                                
                                // 如果原火箭是玩家控制的，设置新的控制目标
                                if (rocket.isPlayer)
                                {
                                    SFS.World.Rocket.SetPlayerToBestControllable(new SFS.World.Rocket[] { rocket, newRocket });
                                }
                                

                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[Control] ProcessRocketExplosion: Failed to disconnect part '{part.name}': {ex.Message}");
                    }
                }

                // 如果火箭没有零件了，摧毁火箭
                if (rocket.partHolder.parts.Count == 0)
                {

                    SFS.World.RocketManager.DestroyRocket(rocket, SFS.World.DestructionReason.TerrainCollision);
                }
                
                return (partsToDestroy.Count, partsToDisconnect.Count);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Control] ProcessRocketExplosion error: {ex.Message}");
                return (0, 0);
            }
        }

        // 计算爆炸伤害概率
        private static float CalculateExplosionDamage(float distance, float explosionRadius, float partMass)
        {
            // 基础破坏概率：距离越近，概率越高
            float baseProbability = 1f - (distance / explosionRadius);
            
            // 质量因子：质量越大的零件越难被摧毁
            float massFactor = Mathf.Clamp01(1f - (partMass / 10f)); // 假设10是最大质量
            
            // 随机因子：增加一些随机性
            float randomFactor = UnityEngine.Random.Range(0.8f, 1.2f);
            
            // 最终概率
            float finalProbability = baseProbability * massFactor * randomFactor;
            
            // 确保概率在合理范围内
            return Mathf.Clamp01(finalProbability);
        }

        // 修改火箭地图图标的RGBA值
        public static string SetMapIconColor(string rgbaValue, string rocketIdOrName = null)
        {
            try
            {
                var rocket = FindRocket(rocketIdOrName);
                if (rocket == null)
                {
                    Debug.LogError("[Control] SetMapIconColor: Rocket not found");
                    return "Error: Rocket not found";
                }

                if (rocket.mapIcon == null || rocket.mapIcon.mapIcon == null)
                {
                    Debug.LogError("[Control] SetMapIconColor: Map icon not available");
                    return "Error: Map icon not available";
                }

                // 获取地图图标的SpriteRenderer组件
                var spriteRenderer = rocket.mapIcon.mapIcon.GetComponentInChildren<SpriteRenderer>();
                if (spriteRenderer == null)
                {
                    Debug.LogError("[Control] SetMapIconColor: SpriteRenderer not found on map icon");
                    return "Error: SpriteRenderer not found on map icon";
                }

                // 解析RGBA值
                Color newColor;
                if (string.IsNullOrEmpty(rgbaValue))
                {
                    Debug.LogError("[Control] SetMapIconColor: RGBA value cannot be null or empty");
                    return "Error: RGBA value cannot be null or empty";
                }

                if (rgbaValue.StartsWith("#"))
                {
                    // 十六进制格式：#RRGGBB 或 #RRGGBBAA
                    if (!ColorUtility.TryParseHtmlString(rgbaValue, out newColor))
                    {
                        Debug.LogError($"[Control] SetMapIconColor: Invalid hex color format: {rgbaValue}");
                        return $"Error: Invalid hex color format: {rgbaValue}";
                    }
                }
                else if (rgbaValue.Contains(","))
                {
                    // 逗号分隔格式：R,G,B,A 或 R,G,B
                    var parts = rgbaValue.Split(',');
                    if (parts.Length < 3 || parts.Length > 4)
                    {
                        Debug.LogError($"[Control] SetMapIconColor: Invalid RGBA format. Expected 3-4 values separated by commas: {rgbaValue}");
                        return $"Error: Invalid RGBA format. Expected 3-4 values separated by commas: {rgbaValue}";
                    }

                    if (!float.TryParse(parts[0], out float r) || !float.TryParse(parts[1], out float g) || 
                        !float.TryParse(parts[2], out float b))
                    {
                        Debug.LogError($"[Control] SetMapIconColor: Invalid number format in RGBA values: {rgbaValue}");
                        return $"Error: Invalid number format in RGBA values: {rgbaValue}";
                    }

                    float a = 1.0f; // 默认透明度
                    if (parts.Length == 4 && float.TryParse(parts[3], out float alpha))
                    {
                        a = alpha;
                    }

                    // 限制值在0-1范围内
                    newColor = new Color(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b), Mathf.Clamp01(a));
                }
                else
                {
                    Debug.LogError($"[Control] SetMapIconColor: Unsupported RGBA format: {rgbaValue}. Use hex (#RRGGBB) or comma-separated (R,G,B,A) format");
                    return $"Error: Unsupported RGBA format: {rgbaValue}. Use hex (#RRGGBB) or comma-separated (R,G,B,A) format";
                }

                // 应用新颜色
                spriteRenderer.color = newColor;

                // 保存用户设置的颜色到补丁系统中，防止缩放时被重置为白色
                var patchType = Type.GetType("SFSControl.Patch_MapIcon_UpdateAlpha, SFSControl");
                if (patchType != null)
                {
                    var setUserColorMethod = patchType.GetMethod("SetUserColor", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (setUserColorMethod != null)
                    {
                        // 保存完整的RGBA值，包括透明度
                        setUserColorMethod.Invoke(null, new object[] { spriteRenderer, newColor, true });
                    }
                }

                //Debug.Log($"[Control] SetMapIconColor: Successfully set map icon color to RGBA({newColor.r:F3}, {newColor.g:F3}, {newColor.b:F3}, {newColor.a:F3}) for rocket '{rocket.rocketName ?? rocketIdOrName}'");
                return $"Success: Map icon color set to RGBA({newColor.r:F3}, {newColor.g:F3}, {newColor.b:F3}, {newColor.a:F3})";
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Control] SetMapIconColor error: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

    }
}