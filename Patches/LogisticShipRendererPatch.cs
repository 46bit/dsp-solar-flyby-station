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
        // FIXME: Stop the original method from calling shipsBuffer.SetData? Or set less here
        [HarmonyPostfix]
        [HarmonyPatch(typeof(LogisticShipRenderer), "Update")]
        public static void UpdatePostfix(LogisticShipRenderer __instance)
        {
            // FIXME: SUPPORT MULTIPLE SailStationComponent
            if (SailStationComponent.instance != null)
            {
                var sailStation = SailStationComponent.instance;
                while (__instance.capacity < __instance.shipCount + 1)
                {
                    __instance.Expand2x();
                }

                __instance.shipsArr[__instance.shipCount] = sailStation.ship.renderingData;
                __instance.shipCount++;
            }

            if (__instance.shipsBuffer != null)
            {
                __instance.shipsBuffer.SetData(__instance.shipsArr, 0, 0, __instance.shipCount);
            }
        }

        // FIXME: Stop the original method from calling shipsBuffer.SetData? Or set less here
        [HarmonyPostfix]
        [HarmonyPatch(typeof(LogisticShipUIRenderer), "Update")]
        public static void UpdateUIPostfix(LogisticShipUIRenderer __instance)
        {
            // FIXME: SUPPORT MULTIPLE SailStationComponent
            if (SailStationComponent.instance != null)
            {
                var sailStation = SailStationComponent.instance;
                while (__instance.capacity < __instance.shipCount + 1)
                {
                    __instance.Expand2x();
                }

                __instance.shipsArr[__instance.shipCount] = sailStation.ship.uiRenderingData;
                __instance.shipCount++;
            }

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