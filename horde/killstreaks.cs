using System.Collections;
using System.Collections.Generic;
using InfinityScript;
using static InfinityScript.GSCFunctions;

namespace horde
{
    public class killstreaks : BaseScript
    {
        private static bool heliSniperOut = false;
        private static Entity level = Entity.Level;
        public static int heliHeight = 1500;

        public static bool hasFreeKillstreakSlot(Entity player)
        {
            string[] streaks = player.GetField<string[]>("killstreaks");
            foreach (string streak in streaks)
            {
                if (string.IsNullOrEmpty(streak))
                    return true;
            }
            return false;
        }
        public static int getNextFreeKillstreakSlot(Entity player)
        {
            string[] streaks = player.GetField<string[]>("killstreaks");
            for (int i = 0; i < 4; i++)
            {
                if (!string.IsNullOrEmpty(streaks[i])) continue;

                return i;
            }

            return 0;//Should never get here unless this is called without checking if there's free slots first
        }

        public static void giveKillstreak(Entity player, string streak)
        {
            bool hasFreeSlot = hasFreeKillstreakSlot(player);

            if (!hasFreeSlot) return;

            int nextSlot = getNextFreeKillstreakSlot(player);
            switch (streak)
            {
                case "ims":
                    player.PlayLocalSound("US_1mc_achieve_ims");
                    player.ShowHudSplash("ims", 0, 0);
                    player.GiveWeapon("killstreak_ims_mp", 0, false);
                    player.SetActionSlot(nextSlot+4, "weapon", "killstreak_ims_mp");
                    break;
                case "sentry":
                    player.PlayLocalSound("US_1mc_achieve_sentrygun");
                    player.ShowHudSplash("sentry", 0, 0);
                    player.GiveWeapon("killstreak_sentry_mp", 0, false);
                    player.SetActionSlot(nextSlot + 4, "weapon", "killstreak_sentry_mp");
                    break;
                case "missile":
                    player.PlayLocalSound("US_1mc_achieve_predator");
                    player.ShowHudSplash("predator_missile", 0, 0);
                    player.GiveWeapon("killstreak_predator_missile_mp", 0, false);
                    player.SetActionSlot(nextSlot + 4, "weapon", "killstreak_predator_missile_mp");
                    break;
                case "helicopter":
                    player.PlayLocalSound("US_1mc_achieve_helicopter");
                    player.ShowHudSplash("helicopter", 0, 0);
                    player.GiveWeapon("killstreak_helicopter_mp", 0, false);
                    player.SetActionSlot(nextSlot + 4, "weapon", "killstreak_helicopter_mp");
                    break;
                case "dragonfly":
                    player.PlayLocalSound("US_1mc_achieve_dragonfly");
                    player.ShowHudSplash("remote_uav", 0, 0);
                    player.GiveWeapon("killstreak_uav_mp", 0, false);
                    player.SetActionSlot(nextSlot + 4, "weapon", "killstreak_uav_mp");
                    break;
                case "heloscout":
                    player.PlayLocalSound("US_1mc_achieve_heli_sniper");
                    player.ShowHudSplash("heli_sniper", 0, 0);
                    player.GiveWeapon("killstreak_helicopter_flares_mp", 0, false);
                    player.SetActionSlot(nextSlot + 4, "weapon", "killstreak_helicopter_flares_mp");
                    break;
            }

            updateStreakSlot(player, nextSlot, streak);
        }

        public static IEnumerator executeKillstreak(Entity player, int slot)
        {
            if (player.Classname != "player"|| !player.HasField("isDown")) yield break;
            hordeUtils.updateAmmoHud(player, false);
            //killstreaks
            string newWeap = player.CurrentWeapon;

            int timePassed = 0;

            while (!newWeap.StartsWith("killstreak_"))
            {
                timePassed++;
                if (timePassed > 25) yield break;
                yield return Wait(.05f);
                newWeap = player.CurrentWeapon;
            }

            string[] streaks = player.GetField<string[]>("killstreaks");

            if (streaks[slot] == "missile" && newWeap == "killstreak_predator_missile_mp")
            {
                player.FreezeControls(true);
                AfterDelay(1000, () => player.VisionSetNakedForPlayer("black_bw", .5f));
                AfterDelay(1500, () => launchMissile(player));
            }
            else if (streaks[slot] == "sentry" && newWeap == "killstreak_sentry_mp")
            {
                spawnSentry(player, "");
                //player.SwitchToWeapon(player.GetField<string>("lastDroppableWeapon"));
            }
            else if (streaks[slot] == "sentrygl" && newWeap == "killstreak_remote_turret_mp")
            {
                spawnSentry(player, "gl");
                //player.SwitchToWeapon(player.GetField<string>("lastDroppableWeapon"));
            }
            else if (streaks[slot] == "ims" && newWeap == "killstreak_ims_mp")
            {
                spawnSentry(player, "ims");
                //player.SwitchToWeapon(player.GetField<string>("lastDroppableWeapon"));
            }
            else if (streaks[slot] == "dragonfly" && newWeap == "killstreak_uav_mp")
            {
                spawnDragonfly(player, player.GetEye().Around(100), player.Angles);
                player.SwitchToWeapon(player.GetField<string>("lastDroppableWeapon"));
                //player.DisableWeapons();
            }
            else if (streaks[slot] == "heloscout" && newWeap == "killstreak_helicopter_flares_mp")
            {
                Vector3 origin = player.GetOrigin();
                Vector3 pos = origin;
                if (canCallInHeliSniper(pos))
                {
                    Vector3 angles = player.GetPlayerAngles();
                    hordeUtils.teamSplash("used_heli_sniper", player);
                    callHeliSniper(player, pos);
                    player.SetField("ownsHeliSniper", false);
                    player.PlaySound("US_1mc_use_heli_sniper");
                    //AfterDelay(1250, () =>
                        //player.TakeWeapon("killstreak_ims_mp"));
                }
                else
                {
                    player.IPrintLnBold("Cannot call in Helo Scout!");
                    player.SwitchToWeapon(player.GetField<string>("lastDroppableWeapon"));
                }
            }
            updateStreakSlot(player, slot, "");
        }

        private static void updateStreakSlot(Entity player, int slot, string streak)
        {
            string[] killstreakList = player.GetField<string[]>("killstreaks");
            killstreakList[slot] = streak;
            player.SetField("killstreaks", new Parameter(killstreakList));
            if (streak == "") player.SetActionSlot(slot + 4, "");

            //Set the HUD for this
            string[] streaks = player.GetField<string[]>("streakSlotText");
            if (streak == "") streaks[slot] = "";
            else streaks[slot] = hordeUtils.createHudShaderString(horde.killstreakIcons[streak]) + "[{+actionslot " + (slot+4) + "}]";

            if (!player.HasField("hud_created")) return;

            HudElem list = player.GetField<HudElem>("hud_killstreakList");
            string newText = streaks[0] + "\n\n" + streaks[1] + "\n\n" + streaks[2] + "\n\n" + streaks[3];
            player.SetField("streakSlotText", new Parameter(streaks));
            list.SetText(newText);
        }

        public static void spawnSentry(Entity player, string type)
        {
            if (player.GetField<bool>("isCarryingSentry")) return;

            string weapon = "sentry_minigun_mp";
            if (type == "gl") weapon = "manned_gl_turret_mp";
            else if (type == "ims") weapon = "ims_projectile_mp";
            string model = "sentry_minigun";
            if (type == "gl") model = "sentry_grenade_launcher";
            else if (type == "ims") model = "ims_scorpion_body";
            Entity turret = SpawnTurret("misc_turret", player.Origin, weapon);
            turret.Angles = new Vector3(0, player.GetPlayerAngles().Y, 0);
            turret.SetModel(model);
            turret.SetField("baseModel", model);
            //turret.Health = 1000;
            //turret.SetCanDamage(true);
            turret.MakeTurretInOperable();
            turret.SetRightArc(80);
            turret.SetLeftArc(80);
            turret.SetBottomArc(50);
            turret.MakeUnUsable();
            turret.SetDefaultDropPitch(-89.0f);
            if (type == "gl") turret.SetConvergenceTime(1);
            turret.SetTurretModeChangeWait(true);
            turret.SetMode("sentry_offline");
            turret.SetField("owner", player);
            turret.SetTurretTeam("allies");
            turret.SetSentryOwner(player);
            turret.SetField("isSentry", true);
            turret.SetField("type", type);
            turret.SetField("readyToFire", true);

            turret.SetTurretMinimapVisible(true);

            turret.SetField("isBeingCarried", true);
            turret.SetField("canBePlaced", true);
            turret.SetField("timeLeft", 90);
            if (type == "gl") turret.SetField("timeLeft", 120);
            turret.SetField("target", turret);
            Entity trigger = Spawn("trigger_radius", turret.Origin + new Vector3(0, 0, 1), 0, 105, 64);
            turret.SetField("trigger", trigger);
            trigger.EnableLinkTo();
            trigger.LinkTo(turret);

            if (type == "ims")
            {
                Vector3 angles;
                Vector3 forward;

                Entity lid1 = Spawn("script_model", turret.GetTagOrigin("tag_lid1_attach"));
                lid1.SetModel("ims_scorpion_lid1");
                lid1.Angles = turret.Angles + new Vector3(0, 45, 0);
                lid1.SetField("tag", "tag_lid1");
                turret.SetField("lid1", lid1);
                angles = VectorToAngles(turret.Origin - lid1.Origin);
                forward = AnglesToForward(angles);
                Entity hinge1 = Spawn("script_model", lid1.Origin + (forward * 5));
                hinge1.SetModel("tag_origin");
                hinge1.Angles = angles;
                hinge1.LinkTo(turret);
                lid1.LinkTo(hinge1);
                lid1.SetField("hinge", hinge1);

                Entity lid2 = Spawn("script_model", turret.GetTagOrigin("tag_lid2_attach"));
                lid2.SetModel("ims_scorpion_lid1");
                lid2.Angles = turret.Angles + new Vector3(0, 45, 0);
                lid2.SetField("tag", "tag_lid2");
                turret.SetField("lid2", lid2);
                angles = VectorToAngles(turret.Origin - lid2.Origin);
                forward = AnglesToForward(angles);
                Entity hinge2 = Spawn("script_model", lid2.Origin + (forward * 5));
                hinge2.SetModel("tag_origin");
                hinge2.Angles = angles;
                hinge2.LinkTo(turret);
                lid2.LinkTo(hinge2);
                lid2.SetField("hinge", hinge2);

                Entity lid3 = Spawn("script_model", turret.GetTagOrigin("tag_lid3_attach"));
                lid3.SetModel("ims_scorpion_lid1");
                lid3.Angles = turret.Angles + new Vector3(0, 45, 0);
                lid3.SetField("tag", "tag_lid3");
                turret.SetField("lid3", lid3);
                angles = VectorToAngles(turret.Origin - lid3.Origin);
                forward = AnglesToForward(angles);
                Entity hinge3 = Spawn("script_model", lid3.Origin + (forward * 5));
                hinge3.SetModel("tag_origin");
                hinge3.Angles = angles;
                hinge3.LinkTo(turret);
                lid3.LinkTo(hinge3);
                lid3.SetField("hinge", hinge3);

                Entity lid4 = Spawn("script_model", turret.GetTagOrigin("tag_lid4_attach"));
                lid4.SetModel("ims_scorpion_lid1");
                lid4.Angles = turret.Angles + new Vector3(0, 45, 0);
                lid4.SetField("tag", "tag_lid4");
                turret.SetField("lid4", lid4);
                angles = VectorToAngles(turret.Origin - lid4.Origin);
                forward = AnglesToForward(angles);
                Entity hinge4 = Spawn("script_model", lid4.Origin + (forward * 5));
                hinge4.SetModel("tag_origin");
                hinge4.Angles = angles;
                hinge4.LinkTo(turret);
                lid4.LinkTo(hinge4);
                lid4.SetField("hinge", hinge4);

                Entity explosive1 = Spawn("script_model", turret.GetTagOrigin("tag_explosive1_attach"));
                explosive1.SetModel("ims_scorpion_explosive1");
                explosive1.SetField("tag", "tag_explosive1");
                explosive1.LinkTo(turret);
                turret.SetField("explosive1", explosive1);

                Entity explosive2 = Spawn("script_model", turret.GetTagOrigin("tag_explosive2_attach"));
                explosive2.SetModel("ims_scorpion_explosive2");
                explosive2.SetField("tag", "tag_explosive2");
                explosive2.LinkTo(turret);
                turret.SetField("explosive2", explosive2);

                Entity explosive3 = Spawn("script_model", turret.GetTagOrigin("tag_explosive3_attach"));
                explosive3.SetModel("ims_scorpion_explosive3");
                explosive3.SetField("tag", "tag_explosive3");
                explosive3.LinkTo(turret);
                turret.SetField("explosive3", explosive3);

                Entity explosive4 = Spawn("script_model", turret.GetTagOrigin("tag_explosive4_attach"));
                explosive4.SetModel("ims_scorpion_explosive4");
                explosive4.SetField("tag", "tag_explosive4");
                explosive4.LinkTo(turret);
                turret.SetField("explosive4", explosive4);

                turret.SetField("attacks", 4);
            }

            OnInterval(1000, () => sentry_timer(turret));
            if (type != "ims") OnInterval(50, () => sentry_targeting(turret));
            else OnInterval(50, () => ims_targeting(turret));
            pickupSentry(player, turret, true);
        }

        public static void pickupSentry(Entity player, Entity sentry, bool canCancel)
        {
            sentry.ClearTargetEntity();
            sentry.SetMode("sentry_offline");
            sentry.SetField("isBeingCarried", true);
            player.SetField("isCarryingSentry", true);
            player.SetField("isCarryingSentry_alt", true);//Used to fix a bug allowing players to 'faux-cancel' a placement causing a persistant sentry being held
            sentry.SetField("canBePlaced", true);
            player.DisableWeapons();
            //sentry.SetCanDamage(false);
            sentry.SetSentryCarrier(player);
            if (sentry.GetField<string>("type") != "ims") sentry.SetModel(sentry.GetField<string>("baseModel") + "_obj");
            else sentry.SetModel(sentry.GetField<string>("baseModel") + "_placement");

            OnInterval(50, () => sentryHoldWatcher(player, sentry, canCancel));
        }
        private static bool sentryHoldWatcher(Entity player, Entity sentry, bool canCancel)
        {
            if (horde.gameEnded) return false;
            if (!player.IsAlive || player.Classname != "player") return false;
            if (sentry.GetField<bool>("canBePlaced") && player.GetField<bool>("isCarryingSentry") && player.AttackButtonPressed() && player.IsOnGround())
            {
                player.EnableWeapons();
                if (canCancel && sentry.GetField<string>("type") == "gl")
                {
                    hordeUtils.teamSplash("used_gl_turret", player);
                    //player.TakeWeapon("killstreak_remote_turret_mp");
                    player.SwitchToWeapon(player.GetField<string>("lastDroppableWeapon"));
                    player.SetField("ownsSentryGL", false);
                }
                if (canCancel && sentry.GetField<string>("type") == "ims")
                {
                    hordeUtils.teamSplash("used_ims", player);
                    //player.TakeWeapon("killstreak_ims_mp");
                    player.SwitchToWeapon(player.GetField<string>("lastDroppableWeapon"));
                    player.SetField("ownsIMS", false);
                }
                else if (canCancel)
                {
                    hordeUtils.teamSplash("used_sentry", player);
                    //player.TakeWeapon("killstreak_sentry_mp");
                    player.SwitchToWeapon(player.GetField<string>("lastDroppableWeapon"));
                    player.SetField("ownsSentry", false);
                }
                player.SetField("isCarryingSentry", false);
                player.ClearField("isCarryingSentry_alt");
                //sentry.SetField("carriedBy");
                sentry.SetSentryCarrier(null);
                Vector3 angleToForward = AnglesToForward(new Vector3(0, player.GetPlayerAngles().Y, 0));
                sentry.Origin = player.Origin + angleToForward * 50;
                sentry.Angles = new Vector3(0, player.GetPlayerAngles().Y, 0);
                sentry.SetField("isBeingCarried", false);
                sentry.SetModel(sentry.GetField<string>("baseModel"));
                sentry.PlaySound("sentry_gun_plant");
                //turret.SetCanDamage(true);
                sentry.SetMode("sentry");
                //AfterDelay(500, () => StartAsync(handlePickup(sentry)));
                AfterDelay(500, () => handlePickupInterval(sentry));
                return false;
            }
            else return true;
        }

        private static bool sentry_timer(Entity sentry)
        {
            if (horde.gameEnded) return false;
            if (sentry.GetField<bool>("isBeingCarried") && sentry.GetField<Entity>("owner").IsAlive) return true;
            sentry.SetField("timeLeft", sentry.GetField<int>("timeLeft") - 1);
            if (sentry.GetField<int>("timeLeft") > 0 && sentry.GetField<Entity>("owner").IsAlive) return true;
            else
            {
                StartAsync(destroySentry(sentry));
                return false;
            }
        }
        private static bool sentry_targeting(Entity sentry)
        {
            if (horde.gameEnded) return false;
            if (sentry.GetField<int>("timeLeft") > 0)
            {
                if (!sentry.GetField<bool>("isBeingCarried"))
                {
                    sentry.SetField("target", sentry);
                    foreach (Entity b in bots.botsInPlay)
                    {
                        if (b.GetField<string>("state") == "dead") continue;
                        Entity botHitbox = b.GetField<Entity>("hitbox");
                        bool tracePass = SightTracePassed(sentry.GetTagOrigin("tag_flash"), botHitbox.Origin, false, botHitbox);
                        if (!tracePass)
                            continue;

                        float yaw = VectorToAngles(botHitbox.Origin - sentry.Origin).Y;
                        float clamp = yaw - sentry.Angles.Y;
                        //Log.Write(LogLevel.Debug, "Sentry: {0}, Bot: {1}, Angle: {2}", sentryYaw, yaw, clamp);
                        if (clamp < 290 && clamp > 70)
                            continue;
                        sentry.SetField("target", botHitbox);
                        break;
                    }

                    if (sentry.GetField<Entity>("target") != sentry)
                    {
                        sentry.SetTargetEntity(sentry.GetField<Entity>("target"));
                        if (sentry.GetField<string>("type") == "gl" && sentry.GetField<bool>("readyToFire")) StartAsync(sentryGL_fireTurret(sentry));
                        else if (sentry.GetField<string>("type") != "gl") sentry.ShootTurret();
                    }
                    else
                        sentry.ClearTargetEntity();

                    return true;
                }
                else return true;
            }
            else return false;
        }
        private static bool ims_targeting(Entity ims)
        {
            if (horde.gameEnded) return false;
            if (!ims.GetField<bool>("readyToFire")) return true;

            if (ims.GetField<int>("timeLeft") > 0)
            {
                if (!ims.GetField<bool>("isBeingCarried"))
                {
                    ims.SetField("target", ims);
                    foreach (Entity b in bots.botsInPlay)
                    {
                        if (b.GetField<string>("state") == "dead") continue;
                        Entity botHitbox = b.GetField<Entity>("hitbox");
                        bool isInRadius = botHitbox.Origin.DistanceTo(ims.Origin) < 256;
                        bool tracePass = SightTracePassed(ims.Origin + new Vector3(0, 0, 100), botHitbox.Origin, false, botHitbox);
                        if (!isInRadius || !tracePass)
                            continue;

                        ims.SetField("target", botHitbox);
                        break;
                    }

                    if (ims.GetField<Entity>("target") != ims)
                        StartAsync(ims_fire(ims));

                    return true;
                }
                else return true;
            }
            else return false;
        }
        private static IEnumerator ims_fire(Entity ims)
        {
            ims.SetField("readyToFire", false);

            ims.PlaySound("ims_trigger");
            Entity target = ims.GetField<Entity>("target");

            yield return Wait(.75f);

            int attacksLeft = ims.GetField<int>("attacks");
            StartAsync(ims_fireExplosive(target, ims.GetField<Entity>("explosive" + attacksLeft), ims.GetField<Entity>("lid" + attacksLeft), ims));

            ims.SetField("attacks", --attacksLeft);

            if (attacksLeft == 0)
            {
                StartAsync(destroySentry(ims));
                yield break;
            }

            yield return Wait(4);

            ims.SetField("readyToFire", true);
        }
        private static IEnumerator ims_fireExplosive(Entity target, Entity explosive, Entity lid, Entity ims)
        {
            Entity hinge = lid.GetField<Entity>("hinge");
            hinge.Unlink();
            hinge.RotatePitch(90, .3f);

            explosive.Unlink();
            explosive.RotateYaw(3600, 1);
            explosive.MoveTo(explosive.Origin + new Vector3(0, 0, 100), 1, .25f, .25f);
            explosive.PlaySound("ims_launch");

            yield return Wait(1);

            MagicBullet("ims_projectile_mp", explosive.Origin, target.Origin, ims.GetField<Entity>("owner"));

            hinge.LinkTo(ims);
            explosive.Hide();
        }
        private static IEnumerator sentryGL_fireTurret(Entity sentry)
        {
            sentry.SetField("readyToFire", false);

            yield return Wait(1);
            sentry.ShootTurret();

            yield return Wait(2);

            sentry.SetField("readyToFire", true);
        }
        /*
        private static IEnumerator handlePickup(Entity turret)
        {
            if (!Utilities.isEntDefined(turret)) yield break;

            Entity owner = turret.GetField<Entity>("owner");
            yield return owner.WaitTill("use_button_pressed:" + owner.EntRef.ToString());
            Entity trigger = turret.GetField<Entity>("trigger");

            if (!Utilities.isEntDefined(trigger)) yield break;

            bool isTouching = owner.IsTouching(trigger);
            bool isOnGround = owner.IsOnGround();

            if (owner.IsAlive && isTouching && isOnGround)
            {
                //bool useButtonPressed = owner.UseButtonPressed();

                if (!owner.GetField<bool>("isCarryingSentry") && !turret.GetField<bool>("isBeingCarried"))
                    sentryHoldWatcher(owner, turret, false);
                yield break;
            }
            else { StartAsync(handlePickup(turret)); yield break; }
        }
        */
        private static void handlePickupInterval(Entity turret)
        {
            OnInterval(100, () => watchForSentryPickup(turret, turret.GetField<Entity>("owner"), turret.GetField<Entity>("trigger")));
        }
        private static bool watchForSentryPickup(Entity turret, Entity owner, Entity trigger)
        {
            if (horde.gameEnded) return false;
            if (owner.IsAlive && owner.IsTouching(trigger) && owner.IsOnGround() && owner.UseButtonPressed())
            {
                if (!owner.GetField<bool>("isCarryingSentry") && !turret.GetField<bool>("isBeingCarried"))
                    pickupSentry(owner, turret, false);
                return false;
            }
            if (owner.IsAlive && turret.Health > 0) return true;
            else return false;
        }

        public static IEnumerator destroySentry(Entity sentry)
        {
            if (!sentry.HasField("isSentry"))
            {
                Utilities.PrintToConsole("Tried to destroy a sentry that was not defined!");
                yield break;
            }

            sentry.SetField("timeLeft", 0);

            Entity trigger = sentry.GetField<Entity>("trigger");
            //if (Utilities.isEntDefined(trigger))
            trigger.Delete();

            //Entity fx = sentry.GetField<Entity>("flashFx");
            //if (Utilities.isEntDefined(fx))
                //fx.Delete();

            sentry.ClearTargetEntity();
            sentry.SetCanDamage(false);
            sentry.SetDefaultDropPitch(40);
            sentry.SetMode("sentry_offline");
            sentry.Health = 0;
            if (sentry.GetField<string>("type") != "ims") sentry.SetModel(sentry.GetField<string>("baseModel") + "_destroyed");
            sentry.PlaySound("sentry_explode");

            Entity owner = sentry.GetField<Entity>("owner");
            if (owner.IsAlive)
            {
                if (sentry.GetField<string>("type") == "gl") owner.PlayLocalSound("US_1mc_turret_destroyed");
                else if (sentry.GetField<string>("type") == "ims") owner.PlayLocalSound("US_1mc_ims_destroyed");
                else owner.PlayLocalSound("US_1mc_sentry_gone");
            }

            PlayFXOnTag(horde.fx_sentryExplode, sentry, "tag_aim");
            yield return Wait(1.5f);
            sentry.PlaySound("sentry_explode_smoke");
            PlayFXOnTag(horde.fx_sentrySmoke, sentry, "tag_aim");

            yield return Wait(5.5f);

            PlayFX(horde.fx_sentryDeath, sentry.Origin + new Vector3(0, 0, 20));
            sentry.ClearField("owner");
            sentry.ClearField("isSentry");
            if (sentry.GetField<string>("type") == "ims")
            {
                for (int i = 1; i < 5; i++)
                {
                    sentry.GetField<Entity>("lid" + i).Delete();
                    sentry.ClearField("lid" + i);
                    sentry.GetField<Entity>("explosive" + i).Delete();
                    sentry.ClearField("explosive" + i);
                }
            }
            sentry.ClearField("type");
            sentry.Delete();
        }

        public static void launchMissile(Entity player)
        {
            if (player.CurrentWeapon != "killstreak_predator_missile_mp") return;

            Entity remoteMissileSpawn = getRandomMissileSpawn();

            if (remoteMissileSpawn == null || remoteMissileSpawn.Target == "")
            {
                Utilities.PrintToConsole("Remote missile spawn doesn't exist in the map!");
                return;
            }

            Entity target = GetEnt(remoteMissileSpawn.Target, "targetname");

            if (target == null)
            {
                Utilities.PrintToConsole("Remote missile spawn doesn't have a spawn point!");
                return;
            }

            Vector3 startPos = remoteMissileSpawn.Origin;
            Vector3 targetPos = target.Origin;

            Vector3 vector = VectorNormalize(startPos - targetPos);
            startPos = (vector * 14000) + targetPos;
            /*
            Vector3 upVector = new Vector3(0, 0, missileRemoteLaunchVert);
            int backDist = missileRemoteLaunchHorz;
            int targetDist = missileRemoteLaunchTargetDist;

            Vector3 forward = AnglesToForward(player.Angles);
            Vector3 startPos = player.Origin + upVector + forward * backDist * -1;
            Vector3 targetPos = player.Origin + forward * targetDist;
            */

            Entity rocket = MagicBullet("remotemissile_projectile_mp", startPos, targetPos, player);
            rocket.SetField("clusters", 2);

            if (rocket == null)
            {
                //clearUsingRemote(player, "remotemissile");
                return;
            }
            rocket.SetCanDamage(true);

            player.SetField("isUsingRemote", true);
            StartAsync(missileEyes(player, rocket));
            StartAsync(watchForClusterUsage(player, rocket));
        }

        private static IEnumerator missileEyes(Entity player, Entity rocket)
        {
            player.VisionSetMissileCamForPlayer("black_bw", 0);

            if (player.Classname != "player") yield return null;

            player.VisionSetMissileCamForPlayer(horde.thermal_vision, 1);
            StartAsync(delayedFOFOverlay(player));

            player.CameraLinkTo(rocket, "tag_origin");
            player.ControlsLinkTo(rocket);

            yield return rocket.WaitTill("death");

            player.ControlsUnlink();
            player.FreezeControls(true);

            if (GetDvarInt("scr_gameended") == 0)
                StartAsync(staticEffect(player, 0.5f));

            yield return Wait(0.5f);

            player.VisionSetNakedForPlayer("", 0);
            player.FreezeControls(false);
            player.ThermalVisionFOFOverlayOff();
            player.ClearField("isUsingRemote");
            player.CameraUnlink();
            player.SwitchToWeapon(player.GetField<string>("lastDroppableWeapon"));

            yield break;
        }
        private static IEnumerator delayedFOFOverlay(Entity player)
        {
            if (!player.IsAlive || player.Classname != "player") yield return null;

            yield return Wait(0.15f);

            player.ThermalVisionFOFOverlayOn();

            yield break;
        }
        private static IEnumerator staticEffect(Entity player, float duration)
        {
            if (player.Classname != "player") yield return null;

            HudElem staticBG = NewClientHudElem(player);
            staticBG.HorzAlign = HudElem.HorzAlignments.Fullscreen;
            staticBG.VertAlign = HudElem.VertAlignments.Fullscreen;
            staticBG.SetShader("white", 640, 480);
            staticBG.Archived = true;
            staticBG.Sort = 10;

            HudElem staticFG = NewClientHudElem(player);
            staticFG.HorzAlign = HudElem.HorzAlignments.Fullscreen;
            staticFG.VertAlign = HudElem.VertAlignments.Fullscreen;
            staticFG.SetShader("ac130_overlay_grain", 640, 480);
            staticFG.Archived = true;
            staticFG.Sort = 20;

            yield return Wait(duration);

            staticFG.Destroy();
            staticBG.Destroy();

            yield break;
        }
        private static IEnumerator watchForClusterUsage(Entity player, Entity rocket)
        {
            yield return player.WaitTill("aim_button_pressed");

            Vector3 forward = AnglesToForward(rocket.Angles);
            Vector3 startPos = rocket.Origin + (forward * 50);
            Vector3 targetPos = rocket.Origin + (forward * 1000);
            Entity cluster = MagicBullet("sam_projectile_mp", startPos, targetPos, player);

            if (player.HasField("isUsingRemote"))
                StartAsync(watchForClusterUsage(player, rocket));
        }

        private static Entity getRandomMissileSpawn()
        {
            Entity ret = null;
            for (int i = 0; i < 700; i++)
            {
                Entity e = Entity.GetEntity(i);
                if (e == null) continue;
                if (e.TargetName == "remoteMissileSpawn")
                {
                    ret = e;
                    if (RandomInt(100) > 50) break;
                }
                else continue;
            }
            return ret;
        }

        private static void spawnDragonfly(Entity owner, Vector3 pos, Vector3 angles)
        {
            hordeUtils.teamSplash("used_remote_uav", owner);
            owner.PlaySound("US_1mc_use_dragonfly");

            Entity uav = SpawnHelicopter(owner, pos + new Vector3(0, 0, 50), angles, "remote_uav_mp", "vehicle_remote_uav");

            uav.Angles = angles;
            uav.SetField("owner", owner);
            owner.SetField("ownedLittlebird", uav);
            uav.SetField("isAlive", true);
            uav.SetField("timeLeft", 60);
            uav.SetField("target", uav);
            uav.SetVehicleTeam("allies");
            uav.SetVehWeapon("remote_uav_weapon_mp");
            uav.SetSpeed(50, 15, 15);
            uav.SetTurningAbility(.5f);
            uav.SetYawSpeed(100, 50, 25);
            uav.SetHoverParams(10, 50, 25);
            //uav.SetVehicleLookAtText(owner.Name, "");
            uav.SetVehGoalPos(pos + new Vector3(0, 0, 50), true);
            uav.ClearTargetYaw();
            uav.ClearGoalYaw();

            OnInterval(1000, () => dragonfly_timer(uav));
            OnInterval(50, () => dragonfly_targeting(uav));
        }
        private static bool dragonfly_timer(Entity uav)
        {
            if (!uav.GetField<bool>("isAlive")) return false;
            uav.SetField("timeLeft", uav.GetField<int>("timeLeft") - 1);
            //Log.Write(LogLevel.All, "Time is {0}", uav.GetField<int>("timeLeft"));
            if (uav.GetField<int>("timeLeft") > 0 && uav.GetField<Entity>("owner").IsAlive) return true;
            else
            {
                StartAsync(destroyLittlebird(uav));
                return false;
            }
        }
        private static bool dragonfly_targeting(Entity uav)
        {
            if (horde.gameEnded) return false;

            if (uav.GetField<bool>("isAlive"))
            {
                Vector3 uavTargetDest = uav.GetField<Entity>("owner").Origin;
                if (uav.Origin.DistanceTo2D(uavTargetDest) > 100)
                {
                    uav.SetVehGoalPos(uavTargetDest + new Vector3(0, 0, 200), true);
                    Vector3 targetYaw = VectorToAngles(uav.Origin - uavTargetDest);
                    uav.SetGoalYaw(targetYaw.Y);
                }
                uav.SetField("target", uav);
                foreach (Entity b in bots.botsInPlay)
                {
                    if (b.GetField<string>("state") == "dead") continue;
                    Entity botHitbox = b.GetField<Entity>("hitbox");
                    Vector3 flashTag = uav.GetTagOrigin("tag_flash");
                    Vector3 botOrigin = botHitbox.Origin;
                    bool tracePass = SightTracePassed(flashTag, botOrigin, false, botHitbox);
                    if (!tracePass)
                        continue;

                    uav.SetField("target", botHitbox);
                    break;
                }
                if (uav.GetField<Entity>("target") != uav)
                {
                    Vector3 forward = VectorToAngles(uav.GetField<Entity>("target").Origin - uav.Origin);
                    uav.SetGoalYaw(forward.Y);
                    uav.SetTurretTargetEnt(uav.GetField<Entity>("target"));
                    uav.FireWeapon("tag_flash", uav.GetField<Entity>("target"));
                }
                return true;
            }
            else return false;
        }

        private static IEnumerator destroyLittlebird(Entity uav)
        {
            uav.SetField("isAlive", false);
            Entity fx = SpawnFX(horde.fx_sentryDeath, uav.Origin);
            TriggerFX(fx);
            uav.Hide();
            uav.TurnEngineOff();
            uav.PlaySound("recondrone_destroyed");
            uav.GetField<Entity>("owner").ClearField("ownedLittlebird");
            yield return Wait(5);
            uav.FreeHelicopter();

            uav.Delete();
            fx.Delete();
        }

        public static bool canCallInHeliSniper(Vector3 pos)
        {
            return !heliSniperOut;
        }

        public static void callHeliSniper(Entity owner, Vector3 location)
        {
            Vector3 pathStart;
            //shuffleStreaks(owner);
            if (heliHeight > location.Z && heliHeight - location.Z > 500)
                pathStart = new Vector3(location.X - 10000, location.Y, heliHeight);
            else pathStart = location + new Vector3(-10000, 0, 2000);

            Vector3 angles = VectorToAngles(location - pathStart);
            Vector3 forward = AnglesToForward(angles);
            Entity lb = SpawnHelicopter(owner, pathStart, forward, "attack_littlebird_mp", "vehicle_little_bird_armed");

            //lb.MakeVehicleSolidSphere(128, 0);//Disabled to avoid trolling players

            lb.Angles = new Vector3(0, angles.Y, 0);
            lb.SetField("owner", owner);
            lb.SetVehicleTeam("allies");
            lb.EnableLinkTo();
            lb.SetSpeed(375, 225, 75);
            lb.SetHoverParams(5, 10, 5);
            lb.SetTurningAbility(1f);
            lb.SetYawSpeed(200, 75);
            lb.SetField("doneService", false);
            lb.SetField("heliTime", 120);
            lb.SetField("location", location);
            owner.SetField("ownsHeliSniper", false);
            StartAsync(heliSniper_flyIn(owner, lb, location));
            StartAsync(heliSniper_doBoarding(lb, owner));
        }
        private static IEnumerator heliSniper_flyIn(Entity owner, Entity lb, Vector3 loc)
        {
            if (heliHeight > loc.Z && heliHeight - loc.Z > 500)
                lb.SetVehGoalPos(new Vector3(loc.X, loc.Y, heliHeight), true);
            else
                lb.SetVehGoalPos(loc + new Vector3(0, 0, 2000), true);

            yield return Wait(3f);

            OnInterval(1000, () => heliSniper_runTimer(lb));
        }
        private static bool heliSniper_runTimer(Entity lb)
        {
            if (!lb.GetField<bool>("doneService"))
            {
                int time = lb.GetField<int>("heliTime");
                lb.SetField("heliTime", time - 1);
                if (horde.gameEnded) { lb.SetField("hasPassenger", false); time = 0; }
                if (time == 0)
                {
                    StartAsync(heliSniper_leave(lb));
                    return false;
                }
                else return true;
            }
            else if (!lb.GetField<bool>("doneService")) return true;
            else return false;
        }
        public static IEnumerator heliSniper_doBoarding(Entity heli, Entity player)
        {
            //player.Hide();
            player.SetField("isInHeliSniper", true);
            player.DisableWeapons();
            player.FreezeControls(true);
            player.GiveWeapon("iw5_as50_mp_as50scope");
            //player.GiveMaxAmmo("iw5_as50_mp");
            player.VisionSetNakedForPlayer("black_bw", 1f);
            //Entity visual = player.ClonePlayer();
            //visual.Origin = player.Origin;
            //visual.Hide();
            yield return Wait(1);

            player.VisionSetNakedForPlayer("", 1f);
            Vector3 tagOrigin = heli.GetTagOrigin("tag_player_attach_left");
            Vector3 tagAngles = heli.GetTagAngles("tag_player_attach_left");
            player.SetOrigin(tagOrigin);
            player.SetPlayerAngles(tagAngles);
            player.PlayerLinkTo(heli, "tag_player_attach_left", .5f, 10, 170, 30, 150, false);
            player.SetStance("crouch");
            player.AllowJump(false);
            player.AllowSprint(false);
            OnInterval(50, () => heliSniper_monitorStance(player));
            player.FreezeControls(false);
            player.EnableWeapons();
            player.SwitchToWeapon("iw5_as50_mp_as50scope");
            player.DisableWeaponSwitch();
            player.SetSpreadOverride(1);
            player.SetField("isInHeliSniper", true);
            AfterDelay(2000, () => player.Player_RecoilScaleOn(0));
            OnInterval(1000, () => heliSniper_leaveOnAmmoDepleted(heli, player));
            OnInterval(100, () => heliSniper_watchViewClamp(heli, player));
            //OnInterval(1000, () => heliSniper_watchPlayerControls(heli, player));
        }
        private static bool heliSniper_monitorStance(Entity player)
        {
            if (!player.IsAlive) return false;

            if (player.GetStance() != "crouch") player.SetStance("crouch");
            if (player.HasField("isInHeliSniper")) return true;
            else return false;
        }
        private static bool heliSniper_watchViewClamp(Entity lb, Entity player)
        {
            float yaw = player.GetPlayerAngles().Y;
            Vector3 lbAngles = lb.Angles;
            float clamp = lbAngles.Y - yaw;
            //Utilities.PrintToConsole(clamp.ToString());
            if (clamp > -185 && clamp < -100)
                lb.SetGoalYaw(lbAngles.Y + 10);
            else if (clamp < 15 && clamp > -60)
                lb.SetGoalYaw(lbAngles.Y - 10);

            if (!lb.GetField<bool>("doneService")) return true;
            else return false;
        }
        private static bool heliSniper_watchPlayerControls(Entity lb, Entity player)
        {
            Vector3 movement = player.GetNormalizedMovement();
            Utilities.PrintToConsole(movement.ToString());

            if (!lb.GetField<bool>("doneService")) return true;
            else return false;
        }
        private static bool heliSniper_leaveOnAmmoDepleted(Entity heli, Entity player)
        {
                if (player.GetAmmoCount("iw5_as50_mp_as50scope") == 0)
                {
                    heli.SetField("heliTime", 0);
                    return false;
                }
                return true;
        }
        private static bool heliSniper_leaveOnPlayerDeath(Entity lb, Entity player)
        {
                if (!player.IsAlive)
                {
                    lb.SetField("hasPassenger", false);
                    StartAsync(heliSniper_leave(lb));
                }

                if (player.IsAlive && !lb.GetField<bool>("doneService")) return true;
                else return false;
        }
        private static IEnumerator heliSniper_leave(Entity lb)
        {
            Vector3 location = lb.GetField<Vector3>("location");
            Entity owner = lb.GetField<Entity>("owner");
            lb.SetField("doneService", true);
            lb.SetSpeed(150, 50, 50);
            lb.SetVehGoalPos(new Vector3(location.X, location.Y, heliHeight), true);

            yield return Wait(3);

            lb.SetVehGoalPos(location + new Vector3(0, 0, 200), true);

            yield return Wait(3.05f);

            owner.EnableWeaponSwitch();
            owner.SwitchToWeapon(owner.GetField<string>("lastDroppableWeapon"));
            hordeUtils.updateAmmoHud(owner, true);
            owner.DisableWeaponSwitch();

            yield return Wait(.75f);

            owner.TakeWeapon("iw5_as50_mp_as50scope");
            owner.Unlink();
            owner.EnableWeaponSwitch();
            owner.ResetSpreadOverride();
            owner.Player_RecoilScaleOff();
            owner.AllowSprint(true);
            owner.AllowJump(true);
            OnInterval(50, () => heliSniper_checkForPlayerClipping(owner));
            lb.ClearGoalYaw();
            lb.ClearTargetYaw();
            lb.SetVehGoalPos(lb.Origin + new Vector3(0, 0, 1800), true);

            yield return Wait(5.05f);

            lb.SetSpeed(350, 225, 75);
            lb.SetVehGoalPos(lb.Origin + new Vector3(-100000, 0, 0), false);

            yield return Wait(5);

            lb.FreeHelicopter();
            lb.Delete();
        }

        private static bool heliSniper_checkForPlayerClipping(Entity player)
        {
            //Vector3 ground = PlayerPhysicsTrace(player.Origin, player.Origin - new Vector3(0, 0, 100));
            bool isGrounded = player.IsOnGround();
            if (player.Classname == "player" && !isGrounded)
            {
                //Temporary fix, push player forward on X axis until they're unstuck
                player.SetOrigin(player.Origin + new Vector3(1, 0, -1));
                return true;
            }
            player.ClearField("isInHeliSniper");
            return false;
        }
    }
}
