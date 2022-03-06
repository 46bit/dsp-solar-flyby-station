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
    public static class LogisticShipUIRendererPatch
    {
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
                    SailStationComponent sailStationComponent = (SailStationComponent)pool.pool[j];
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
    }
}