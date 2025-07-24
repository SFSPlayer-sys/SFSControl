using UnityEngine;

namespace SFSControl
{
    public static class CallMod
    {
        // 检查 SmartSASMod 是否安装
        public static bool IsSmartSASModInstalled()
        {
            return System.Type.GetType("SmartSASMod.GUI, SmartSASMod") != null;
        }



        // 设置SmartSASMod的SAS模式和角度偏移
        // Prograde/Target/Surface/None/Default
        public static string SetSAS(string rocketIdOrName, string directionMode, float? offset = null)
        {
            // 确保Control.FindRocket是public static
            if (!IsSmartSASModInstalled()) return "SmartSASMod is not installed";
            var rocket = SFSControl.Control.FindRocket(rocketIdOrName);
            if (rocket == null) return "Error: Rocket not found";
            var sasType = System.Type.GetType("SmartSASMod.SASComponent, SmartSASMod");
            if (sasType == null) return "Error: SASComponent type not found";
            var sas = rocket.GetComponent(sasType) ?? rocket.gameObject.AddComponent(sasType);
            var dirEnumType = sasType.Assembly.GetType("SmartSASMod.DirectionMode");
            if (dirEnumType == null) return "Error: DirectionMode type not found";
            object dirEnum;
            try {
                dirEnum = System.Enum.Parse(dirEnumType, directionMode, true);
            } catch {
                return "Error: Invalid directionMode (use Prograde/Target/Surface/None/Default)";
            }
            var dirProp = sasType.GetProperty("Direction");
            if (dirProp == null) return "Error: Direction property not found";
            dirProp.SetValue(sas, dirEnum);
            if (offset.HasValue)
            {
                var offsetProp = sasType.GetProperty("Offset");
                if (offsetProp == null) return "Error: Offset property not found";
                offsetProp.SetValue(sas, offset.Value);
            }
            return "Success";
        }



    }
} 