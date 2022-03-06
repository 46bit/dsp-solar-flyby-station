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

            EntityData entity = factory.entityPool[entityId];
            AstroPose astroPose = factory.planet.star.galaxy.astroPoses[factory.planet.id];

            ship.inner.uPos += (VectorLF3)ship.inner.uVel;
            float tripLength = 1000;

            switch (ship.stage)
            {
                case EFlybyStage.Idle:
                    UpdateIdleShip(factory);
                    break;
                case EFlybyStage.Warmup:
                    ship.inner.t += 0.03335f;
                    if (ship.inner.t > 1f)
                    {
                        ship.inner.t = 0f;
                        ship.stage = EFlybyStage.Takeoff;
                    }
                    ship.inner.uPos = astroPose.uPos + Maths.QRotateLF(astroPose.uRot, entity.pos);
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
                    break;
                case EFlybyStage.Takeoff:
                    //// FIXME: Take into account the orbit radius AND update the rotation
                    //ship.inner.uVel = (factory.planet.star.uPosition - ship.inner.uPos).normalized * moveSpeed;
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
                    // Could this be taking off code? Maybe ShipData.t is used to control the takeoff/arrival to take a fixed time
                    ship.renderingData.anim.z = num52;
                    num52 = (3f - num52 - num52) * num52 * num52;
                    ship.inner.uPos = astroPose.uPos + Maths.QRotateLF(astroPose.uRot, entity.pos + entity.pos.normalized * (25f * num52));
                    ship.inner.uRot = astroPose.uRot * entity.rot;
                    // Stop accelerating in order to rely on the exact position code above
                    ship.inner.uVel.x = 0f;
                    ship.inner.uVel.y = 0f;
                    ship.inner.uVel.z = 0f;
                    ship.inner.uSpeed = 0f;
                    ship.inner.uAngularVel.x = 0f;
                    ship.inner.uAngularVel.y = 0f;
                    ship.inner.uAngularVel.z = 0f;
                    ship.inner.uAngularSpeed = 0f;
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

            VectorLF3 viewTargetUPos;
            if (ship.stage == EFlybyStage.EnRoute)
            {
                ship.renderingData.gid = 1;
                ship.uiRenderingData.gid = 1;
                viewTargetUPos = UIRoot.instance.uiGame.starmap.viewTargetUPos;
                ship.uiRenderingData.rpos = (ship.uiRenderingData.upos - viewTargetUPos) * 0.00025;
                return 0;
            }

            // FIXME: Dig into rotations and positioning. It's all a bit weird from my
            // perspective, and so rotation during orbit is completely broken.
            VectorLF3 relativePos = factory.transport.gameData.relativePos;
            Quaternion relativeRot = factory.transport.gameData.relativeRot;
            ship.renderingData.SetPose(
                ship.inner.uPos,
                ship.inner.uRot, // FIXME: original code often uses another quaternion
                relativePos,
                relativeRot,
                ship.inner.uVel * ship.inner.uSpeed,
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
                ship.inner.uRot, // FIXME: original code often uses another quaternion
                tripLength,
                ship.inner.uVel.magnitude + ship.inner.uSpeed, // FIXME: Only use one
                1501
            );
            ship.uiRenderingData.gid = 1;
            viewTargetUPos = UIRoot.instance.uiGame.starmap.viewTargetUPos;
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

            EntityData entity = factory.entityPool[entityId];
            AstroPose astroPose = star.galaxy.astroPoses[factory.planet.id];
            AstroPose astroPose2 = star.galaxy.astroPoses[star.id];
            AstroPose[] astroPoses = star.galaxy.astroPoses;

            ship.inner.planetB = factory.planet.star.id;
            ship.inner.direction = 1;
            bool flag = false;
            double dt = 1.0 / 60.0;
            float shipWarpSpeed = GameMain.history.logisticShipWarpSpeedModified;
            float shipSailSpeed = GameMain.history.logisticShipSailSpeedModified;
            bool warperFree = false;
            double warpEnableDist = 0.0;
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
            Quaternion quaternion = Quaternion.identity;
            bool flag8 = false;

            VectorLF3 lhs3;
            lhs3 = astroPose2.uPos + Maths.QRotateLF(orbit.rotation, new VectorLF3(0, 0, orbit.radius));
            VectorLF3 vectorLF = lhs3 - ship.inner.uPos;
            double num53 = Math.Sqrt(vectorLF.x * vectorLF.x + vectorLF.y * vectorLF.y + vectorLF.z * vectorLF.z);
            VectorLF3 vectorLF2 = ((ship.inner.direction > 0) ? (astroPose.uPos - ship.inner.uPos) : (astroPose2.uPos - ship.inner.uPos));
            double num54 = vectorLF2.x * vectorLF2.x + vectorLF2.y * vectorLF2.y + vectorLF2.z * vectorLF2.z;
            bool flag9 = num54 <= (double)(astroPose.uRadius * astroPose.uRadius) * 2.25;
            bool flag10 = false;
            if (num53 < 6.0)
            {
                ship.inner.t = 1f;
                ship.stage = EFlybyStage.Flyby;
                flag10 = true;
            }
            float num55 = 0f;
            if (flag)
            {
                double num56 = (astroPose.uPos - astroPose2.uPos).magnitude * 2.0;
                double num57 = (((double)shipWarpSpeed < num56) ? ((double)shipWarpSpeed) : num56);
                double num58 = warpEnableDist * 0.5;
                if (ship.inner.warpState <= 0f)
                {
                    ship.inner.warpState = 0f;
                    if (num54 > 25000000.0 && num53 > num58 && ship.inner.uSpeed >= shipSailSpeed && (ship.inner.warperCnt > 0 || warperFree))
                    {
                        ship.inner.warperCnt--;
                        ship.inner.warpState += (float)dt;
                    }
                }
                else
                {
                    num55 = (float)(num57 * ((Math.Pow(1001.0, (double)ship.inner.warpState) - 1.0) / 1000.0));
                    double num59 = (double)num55 * 0.0449 + 5000.0 + (double)shipSailSpeed * 0.25;
                    double num60 = num53 - num59;
                    if (num60 < 0.0)
                    {
                        num60 = 0.0;
                    }
                    if (num53 < num59)
                    {
                        ship.inner.warpState -= (float)(dt * 4.0);
                    }
                    else
                    {
                        ship.inner.warpState += (float)dt;
                    }
                    if (ship.inner.warpState < 0f)
                    {
                        ship.inner.warpState = 0f;
                    }
                    else if (ship.inner.warpState > 1f)
                    {
                        ship.inner.warpState = 1f;
                    }
                    if (ship.inner.warpState > 0f)
                    {
                        num55 = (float)(num57 * ((Math.Pow(1001.0, (double)ship.inner.warpState) - 1.0) / 1000.0));
                        if ((double)num55 * dt > num60)
                        {
                            num55 = (float)(num60 / dt * 1.01);
                        }
                    }
                }
            }
            double num61 = num53 / ((double)ship.inner.uSpeed + 0.1) * 0.382 * (double)num47;
            float num62;
            if (ship.inner.warpState > 0f)
            {
                num62 = (ship.inner.uSpeed = shipSailSpeed + num55);
                if (num62 > shipSailSpeed)
                {
                    num62 = shipSailSpeed;
                }
            }
            else
            {
                float num63 = (float)((double)ship.inner.uSpeed * num61) + 6f;
                if (num63 > shipSailSpeed)
                {
                    num63 = shipSailSpeed;
                }
                float num64 = (float)dt * (flag9 ? num48 : num49);
                if (ship.inner.uSpeed < num63 - num64)
                {
                    ship.inner.uSpeed += num64;
                }
                else if (ship.inner.uSpeed > num63 + num50)
                {
                    ship.inner.uSpeed -= num50;
                }
                else
                {
                    ship.inner.uSpeed = num63;
                }
                num62 = ship.inner.uSpeed;
            }
            int num65 = -1;
            double rhs = 0.0;
            double num66 = 1E+40;
            int num67 = ship.inner.planetA / 100 * 100;
            int num68 = ship.inner.planetB / 100 * 100;
            for (int k = num67; k < num67 + 10; k++)
            {
                float uRadius = astroPoses[k].uRadius;
                if (uRadius >= 1f)
                {
                    VectorLF3 vectorLF3 = ship.inner.uPos - astroPoses[k].uPos;
                    double num69 = vectorLF3.x * vectorLF3.x + vectorLF3.y * vectorLF3.y + vectorLF3.z * vectorLF3.z;
                    double num70 = -((double)ship.inner.uVel.x * vectorLF3.x + (double)ship.inner.uVel.y * vectorLF3.y + (double)ship.inner.uVel.z * vectorLF3.z);
                    if ((num70 > 0.0 || num69 < (double)(uRadius * uRadius * 7f)) && num69 < num66)
                    {
                        rhs = ((num70 < 0.0) ? 0.0 : num70);
                        num65 = k;
                        num66 = num69;
                    }
                }
            }
            if (num68 != num67)
            {
                for (int l = num68; l < num68 + 10; l++)
                {
                    float uRadius2 = astroPoses[l].uRadius;
                    if (uRadius2 >= 1f)
                    {
                        VectorLF3 vectorLF4 = ship.inner.uPos - astroPoses[l].uPos;
                        double num71 = vectorLF4.x * vectorLF4.x + vectorLF4.y * vectorLF4.y + vectorLF4.z * vectorLF4.z;
                        double num72 = -((double)ship.inner.uVel.x * vectorLF4.x + (double)ship.inner.uVel.y * vectorLF4.y + (double)ship.inner.uVel.z * vectorLF4.z);
                        if ((num72 > 0.0 || num71 < (double)(uRadius2 * uRadius2 * 7f)) && num71 < num66)
                        {
                            rhs = ((num72 < 0.0) ? 0.0 : num72);
                            num65 = l;
                            num66 = num71;
                        }
                    }
                }
            }
            VectorLF3 vectorLF5 = VectorLF3.zero;
            VectorLF3 rhs2 = VectorLF3.zero;
            float num73 = 0f;
            VectorLF3 vectorLF6 = Vector3.zero;
            if (num65 > 0)
            {
                float num74 = astroPoses[num65].uRadius;
                if (num65 % 100 == 0)
                {
                    num74 *= 2.5f;
                }
                double num75 = Math.Max(1.0, ((astroPoses[num65].uPosNext - astroPoses[num65].uPos).magnitude - 0.5) * 0.6);
                double num76 = 1.0 + 1600.0 / (double)num74;
                double num77 = 1.0 + 250.0 / (double)num74;
                num76 *= num75 * num75;
                double num78 = (double)((num65 == ship.inner.planetA || num65 == ship.inner.planetB) ? 1.25f : 1.5f);
                double num79 = Math.Sqrt(num66);
                double num80 = (double)num74 / num79 * 1.6 - 0.1;
                if (num80 > 1.0)
                {
                    num80 = 1.0;
                }
                else if (num80 < 0.0)
                {
                    num80 = 0.0;
                }
                double num81 = num79 - (double)num74 * 0.82;
                if (num81 < 1.0)
                {
                    num81 = 1.0;
                }
                double num82 = (double)(num62 - 6f) / (num81 * (double)num47) * 0.6 - 0.01;
                if (num82 > 1.5)
                {
                    num82 = 1.5;
                }
                else if (num82 < 0.0)
                {
                    num82 = 0.0;
                }
                VectorLF3 vectorLF7 = ship.inner.uPos + ((VectorLF3) ship.inner.uVel * rhs) - astroPoses[num65].uPos;
                double num83 = vectorLF7.magnitude / (double)num74;
                if (num83 < num78)
                {
                    double num84 = (num83 - 1.0) / (num78 - 1.0);
                    if (num84 < 0.0)
                    {
                        num84 = 0.0;
                    }
                    num84 = 1.0 - num84 * num84;
                    rhs2 = vectorLF7.normalized * (num82 * num82 * num84 * 2.0 * (double)(1f - ship.inner.warpState));
                }
                VectorLF3 vectorLF8 = ship.inner.uPos - astroPoses[num65].uPos;
                VectorLF3 lhs4 = new VectorLF3(vectorLF8.x / num79, vectorLF8.y / num79, vectorLF8.z / num79);
                vectorLF5 += lhs4 * num80;
                num73 = (float)num80;
                double num85 = num79 / (double)num74;
                num85 *= num85;
                num85 = (num76 - num85) / (num76 - num77);
                if (num85 > 1.0)
                {
                    num85 = 1.0;
                }
                else if (num85 < 0.0)
                {
                    num85 = 0.0;
                }
                if (num85 > 0.0)
                {
                    VectorLF3 v = Maths.QInvRotateLF(astroPoses[num65].uRot, vectorLF8);
                    VectorLF3 lhs5 = Maths.QRotateLF(astroPoses[num65].uRotNext, v) + astroPoses[num65].uPosNext;
                    num85 = (3.0 - num85 - num85) * num85 * num85;
                    vectorLF6 = (lhs5 - ship.inner.uPos) * num85;
                }
            }
            Vector3 vector;
            ship.inner.uRot.ForwardUp(out ship.inner.uVel, out vector);
            Vector3 vector2 = vector * (1f - num73) + (Vector3)vectorLF5 * num73;
            vector2 -= Vector3.Dot(vector2, ship.inner.uVel) * ship.inner.uVel;
            vector2.Normalize();
            Vector3 vector3 = vectorLF.normalized + rhs2;
            Vector3 a = Vector3.Cross(ship.inner.uVel, vector3);
            float num86 = ship.inner.uVel.x * vector3.x + ship.inner.uVel.y * vector3.y + ship.inner.uVel.z * vector3.z;
            Vector3 a2 = Vector3.Cross(vector, vector2);
            float num87 = vector.x * vector2.x + vector.y * vector2.y + vector.z * vector2.z;
            if (num86 < 0f)
            {
                a = a.normalized;
            }
            if (num87 < 0f)
            {
                a2 = a2.normalized;
            }
            float d = ((num61 < 3.0) ? ((3.25f - (float)num61) * 4f) : (num62 / shipSailSpeed * (flag9 ? 0.2f : 1f)));
            a = a * d + a2 * 2f;
            Vector3 a3 = a - ship.inner.uAngularVel;
            float d2 = ((a3.sqrMagnitude < 0.1f) ? 1f : 0.05f);
            ship.inner.uAngularVel += a3 * d2;
            double num88 = (double)ship.inner.uSpeed * dt;
            ship.inner.uPos.x = ship.inner.uPos.x + (double)ship.inner.uVel.x * num88 + vectorLF6.x;
            ship.inner.uPos.y = ship.inner.uPos.y + (double)ship.inner.uVel.y * num88 + vectorLF6.y;
            ship.inner.uPos.z = ship.inner.uPos.z + (double)ship.inner.uVel.z * num88 + vectorLF6.z;
            Vector3 normalized = ship.inner.uAngularVel.normalized;
            double num89 = (double)ship.inner.uAngularVel.magnitude * dt * 0.5;
            float w = (float)Math.Cos(num89);
            float num90 = (float)Math.Sin(num89);
            Quaternion lhs6 = new Quaternion(normalized.x * num90, normalized.y * num90, normalized.z * num90, w);
            ship.inner.uRot = lhs6 * ship.inner.uRot;
            if (ship.inner.warpState > 0f)
            {
                float num91 = ship.inner.warpState * ship.inner.warpState * ship.inner.warpState;
                ship.inner.uRot = Quaternion.Slerp(ship.inner.uRot, Quaternion.LookRotation(vector3, vector2), num91);
                ship.inner.uAngularVel *= 1f - num91;
            }
            if (num53 < 100.0)
            {
                float num92 = 1f - (float)num53 / 100f;
                num92 = (3f - num92 - num92) * num92 * num92;
                num92 *= num92;
                if (ship.inner.direction > 0)
                {
                    // FIXME: Figure out what it should be for the orbit...
                    //quaternion = Quaternion.Slerp(ship.inner.uRot, astroPose2.uRot * (gStationPool[shipData.otherGId].shipDockRot * new Quaternion(0.70710677f, 0f, 0f, -0.70710677f)), num92);
                    quaternion = Quaternion.Slerp(ship.inner.uRot, astroPose2.uRot, num92);
                }
                else
                {
                    Vector3 vector4 = (ship.inner.uPos - astroPose.uPos).normalized;
                    Vector3 normalized2 = (ship.inner.uVel - Vector3.Dot(ship.inner.uVel, vector4) * vector4).normalized;
                    quaternion = Quaternion.Slerp(ship.inner.uRot, Quaternion.LookRotation(normalized2, vector4), num92);
                }
                flag8 = true;
            }
            if (flag10)
            {
                ship.inner.uRot = quaternion;
                if (ship.inner.direction > 0)
                {
                    ship.inner.pPosTemp = Maths.QInvRotateLF(astroPose2.uRot, ship.inner.uPos - astroPose2.uPos);
                    ship.inner.pRotTemp = Quaternion.Inverse(astroPose2.uRot) * ship.inner.uRot;
                }
                else
                {
                    ship.inner.pPosTemp = Maths.QInvRotateLF(astroPose.uRot, ship.inner.uPos - astroPose.uPos);
                    ship.inner.pRotTemp = Quaternion.Inverse(astroPose.uRot) * ship.inner.uRot;
                }
                quaternion = Quaternion.identity;
                flag8 = false;
            }
            if (ship.renderingData.anim.z > 1f)
            {
                ship.renderingData.anim.z -= - (float)dt * 0.3f;
            }
            else
            {
                ship.renderingData.anim.z = 1f;
            }
            ship.renderingData.anim.w = ship.inner.warpState;
            VectorLF3 relativePos = GameMain.data.relativePos;
            Quaternion relativeRot = GameMain.data.relativeRot;
            if (flag8)
            {
                ship.renderingData.SetPose(ship.inner.uPos, quaternion, relativePos, relativeRot, ship.inner.uVel * ship.inner.uSpeed, 1501);
                ship.uiRenderingData.SetPose(ship.inner.uPos, quaternion, (float)(astroPose.uPos - astroPose2.uPos).magnitude, ship.inner.uSpeed, 1501);
            }
            else
            {
                ship.renderingData.SetPose(ship.inner.uPos, ship.inner.uRot, relativePos, relativeRot, ship.inner.uVel * ship.inner.uSpeed, 1501);
                ship.uiRenderingData.SetPose(ship.inner.uPos, ship.inner.uRot, (float)(astroPose.uPos - astroPose2.uPos).magnitude, ship.inner.uSpeed, 1501);
            }
            if (ship.renderingData.anim.z < 0f)
            {
                ship.renderingData.anim.z = 0f;
            }
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
