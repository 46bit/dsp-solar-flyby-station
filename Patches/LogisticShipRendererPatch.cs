using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System;
using HarmonyLib;
using UnityEngine;
using CommonAPI.Systems;

// ReSharper disable InconsistentNaming

namespace DSPSailFlyby
{
    [HarmonyPatch]
    public static class LogisticShipRendererPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(LogisticShipRenderer), "Update")]
        public static void UpdatePostfix(LogisticShipRenderer __instance)
        {
            for (int i = 0; i < GameMain.data.localStar.planetCount; i++)
            {
                PlanetData planet = GameMain.data.localStar.planets[i];
                PlanetFactory factory = planet.factory;
                if (factory == null || factory.entityCursor < 2)
                {
                    continue;
                }
                var pool = factory.GetSystem<ComponentExtension>(ComponentExtension.cachedId).GetPool(SailStationComponent.cachedId);
                while (__instance.capacity < __instance.shipCount + pool.poolCursor - 1)
                {
                    __instance.Expand2x();
                }
                for (int j = 1; j < pool.poolCursor; j++)
                {
                    if (pool.pool[j] == null)
                    {
                        continue;
                    }
                    SailStationComponent sailStationComponent = (SailStationComponent)pool.pool[j];
                    __instance.shipsArr[__instance.shipCount] = sailStationComponent.ship.renderingData;
                    __instance.shipCount++;
                }
            }

            // FIXME: Don't re-render data not added by this function
            if (__instance.shipsBuffer != null)
            {
                __instance.shipsBuffer.SetData(__instance.shipsArr, 0, 0, __instance.shipCount);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LogisticShipUIRenderer), "Update")]
        public static void UpdateUIPostfix(LogisticShipUIRenderer __instance)
        {
            // FIXME: Support rendering sail ships in other star systems when viewed on starmap
            for (int i = 0; i < GameMain.data.localStar.planetCount; i++)
            {
                PlanetData planet = GameMain.data.localStar.planets[i];
                PlanetFactory factory = planet.factory;
                if (factory == null || factory.entityCursor < 2)
                {
                    continue;
                }
                var pool = factory.GetSystem<ComponentExtension>(ComponentExtension.cachedId).GetPool(SailStationComponent.cachedId);
                while (__instance.capacity < __instance.shipCount + pool.poolCursor - 1)
                {
                    __instance.Expand2x();
                }
                for (int j = 1; j < pool.poolCursor; j++)
                {
                    if (pool.pool[j] == null)
                    {
                        continue;
                    }
                    SailStationComponent sailStationComponent = (SailStationComponent) pool.pool[j];
                    __instance.shipsArr[__instance.shipCount] = sailStationComponent.ship.uiRenderingData;
                    __instance.shipCount++;
                }
            }

            // FIXME: Don't re-render data not added by this function
            if (__instance.shipsBuffer != null)
            {
                __instance.shipsBuffer.SetData(__instance.shipsArr, 0, 0, __instance.shipCount);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlanetFactory), "InsertInto")]
        public static bool InsertIntoPrefix(PlanetFactory __instance, ref int __result, int entityId, int offset, int itemId, byte itemCount, byte itemInc, out byte remainInc)
        {
            remainInc = itemInc;
            int beltId = __instance.entityPool[entityId].beltId;
            if (beltId <= 0 && entityId > 0)
            {
                lock (__instance.entityMutexs[entityId])
                {
                    int customId = __instance.entityPool[entityId].customId;
                    int customType = __instance.entityPool[entityId].customType;
                    if (customId > 0)
                    {
                        if (customType == SailStationComponent.cachedId && itemId == 1501)
                        {
                            SailStationComponent sailStationComponent = (SailStationComponent)ComponentExtension.GetComponent(__instance, customType, customId);
                            if (sailStationComponent.ship.stage == EFlybyStage.Idle && sailStationComponent.ship.sailPayload < 1000)
                            {
                                // FIXME: Stop allowing payload to slightly exceed 1000
                                sailStationComponent.ship.sailPayload += itemCount;
                                remainInc = 0;
                                __result = (int)itemCount;
                                return false;
                            }
                        }
                    }
                }
            }
            return true;
        }
    }
}