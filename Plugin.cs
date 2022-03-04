using System;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using CommonAPI;
using CommonAPI.Systems;
using HarmonyLib;
using UnityEngine;

[module: UnverifiableCode]
#pragma warning disable 618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore 618
namespace DSPSailFlyby
{
    [BepInDependency(CommonAPIPlugin.GUID)]
    [BepInDependency("me.xiaoye97.plugin.Dyson.LDBTool")]
    [BepInPlugin(MODGUID, MODNAME, VERSION)]
    [BepInProcess("DSPGAME.exe")]
    [CommonAPISubmoduleDependency(nameof(ProtoRegistry), nameof(UtilSystem), nameof(StarExtensionSystem), nameof(PlanetExtensionSystem), nameof(CustomDescSystem), nameof(ComponentExtension))]
    public class Plugin : BaseUnityPlugin
    {
        public const string MODGUID = "46bit.plugin.DSPSailFlyby";
        public const string MODNAME = "SailFlyby";
        public const string VERSION = "0.0.1";

        public static ResourceData resourceData;
        public static ItemProto sailStationItem;
        public static ModelProto sailStationModel;

        void Awake()
        {
            Log.logger = Logger;
            Log.Info("starting");

            resourceData = new ResourceData(MODNAME, "dsp_sail_flyby_station");
            resourceData.LoadAssetBundle("dsp_sail_flyby_station");
            Assert.True(resourceData.HasAssetBundle());
            ProtoRegistry.AddResource(resourceData);

            ComponentExtension.componentRegistry.Register(SailStationComponent.componentID, typeof(SailStationComponent));

            ProtoRegistry.RegisterString("SailStationName", "Sail Station");
            ProtoRegistry.RegisterString("SailStationDesc", "FIXME");
            ProtoRegistry.RegisterString("SailStationRecipeDesc", "FIXME");
            sailStationItem = ProtoRegistry.RegisterItem(
                2115,
                "SailStationName",
                "SailStationDesc",
                "dsp_factory_space_stations_icon_drone",
                1707,
                10
            );
            sailStationItem.CanBuild = true;
            ProtoRegistry.RegisterRecipe(
                415,
                ERecipeType.Assemble,
                400,
                new[] { 2105 },
                new[] { 1 },
                new[] { sailStationItem.ID },
                new[] { 1 },
                "SailStationRecipeDesc",
                1606
            );
            sailStationModel = ProtoRegistry.RegisterModel(
                304,
                sailStationItem,
                "dsp_sail_flyby_station_v1",
                null,
                new[] { 18, 11, 32, 1 },
                609
            );

            Harmony harmony = new Harmony(MODGUID);
            harmony.PatchAll(typeof(LogisticShipRendererPatch));

            ProtoRegistry.onLoadingFinished += OnLoadingFinished;

            Log.Info("waiting");
        }

        void OnLoadingFinished()
        {
            Log.Info("loaded");

            if (!sailStationModel.prefabDesc.hasObject)
            {
                throw new Exception("could not load GameObject from asset");
            }

            var interstellarLogisticsTower = LDB.items.Select(2104);
            sailStationModel.prefabDesc.materials = interstellarLogisticsTower.prefabDesc.materials;
            sailStationModel.prefabDesc.lodMaterials = interstellarLogisticsTower.prefabDesc.lodMaterials;

            Log.Info("ready");
        }
    }
}