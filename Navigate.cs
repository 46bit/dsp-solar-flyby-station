using System;
using UnityEngine;

namespace DSPSailFlyby
{
    public enum EShipTravelDirection
    {
        GoingAway = 1,
        Idle = 0,
        GoingHome = -1,
    }

    public class Navigate
    {
        // Usage instructions:
        // Must set `ship.planetA` and `ship.planetB` to closest astroBody IDs. Seems to work even if actual positions are in high orbit.
        // For planets the astroBody IDs are `planet.id`. For stars it's `star.id * 100`.
        // Must set `ship.direction` to `-1` (if going to `positionA`) or `1` (if going to `positionB`)
        // Positions and rotations are both absolute positions, you may need to recalculate before every call to keep up to date with moving bodies
        // Returns true when within `desiredDistance` of `positionA` if `ship.direction == -1` or `positionB` if `ship.direction == 1`
        public static bool update(AstroPose[] astroPoses, VectorLF3 positionA, VectorLF3 positionB, Quaternion rotationA, Quaternion rotationB, double warpEnableDist, double desiredDistance, ref ShipData ship, ref ShipRenderingData shipRenderingData, ref ShipUIRenderingData shipUIRenderingData)
        {
            double dt = 1.0 / 60.0;
            float shipWarpSpeed = GameMain.history.logisticShipWarpSpeedModified;
            float shipSailSpeed = GameMain.history.logisticShipSailSpeedModified;
            bool flag = shipWarpSpeed > shipSailSpeed + 1f;
            bool warperFree = false;
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
            AstroPose astroPose = astroPoses[ship.planetA];
            AstroPose astroPose2 = astroPoses[ship.planetB];
            bool returnValue = false;

            VectorLF3 lhs3 = (ship.direction > 0) ? positionB : positionA;
            VectorLF3 vectorLF = lhs3 - ship.uPos;
            double num53 = Math.Sqrt(vectorLF.x * vectorLF.x + vectorLF.y * vectorLF.y + vectorLF.z * vectorLF.z);
            VectorLF3 vectorLF2 = ((ship.direction > 0) ? (astroPose.uPos - ship.uPos) : (astroPose2.uPos - ship.uPos));
            double num54 = vectorLF2.x * vectorLF2.x + vectorLF2.y * vectorLF2.y + vectorLF2.z * vectorLF2.z;
            bool flag9 = num54 <= (double)(astroPose.uRadius * astroPose.uRadius) * 2.25;
            bool flag10 = false;
            if (num53 <= desiredDistance)
            {
                ship.t = 1f;
                returnValue = true;
                flag10 = true;
            }
            float num55 = 0f;
            if (flag)
            {
                double num56 = (astroPose.uPos - astroPose2.uPos).magnitude * 2.0;
                double num57 = (((double)shipWarpSpeed < num56) ? ((double)shipWarpSpeed) : num56);
                double num58 = warpEnableDist * 0.5;
                if (ship.warpState <= 0f)
                {
                    ship.warpState = 0f;
                    if (num54 > 25000000.0 && num53 > num58 && ship.uSpeed >= shipSailSpeed && (ship.warperCnt > 0 || warperFree))
                    {
                        ship.warperCnt--;
                        ship.warpState += (float)dt;
                    }
                }
                else
                {
                    num55 = (float)(num57 * ((Math.Pow(1001.0, (double)ship.warpState) - 1.0) / 1000.0));
                    double num59 = (double)num55 * 0.0449 + 5000.0 + (double)shipSailSpeed * 0.25;
                    double num60 = num53 - num59;
                    if (num60 < 0.0)
                    {
                        num60 = 0.0;
                    }
                    if (num53 < num59)
                    {
                        ship.warpState -= (float)(dt * 4.0);
                    }
                    else
                    {
                        ship.warpState += (float)dt;
                    }
                    if (ship.warpState < 0f)
                    {
                        ship.warpState = 0f;
                    }
                    else if (ship.warpState > 1f)
                    {
                        ship.warpState = 1f;
                    }
                    if (ship.warpState > 0f)
                    {
                        num55 = (float)(num57 * ((Math.Pow(1001.0, (double)ship.warpState) - 1.0) / 1000.0));
                        if ((double)num55 * dt > num60)
                        {
                            num55 = (float)(num60 / dt * 1.01);
                        }
                    }
                }
            }
            double num61 = num53 / ((double)ship.uSpeed + 0.1) * 0.382 * (double)num47;
            float num62;
            if (ship.warpState > 0f)
            {
                num62 = (ship.uSpeed = shipSailSpeed + num55);
                if (num62 > shipSailSpeed)
                {
                    num62 = shipSailSpeed;
                }
            }
            else
            {
                float num63 = (float)((double)ship.uSpeed * num61) + 6f;
                if (num63 > shipSailSpeed)
                {
                    num63 = shipSailSpeed;
                }
                float num64 = (float)dt * (flag9 ? num48 : num49);
                if (ship.uSpeed < num63 - num64)
                {
                    ship.uSpeed += num64;
                }
                else if (ship.uSpeed > num63 + num50)
                {
                    ship.uSpeed -= num50;
                }
                else
                {
                    ship.uSpeed = num63;
                }
                num62 = ship.uSpeed;
            }
            int num65 = -1;
            double rhs = 0.0;
            double num66 = 1E+40;
            int num67 = ship.planetA / 100 * 100;
            int num68 = ship.planetB / 100 * 100;
            for (int k = num67; k < num67 + 99; k++)
            {
                float uRadius = astroPoses[k].uRadius;
                if (uRadius >= 1f)
                {
                    VectorLF3 vectorLF3 = ship.uPos - astroPoses[k].uPos;
                    double num69 = vectorLF3.x * vectorLF3.x + vectorLF3.y * vectorLF3.y + vectorLF3.z * vectorLF3.z;
                    double num70 = -((double)ship.uVel.x * vectorLF3.x + (double)ship.uVel.y * vectorLF3.y + (double)ship.uVel.z * vectorLF3.z);
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
                for (int l = num68; l < num68 + 99; l++)
                {
                    float uRadius2 = astroPoses[l].uRadius;
                    if (uRadius2 >= 1f)
                    {
                        VectorLF3 vectorLF4 = ship.uPos - astroPoses[l].uPos;
                        double num71 = vectorLF4.x * vectorLF4.x + vectorLF4.y * vectorLF4.y + vectorLF4.z * vectorLF4.z;
                        double num72 = -((double)ship.uVel.x * vectorLF4.x + (double)ship.uVel.y * vectorLF4.y + (double)ship.uVel.z * vectorLF4.z);
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
                // The original code makes logistics vessels avoid stars by 2.5x their radius
                // That works very badly with the larger stars introduced by GalacticScale, to the
                // extreme that vessels could never reach inner swarm orbits
                // if (num65 % 100 == 0)
                // {
                //     num74 *= 2.5f;
                // }
                double num75 = Math.Max(1.0, ((astroPoses[num65].uPosNext - astroPoses[num65].uPos).magnitude - 0.5) * 0.6);
                double num76 = 1.0 + 1600.0 / (double)num74;
                double num77 = 1.0 + 250.0 / (double)num74;
                num76 *= num75 * num75;
                double num78 = (double)((num65 == ship.planetA || num65 == ship.planetB) ? 1.25f : 1.5f);
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
                VectorLF3 vectorLF7 = ship.uPos + ((VectorLF3)ship.uVel * rhs) - astroPoses[num65].uPos;
                double num83 = vectorLF7.magnitude / (double)num74;
                if (num83 < num78)
                {
                    double num84 = (num83 - 1.0) / (num78 - 1.0);
                    if (num84 < 0.0)
                    {
                        num84 = 0.0;
                    }
                    num84 = 1.0 - num84 * num84;
                    rhs2 = vectorLF7.normalized * (num82 * num82 * num84 * 2.0 * (double)(1f - ship.warpState));
                }
                VectorLF3 vectorLF8 = ship.uPos - astroPoses[num65].uPos;
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
                    vectorLF6 = (lhs5 - ship.uPos) * num85;
                }
            }
            Vector3 vector;
            ship.uRot.ForwardUp(out ship.uVel, out vector);
            Vector3 vector2 = vector * (1f - num73) + (Vector3)vectorLF5 * num73;
            vector2 -= Vector3.Dot(vector2, ship.uVel) * ship.uVel;
            vector2.Normalize();
            Vector3 vector3 = vectorLF.normalized + rhs2;
            Vector3 a = Vector3.Cross(ship.uVel, vector3);
            float num86 = ship.uVel.x * vector3.x + ship.uVel.y * vector3.y + ship.uVel.z * vector3.z;
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
            Vector3 a3 = a - ship.uAngularVel;
            float d2 = ((a3.sqrMagnitude < 0.1f) ? 1f : 0.05f);
            ship.uAngularVel += a3 * d2;
            double num88 = (double)ship.uSpeed * dt;
            ship.uPos.x = ship.uPos.x + (double)ship.uVel.x * num88 + vectorLF6.x;
            ship.uPos.y = ship.uPos.y + (double)ship.uVel.y * num88 + vectorLF6.y;
            ship.uPos.z = ship.uPos.z + (double)ship.uVel.z * num88 + vectorLF6.z;
            Vector3 normalized = ship.uAngularVel.normalized;
            double num89 = (double)ship.uAngularVel.magnitude * dt * 0.5;
            float w = (float)Math.Cos(num89);
            float num90 = (float)Math.Sin(num89);
            Quaternion lhs6 = new Quaternion(normalized.x * num90, normalized.y * num90, normalized.z * num90, w);
            ship.uRot = lhs6 * ship.uRot;
            if (ship.warpState > 0f)
            {
                float num91 = ship.warpState * ship.warpState * ship.warpState;
                ship.uRot = Quaternion.Slerp(ship.uRot, Quaternion.LookRotation(vector3, vector2), num91);
                ship.uAngularVel *= 1f - num91;
            }
            if (num53 < 100.0)
            {
                float num92 = 1f - (float)num53 / 100f;
                num92 = (3f - num92 - num92) * num92 * num92;
                num92 *= num92;
                if (ship.direction > 0) // FIXME: Try to understand this. What does the else even do?? That's normally for returning to the ship dock, yet it doesn't use any input
                {
                    quaternion = Quaternion.Slerp(ship.uRot, rotationA, num92);
                }
                else
                {
                    Vector3 vector4 = (ship.uPos - astroPose.uPos).normalized;
                    Vector3 normalized2 = (ship.uVel - Vector3.Dot(ship.uVel, vector4) * vector4).normalized;
                    quaternion = Quaternion.Slerp(ship.uRot, Quaternion.LookRotation(normalized2, vector4), num92);
                }
                flag8 = true;
            }
            if (flag10)
            {
                ship.uRot = quaternion;
                if (ship.direction > 0)
                {
                    ship.pPosTemp = Maths.QInvRotateLF(astroPose2.uRot, ship.uPos - astroPose2.uPos);
                    ship.pRotTemp = Quaternion.Inverse(astroPose2.uRot) * ship.uRot;
                }
                else
                {
                    ship.pPosTemp = Maths.QInvRotateLF(astroPose.uRot, ship.uPos - astroPose.uPos);
                    ship.pRotTemp = Quaternion.Inverse(astroPose.uRot) * ship.uRot;
                }
                quaternion = Quaternion.identity;
                flag8 = false;
            }
            if (shipRenderingData.anim.z > 1f)
            {
                shipRenderingData.anim.z -= -(float)dt * 0.3f;
            }
            else
            {
                shipRenderingData.anim.z = 1f;
            }
            shipRenderingData.anim.w = ship.warpState;
            VectorLF3 relativePos = GameMain.data.relativePos;
            Quaternion relativeRot = GameMain.data.relativeRot;
            if (flag8)
            {
                shipRenderingData.SetPose(ship.uPos, quaternion, relativePos, relativeRot, ship.uVel * ship.uSpeed, 1501);
                shipUIRenderingData.SetPose(ship.uPos, quaternion, (float)(astroPose.uPos - astroPose2.uPos).magnitude, ship.uSpeed, 1501);
            }
            else
            {
                shipRenderingData.SetPose(ship.uPos, ship.uRot, relativePos, relativeRot, ship.uVel * ship.uSpeed, 1501);
                shipUIRenderingData.SetPose(ship.uPos, ship.uRot, (float)(astroPose.uPos - astroPose2.uPos).magnitude, ship.uSpeed, 1501);
            }
            if (shipRenderingData.anim.z < 0f)
            {
                shipRenderingData.anim.z = 0f;
            }

            // FIXME: Maybe not all of this needs setting on every update
            shipRenderingData.gid = 1;
            shipUIRenderingData.gid = 1;
            VectorLF3 viewTargetUPos = UIRoot.instance.uiGame.starmap.viewTargetUPos;
            shipUIRenderingData.rpos = (shipUIRenderingData.upos - viewTargetUPos) * 0.00025;

            return returnValue;
        }
    }
}
