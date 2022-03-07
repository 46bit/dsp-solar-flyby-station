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
            StarData star = GameMain.data.localStar;
            if (star == null)
            {
                return;
            }
            for (int i = 0; i < star.planetCount; i++)
            {
                PlanetData planet = star.planets[i];
                if (planet == null)
                {
                    continue;
                }
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
                    if (sailStationComponent.ship != null)
                    {
                        __instance.shipsArr[__instance.shipCount] = sailStationComponent.ship.renderingData;
                        __instance.shipCount++;
                    }
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