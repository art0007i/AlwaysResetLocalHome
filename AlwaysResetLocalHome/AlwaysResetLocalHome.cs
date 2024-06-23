using HarmonyLib;
using ResoniteModLoader;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using FrooxEngine;
using FrooxEngine.Store;
using System.Reflection.Emit;
using System.IO;

namespace AlwaysResetLocalHome
{
    public class AlwaysResetLocalHome : ResoniteMod
    {
        public override string Name => "AlwaysResetLocalHome";
        public override string Author => "art0007i";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/art0007i/AlwaysResetLocalHome/";

        [AutoRegisterConfigKey]
        public static ModConfigurationKey<bool> KEY_ENABLE = new("enable", "If true local home will be reset every restart.", () => true);

        public static ModConfiguration config;
        public override void OnEngineInit()
        {
            config = GetConfiguration();
            Harmony harmony = new Harmony("me.art0007i.AlwaysResetLocalHome");
            harmony.PatchAll();

        }

        [HarmonyPatch(typeof(WorldPresets), nameof(WorldPresets.LocalWorld))]
        class LocalHomeAlternateFilePatch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
            {
                foreach (var code in codes)
                {
                    yield return code;
                    if (code.Is(OpCodes.Ldstr, "Local.bin"))
                        yield return new(OpCodes.Call, typeof(LocalHomeAlternateFilePatch).GetMethod(nameof(GetValidHomePath)));
                }
            }

            public static string GetValidHomePath(string orig)
            {
                var modPath = Path.Combine(Engine.Current.AppPath, "RuntimeData", "LocalModded.bin");
                if (File.Exists(modPath))
                {
                    return "LocalModded.bin";
                }
                return orig;
            }
        }

        [HarmonyPatch(typeof(Userspace), nameof(Userspace.OpenLocalHomeAsync))]
        class AlwaysResetLocalHomePatch
        {
            public static bool Prefix(ref Task __result)
            {
                if (!config.GetValue(KEY_ENABLE)) return true;

                __result = Task.Run(() =>
                {
                    var world = Userspace.StartUtilityWorld(WorldPresets.LocalHome());
                    world.AssignNewRecord("M-" + world.Engine.LocalDB.MachineID, "R-Home");
                    world.CorrespondingRecord.Name = "Local";
                    world.Name = world.CorrespondingRecord.Name;
                    return;
                });

                return false;
            }
        }
    }
}