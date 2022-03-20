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
    [BepInDependency(CommonAPIPlugin.GUID, "1.4.5")]
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
        public static RecipeProto sailStationRecipe;
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

            ProtoRegistry.RegisterString("SailStationName", "Solar Flyby Station");
            ProtoRegistry.RegisterString("SailStationDesc", "Deploy solar sails into orbit using a logistic vessel.");
            ProtoRegistry.RegisterString("SailStationRecipeDesc", "Deploy solar sails into orbit using a logistic vessel.");
            sailStationItem = ProtoRegistry.RegisterItem(
                2115,
                "SailStationName",
                "SailStationDesc",
                "dsp_sail_flyby_station_icon",
                2705,
                10
            );

            sailStationItem.CanBuild = true;
            sailStationRecipe = ProtoRegistry.RegisterRecipe(
                415,
                ERecipeType.Assemble,
                60 * 30,
                new[] { 1107, 1206, 1305, 5002 },
                new[] { 30, 20, 10, 1 },
                new[] { sailStationItem.ID },
                new[] { 1 },
                "SailStationRecipeDesc",
                1512
            );
            sailStationModel = ProtoRegistry.RegisterModel(
                304,
                sailStationItem,
                "dsp_sail_flyby_station_v1",
                null,
                new[] { 18, 11, 32, 1 },
                809
            );

            Harmony harmony = new Harmony(MODGUID);
            harmony.PatchAll(typeof(PlanetFactoryPatch));
            harmony.PatchAll(typeof(LogisticShipRendererPatch));
            harmony.PatchAll(typeof(LogisticShipUIRendererPatch));

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
            Material newMat = Instantiate(interstellarLogisticsTower.prefabDesc.lodMaterials[0][0]);
            newMat.color = new Color(0.45f, 0.78f, 0.9f, 1f);
            sailStationModel.prefabDesc.lodMaterials = new[] { new[] { newMat } };

            Log.Info("ready");
        }
    }
}