using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InfinityScript;
using static InfinityScript.GSCFunctions;
using static horde.horde;
using static horde.hordeUtils;
using static horde.pathfinding;

namespace horde
{
    public class bots : BaseScript
    {
        public static List<Entity> botsInPlay = new List<Entity>();
        public static List<Entity> botPool = new List<Entity>();
        public static List<Entity> ragdolls = new List<Entity>();
        public static uint spawnedBots = 0;
        public static uint botsForWave = 5;
        public static Dictionary<string, int> botHealth = new Dictionary<string, int>();

        public static void spawnBot()
        {
            Entity bot = Spawn("script_model", Vector3.Zero);
            bot.Angles = Vector3.Zero;
            bot.EnableLinkTo();
            bot.SetModel(bodyModel);

            Vector3 weaponTag = bot.GetTagOrigin("tag_weapon_left");
            Entity gun = Spawn("script_model", weaponTag);
            gun.SetModel("viewmodel_default");
            bot.SetField("gun", gun);

            Entity bothead = Spawn("script_model", bot.Origin);
            bothead.SetModel(headModel);
            bothead.LinkTo(bot, "j_spine4", Vector3.Zero, Vector3.Zero);
            bot.SetField("head", bothead);
            bot.SetField("state", "idle");
            bot.SetField("animType", "ar");
            bot.SetField("shots", 0);

            Vector3 headOrigin = bot.GetTagOrigin("j_head");
            Entity headHitbox = Spawn("script_model", headOrigin);
            headHitbox.SetModel("ims_scorpion_explosive1");
            headHitbox.Angles = Vector3.Zero;
            headHitbox.Hide();
            headHitbox.SetCanDamage(true);
            headHitbox.SetCanRadiusDamage(false);
            headHitbox.LinkTo(bot, "j_head", Vector3.Zero, Vector3.Zero);
            headHitbox.SetField("parent", bot);
            bot.SetField("headHitbox", headHitbox);

            Entity botHitbox = Spawn("script_model", bot.Origin + new Vector3(0, 0, 30));
            botHitbox.SetModel("com_plasticcase_dummy");
            botHitbox.Angles = Vector3.Zero;
            botHitbox.Hide();
            botHitbox.SetCanDamage(false);
            botHitbox.SetCanRadiusDamage(false);
            botHitbox.SetField("currentHealth", 0);
            botHitbox.SetField("damageTaken", 0);
            botHitbox.LinkTo(bot, "j_mainroot", Vector3.Zero, Vector3.Zero);
            botHitbox.SetField("parent", bot);

            bot.SetField("hitbox", botHitbox);
            bot.SetField("isMoving", false);
            bot.SetField("currentGun", "");
            bot.SetField("target", bot);

            botHitbox.OnNotify("damage", (ent, damage, attacker, direction_vec, point, meansOfDeath, modelName, partName, tagName, iDFlags, weapon) => onBotDamage(botHitbox, damage, attacker, direction_vec, point, meansOfDeath, modelName, partName, tagName, iDFlags, weapon));
            headHitbox.OnNotify("damage", (ent, damage, attacker, direction_vec, point, meansOfDeath, modelName, partName, tagName, iDFlags, weapon) => onBotDamage(botHitbox, damage, attacker, direction_vec, point, "MOD_HEADSHOT", modelName, partName, tagName, iDFlags, weapon));

            bot.Hide();
            bothead.Hide();
            gun.Hide();

            botPool.Add(bot);
        }
        public static bool respawnBot(string type)
        {
            if (botPool.Count == 0) return true;//True so in case all 30 have spawned, don't error out
            Entity bot;
            bot = botPool[0];

            Entity spawn = getRandomBotSpawnpoint();
            bot.Origin = GetGroundPosition(spawn.Origin, 8);
            bot.Angles = spawn.Angles;
            bot.Show();
            bot.ShowAllParts();

            bot.SetField("state", "idle");
            bot.SetField("target", bot);
            bot.SetField("shots", 0);
            if (bot.HasField("head"))
            {
                Entity botHead = bot.GetField<Entity>("head");
                botHead.Show();
                //Remove helmet
                //botHead.HidePart("j_head_end");
                //botHead.HidePart("j_helmet");
                //botHead.HidePart("j_collar_rear");
                bot.GetField<Entity>("headHitbox").SetCanDamage(true);
            }
            spawnedBots++;
            Entity botHitbox = bot.GetField<Entity>("hitbox");
            botHitbox.SetField("currentHealth", botHealth[type]);
            botHitbox.SetField("damageTaken", 0);
            botHitbox.SetCanDamage(true);
            botHitbox.SetCanRadiusDamage(true);
            botHitbox.SetModel("com_plasticcase_dummy");

            switch (type)
            {
                case "enforcer":
                    updateBotGun(bot, "iw5_fad_mp");
                    bot.SetModel(bodyModel);
                    break;
                case "tower":
                    updateBotGun(bot, "none");
                    playAnimOnBot(bot, botAnims["tower_idle"]);//Play the anim since updateBotGun returns before that
                    //ghillie model
                    bot.SetModel(bodyModel);
                    break;
                case "striker":
                    updateBotGun(bot, "riotshield_mp");
                    //opforce model
                    bot.SetModel(bodyModel);
                    break;
                case "destructor":
                    updateBotGun(bot, "m320_mp");
                    bot.SetModel("mp_fullbody_opforce_juggernaut");
                    break;
                case "hammer":
                    updateBotGun(bot, "iw5_mk46_mp");
                    //opforce model
                    bot.SetModel(bodyModel);
                    break;
                default:
                    updateBotGun(bot, "iw5_deserteagle_mp");//Temporary fix for shotguns crashing
                    bot.SetModel(bodyModel);
                    break;
            }
            bot.SetField("type", type);

            botsInPlay.Add(bot);
            botPool.Remove(bot);

            if (type == "tower" || type == "striker")
            {
                OnInterval(100, () => botAI_noWeapon(bot, botHitbox));
            }
            else
            {
                OnInterval(100, () => botAI(bot, botHitbox));
                OnInterval(100, () => botAI_targeting(bot));
            }

            //Check for waypoints on spawn once
            foreach (pathNode v in getAllNodes())
            {
                bool waypointTrace = SightTracePassed(bot.GetTagOrigin("j_head"), v.location, false, botHitbox);//Check for waypoints
                if (waypointTrace)
                {
                    bot.SetField("currentWaypoint", new Parameter(v));//Set the first seen one as current
                    break;
                }
            }

            return true;
        }
        public static void updateBotGun(Entity bot, string weapon)
        {
            if (bot.GetField<string>("state") == "dead") return;

            Entity gun = bot.GetField<Entity>("gun");
            string weaponModel = GetWeaponModel(weapon);
            if (weaponModel == null || weaponModel == "" || weapon == "none") weaponModel = "tag_origin";
            gun.SetModel(weaponModel);
            gun.Show();
            bot.SetField("currentGun", weapon);
            bot.SetField("animType", WeaponClass(weapon));

            if (weapon == "none") return;

            gun.Angles = bot.GetTagAngles("tag_weapon_left");
            switch (bot.GetField<string>("animType"))
            {
                case "pistol":
                case "spread":
                    gun.LinkTo(bot, "tag_weapon_right", Vector3.Zero, Vector3.Zero);
                    break;
                default:
                    gun.LinkTo(bot, "tag_weapon_left", Vector3.Zero, Vector3.Zero);
                    break;
            }
            if (bot.GetField<string>("state") != "running")
                switch (bot.GetField<string>("animType"))
                {
                    case "pistol":
                        playAnimOnBot(bot, botAnims["idle_pistol"]);
                        break;
                    case "mg":
                        playAnimOnBot(bot, botAnims["idle_mg"]);
                        break;
                    case "rocketlauncher":
                        playAnimOnBot(bot, botAnims["idle_rpg"]);
                        break;
                    default:
                        playAnimOnBot(bot, botAnims["idle"]);
                        break;
                }
            else
                switch (bot.GetField<string>("animType"))
                {
                    case "pistol":
                        playAnimOnBot(bot, botAnims["run_pistol"]);
                        break;
                    case "mg":
                        playAnimOnBot(bot, botAnims["run_mg"]);
                        break;
                    case "rocketlauncher":
                        playAnimOnBot(bot, botAnims["run_rpg"]);
                        break;
                    case "spread":
                        playAnimOnBot(bot, botAnims["run_shotgun"]);
                        break;
                    case "sniper":
                        playAnimOnBot(bot, botAnims["run_sniper"]);
                        break;
                    case "smg":
                        playAnimOnBot(bot, botAnims["run_smg"]);
                        break;
                    default:
                        playAnimOnBot(bot, botAnims["run"]);
                        break;
                }
        }
        public static void killBotIfUnderMap(Entity bot)
        {
            if (!bot.HasField("state")) return;
            if (bot.GetField<string>("state") != "dead" && bot.Origin.Z < mapHeight)
            {
                bot.SetField("isAlive", false);
                bot.SetField("state", "dead");
                Entity hitbox = bot.GetField<Entity>("hitbox");
                hitbox.SetCanDamage(false);
                hitbox.SetCanRadiusDamage(false);
                hitbox.SetModel("tag_origin");//Change model to avoid the dead bot's hitbox blocking shots
                bot.MoveTo(bot.Origin, 0.05f);

                spawnBotRagdoll(bot, "MOD_PASSTHRU", Vector3.Zero);
                despawnBot(bot);
                botsInPlay.Remove(bot);
                roundUtil.checkForEndRound();
            }
        }

        public static bool botAI(Entity ai, Entity botHitbox)
        {
            if (gameEnded) return false;
            if (!ai.HasField("state")) return false;//Return if our bot isn't set up correctly

            if (ai.GetField<string>("state") == "dead" || !botsInPlay.Contains(ai) || botHitbox.GetField<int>("currentHealth") <= botHitbox.GetField<int>("damageTaken")) return false;
            killBotIfUnderMap(ai);
            if (ai.GetField<string>("state") == "dead") return false;

            if (ai.GetField<string>("state") == "shooting") return true;//Don't trace any movement while shooting

            pathNode targetPathNode = null;
            Vector3 botOrigin = ai.Origin;
            Vector3 botHeadTag = ai.GetTagOrigin("j_head");

            #region pathing
            //find a pathNode closes to the nearest player and path towards them
            if (ai.HasField("target") && ai.HasField("currentWaypoint"))
            {
                if (botOrigin.DistanceTo(ai.GetField<pathNode>("currentWaypoint").location) < 50)
                {
                    ai.ClearField("currentWaypoint");
                    return true;
                }
            }
            else if (!ai.HasField("currentWaypoint"))//Recalculate point
            {
                foreach (pathNode v in getAllNodes())
                {
                    bool waypointTrace = SightTracePassed(ai.GetTagOrigin("j_head"), v.location, false, botHitbox);//Check for waypoints
                    if (waypointTrace)
                    {
                        ai.SetField("currentWaypoint", new Parameter(v));//Set the first seen one as current
                        break;
                    }
                }
            }
            if (ai.HasField("currentWaypoint")) targetPathNode = ai.GetField<pathNode>("currentWaypoint");
            #endregion
            //Now we are done targeting, do the action

            #region motion
            if (ai.GetField<string>("state") == "shooting") return true;

            float ground = GetGroundPosition(botOrigin, 12).Z;

            if (targetPathNode != null)//Move to our targetPathNode
            {
                Vector3 targetOrigin = targetPathNode.location;
                float angleY = VectorToAngles(targetOrigin - botOrigin).Y;
                ai.RotateTo(new Vector3(0, angleY, 0), .3f, .1f, .1f);

                if (botOrigin.DistanceTo2D(targetOrigin) < 100 || ground == botOrigin.Z) ground = targetOrigin.Z;

                float distance = botOrigin.DistanceTo(targetOrigin);
                int speed;

                switch (ai.GetField<string>("type"))
                {
                    case "tower":
                        speed = 170;
                        break;
                    default:
                        speed = 100;
                        break;
                }

                float groundDist = ground - botOrigin.Z;
                groundDist *= 8;//Overcompansate to move faster and track along ground in a better way
                if (ground == targetOrigin.Z) groundDist = 0;//Fix 'jumping bots'

                ai.MoveTo(new Vector3(targetOrigin.X, targetOrigin.Y, ground + groundDist), distance / speed);

                string state = ai.GetField<string>("state");
                if ((state == "post_hurt" || state == "idle") && state != "hurt" && state != "attacking")
                {
                    if (ai.GetField<string>("type") == "tower") playAnimOnBot(ai, botAnims["tower_run"]);
                    else playAnimOnBot(ai, botAnims["run"]);
                    ai.SetField("state", "moving");
                }
            }
            else//failsafe, just stand still if there is no other options
            {
                ai.MoveTo(new Vector3(botOrigin.X, botOrigin.Y, ground), 1);
                string state = ai.GetField<string>("state");
                if (state != "idle" && state != "hurt" && state != "attacking")
                {
                    if (ai.GetField<string>("type") == "tower") playAnimOnBot(ai, botAnims["tower_idle"]);
                    else playAnimOnBot(ai, botAnims["idle"]);
                    ai.SetField("state", "idle");
                }
            }
            #endregion

            return true;
        }
        private static bool botAI_targeting(Entity bot)
        {
            if (gameEnded) return false;
            if (bot.GetField<string>("state") == "dead") return false;
            if (bot.GetField<string>("currentGun") == "none" || bot.GetField<string>("currentGun").Contains("killstreak") || bot.GetField<string>("currentGun").Contains("airdrop")) return true;
            if (bot.GetField<Entity>("target") != bot) return true;

            foreach (Entity player in Players)
            {
                if (!player.IsAlive || player.SessionTeam != "allies" || player.Classname != "player") continue;
                if (player.GetField<bool>("isDown")) continue;
                if (player.HasField("isInHeliSniper")) continue;

                bool tracePass = SightTracePassed(bot.Origin + new Vector3(0, 0, 30), player.GetEye(), false, bot);
                if (!tracePass)
                {
                    bot.SetField("target", bot);
                    continue;
                }
                bot.SetField("target", player);
                break;
            }
            if (bot.GetField<Entity>("target") != bot && bot.GetField<string>("state") != "shooting")
            {
                float anglesY = VectorToAngles(bot.GetField<Entity>("target").Origin - bot.Origin).Y;
                bot.RotateTo(new Vector3(0, anglesY, 0), 0.4f);
                bot.SetField("state", "shooting");
                bot.Origin = bot.Origin;
                int waitForShot;
                switch (bot.GetField<string>("animType"))
                {
                    case "pistol":
                        waitForShot = 1000;//300
                        break;
                    case "smg":
                        waitForShot = 50;
                        break;
                    case "rifle":
                        waitForShot = 250;
                        break;
                    case "spread":
                        waitForShot = 1000;
                        break;
                    case "mg":
                        waitForShot = 100;
                        break;
                    case "sniper":
                        waitForShot = 1000;
                        break;
                    case "rocketlauncher":
                        waitForShot = 2000;
                        break;
                    default:
                        waitForShot = 100;
                        break;
                }
                //bot.SetField("shots", 0);
                OnInterval(waitForShot, () => botAI_fireBotWeapon(bot));
            }
            return true;
        }
        private static bool botAI_fireBotWeapon(Entity bot)
        {
            if (gameEnded) return false;
            if (bot.GetField<string>("state") == "dead") return false;
            bool trace = SightTracePassed(bot.GetTagOrigin("j_head"), bot.GetField<Entity>("target").GetTagOrigin("j_head"), false, bot, bot.GetField<Entity>("hitbox"), bot.GetField<Entity>("headHitbox"));
            if (!bot.GetField<Entity>("target").IsAlive || bot.GetField<Entity>("target").Classname != "player" || bot.GetField<Entity>("target") == bot || bot.GetField<Entity>("target").GetField<bool>("isDown") || !trace)
            {
                switch (bot.GetField<string>("animType"))
                {
                    case "pistol":
                        playAnimOnBot(bot, botAnims["idle_pistol"]);
                        break;
                    case "mg":
                        playAnimOnBot(bot, botAnims["idle_mg"]);
                        break;
                    case "rocketlauncher":
                        playAnimOnBot(bot, botAnims["idle_rpg"]);
                        break;
                    default:
                        playAnimOnBot(bot, botAnims["idle"]);
                        break;
                }
                bot.SetField("state", "idle");
                bot.SetField("target", bot);
                return false;
            }

            bot.RotateTo(new Vector3(0, VectorToYaw(bot.GetField<Entity>("target").Origin - bot.Origin), 0), 0.4f);

            switch (bot.GetField<string>("animType"))
            {
                case "pistol":
                    playAnimOnBot(bot, botAnims["shoot_pistol"]);
                    break;
                case "mg":
                    playAnimOnBot(bot, botAnims["shoot_mg"]);
                    break;
                case "rocketlauncher":
                    playAnimOnBot(bot, botAnims["shoot_rpg"]);
                    break;
                default:
                    playAnimOnBot(bot, botAnims["shoot"]);
                    break;
            }
            Entity botGunEnt = bot.GetField<Entity>("gun");
            Vector3 flashTag = botGunEnt.GetTagOrigin("tag_flash");
            string botGun = bot.GetField<string>("currentGun");
            Vector3 randomAim = new Vector3(RandomInt(25), RandomInt(25), RandomIntRange(20, 55));
            //Utilities.PrintToConsole("Shooting gun " + botGun);
            MagicBullet(botGun, flashTag, bot.GetField<Entity>("target").Origin + randomAim, bot);
            Vector3 forward = AnglesToForward(botGunEnt.GetTagAngles("tag_flash") + randomAim);
            Vector3 up = AnglesToUp(botGunEnt.GetTagAngles("tag_flash") + randomAim);
            PlayFX(fx_tracer_single, flashTag, forward, up);

            bot.SetField("shots", bot.GetField<int>("shots") + 1);
            int ammo = WeaponClipSize(botGun);
            if (bot.GetField<int>("shots") >= ammo)
            {
                /*
                Entity clip = Spawn("script_model", bot.Origin);
                clip.SetModel(getWeaponClipModel(botGun));
                clip.LinkTo(bot, "tag_weapon_right", Vector3.Zero, Vector3.Zero);
                */
                botGunEnt.HidePart("tag_clip");
                switch (bot.GetField<string>("animType"))
                {
                    case "pistol":
                        playAnimOnBot(bot, botAnims["reload_pistol"]);
                        bot.PlaySound("weap_usp45_reload_npc");
                        AfterDelay(1500, () => resetBotWeaponAnim(bot, botGun, botAnims["idle_pistol"]));
                        break;
                    case "mg":
                        playAnimOnBot(bot, botAnims["reload_mg"]);
                        bot.PlaySound("weap_m60_reload_npc");
                        AfterDelay(4000, () => resetBotWeaponAnim(bot, botGun, botAnims["idle_mg"]));
                        break;
                    case "rocketlauncher":
                        playAnimOnBot(bot, botAnims["reload_rpg"]);
                        bot.PlaySound("weap_rpg_reload_npc");
                        AfterDelay(2500, () => resetBotWeaponAnim(bot, botGun, botAnims["idle_rpg"]));
                        break;
                    default:
                        playAnimOnBot(bot, botAnims["reload"]);
                        bot.PlaySound("weap_ak47_reload_npc");
                        AfterDelay(2000, () => resetBotWeaponAnim(bot, botGun, botAnims["idle"]));
                        break;
                }
                return false;
            }
            return true;
        }
        public static bool botAI_noWeapon(Entity ai, Entity botHitbox)
        {
            if (gameEnded) return false;
            if (!ai.HasField("state")) return false;//Return if our bot isn't set up correctly

            if (ai.GetField<string>("state") == "dead" || ai.GetField<string>("state") == "shooting" || !botsInPlay.Contains(ai) || botHitbox.GetField<int>("currentHealth") <= botHitbox.GetField<int>("damageTaken")) return false;
            killBotIfUnderMap(ai);
            if (ai.GetField<string>("state") == "dead") return false;

            pathNode targetPathNode = null;
            Vector3 botOrigin = ai.Origin;
            Vector3 botHeadTag = ai.GetTagOrigin("j_head");

            #region pathing
            if (ai.GetField<Entity>("target") == ai)//Find a player
            {
                float tempDist = 999999999;
                foreach (Entity p in Players)//Find a player
                {
                    if (!p.HasField("isDown")) continue;//Skip this player if they're not initiated

                    if (p.SessionTeam != "allies" || !p.IsAlive || p.GetField<bool>("isDown")) continue;

                    Vector3 playerOrigin = p.Origin;
                    if (botOrigin.DistanceTo(playerOrigin) > 600) continue;

                    Vector3 playerHeadTag = p.GetTagOrigin("j_head");
                    bool trace = SightTracePassed(botHeadTag, playerHeadTag, false, botHitbox, ai.GetField<Entity>("head"), ai.GetField<Entity>("headHitbox"));
                    if (trace)
                    {
                        bool isCloser = playerOrigin.DistanceTo(botOrigin) < tempDist;
                        if (isCloser)
                        {
                            tempDist = playerOrigin.DistanceTo(botOrigin);
                            ai.SetField("target", p);
                            ai.ClearField("currentWaypoint");
                        }
                    }
                    //Attacking players
                    if (botHitbox.Origin.DistanceTo(playerOrigin) <= 50 && ai.GetField<string>("state") != "melee")
                        StartAsync(botAI_meleePlayer(ai, p));
                    //End attacking
                }
                if (ai.HasField("currentWaypoint")) targetPathNode = ai.GetField<pathNode>("currentWaypoint");
            }
            //find a pathNode closest to the nearest player and path towards them
            if (ai.GetField<Entity>("target") == ai && !ai.HasField("currentWaypoint"))
            {
                foreach (pathNode v in getAllNodes())
                {
                    bool waypointTrace = SightTracePassed(ai.GetTagOrigin("j_head"), v.location, false, botHitbox);//Check for waypoints
                    if (waypointTrace)
                    {
                        ai.SetField("currentWaypoint", new Parameter(v));//Set the first seen one as current
                        break;
                    }
                }
            }
            //Check whether we get to our current pathNode to recalc
            else if (ai.HasField("currentWaypoint"))
            {
                if (botOrigin.DistanceTo(ai.GetField<pathNode>("currentWaypoint").location) < 50)
                {
                    ai.ClearField("currentWaypoint");
                    return true;
                }
            }
            #endregion
            //Now we are done targeting, do the action

            #region motion
            if (ai.GetField<string>("state") == "melee") return true;

            float ground = GetGroundPosition(botOrigin, 12).Z;

            if (ai.HasField("target") && ai.GetField<Entity>("target") != ai)//Move to our target
            {
                Entity target = ai.GetField<Entity>("target");
                Vector3 targetOrigin = target.Origin;
                float angleY = VectorToAngles(targetOrigin - botOrigin).Y;
                ai.RotateTo(new Vector3(0, angleY, 0), .3f, .1f, .1f);

                if (botOrigin.DistanceTo2D(targetOrigin) < 100 || ground == botOrigin.Z) ground = targetOrigin.Z;

                float distance = botOrigin.DistanceTo(targetOrigin);
                int speed;
                switch (ai.GetField<string>("type"))
                {
                    case "tower":
                        speed = 200;
                        break;
                    default:
                        speed = 100;
                        break;
                }

                float groundDist = ground - botOrigin.Z;
                groundDist *= 8;//Overcompansate to move faster and track along ground in a better way
                if (ground == targetOrigin.Z) groundDist = 0;//Fix 'jumping bots'

                ai.MoveTo(new Vector3(targetOrigin.X, targetOrigin.Y, ground + groundDist), distance / speed);

                string state = ai.GetField<string>("state");
                if ((state == "post_hurt" || state == "idle") && state != "hurt" && state != "attacking")
                {
                    if (ai.GetField<string>("type") == "tower") playAnimOnBot(ai, botAnims["tower_run"]);
                    else playAnimOnBot(ai, botAnims["run"]);
                    ai.SetField("state", "moving");
                }

                //Clear the target for next calculation
                ai.SetField("target", ai);
            }
            else if (targetPathNode != null)//Move to our targetPathNode
            {
                Vector3 targetOrigin = targetPathNode.location;
                float angleY = VectorToAngles(targetOrigin - botOrigin).Y;
                ai.RotateTo(new Vector3(0, angleY, 0), .3f, .1f, .1f);

                if (botOrigin.DistanceTo2D(targetOrigin) < 100 || ground == botOrigin.Z) ground = targetOrigin.Z;

                float distance = botOrigin.DistanceTo(targetOrigin);
                int speed;
                switch (ai.GetField<string>("type"))
                {
                    case "tower":
                        speed = 200;
                        break;
                    default:
                        speed = 100;
                        break;
                }

                float groundDist = ground - botOrigin.Z;
                groundDist *= 8;//Overcompansate to move faster and track along ground in a better way
                if (ground == targetOrigin.Z) groundDist = 0;//Fix 'jumping bots'

                ai.MoveTo(new Vector3(targetOrigin.X, targetOrigin.Y, ground + groundDist), distance / speed);

                string state = ai.GetField<string>("state");
                if ((state == "post_hurt" || state == "idle") && state != "hurt" && state != "attacking")
                {
                    if (ai.GetField<string>("type") == "tower") playAnimOnBot(ai, botAnims["tower_run"]);
                    else playAnimOnBot(ai, botAnims["run"]);
                    ai.SetField("state", "moving");
                }
            }
            else//failsafe, just stand still if there is no other options
            {
                ai.MoveTo(new Vector3(botOrigin.X, botOrigin.Y, ground), 1);
                string state = ai.GetField<string>("state");
                if (state != "idle" && state != "hurt" && state != "attacking")
                {
                    if (ai.GetField<string>("type") == "tower") playAnimOnBot(ai, botAnims["tower_run"]);
                    else playAnimOnBot(ai, botAnims["run"]);
                    ai.SetField("state", "idle");
                }
            }
            #endregion
            return true;
        }
        private static IEnumerator botAI_meleePlayer(Entity ai, Entity target)
        {
            if (target.GetField<bool>("isDown")) yield break;

            ai.SetField("state", "melee");

            if (ai.GetField<string>("type") == "tower") playAnimOnBot(ai, botAnims["tower_melee"]);
            else playAnimOnBot(ai, botAnims["melee"]);

            yield return Wait(.1f);
            PlayFX(fx_blood, target.Origin + new Vector3(0, 0, 30));

            Vector3 dir = VectorToAngles(ai.Origin - target.Origin);
            dir.Normalize();
            float hitDir = dir.Y - target.GetPlayerAngles().Y;

            int dmg = ai.GetField<string>("type") == "tower" || ai.GetField<string>("type") == "striker" ? 100 : 250;
            target.PlaySound("melee_punch_other");
            target.FinishPlayerDamage(null, null, dmg, 0, "MOD_FALLING", "none", target.Origin, dir, "none", 0);
            if (target.IsAlive)
            {
                int time = GetTime();
                target.SetField("lastDamageTime", time);

                //Utilities.PrintToConsole(player.HasField("specialty_regenspeed").ToString());
                int regenTime = target.HasField("specialty_regenspeed") ? 5000 : 7000;

                AfterDelay(regenTime, () =>
                {
                    //Utilities.PrintToConsole("Healing player" + player.Name);
                    if (target.GetField<int>("lastDamageTime") == time && target.SessionState == "playing")
                    {
                        target.Health = target.MaxHealth;
                        //Utilities.PrintToConsole("Healed to " + player.MaxHealth);
                    }
                });
            }

            yield return Wait(.6f);

            if (ai.GetField<string>("state") != "dead")
            {
                if (ai.GetField<string>("type") == "tower") playAnimOnBot(ai, botAnims["tower_idle"]);
                else playAnimOnBot(ai, botAnims["idle"]);
                ai.SetField("state", "idle");
            }
        }

        public static void onBotDamage(Entity hitbox, Parameter damage, Parameter attacker, Parameter direction_vec, Parameter point, Parameter meansOfDeath, Parameter modelName, Parameter partName, Parameter tagName, Parameter iDFlags, Parameter weapon)
        {
            //if (attacker.As<Entity>().Classname != "player") return;

            Entity currentBot = hitbox.GetField<Entity>("parent");
            if (!botsInPlay.Contains(currentBot)) return;
            Entity player = (Entity)attacker;

            if ((string)weapon == "remote_uav_weapon_mp" && attacker.As<Entity>().HasField("owner"))//UAV tweaks
            {
                player = attacker.As<Entity>().GetField<Entity>("owner");
                meansOfDeath = "MOD_PASSTHRU";
                damage = 20;
            }
            else if ((string)weapon == "sentry_minigun_mp")//Sentry tweaks
            {
                player = attacker.As<Entity>().GetField<Entity>("owner");
                meansOfDeath = "MOD_PASSTHRU";
                damage = 10;
            }
            else if ((string)weapon == "manned_gl_turret_mp")//Sentry tweaks
            {
                player = attacker.As<Entity>().GetField<Entity>("owner");
                meansOfDeath = "MOD_PASSTHRU";
                damage = 300;
            }

            //Utilities.PrintToConsole((string)meansOfDeath);

            if ((string)weapon != "sentry_minigun_mp" && (string)weapon != "remote_uav_weapon_mp" && (string)meansOfDeath != "MOD_EXPLOSIVE_BULLET") PlayFX(fx_blood, point.As<Vector3>());//Only play FX if the weapon isn't a script weapon
            doBotDamage((int)damage, player, (string)weapon, hitbox, (string)meansOfDeath, point.As<Vector3>());

            string botState = currentBot.GetField<string>("state");
            if (botState != "hurt" && botState != "attacking" && (string)meansOfDeath != "MOD_BLEEDOUT")
            {
                if (currentBot.GetField<string>("type") == "tower") playAnimOnBot(currentBot, botAnims["tower_run_hurt"]);
                else playAnimOnBot(currentBot, botAnims["run_hurt"]);
                currentBot.SetField("state", "hurt");
                AfterDelay(500, () =>
                {
                    if (currentBot.GetField<string>("state") != "dead")
                        currentBot.SetField("state", "post_hurt");
                });
            }

            if (hitbox.GetField<int>("damageTaken") >= hitbox.GetField<int>("currentHealth"))
            {
                currentBot.SetField("isAlive", false);
                currentBot.SetField("state", "dead");
                hitbox.SetCanDamage(false);
                hitbox.SetCanRadiusDamage(false);
                hitbox.SetModel("tag_origin");//Change model to avoid the dead bot's hitbox blocking shots
                if (player.Classname == "player")
                {
                    int pointGain = 20;
                    if ((string)meansOfDeath == "MOD_HEADSHOT")
                        pointGain = 50;

                    scorePopup(player, pointGain);
                    addRank(player, pointGain);

                    player.Kills++;

                    if (player.HasField("specialty_triggerhappy") && (string)weapon == player.CurrentWeapon)
                    {
                        int ammoInClip = player.GetWeaponAmmoClip(player.CurrentWeapon);
                        int clipSize = WeaponClipSize(player.CurrentWeapon);
                        int difference = clipSize - ammoInClip;
                        int ammoReserves = player.GetWeaponAmmoStock(player.CurrentWeapon);

                        if (ammoInClip != clipSize && ammoReserves > 0)
                        {
                            if (ammoInClip + ammoReserves >= clipSize)
                            {
                                player.SetWeaponAmmoClip(player.CurrentWeapon, clipSize);
                                player.SetWeaponAmmoStock(player.CurrentWeapon, (ammoReserves - difference));
                            }
                            else
                            {
                                player.SetWeaponAmmoClip(player.CurrentWeapon, ammoInClip + ammoReserves);

                                if (ammoReserves - difference > 0)
                                    player.SetWeaponAmmoStock(player.CurrentWeapon, (ammoReserves - difference));
                                else
                                    player.SetWeaponAmmoStock(player.CurrentWeapon, 0);
                            }
                        }
                    }
                }
                currentBot.MoveTo(currentBot.Origin, 0.05f);

                spawnBotRagdoll(currentBot, (string)meansOfDeath, point.As<Vector3>());
                despawnBot(currentBot);
                botsInPlay.Remove(currentBot);
                roundUtil.checkForEndRound();
            }
        }

        private static void doBotDamage(int damage, Entity player, string weapon, Entity botHitbox, string MOD, Vector3 point, bool skipFeedback = false)
        {
            float hitDamage = damage;

            if (weaponNames.Keys.Contains(getBaseWeaponName(weapon)))
                hitDamage += weaponLevels[player.EntRef][getBaseWeaponName(weapon)] * 2;

            if (MOD != "MOD_MELEE")
            {
                //Weapon tweaks
                if (weapon == "iw5_as50_mp_as50scope") hitDamage = 10000;//Heli Sniper damage
                else if (weapon == "iw5_44magnum_mp") hitDamage = 400;
            }

            if (MOD == "MOD_HEADSHOT") hitDamage *= 1.5f;

            else if (MOD == "MOD_PASSTHRU") hitDamage = damage;//Script usage

            botHitbox.SetField("damageTaken", botHitbox.GetField<int>("damageTaken") + hitDamage);

            if (player.Classname != "player" || !player.HasField("isDown")) return;

            int pointGain = 10;
            if (MOD == "MOD_HEADSHOT")
                pointGain = 30;

            if (MOD != "MOD_PASSTHRU")
                scorePopup(player, pointGain);
            addRank(player, pointGain);

            if (skipFeedback || player.Classname != "player" || !player.HasField("hud_damageFeedback")) return;

            HudElem combatHighFeedback = player.GetField<HudElem>("hud_damageFeedback");
            combatHighFeedback.Alpha = 1;
            player.PlayLocalSound("MP_hit_alert");
            combatHighFeedback.FadeOverTime(1);
            combatHighFeedback.Alpha = 0;
        }

        private static void resetBotWeaponAnim(Entity bot, string oldGun, string anim)
        {
            if (bot.GetField<string>("state") == "dead") return;
            if (!bot.HasField("gun")) return;

            Entity botGunEnt = bot.GetField<Entity>("gun");
            playAnimOnBot(bot, anim);
            if (oldGun == bot.GetField<string>("currentGun")) botGunEnt.ShowPart("tag_clip");//Avoid trying to show the tag if we switched guns, causes a crash
            bot.SetField("shots", 0);
            bot.SetField("state", "idle");
            bot.SetField("target", bot);
        }
        public static void playAnimOnBot(Entity bot, string anim)
        {
            bot.ScriptModelPlayAnim(anim);
            if (bot.HasField("head"))
            {
                bot.GetField<Entity>("head").ScriptModelPlayAnim(anim);
            }
        }
        private static void despawnBot(Entity bot)
        {
            bot.Hide();
            if (bot.HasField("head"))
            {
                Entity botHead = bot.GetField<Entity>("head");
                botHead.Hide();
            }
            if (bot.HasField("gun"))
            {
                Entity botGun = bot.GetField<Entity>("gun");
                botGun.Hide();
            }
            bot.Origin = Vector3.Zero;
            bot.SetField("isAlive", false);
            bot.SetField("state", "dead");
            botPool.Add(bot);
        }
        private static void spawnBotRagdoll(Entity bot, string meansOfDeath, Vector3 point)
        {
            string bodyModel = bot.Model;
            string headModel = bot.GetField<Entity>("head").Model;

            Entity body = Spawn("script_model", bot.Origin);
            body.Angles = bot.Angles;
            body.SetModel(bodyModel);
            Entity head = Spawn("script_model", bot.GetField<Entity>("head").Origin);
            head.Angles = bot.GetField<Entity>("head").Angles;
            head.SetModel(headModel);
            head.LinkTo(body, "j_spine4", Vector3.Zero, Vector3.Zero);
            body.SetField("head", head);

            if (bot.GetField<string>("type") == "tower")
            {
                playAnimOnBot(body, botAnims["tower_death"]);
            }
            else
            {
                if (meansOfDeath == "MOD_EXPLOSIVE" || meansOfDeath == "MOD_GRENADE_SPLASH")
                {
                    body.Angles = VectorToAngles(point - body.Origin);
                    int randomAnim = RandomInt(botAnims_death_explode.Length);
                    playAnimOnBot(body, botAnims_death_explode[randomAnim]);
                }
                else
                {
                    int randomAnim = RandomInt(botAnims_deaths.Length);
                    playAnimOnBot(body, botAnims_deaths[randomAnim]);
                }
            }
            //AfterDelay(1000, () => body.StartRagdoll());

            ragdolls.Add(body);
        }
        private static void deleteRagdoll(Entity ragdoll)
        {
            if (ragdoll.HasField("head"))
            {
                ragdoll.GetField<Entity>("head").Delete();
                ragdoll.ClearField("head");
            }

            ragdolls.Remove(ragdoll);
            ragdoll.Delete();
        }
        public static void clearAllRagdolls()
        {
            Entity[] currentRagdolls = ragdolls.ToArray();
            int ragdollCount = ragdolls.Count;
            for (int i = 0; i < ragdollCount; i++)
                deleteRagdoll(currentRagdolls[i]);

            ragdolls.Clear();
        }

        public static string getRandomBotType()
        {
            string type;
            List<string> outcomes = botHealth.Keys.ToList();

            if (roundUtil.round > 23)
                type = outcomes[RandomInt(outcomes.Count)];
            else if (roundUtil.round > 12)
            {
                outcomes.Remove("hammer");
                type = outcomes[RandomInt(outcomes.Count)];
            }
            else if (roundUtil.round > 8)
            {
                outcomes.Remove("hammer");
                outcomes.Remove("destructor");
                type = outcomes[RandomInt(outcomes.Count)];
            }
            else if (roundUtil.round > 4)
            {
                outcomes.Remove("hammer");
                outcomes.Remove("destructor");
                outcomes.Remove("striker");
                type = outcomes[RandomInt(outcomes.Count)];
            }
            else if (roundUtil.round > 3)
            {
                outcomes.Remove("hammer");
                outcomes.Remove("destructor");
                outcomes.Remove("striker");
                outcomes.Remove("enforcer");
                type = outcomes[RandomInt(outcomes.Count)];
            }
            else
                type = "ravager";//ravager

            return type;
        }
    }
}
