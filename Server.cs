using System;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using SFS;
using SFS.World;
using System.IO;
using System.Reflection;
using Newtonsoft.Json.Serialization;
using System.Linq;
using System.Collections.Generic;
using System.Dynamic;
using System.Drawing;
using System.Drawing.Imaging;



namespace SFSControl
{


    public class IgnoreNormalizedResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(System.Reflection.MemberInfo member, MemberSerialization memberSerialization)
        {
            var prop = base.CreateProperty(member, memberSerialization);
            if (prop.PropertyName == "normalized")
            {
                prop.ShouldSerialize = _ => false;
            }
            return prop;
        }
    }

    public static class RocketInfoHelper
    {
        public static object AddNormalizedFields(object obj)
        {
            if (obj == null) return null;
            var type = obj.GetType();
            // 只处理Double2类型
            if (type.Name == "Double2")
            {
                dynamic exp = new ExpandoObject();
                var dict = (IDictionary<string, object>)exp;
                double x = 0, y = 0;
                foreach (var prop in type.GetProperties())
                {
                    var val = prop.GetValue(obj);
                    dict[prop.Name] = val;
                    if (prop.Name == "x") x = (double)val;
                    if (prop.Name == "y") y = (double)val;
                }
                double len = Math.Sqrt(x * x + y * y);
                dict["normalized_x"] = len != 0 ? x / len : 0;
                dict["normalized_y"] = len != 0 ? y / len : 0;
                return exp;
            }
            // 递归处理List
            if (obj is System.Collections.IEnumerable && !(obj is string))
            {
                var list = new List<object>();
                foreach (var item in (System.Collections.IEnumerable)obj)
                    list.Add(AddNormalizedFields(item));
                return list;
            }
            // 递归处理其它对象
            if (type.IsClass && type != typeof(string))
            {
                dynamic exp = new ExpandoObject();
                var dict = (IDictionary<string, object>)exp;
                foreach (var prop in type.GetProperties())
                {
                    dict[prop.Name] = AddNormalizedFields(prop.GetValue(obj));
                }
                return exp;
            }
            return obj;
        }
    }

    // 跳过UnityEngine.Object字段
    public class UnityObjectIgnoreResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(System.Reflection.MemberInfo member, MemberSerialization memberSerialization)
        {
            var prop = base.CreateProperty(member, memberSerialization);
            if (typeof(UnityEngine.Object).IsAssignableFrom(prop.PropertyType))
            {
                prop.ShouldSerialize = _ => false;
            }
            return prop;
        }
    }

    public class Server : MonoBehaviour
    {
        private HttpListener listener;
        private Thread listenerThread;
        private bool isRunning = false;
        private readonly ConcurrentQueue<HttpListenerContext> requestQueue = new ConcurrentQueue<HttpListenerContext>();
        


        public void StartServer(int port)
        {
            if (isRunning) return;

            listener = new HttpListener();
            listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            
            listenerThread = new Thread(() =>
            {
                try
                {
                    listener.Start();
                    isRunning = true;
                    Debug.Log($"[Server] Started listening on port {port}");

                    while (isRunning)
                    {
                        var context = listener.GetContext();
                        requestQueue.Enqueue(context);
                    }
                }
                catch (Exception ex) when (ex is HttpListenerException)
                {

                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Server] Error: {ex.Message}");
                }
            });

            listenerThread.IsBackground = true;
            listenerThread.Start();
        }

        void Update()
        {
            // 处理HTTP请求队列
            while (requestQueue.TryDequeue(out var context))
            {
                HandleRequestOnMainThread(context);
            }
        }
        


        private void SendErrorResponse(HttpListenerContext context, string errorMessage)
        {
            try
            {
                if (context?.Response == null)
                {
                    UnityEngine.Debug.LogWarning("[SFSControl] Cannot send error response: context or response is null");
                    return;
                }

                var response = context.Response;
                response.StatusCode = 500;
                response.ContentType = "application/json";
                string responseString = JsonConvert.SerializeObject(new { error = errorMessage });
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[SFSControl] Error sending response: {ex.Message}");
            }
        }

        private void HandleRequestOnMainThread(HttpListenerContext context)
        {
            //Debug.Log("[Server] Handling HTTP request: " + context.Request.Url.AbsolutePath);
            var request = context.Request;
            var response = context.Response;
            string responseString = "";
            int statusCode = 200;

            try
            {
                //Debug.Log("[Server] Entering switch for: " + context.Request.Url.AbsolutePath);
                switch (request.Url.AbsolutePath)
                {
                    case "/screenshot":
                        // 检查设置是否已加载
                        if (SettingsManager.settings == null)
                        {
                            SettingsManager.Load();
                        }
                        
                        // 检查是否允许截屏
                        if (!SettingsManager.settings.allowScreenshot)
                        {
                            statusCode = 403;
                            responseString = JsonConvert.SerializeObject(new { error = "The server has not enabled permission for screen capture." });
                        }
                        else
                        {
                            try
                            {
                                // 获取SFS窗口的截图
                                byte[] screenshotData = Info.CaptureSFSWindow();
                                if (screenshotData != null && screenshotData.Length > 0)
                                {
                                    // 检查是否是有效的PNG图片
                                    if (screenshotData.Length > 8 && 
                                        screenshotData[0] == 0x89 && screenshotData[1] == 0x50 && 
                                        screenshotData[2] == 0x4E && screenshotData[3] == 0x47)
                                    {
                                        response.ContentType = "image/png";
                                        response.ContentLength64 = screenshotData.Length;
                                        response.OutputStream.Write(screenshotData, 0, screenshotData.Length);
                                        response.OutputStream.Close();
                                        return; // 直接返回，不执行后面的finally块
                                    }
                                    else
                                    {
                                        statusCode = 500;
                                        responseString = JsonConvert.SerializeObject(new { error = "Screenshot capture failed: Invalid image data" });
                                    }
                                }
                                else
                                {
                                    statusCode = 500;
                                    responseString = JsonConvert.SerializeObject(new { error = "Screenshot capture failed: Unable to capture screenshot" });
                                }
                            }
                            catch (Exception ex)
                            {
                                statusCode = 500;
                                responseString = JsonConvert.SerializeObject(new { error = $"Screenshot capture failed: {ex.Message}" });
                            }
                        }
                        break;
                    case "/rocket_sim":
                        string rocketIdOrName2 = null;
                        var rocketQueryDict = ParseQueryString(request.Url.Query);
                        if (rocketQueryDict.ContainsKey("rocketIdOrName"))
                            rocketIdOrName2 = rocketQueryDict["rocketIdOrName"];
                        var rocketInfo = Info.GetRocketInfo(rocketIdOrName2);
                        responseString = JsonConvert.SerializeObject(rocketInfo, Formatting.Indented, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
                        break;
                    case "/rockets":
                        //Debug.Log("[Server] Calling RocketSave for all rockets...");
                        var saves = new System.Collections.Generic.List<SFS.World.RocketSave>();
                        if (GameManager.main?.rockets != null)
                        {
                            foreach (var rocket in GameManager.main.rockets)
                            {
                                if (rocket != null)
                                    saves.Add(new SFS.World.RocketSave(rocket));
                            }
                        }
                        responseString = JsonConvert.SerializeObject(saves, Formatting.Indented, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
                        break;
                    case "/planet":
                        string codename = null;
                        var planetQueryDict = ParseQueryString(request.Url.Query);
                        if (planetQueryDict.ContainsKey("codename"))
                            codename = planetQueryDict["codename"];
                        var planetInfo = Info.GetCurrentPlanetInfo(codename);
                        responseString = JsonConvert.SerializeObject(planetInfo, Formatting.Indented, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
                        break;
                    case "/planets":
                        var planetsInfo = Info.GetAllPlanetsInfo();
                        responseString = JsonConvert.SerializeObject(planetsInfo, Formatting.Indented, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
                        break;
                    case "/other":
                        string rocketIdOrName_other = null;
                        var otherQueryDict = ParseQueryString(request.Url.Query);
                        if (otherQueryDict.ContainsKey("rocketIdOrName"))
                            rocketIdOrName_other = otherQueryDict["rocketIdOrName"];
                        var otherInfo = Info.GetOtherInfo(rocketIdOrName_other);
                        responseString = JsonConvert.SerializeObject(otherInfo, Formatting.Indented, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
                        break;
                    case "/rocket":
                        string rocketIdOrName_rocket = null;
                        var rocketSaveQueryDict = ParseQueryString(request.Url.Query);
                        if (rocketSaveQueryDict.ContainsKey("rocketIdOrName"))
                            rocketIdOrName_rocket = rocketSaveQueryDict["rocketIdOrName"];
                        var rocketSaveInfo = Info.GetRocketSaveInfo(rocketIdOrName_rocket);
                        if (rocketSaveInfo != null)
                        {
                            var settings = new JsonSerializerSettings {
                                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                                PreserveReferencesHandling = PreserveReferencesHandling.None,
                                ContractResolver = new UnityObjectIgnoreResolver()
                            };
                            responseString = JsonConvert.SerializeObject(rocketSaveInfo, Formatting.Indented, settings);
                        }
                        else
                        {
                            responseString = JsonConvert.SerializeObject(new { error = "Player is not controlling a rocket" }, Formatting.Indented);
                        }
                        break;
                    case "/debuglog":
                        var logList = Info.GetConsoleLog();
                        responseString = JsonConvert.SerializeObject(new { log = logList });
                        break;
                    case "/mission":
                        var missionInfo = Info.GetMissionInfo();
                        responseString = JsonConvert.SerializeObject(missionInfo, Formatting.Indented, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
                        break;
                    case "/control":
                        string body;
                        using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                            body = reader.ReadToEnd();
                        var controlReq = JsonConvert.DeserializeObject<ControlRequest>(body);
                        string result = "Error: Unknown method";
                        try
                        {
                            switch (controlReq.method)
                            {
                                case "SetThrottle":
                                    string rocketIdOrName_throttle = controlReq.args.Length > 1 && controlReq.args[1] != null ? controlReq.args[1].ToString() : null;
                                    result = Control.SetThrottle(Convert.ToDouble(controlReq.args[0]), rocketIdOrName_throttle);
                                    break;
                                case "SetRCS":
                                    string rocketIdOrName_rcs = controlReq.args.Length > 1 && controlReq.args[1] != null ? controlReq.args[1].ToString() : null;
                                    result = Control.SetRCS(Convert.ToBoolean(controlReq.args[0]), rocketIdOrName_rcs);
                                    break;
                                case "Stage":
                                    string rocketIdOrName_stage = controlReq.args.Length > 0 && controlReq.args[0] != null ? controlReq.args[0].ToString() : null;
                                    result = Control.Stage(rocketIdOrName_stage);
                                    break;
                                case "Rotate":
                                    bool isTarget = false;
                                    float angle = 0;
                                    string reference = null;
                                    string direction = null;
                                    string rocketIdOrName = null;
                                    if (controlReq.args.Length > 0 && controlReq.args[0] != null)
                                        isTarget = Convert.ToBoolean(controlReq.args[0]);
                                    if (controlReq.args.Length > 1 && controlReq.args[1] != null)
                                        angle = Convert.ToSingle(controlReq.args[1]);
                                    if (controlReq.args.Length > 2 && controlReq.args[2] != null)
                                        reference = controlReq.args[2].ToString();
                                    if (controlReq.args.Length > 3 && controlReq.args[3] != null)
                                        direction = controlReq.args[3].ToString();
                                    if (controlReq.args.Length > 4 && controlReq.args[4] != null)
                                        rocketIdOrName = controlReq.args[4].ToString();
                                    result = Control.Rotate(isTarget, angle, reference, direction, rocketIdOrName);
                                    break;
                                case "UsePart":
                                    string rocketIdOrName_usepart = controlReq.args.Length > 1 && controlReq.args[1] != null ? controlReq.args[1].ToString() : null;
                                    result = Control.UsePart(Convert.ToInt32(controlReq.args[0]), rocketIdOrName_usepart);
                                    break;
                                case "ClearDebris":
                                    result = Control.ClearDebris();
                                    break;
                                case "Build":
                                    result = Control.Build(controlReq.args[0].ToString());
                                    break;
                                case "RcsThrust":
                                    string rocketIdOrName_rcsthrust = controlReq.args.Length > 2 && controlReq.args[2] != null ? controlReq.args[2].ToString() : null;
                                    result = Control.RcsThrust(controlReq.args[0].ToString(), Convert.ToSingle(controlReq.args[1]), rocketIdOrName_rcsthrust);
                                    break;
                                case "SwitchToBuild":
                                    result = Control.SwitchToBuild();
                                    break;
                                case "ClearBlueprint":
                                    result = Control.ClearBlueprint();
                                    break;
                                case "SetRotation":
                                    string rocketIdOrName_setrot = controlReq.args.Length > 1 && controlReq.args[1] != null ? controlReq.args[1].ToString() : null;
                                    result = Control.SetRotation(Convert.ToSingle(controlReq.args[0]), rocketIdOrName_setrot);
                                    break;
                                case "SetState":
                                    double? x = controlReq.args.Length > 0 && controlReq.args[0] != null ? (double?)Convert.ToDouble(controlReq.args[0]) : null;
                                    double? y = controlReq.args.Length > 1 && controlReq.args[1] != null ? (double?)Convert.ToDouble(controlReq.args[1]) : null;
                                    double? vx = controlReq.args.Length > 2 && controlReq.args[2] != null ? (double?)Convert.ToDouble(controlReq.args[2]) : null;
                                    double? vy = controlReq.args.Length > 3 && controlReq.args[3] != null ? (double?)Convert.ToDouble(controlReq.args[3]) : null;
                                    double? angularVelocity = controlReq.args.Length > 4 && controlReq.args[4] != null ? (double?)Convert.ToDouble(controlReq.args[4]) : null;
                                    string blueprintJson = controlReq.args.Length > 5 && controlReq.args[5] != null ? controlReq.args[5].ToString() : null;
                                    string rocketIdOrName_state = controlReq.args.Length > 6 && controlReq.args[6] != null ? controlReq.args[6].ToString() : null;
                                    result = Control.SetState(x, y, vx, vy, angularVelocity, blueprintJson, rocketIdOrName_state);
                                    break;
                                case "ShowToast":
                                    result = Control.ShowToast(controlReq.args[0].ToString());
                                    break;
                                case "StopRotate":
                                    string rocketIdOrName_stoprotate = controlReq.args.Length > 0 && controlReq.args[0] != null ? controlReq.args[0].ToString() : null;
                                    bool stopCoroutine = controlReq.args.Length > 1 && controlReq.args[1] != null ? Convert.ToBoolean(controlReq.args[1]) : true;
                                    result = Control.StopRotate(rocketIdOrName_stoprotate, stopCoroutine);
                                    break;
                                case "AddStage":
                                    string rocketIdOrName_addstage = controlReq.args.Length > 2 && controlReq.args[2] != null ? controlReq.args[2].ToString() : null;
                                    result = Control.AddStage(Convert.ToInt32(controlReq.args[0]), JsonConvert.DeserializeObject<int[]>(controlReq.args[1].ToString()), rocketIdOrName_addstage);
                                    break;
                                case "RemoveStage":
                                    string rocketIdOrName_removestage = controlReq.args.Length > 1 && controlReq.args[1] != null ? controlReq.args[1].ToString() : null;
                                    result = Control.RemoveStage(Convert.ToInt32(controlReq.args[0]), rocketIdOrName_removestage);
                                    break;
                                case "Launch":
                                    result = Control.Launch();
                                    break;
                                case "SwitchRocket":
                                    result = Control.SwitchRocket(controlReq.args[0].ToString());
                                    break;
                                case "RenameRocket":
                                    result = Control.RenameRocket(controlReq.args[0].ToString(), controlReq.args[1].ToString());
                                    break;
                                case "SetTarget":
                                    result = Control.SetTarget(controlReq.args[0].ToString());
                                    break;
                                case "ClearTarget":
                                    result = Control.ClearTarget();
                                    break;
                                case "TimewarpPlus":
                                    result = Control.TimewarpPlus();
                                    break;
                                case "TimewarpMinus":
                                    result = Control.TimewarpMinus();
                                    break;
                                case "SetTimewarp":
                                    double speed = Convert.ToDouble(controlReq.args[0]);
                                    bool realtimePhysics = controlReq.args.Length > 1 && controlReq.args[1] != null ? Convert.ToBoolean(controlReq.args[1]) : false;
                                    bool showMessage = controlReq.args.Length > 2 && controlReq.args[2] != null ? Convert.ToBoolean(controlReq.args[2]) : true;
                                    result = Control.SetTimewarp(speed, realtimePhysics, showMessage);
                                    break;
                                case "Wait":
                                    string mode = controlReq.args.Length > 0 && controlReq.args[0] != null ? controlReq.args[0].ToString() : "transfer";
                                    double? parameter = controlReq.args.Length > 1 && controlReq.args[1] != null ? (double?)Convert.ToDouble(controlReq.args[1]) : null;
                                    result = Control.WaitForWindow(mode, parameter);
                                    break;
                                case "SetCheat":
                                    result = Control.SetCheat(controlReq.args[0].ToString(), Convert.ToBoolean(controlReq.args[1]));
                                    break;
                                case "Revert":
                                    result = Control.Revert(controlReq.args[0].ToString());
                                    break;
                                case "SetMainEngineOn":
                                    string rocketIdOrName_engine = controlReq.args.Length > 1 && controlReq.args[1] != null ? controlReq.args[1].ToString() : null;
                                    result = Control.SetMainEngineOn(Convert.ToBoolean(controlReq.args[0]), rocketIdOrName_engine);
                                    break;
                                case "SetOrbit":
                                    double radius = Convert.ToDouble(controlReq.args[0]);
                                    double? eccentricity = controlReq.args.Length > 1 && controlReq.args[1] != null ? (double?)Convert.ToDouble(controlReq.args[1]) : null;
                                    double? trueAnomaly = controlReq.args.Length > 2 && controlReq.args[2] != null ? (double?)Convert.ToDouble(controlReq.args[2]) : null;
                                    bool counterclockwise = controlReq.args.Length > 3 && controlReq.args[3] != null ? Convert.ToBoolean(controlReq.args[3]) : true;
                                    string planetCode = controlReq.args.Length > 4 && controlReq.args[4] != null ? controlReq.args[4].ToString() : null;
                                    string rocketIdOrName_orbit = controlReq.args.Length > 5 && controlReq.args[5] != null ? controlReq.args[5].ToString() : null;
                                    result = Control.SetOrbit(radius, eccentricity, trueAnomaly, counterclockwise, planetCode, rocketIdOrName_orbit);
                                    break;
                                case "DeleteRocket":
                                    string rocketIdOrName_del = controlReq.args.Length > 0 && controlReq.args[0] != null ? controlReq.args[0].ToString() : null;
                                    result = Control.DeleteRocket(rocketIdOrName_del);
                                    break;
                                case "LogMessage":
                                    result = Control.LogMessage(controlReq.args[0].ToString(), controlReq.args[1].ToString());
                                    break;
                                case "CompleteChallenge":
                                    result = Control.CompleteChallenge(controlReq.args[0]?.ToString());
                                    break;
                                case "Track":
                                    result = Control.Track(controlReq.args[0]?.ToString());
                                    break;
                                case "SwitchMapView":
                                    bool? on = null;
                                    if (controlReq.args.Length > 0 && controlReq.args[0] != null)
                                        on = Convert.ToBoolean(controlReq.args[0]);
                                    result = Control.SwitchMapView(on);
                                    break;
                                case "Unfocus":
                                    result = Control.Unfocus();
                                    break;
                                case "TransferFuel":
                                    int fromTankId = Convert.ToInt32(controlReq.args[0]);
                                    int toTankId = Convert.ToInt32(controlReq.args[1]);
                                    string rocketIdOrName_transfer = controlReq.args.Length > 2 && controlReq.args[2] != null ? controlReq.args[2].ToString() : null;
                                    result = Control.TransferFuel(fromTankId, toTankId, rocketIdOrName_transfer);
                                    break;
                                case "StopFuelTransfer":
                                    string rocketIdOrName_stop = controlReq.args.Length > 0 && controlReq.args[0] != null ? controlReq.args[0].ToString() : null;
                                    result = Control.StopFuelTransfer(rocketIdOrName_stop);
                                    break;
                                case "QuicksaveManager":
                                    string operation = controlReq.args.Length > 0 && controlReq.args[0] != null ? controlReq.args[0].ToString() : "save";
                                    string quicksaveName = controlReq.args.Length > 1 && controlReq.args[1] != null ? controlReq.args[1].ToString() : null;
                                    result = Control.QuicksaveManager(operation, quicksaveName);
                                    break;
                                case "WheelControl":
                                    bool? enable = controlReq.args.Length > 0 && controlReq.args[0] != null ? (bool?)Convert.ToBoolean(controlReq.args[0]) : null;
                                    float turnAxis = controlReq.args.Length > 1 && controlReq.args[1] != null ? Convert.ToSingle(controlReq.args[1]) : 0f;
                                    string rocketIdOrName_wheel = controlReq.args.Length > 2 && controlReq.args[2] != null ? controlReq.args[2].ToString() : null;
                                    result = Control.WheelControl(enable, turnAxis, rocketIdOrName_wheel);
                                    break;
                                case "SetMapIconColor":
                                    string rgbaValue = controlReq.args[0].ToString();
                                    string rocketIdOrName_color = controlReq.args.Length > 1 && controlReq.args[1] != null ? controlReq.args[1].ToString() : null;
                                    result = Control.SetMapIconColor(rgbaValue, rocketIdOrName_color);
                                    break;
                                case "CreateRocket":
                                    string planetCode_rocket = controlReq.args.Length > 0 && controlReq.args[0] != null ? controlReq.args[0].ToString() : null;
                                    string blueprintJson_rocket = controlReq.args.Length > 1 && controlReq.args[1] != null ? controlReq.args[1].ToString() : null;
                                    string rocketName = controlReq.args.Length > 2 && controlReq.args[2] != null ? controlReq.args[2].ToString() : "";
                                    double x_rocket = controlReq.args.Length > 3 && controlReq.args[3] != null ? Convert.ToDouble(controlReq.args[3]) : 0.0;
                                    double y_rocket = controlReq.args.Length > 4 && controlReq.args[4] != null ? Convert.ToDouble(controlReq.args[4]) : 0.0;
                                    double vx_rocket = controlReq.args.Length > 5 && controlReq.args[5] != null ? Convert.ToDouble(controlReq.args[5]) : 0.0;
                                    double vy_rocket = controlReq.args.Length > 6 && controlReq.args[6] != null ? Convert.ToDouble(controlReq.args[6]) : 0.0;
                                    double vr_rocket = controlReq.args.Length > 7 && controlReq.args[7] != null ? Convert.ToDouble(controlReq.args[7]) : 0.0;
                                    result = Control.CreateRocket(planetCode_rocket, blueprintJson_rocket, rocketName, x_rocket, y_rocket, vx_rocket, vy_rocket, vr_rocket);
                                    break;
                                case "CreateObject":
                                    string objectType = controlReq.args.Length > 0 ? controlReq.args[0]?.ToString() ?? "" : "";
                                    string planetCode_obj = controlReq.args.Length > 1 ? controlReq.args[1]?.ToString() ?? "" : "";
                                    double x_obj = controlReq.args.Length > 2 && controlReq.args[2] != null ? Convert.ToDouble(controlReq.args[2]) : 0.0;
                                    double y_obj = controlReq.args.Length > 3 && controlReq.args[3] != null ? Convert.ToDouble(controlReq.args[3]) : 0.0;
                                    string objectName = controlReq.args.Length > 4 ? controlReq.args[4]?.ToString() ?? "" : "";
                                    bool hidden = controlReq.args.Length > 5 && controlReq.args[5] != null ? Convert.ToBoolean(controlReq.args[5]) : false;
                                    float explosionSize = controlReq.args.Length > 6 && controlReq.args[6] != null ? Convert.ToSingle(controlReq.args[6]) : 2.0f;
                                    bool createSound = controlReq.args.Length > 7 && controlReq.args[7] != null ? Convert.ToBoolean(controlReq.args[7]) : true;
                                    bool createShake = controlReq.args.Length > 8 && controlReq.args[8] != null ? Convert.ToBoolean(controlReq.args[8]) : true;
                                    float rotation_obj = controlReq.args.Length > 9 && controlReq.args[9] != null ? Convert.ToSingle(controlReq.args[9]) : 0f;
                                    float angularVelocity_obj = controlReq.args.Length > 10 && controlReq.args[10] != null ? Convert.ToSingle(controlReq.args[10]) : 0f;
                                    bool ragdoll = controlReq.args.Length > 11 && controlReq.args[11] != null ? Convert.ToBoolean(controlReq.args[11]) : false;
                                    double fuelPercent = controlReq.args.Length > 12 && controlReq.args[12] != null ? Convert.ToDouble(controlReq.args[12]) : 1.0;
                                    float temperature = controlReq.args.Length > 13 && controlReq.args[13] != null ? Convert.ToSingle(controlReq.args[13]) : 293.15f;
                                    int flagDirection = controlReq.args.Length > 14 && controlReq.args[14] != null ? Convert.ToInt32(controlReq.args[14]) : 1;
                                    bool showFlagAnimation = controlReq.args.Length > 15 && controlReq.args[15] != null ? Convert.ToBoolean(controlReq.args[15]) : true;
                                    bool createDamage = controlReq.args.Length > 16 && controlReq.args[16] != null ? Convert.ToBoolean(controlReq.args[16]) : true;
                                    
                                    result = Control.CreateObject(objectType, planetCode_obj, x_obj, y_obj, objectName, hidden, explosionSize, createSound, createShake, rotation_obj, angularVelocity_obj, ragdoll, fuelPercent, temperature, flagDirection, showFlagAnimation, createDamage);
                                    break;
                                default:
                                    result = "Error: Unknown method";
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            result = $"Error: {ex.Message}";
                        }
                        responseString = JsonConvert.SerializeObject(new { result });
                        break;
                    case "/rcall":
                        // 反射调用接口，可调用任何public静态方法
                        string modBody;
                        using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                            modBody = reader.ReadToEnd();
                        var rcallReq = JsonConvert.DeserializeObject<RcallRequest>(modBody);
                        string modResult = "Error: Unknown method";
                        try
                        {
                            string typeName = rcallReq.type;
                            string methodName = rcallReq.methodName;
                            object[] callArgs = rcallReq.callArgs ?? new object[0];
                            if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(methodName))
                            {
                                modResult = "Error: type and methodName required";
                            }
                            else
                            {
                                var type = Type.GetType(typeName) ?? AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => {
                                    try { return a.GetTypes(); } catch { return new Type[0]; }
                                }).FirstOrDefault(t => t.Name == typeName);
                                if (type == null)
                                {
                                    modResult = $"Error: Type '{typeName}' not found";
                                }
                                else
                                {
                                    var method = type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                                        .FirstOrDefault(m => m.Name == methodName && m.GetParameters().Length == callArgs.Length);
                                    if (method == null)
                                    {
                                        modResult = $"Error: Method '{methodName}' not found in type '{typeName}'";
                                    }
                                    else
                                    {
                                        var paramInfos = method.GetParameters();
                                        object[] realArgs = new object[callArgs.Length];
                                        for (int i = 0; i < callArgs.Length; i++)
                                        {
                                            realArgs[i] = Convert.ChangeType(callArgs[i], paramInfos[i].ParameterType);
                                        }
                                        var invokeResult = method.Invoke(null, realArgs);
                                        modResult = invokeResult == null ? "Success" : JsonConvert.SerializeObject(invokeResult);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            modResult = $"Error: {ex.Message}";
                        }
                        responseString = modResult;
                        break;
                    case "/planet_terrain":
                        string terrainPlanetCode = null;
                        double start = 0, end = 360;
                        int count = 360;
                        var terrainQueryDict = ParseQueryString(request.Url.Query);
                        if (terrainQueryDict.ContainsKey("planetCode"))
                            terrainPlanetCode = terrainQueryDict["planetCode"];
                        if (terrainQueryDict.ContainsKey("start"))
                            double.TryParse(terrainQueryDict["start"], out start);
                        if (terrainQueryDict.ContainsKey("end"))
                            double.TryParse(terrainQueryDict["end"], out end);
                        if (terrainQueryDict.ContainsKey("count"))
                            int.TryParse(terrainQueryDict["count"], out count);
                        var terrainArr = Info.GetTerrainProfile(terrainPlanetCode, start, end, count);
                        responseString = JsonConvert.SerializeObject(terrainArr);
                        break;
                    default:
                        statusCode = 404;
                        responseString = "Not Found";
                        break;
                }
            }
            catch (Exception ex)
            {
                statusCode = 500;
                responseString = $"Internal Server Error: {ex.Message}";
                Debug.LogError($"[Server] Request handling error: {ex}");
            }
            finally
            {
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.StatusCode = statusCode;
                response.ContentType = "application/json";
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
        }

        // 工具方法：解析 ?a=1&b=2 形式的查询字符串为字典
        private static Dictionary<string, string> ParseQueryString(string query)
        {
            var dict = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(query)) return dict;
            if (query.StartsWith("?")) query = query.Substring(1);
            foreach (var pair in query.Split('&'))
            {
                if (string.IsNullOrEmpty(pair)) continue;
                var kv = pair.Split(new[] { '=' }, 2);
                var key = Uri.UnescapeDataString(kv[0]);
                var value = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : "";
                dict[key] = value;
            }
            return dict;
        }

        public void StopServer()
        {
            if (isRunning)
            {
                isRunning = false;
                listener?.Stop();
                listenerThread?.Join();
                Debug.Log("[Server] Server stopped.");
            }
        }

        void OnDestroy()
        {
            StopServer();
        }
    }

    public class ControlRequest
    {
        public string method { get; set; }
        public object[] args { get; set; }
    }

    public class RcallRequest
    {
        public string type { get; set; }
        public string methodName { get; set; }
        public object[] callArgs { get; set; }
    }
}
