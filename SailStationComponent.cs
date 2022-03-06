using CommonAPI;
using CommonAPI.Systems;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using BepInEx;

namespace DSPSailFlyby
{
    public enum EFlybyStage {
        Idle = 0,
        Warmup = 1,
        Takeoff = 2,
        EnRoute = 3,
        Flyby = 4,
        Returning = 5,
        Landing = 6,
        Cooldown = 7,
    }

    public class SailFlybyShipData {
        public ShipData inner;
        public ShipRenderingData renderingData;
        public ShipUIRenderingData uiRenderingData;

        public int orbitId;
        public EFlybyStage stage;
        public int sailPayload;
        public double orbitAngle;

        public void Import(BinaryReader r)
        {
            // Version control
            Assert.Equals(r.ReadByte(), 3);

            orbitId = r.ReadInt32();
            stage = (EFlybyStage)r.ReadByte();
            sailPayload = r.ReadInt32();
            orbitAngle = r.ReadDouble();

            inner.Import(r);

            renderingData = new ShipRenderingData();
            renderingData.gid = r.ReadInt32();
            renderingData.pos = new Vector3(
                r.ReadSingle(),
                r.ReadSingle(),
                r.ReadSingle()
            );
            renderingData.vel = new Vector3(
                r.ReadSingle(),
                r.ReadSingle(),
                r.ReadSingle()
            );
            renderingData.anim = new Vector4(
                r.ReadSingle(),
                r.ReadSingle(),
                r.ReadSingle(),
                r.ReadSingle()
            );
            renderingData.rot = new Quaternion(
                r.ReadSingle(),
                r.ReadSingle(),
                r.ReadSingle(),
                r.ReadSingle()
            );
            renderingData.itemId = r.ReadUInt32();

            uiRenderingData = new ShipUIRenderingData();
            uiRenderingData.gid = r.ReadInt32();
            uiRenderingData.trip = r.ReadSingle();
            uiRenderingData.upos = new VectorLF3(
                r.ReadDouble(),
                r.ReadDouble(),
                r.ReadDouble()
            );
            uiRenderingData.rpos = new Vector3(
                r.ReadSingle(),
                r.ReadSingle(),
                r.ReadSingle()
            );
            uiRenderingData.urot = new Quaternion(
                r.ReadSingle(),
                r.ReadSingle(),
                r.ReadSingle(),
                r.ReadSingle()
            );
            uiRenderingData.speed = r.ReadSingle();
            uiRenderingData.itemId = r.ReadUInt32();
            uiRenderingData.param = r.ReadSingle();
        }

        public void Export(BinaryWriter w)
        {
            // Version control
            w.Write((byte)3);

            w.Write(orbitId);
            w.Write((byte)stage);
            w.Write(sailPayload);
            w.Write(orbitAngle);

            inner.Export(w);

            w.Write(renderingData.gid);
            w.Write(renderingData.pos.x);
            w.Write(renderingData.pos.y);
            w.Write(renderingData.pos.z);
            w.Write(renderingData.vel.x);
            w.Write(renderingData.vel.y);
            w.Write(renderingData.vel.z);
            w.Write(renderingData.anim.x);
            w.Write(renderingData.anim.y);
            w.Write(renderingData.anim.z);
            w.Write(renderingData.anim.w);
            w.Write(renderingData.rot.x);
            w.Write(renderingData.rot.y);
            w.Write(renderingData.rot.z);
            w.Write(renderingData.rot.w);
            w.Write(renderingData.itemId);

            w.Write(uiRenderingData.gid);
            w.Write(uiRenderingData.trip);
            w.Write(uiRenderingData.upos.x);
            w.Write(uiRenderingData.upos.y);
            w.Write(uiRenderingData.upos.z);
            w.Write(uiRenderingData.rpos.x);
            w.Write(uiRenderingData.rpos.y);
            w.Write(uiRenderingData.rpos.z);
            w.Write(uiRenderingData.urot.x);
            w.Write(uiRenderingData.urot.y);
            w.Write(uiRenderingData.urot.z);
            w.Write(uiRenderingData.urot.w);
            w.Write(uiRenderingData.speed);
            w.Write(uiRenderingData.itemId);
            w.Write(uiRenderingData.param);
        }
    }

    public class SailStationComponent : FactoryComponent
    {
        // Do not modify this code. This is template to help with dynamic ID assignment
        public static readonly string componentID = $"{Plugin.MODGUID}:SailStationComponent";
        private static int _cachedId;
        public static int cachedId
        {
            get
            {
                if (_cachedId == 0)
                    _cachedId = ComponentExtension.componentRegistry.GetUniqueId(componentID);

                return _cachedId;
            }
        }

        public int planetId;
        public VectorLF3 dockPosition;
        public SailFlybyShipData ship;

        public override void OnAdded(PrebuildData data, PlanetFactory factory)
        {
            base.OnAdded(data, factory);
            EntityData entity = factory.entityPool[entityId];
            AstroPose astroPose = factory.planet.star.galaxy.astroPoses[factory.planet.id];

            planetId = factory.planetId;

            dockPosition = entity.pos + entity.rot * new VectorLF3(0, 2.6, 0);

            ship = new();
            ship.inner = new ShipData();
            ship.inner.uPos = astroPose.uPos + Maths.QRotateLF(astroPose.uRot, dockPosition);
            ship.inner.uRot = astroPose.uRot * entity.rot;
            ship.stage = EFlybyStage.Idle;
            ship.sailPayload = 0;
            ship.orbitId = 1;
        }

        public override void OnRemoved(PlanetFactory factory) {
            base.OnRemoved(factory);

            ship = null;
        }

        public override void UpdateSigns(ref SignData data, int updateResult, float power, PlanetFactory factory)
        {
            data.iconId0 = 1501;
            data.iconType = 1U;
        }

        public override int InternalUpdate(float power, PlanetFactory factory)
        {
            base.InternalUpdate(power, factory);

            EntityData entity = factory.entityPool[entityId];
            AstroPose astroPose = factory.planet.star.galaxy.astroPoses[factory.planet.id];

            if (ship.stage == EFlybyStage.Flyby)
            {
                ship.inner.uPos += (VectorLF3)ship.inner.uVel;
            }
            float tripLength = 1000;

            switch (ship.stage)
            {
                case EFlybyStage.Idle:
                    UpdateIdleShip(factory);
                    break;
                case EFlybyStage.Warmup:
                    UpdateWarmupShip(factory);
                    break;
                case EFlybyStage.Takeoff:
                    UpdateTakeoffShip(factory);
                    break;
                case EFlybyStage.EnRoute:
                    UpdateEnRouteShip(factory);
                    break;
                case EFlybyStage.Flyby:
                    UpdateFlybyShip(factory, ref tripLength);
                    break;
                case EFlybyStage.Returning:
                    UpdateReturningShip(factory);
                    break;
                case EFlybyStage.Landing:
                    UpdateLandingShip(factory);
                    break;
                case EFlybyStage.Cooldown:
                    UpdateCooldownShip(factory);
                    break;
            }

            if (ship.stage == EFlybyStage.EnRoute || ship.stage == EFlybyStage.Returning)
            {
                return 0;
            }

            VectorLF3 relativePos = factory.transport.gameData.relativePos;
            Quaternion relativeRot = factory.transport.gameData.relativeRot;
            ship.renderingData.SetPose(
                ship.inner.uPos,
                ship.inner.uRot,
                relativePos,
                relativeRot,
                ship.inner.uVel * ship.inner.uSpeed,
                1501
            );
            ship.renderingData.gid = 1;
            ship.renderingData.anim.z = (ship.stage == EFlybyStage.Idle) ? 0f : 1.5f;

            ship.uiRenderingData.SetPose(
                ship.inner.uPos,
                ship.inner.uRot,
                tripLength,
                ship.inner.uVel.magnitude + ship.inner.uSpeed,
                1501
            );
            ship.uiRenderingData.gid = 1;
            VectorLF3 viewTargetUPos = UIRoot.instance.uiGame.starmap.viewTargetUPos;
            ship.uiRenderingData.rpos = (ship.uiRenderingData.upos - viewTargetUPos) * 0.00025;

            return 0;
        }

        protected void UpdateIdleShip(PlanetFactory factory)
        {
            EntityData entity = factory.entityPool[entityId];
            AstroPose astroPose = factory.planet.star.galaxy.astroPoses[factory.planet.id];

            //ship.sailPayload = Math.Min(ship.sailPayload + 379, 1000);
            ship.inner.uPos = astroPose.uPos + Maths.QRotateLF(astroPose.uRot, dockPosition);
            ship.inner.uRot = astroPose.uRot * entity.rot;

            if (ship.sailPayload >= 1000 && factory.dysonSphere?.swarm != null && factory.dysonSphere.swarm.OrbitEnabled(ship.orbitId))
            {
                ship.stage = EFlybyStage.Warmup;
            }
        }

        protected void UpdateWarmupShip(PlanetFactory factory)
        {
            EntityData entity = factory.entityPool[entityId];
            AstroPose astroPose = factory.planet.star.galaxy.astroPoses[factory.planet.id];

            ship.inner.t += 0.03335f;
            if (ship.inner.t > 1f)
            {
                ship.inner.t = 0f;
                ship.stage = EFlybyStage.Takeoff;
            }
            ship.inner.uPos = astroPose.uPos + Maths.QRotateLF(astroPose.uRot, dockPosition);
            ship.inner.uVel.x = 0f;
            ship.inner.uVel.y = 0f;
            ship.inner.uVel.z = 0f;
            ship.inner.uSpeed = 0f;
            ship.inner.uRot = astroPose.uRot * entity.rot;
            ship.inner.uAngularVel.x = 0f;
            ship.inner.uAngularVel.y = 0f;
            ship.inner.uAngularVel.z = 0f;
            ship.inner.uAngularSpeed = 0f;
            ship.inner.pPosTemp = Vector3.zero;
            ship.inner.pRotTemp = Quaternion.identity;
            ship.renderingData.anim.z = 0f;
        }

        protected void UpdateTakeoffShip(PlanetFactory factory)
        {
            EntityData entity = factory.entityPool[entityId];
            AstroPose astroPose = factory.planet.star.galaxy.astroPoses[factory.planet.id];

            float shipSailSpeed = GameMain.data.history.logisticShipSailSpeedModified;
            float num46 = Mathf.Sqrt(shipSailSpeed / 600f);
            float num47 = num46;
            if (num47 > 1f)
            {
                num47 = Mathf.Log(num47) + 1f;
            }
            float num48 = shipSailSpeed * 0.03f * num47;
            float num49 = shipSailSpeed * 0.12f * num47;
            float num50 = shipSailSpeed * 0.4f * num46;
            float num51 = num46 * 0.006f + 1E-05f;

            ship.inner.t += num51;
            float num52 = ship.inner.t;
            if (ship.inner.t > 1f)
            {
                ship.inner.t = 1f;
                num52 = 1f;
                ship.stage = EFlybyStage.EnRoute;
            }
            ship.renderingData.anim.z = num52;
            num52 = (3f - num52 - num52) * num52 * num52;
            ship.inner.uPos = astroPose.uPos + Maths.QRotateLF(astroPose.uRot, dockPosition + dockPosition.normalized * (25f * num52));
            ship.inner.uRot = astroPose.uRot * entity.rot;
            ship.inner.uVel.x = 0f;
            ship.inner.uVel.y = 0f;
            ship.inner.uVel.z = 0f;
            ship.inner.uSpeed = 0f;
            ship.inner.uAngularVel.x = 0f;
            ship.inner.uAngularVel.y = 0f;
            ship.inner.uAngularVel.z = 0f;
            ship.inner.uAngularSpeed = 0f;
        }

        protected void UpdateEnRouteShip(PlanetFactory factory)
        {
            StarData star = factory.planet.star;
            DysonSwarm swarm = factory.dysonSphere.swarm;
            SailOrbit orbit = swarm.orbits[1];

            EntityData entity = factory.entityPool[entityId];
            AstroPose[] astroPoses = star.galaxy.astroPoses;

            ship.inner.planetA = factory.planet.id;
            AstroPose astroPose = astroPoses[factory.planet.id];
            VectorLF3 positionA = astroPose.uPos + Maths.QRotateLF(astroPose.uRot, dockPosition);
            Quaternion rotationA = astroPose.uRot * entity.rot * new Quaternion(0.70710677f, 0f, 0f, -0.70710677f);

            ship.inner.planetB = star.id;
            AstroPose astroPose2 = astroPoses[star.id];
            VectorLF3 positionB = astroPose2.uPos + Maths.QRotateLF(orbit.rotation, new VectorLF3(0, 0, orbit.radius));
            Quaternion rotationB = Quaternion.identity;

            ship.inner.direction = 1;

            bool reached = Navigate.update(astroPoses, positionA, positionB, rotationA, rotationB, 1E40, 6, ref ship.inner, ref ship.renderingData, ref ship.uiRenderingData);
            if (reached)
            {
                ship.stage = EFlybyStage.Flyby;
                ship.orbitAngle = 0;
            }
        }

        protected void UpdateFlybyShip(PlanetFactory factory, ref float tripLength)
        {
            int solarSailLife = (int)(GameMain.history.solarSailLife * 60f + 0.1f);
            long expiryTime = GameMain.gameTick + (long)solarSailLife;

            StarData star = factory.planet.star;
            DysonSwarm swarm = factory.dysonSphere.swarm;
            SailOrbit orbit = swarm.orbits[1];

            ship.orbitAngle -= 360.0 / 4000;
            VectorLF3 newShipPos = Maths.QRotateLF(orbit.rotation, new VectorLF3(
                Math.Sin(ship.orbitAngle * 0.017453292) * orbit.radius,
                0,
                Math.Cos(ship.orbitAngle * 0.017453292) * orbit.radius
            )) + star.uPosition;
            ship.inner.uVel = newShipPos - ship.inner.uPos;
            ship.inner.uRot = star.galaxy.astroPoses[star.id].uRot * orbit.rotation * Quaternion.Euler(0, -90+(float)ship.orbitAngle, 0);
            ship.inner.uPos = newShipPos;

            tripLength = (float) 6.28 * orbit.radius;

            // Drop sails randomly, on average every third game update
            // FIXME: Do in regular configurable pattern
            if (RandomTable.Integer(ref swarm.randSeed, 8) == 7)
            {
                VectorLF3 vel = VectorLF3.Cross(ship.inner.uPos, orbit.up).normalized;
                vel *= Math.Sqrt(factory.dysonSphere.gravity / orbit.radius);
                vel += RandomTable.SphericNormal(ref swarm.randSeed, 0.5);

                DysonSail sail = default;
                VectorLF3 jitter = RandomTable.SphericNormal(ref swarm.randSeed, 0.5);
                sail.px = (float)ship.inner.uPos.x;
                sail.py = (float)ship.inner.uPos.y;
                sail.pz = (float)ship.inner.uPos.z;
                sail.vx = (float)vel.x;
                sail.vy = (float)vel.y;
                sail.vz = (float)vel.z;
                sail.gs = 1f;
                ThreadingHelper.Instance.StartSyncInvoke(() =>
                {
                    swarm.AddSolarSail(sail, 1, expiryTime);
                });
                ship.sailPayload--;
            }

            // 2. change stage once sails are empty
            if (ship.sailPayload <= 0)
            {
                ship.stage = EFlybyStage.Returning;
            }
        }

        protected void UpdateReturningShip(PlanetFactory factory)
        {
            StarData star = factory.planet.star;
            DysonSwarm swarm = factory.dysonSphere.swarm;
            SailOrbit orbit = swarm.orbits[1];

            EntityData entity = factory.entityPool[entityId];
            AstroPose[] astroPoses = star.galaxy.astroPoses;

            ship.inner.planetA = factory.planet.id;
            AstroPose astroPose = astroPoses[factory.planet.id];
            VectorLF3 positionA = astroPose.uPos + Maths.QRotateLF(astroPose.uRot, dockPosition);
            Quaternion rotationA = astroPose.uRot * entity.rot * new Quaternion(0.70710677f, 0f, 0f, -0.70710677f);

            ship.inner.planetB = star.id;
            AstroPose astroPose2 = astroPoses[star.id];
            VectorLF3 positionB = ship.inner.uPos;
            Quaternion rotationB = ship.inner.uRot;

            ship.inner.direction = -1;

            bool reached = Navigate.update(astroPoses, positionA, positionB, rotationA, rotationB, 1E40, 6, ref ship.inner, ref ship.renderingData, ref ship.uiRenderingData);
            if (reached)
            {
                ship.stage = EFlybyStage.Landing;
            }
        }

        protected void UpdateLandingShip(PlanetFactory factory)
        {
            EntityData entity = factory.entityPool[entityId];
            AstroPose astroPose = factory.planet.star.galaxy.astroPoses[factory.planet.id];

            float shipSailSpeed = GameMain.data.history.logisticShipSailSpeedModified;
            float num46 = Mathf.Sqrt(shipSailSpeed / 600f);
            float num47 = num46;
            if (num47 > 1f)
            {
                num47 = Mathf.Log(num47) + 1f;
            }
            float num48 = shipSailSpeed * 0.03f * num47;
            float num49 = shipSailSpeed * 0.12f * num47;
            float num50 = shipSailSpeed * 0.4f * num46;
            float num51 = num46 * 0.006f + 1E-05f;

            ship.inner.t -= num51 * 0.6666667f;
            float num52 = ship.inner.t;
            if (ship.inner.t < 0f)
            {
                ship.inner.t = 1f;
                num52 = 0f;
                ship.stage = EFlybyStage.Cooldown;
            }
            ship.renderingData.anim.z = num52;
            num52 = (3f - num52 - num52) * num52 * num52;
            VectorLF3 lhs = astroPose.uPos + Maths.QRotateLF(astroPose.uRot, dockPosition);
            VectorLF3 lhs2 = astroPose.uPos + Maths.QRotateLF(astroPose.uRot, ship.inner.pPosTemp);
            ship.inner.uPos = lhs * (double)(1f - num52) + lhs2 * (double)num52;
            ship.inner.uRot = astroPose.uRot * Quaternion.Slerp(entity.rot, ship.inner.pRotTemp, num52 * 2f - 1f);
            ship.inner.uVel.x = 0f;
			ship.inner.uVel.y = 0f;
			ship.inner.uVel.z = 0f;
			ship.inner.uSpeed = 0f;
			ship.inner.uAngularVel.x = 0f;
			ship.inner.uAngularVel.y = 0f;
			ship.inner.uAngularVel.z = 0f;
            ship.inner.uAngularSpeed = 0f;
        }

        protected void UpdateCooldownShip(PlanetFactory factory)
        {
            EntityData entity = factory.entityPool[entityId];
            AstroPose astroPose = factory.planet.star.galaxy.astroPoses[factory.planet.id];

            ship.inner.t -= 0.03335f;
            if (ship.inner.t < 0f)
            {
                ship.inner.t = 0f;
                ship.stage = EFlybyStage.Idle;
            }
            ship.inner.uPos = astroPose.uPos + Maths.QRotateLF(astroPose.uRot, dockPosition);
            ship.inner.uVel.x = 0f;
            ship.inner.uVel.y = 0f;
            ship.inner.uVel.z = 0f;
            ship.inner.uSpeed = 0f;
            ship.inner.uRot = astroPose.uRot * entity.rot;
            ship.inner.uAngularVel.x = 0f;
            ship.inner.uAngularVel.y = 0f;
            ship.inner.uAngularVel.z = 0f;
            ship.inner.uAngularSpeed = 0f;
            ship.inner.pPosTemp = Vector3.zero;
            ship.inner.pRotTemp = Quaternion.identity;
			ship.renderingData.anim.z = 0f;
        }

        public override void Import(BinaryReader r)
        {
            base.Import(r);

            // Version number
            Assert.Equals(r.ReadByte(), 3);

            planetId = r.ReadInt32();
            dockPosition = new();
            dockPosition.x = r.ReadDouble();
            dockPosition.y = r.ReadDouble();
            dockPosition.z = r.ReadDouble();

            ship = new();
            ship.Import(r);
        }

        public override void Export(BinaryWriter w)
        {
            base.Export(w);

            // Version number
            w.Write((byte)3);

            w.Write(planetId);
            w.Write(dockPosition.x);
            w.Write(dockPosition.y);
            w.Write(dockPosition.z);

            ship.Export(w);
        }
    }
}
