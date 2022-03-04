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
            VectorLF3 relativePos = __instance.transport.gameData.relativePos;
            Quaternion relativeRot = __instance.transport.gameData.relativeRot;

            if (SailStationComponent.instance != null)
            {
                var sailStation = SailStationComponent.instance;
                while (__instance.capacity < __instance.shipCount + 1)
                {
                    __instance.Expand2x();
                }

                ShipRenderingData shipRenderingData = new();
                shipRenderingData.SetPose(
                    sailStation.ship.inner.uPos,
                    sailStation.ship.inner.uRot,
                    relativePos,
                    relativeRot,
                    sailStation.ship.inner.uVel,
                    0
                );
                shipRenderingData.gid = 1;
                shipRenderingData.anim = Vector3.zero;
                __instance.shipsArr[__instance.shipCount] = shipRenderingData;
                __instance.shipCount++;
            }

            if (__instance.shipsArr != null)
            {
                while (__instance.capacity < __instance.shipCount + 1)
                {
                    __instance.Expand2x();
                }

                ShipRenderingData shipRenderingData = new();

                var player = GameMain.data.mainPlayer;
                shipRenderingData.SetPose(
                    player.uPosition,
                    player.uRotation,
                    relativePos,
                    relativeRot,
                    Vector3.zero,
                    0
                );
                shipRenderingData.gid = 1;
                shipRenderingData.anim = Vector3.zero;
                __instance.shipsArr[__instance.shipCount] = shipRenderingData;
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
            if (SailStationComponent.instance != null)
            {
                var sailStation = SailStationComponent.instance;
                while (__instance.capacity < __instance.shipCount + 1)
                {
                    __instance.Expand2x();
                }

                ShipUIRenderingData shipUIRenderingData = new();
                shipUIRenderingData.SetPose(
                    sailStation.ship.inner.uPos,
                    sailStation.ship.inner.uRot,
                    1000,
                    0,
                    0
                );
                shipUIRenderingData.gid = 1;

                VectorLF3 viewTargetUPos = __instance.uiStarmap.viewTargetUPos;
                shipUIRenderingData.rpos = (shipUIRenderingData.upos - viewTargetUPos) * 0.00025;

                __instance.shipsArr[__instance.shipCount] = shipUIRenderingData;
                __instance.shipCount++;
            }

            if (__instance.shipsArr != null)
            {
                while (__instance.capacity < __instance.shipCount + 1)
                {
                    __instance.Expand2x();
                }

                ShipUIRenderingData shipUIRenderingData = new();
                var player = GameMain.data.mainPlayer;
                shipUIRenderingData.SetPose(
                    player.uPosition,
                    player.uRotation,
                    10000,
                    player.speed,
                    0
                );
                shipUIRenderingData.gid = 1;

                VectorLF3 viewTargetUPos = __instance.uiStarmap.viewTargetUPos;
                shipUIRenderingData.rpos = (shipUIRenderingData.upos - viewTargetUPos) * 0.00025;

                __instance.shipsArr[__instance.shipCount] = shipUIRenderingData;
                __instance.shipCount++;
            }

            if (__instance.shipsBuffer != null)
            {
                __instance.shipsBuffer.SetData(__instance.shipsArr, 0, 0, __instance.shipCount);
            }
        }
    }
}