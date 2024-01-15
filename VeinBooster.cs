//#define HIGHLOGGING

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using Rewired.Integration.UnityUI;
using UnityEngine.EventSystems;
using System.Reflection;
using System.Security.Cryptography;
using BepInEx.Configuration;

namespace VeinBooster
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class VeinBooster : BaseUnityPlugin
    {
        public const string pluginGuid = "veinbooster.nhickling.co.uk";
        public const string pluginName = "VeinBooster";
        public const string pluginVersion = "0.0.0.5";
        private static BepInEx.Logging.ManualLogSource ModLogger;
        public static ConfigEntry<int> ProtectionChance;


        public void Awake()
        {
            ModLogger = Logger;
            ModLogger.LogInfo("VeinBooster: started");
            Harmony harmony = new Harmony(pluginGuid);
            ModLogger.LogInfo("VeinBooster: Fetching patch references");
            MethodInfo original = AccessTools.Method(typeof(PendingVoxelChanges), "TryDig");
            MethodInfo patch = AccessTools.Method(typeof(VeinBooster), "TryDig_Patch");
            ModLogger.LogInfo("VeinBooster: Starting Patch");
            harmony.Patch(original, null, new HarmonyMethod(patch));
            ModLogger.LogInfo("VeinBooster: Patched");
            ProtectionChance = ((BaseUnityPlugin)this).Config.Bind<int>("Config", "ProtectionChance", 100, new ConfigDescription("Percentage chance damage to the ore is prevented, reduce this to lower the power of the mod.", (AcceptableValueBase)(object)new AcceptableValueRange<int>(0, 100), Array.Empty<object>()));
        }

        public static void TryDig_Patch(PendingVoxelChanges __instance, in Vector3Int coord, int digStrength, int miningTier, ref int numResourcesTaken, bool __result)
        {
            try
            {
                if (!__result)
                {
                    // Change happened which did not destroy voxel.
                    if (numResourcesTaken > 0)
                    {
                        if (ShouldRepairVoxel())
                        {
                            // De-reference in same manner as original modification. As we know voxel was not broken, but resources aquired, we can shortcut some of the safety checks.
                            int chunkId = VoxelManager.GetChunkId(coord.x, coord.y, coord.z);
                            ref ChunkPendingVoxelChanges local1 = ref __instance.chunkData[__instance.GetOrCreateIndexForChunk(in chunkId)];
                            int indexWithinChunk = local1.GetIndex(coord.x, coord.y, coord.z);
                            ref ModifiedCoordData local2 = ref local1.GetOrAdd(indexWithinChunk);
                            local2.integrity += numResourcesTaken;
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                ModLogger.LogError($"VeinBooster: TryDig: {ex.Message}");

            }
        }

        private static bool ShouldRepairVoxel()
        {
            return ProtectionChance.Value > UnityEngine.Random.Range(0, 99);
        }
    }
}
