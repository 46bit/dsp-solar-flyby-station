using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System;
using HarmonyLib;
using UnityEngine;
using CommonAPI.Systems;

namespace DSPSailFlyby
{
    [HarmonyPatch]
    public static class FactorySystemPatch
    {
        // FIXME: Use threading logic from original functions?

        [HarmonyPostfix]
        [HarmonyPatch(typeof(FactorySystem), "GameTick", new[] { typeof(long), typeof(bool) })]
        public static void GameTickPostfix1(FactorySystem __instance, long time, bool isActive)
        {
            int[] sailStationNeeds = new int[] { 1501 };
            int[][] entityNeeds = __instance.factory.entityNeeds;
            var pool = __instance.factory.GetSystem<ComponentExtension>(ComponentExtension.cachedId).GetPool(SailStationComponent.cachedId);
            for (int j = 1; j < pool.poolCursor; j++)
            {
                if (pool.pool[j] == null)
                {
                    continue;
                }
                SailStationComponent sailStationComponent = (SailStationComponent)pool.pool[j];
                entityNeeds[sailStationComponent.entityId] = sailStationNeeds;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(FactorySystem), "GameTick", new[] { typeof(long), typeof(bool), typeof(int), typeof(int), typeof(int) })]
        public static void GameTickPostfix2(FactorySystem __instance, long time, bool isActive, int _usedThreadCnt, int _curThreadIdx, int _minimumMissionCnt)
        {
            FactorySystemPatch.GameTickPostfix1(__instance, time, isActive);
        }
    }
}
