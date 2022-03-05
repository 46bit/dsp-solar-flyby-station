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
            Assert.Equals(r.ReadByte(), 2);

            inner.Import(r);
            orbitId = r.ReadInt32();
            stage = (EFlybyStage) r.ReadByte();
            sailPayload = r.ReadInt32();
            orbitAngle = r.ReadDouble();
        }

        public void Export(BinaryWriter w)
        {
            // Version control
            w.Write((byte)2);

            inner.Export(w);
            w.Write(orbitId);
            w.Write((int)stage);
            w.Write(sailPayload);
            w.Write(orbitAngle);
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

        public static SailStationComponent instance;
        const int moveSpeed = 25;

        public int planetId;
        public VectorLF3 dockPosition;
        public SailFlybyShipData ship;

        public override void OnAdded(PrebuildData data, PlanetFactory factory)
        {
            base.OnAdded(data, factory);
            EntityData entity = factory.entityPool[entityId];
            AstroPose astroPose = factory.planet.star.galaxy.astroPoses[factory.planet.id];

            instance = this;

            planetId = factory.planetId;

            dockPosition = entity.pos * 1.1f;

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

        public override int InternalUpdate(float power, PlanetFactory factory)
        {
            base.InternalUpdate(power, factory);

            ship.inner.uPos += (VectorLF3)ship.inner.uVel;
            float tripLength = 1000;

            switch (ship.stage)
            {
                case EFlybyStage.Idle:
                    UpdateIdleShip(factory);
                    break;
                case EFlybyStage.Warmup:
                    ship.stage = EFlybyStage.Takeoff;
                    break;
                case EFlybyStage.Takeoff:
                    ship.stage = EFlybyStage.EnRoute;
                    //// FIXME: Take into account the orbit radius AND update the rotation
                    //ship.inner.uVel = (factory.planet.star.uPosition - ship.inner.uPos).normalized * moveSpeed;
                    break;
                case EFlybyStage.EnRoute:
                    UpdateEnRouteShip(factory, ref tripLength);
                    break;
                case EFlybyStage.Flyby:
                    UpdateFlybyShip(factory, ref tripLength);
                    break;
                case EFlybyStage.Returning:
                    UpdateReturningShip(factory, ref tripLength);
                    break;
                case EFlybyStage.Landing:
                    ship.stage = EFlybyStage.Cooldown;
                    break;
                case EFlybyStage.Cooldown:
                    ship.stage = EFlybyStage.Idle;
                    break;
            }

            // FIXME: Dig into rotations and positioning. It's all a bit weird from my
            // perspective, and so rotation during orbit is completely broken.
            VectorLF3 relativePos = factory.transport.gameData.relativePos;
            Quaternion relativeRot = factory.transport.gameData.relativeRot;
            ship.renderingData.SetPose(
                ship.inner.uPos,
                ship.inner.uRot,
                relativePos,
                relativeRot,
                ship.inner.uVel,
                1501
            );
            ship.renderingData.gid = 1;
            ship.renderingData.anim = Vector3.zero;
            // FIXME: Figure out full effects of anim.z
            // It seems to need to be >=1 in order for the payload markers to appear
            // Can this also control the trail 'flames' visible when ships zoom away from
            // planets? It's not controlled by velocity, because the flames still exist when
            // ships are momentarily stationary before dropping onto home platforms.
            ship.renderingData.anim.z = 1.5f; // 0.7f; // 1;

            ship.uiRenderingData.SetPose(
                ship.inner.uPos,
                ship.inner.uRot,
                tripLength,
                ship.inner.uVel.magnitude,
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

            ship.sailPayload = Math.Min(ship.sailPayload + 379, 1000);
            ship.inner.uPos = astroPose.uPos + Maths.QRotateLF(astroPose.uRot, dockPosition);
            ship.inner.uRot = astroPose.uRot * entity.rot;

            if (ship.sailPayload >= 1000 && factory.dysonSphere?.swarm != null && factory.dysonSphere.swarm.OrbitEnabled(ship.orbitId))
            {
                ship.stage = EFlybyStage.Warmup;
            }
        }

        protected void UpdateEnRouteShip(PlanetFactory factory, ref float tripLength)
        {
            StarData star = factory.planet.star;
            DysonSwarm swarm = factory.dysonSphere.swarm;
            SailOrbit orbit = swarm.orbits[1];

            //if (factory.planet.star.uPosition.Distance(ship.inner.uPos) < orbit.radius)
            //{
                ship.stage = EFlybyStage.Flyby;
                // FIXME: Sort out movement properly
                ship.inner.uVel.Set(0, 0, 0);

                ship.orbitAngle = 0;
                ship.inner.uPos = Maths.QRotateLF(orbit.rotation, new VectorLF3(
                    Math.Sin(ship.orbitAngle * 0.017453292) * orbit.radius,
                    0,
                    Math.Cos(ship.orbitAngle * 0.017453292) * orbit.radius
                )) + star.uPosition;

                //ship.shipData.uRot = Quaternion. shipPos.normalized;
            //}
        }

        protected void UpdateFlybyShip(PlanetFactory factory, ref float tripLength)
        {
            int solarSailLife = (int)(GameMain.history.solarSailLife * 60f + 0.1f);
            long expiryTime = GameMain.gameTick + (long)solarSailLife;

            StarData star = factory.planet.star;
            DysonSwarm swarm = factory.dysonSphere.swarm;
            SailOrbit orbit = swarm.orbits[1];

            ship.orbitAngle -= 360.0 / 2000;
            VectorLF3 newShipPos = Maths.QRotateLF(orbit.rotation, new VectorLF3(
                Math.Sin(ship.orbitAngle * 0.017453292) * orbit.radius,
                0,
                Math.Cos(ship.orbitAngle * 0.017453292) * orbit.radius
            )) + star.uPosition;
            ship.inner.uVel = newShipPos - ship.inner.uPos;
            ship.inner.uRot.SetFromToRotation(Vector3.forward, ship.inner.uVel);
            ship.inner.uPos = newShipPos;

            tripLength = (float) 6.14 * orbit.radius;

            // Drop sails randomly, on average every third game update
            // Would be nicer to drop sails in even pattern, but it made the lack of alignment
            // obvious. Need to rotate a bit more than 360 degrees, because the first sails
            // move on. Use orbital period to figure out how much further to go, how often to
            // drop sail, etc.
            if (RandomTable.Integer(ref swarm.randSeed, 6) == 5)
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

        protected void UpdateReturningShip(PlanetFactory factory, ref float tripLength)
        {
            EntityData entity = factory.entityPool[entityId];
            AstroPose astroPose = factory.planet.star.galaxy.astroPoses[factory.planet.id];

            VectorLF3 currentTargetPos = astroPose.uPos + Maths.QRotateLF(astroPose.uRot, dockPosition);
            ship.inner.uRot.SetFromToRotation(Vector3.forward, currentTargetPos - ship.inner.uPos);
            ship.inner.uVel = (currentTargetPos - ship.inner.uPos).normalized * moveSpeed;
            double remainingDist = ship.inner.uPos.Distance(currentTargetPos);
            tripLength = (float)remainingDist;
            if (remainingDist < moveSpeed * 1.5)
            {
                ship.stage = EFlybyStage.Landing;
                ship.inner.uPos = currentTargetPos;
                ship.inner.uVel.Set(0, 0, 0);
            }
        }

        public override void Import(BinaryReader r)
        {
            base.Import(r);

            // Version number
            Assert.Equals(r.ReadByte(), 2);

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
            w.Write(2);

            w.Write(planetId);
            w.Write(dockPosition.x);
            w.Write(dockPosition.y);
            w.Write(dockPosition.z);
            ship.Export(w);
        }
    }
}
