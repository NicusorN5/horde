using System.Collections.Generic;
using InfinityScript;
using static AIZombiesSupreme.f_botUtil;

namespace AIZombiesSupreme
{
    public class a_bots : BaseScript
    {

        public static bool spawnBot(int spawnLoc, bool isCrawler)
        {
            if ((!isCrawler && botPool.Count == 0) || (isCrawler && crawlerPool.Count == 0)) return true;//True so in case all 30 have spawned, don't error out
            Entity bot;
            if (isCrawler) bot = crawlerPool[0];
            else bot = botPool[0];

            if (botSpawns.Count == 0)
            {
                Utilities.PrintToConsole("No bot spawns available! Please have at least one \"zombiespawn\" in your map file.");
                GSCFunctions.Announcement("^1No bot spawns available! Check console for details");
                return false;
            }

            int randomInt = g_AIZ.rng.Next(20);
            bot.Origin = botSpawns[spawnLoc] + new Vector3(randomInt, randomInt, 0);
            bot.Angles = spawnAngles[spawnLoc];
            bot.Show();
            bot.ShowAllParts();

            if (isCrawler) playAnimOnBot(bot, crawlerAnim_idle);
            else playAnimOnBot(bot, anim_idle);

            bot.SetField("state", "idle");
            if (!isCrawler && bot.HasField("head"))
            {
                Entity botHead = bot.GetField<Entity>("head");
                botHead.Show();
                //Remove helmet
                botHead.HidePart("j_head_end");
                botHead.HidePart("j_helmet");
                botHead.HidePart("j_collar_rear");
                Entity headHitbox = bot.GetField<Entity>("headHitbox");
                headHitbox.SetCanDamage(true);
            }
            bot.SetField("isAlive", true);
            bot.SetField("isAttacking", false);
            int time = GSCFunctions.GetTime();
            bot.SetField("lastActiveTime", time);
            spawnedBots++;
            Entity botHitbox = bot.GetField<Entity>("hitbox");
            if (isCrawler) botHitbox.SetField("currentHealth", crawlerHealth);
            else botHitbox.SetField("currentHealth", health);
            botHitbox.SetField("damageTaken", 0);
            botHitbox.SetCanDamage(true);
            botHitbox.SetCanRadiusDamage(true);
            //botHitbox.Show();
            botHitbox.SetModel("com_plasticcase_dummy");

            botsInPlay.Add(bot);
            if (isCrawler) crawlerPool.Remove(bot);
            else botPool.Remove(bot);

            onBotUpdate();

            OnInterval(100, () => botAI(bot, botHitbox, isCrawler, false));

            //Check for waypoints on spawn once
            foreach (Entity v in h_mapEdit.waypoints)
            {
                Vector3 botHeadTag = bot.GetTagOrigin("j_head");
                bool waypointTrace = GSCFunctions.SightTracePassed(botHeadTag, v.Origin, false, botHitbox);//Check for waypoints
                if (waypointTrace)
                {
                    bot.SetField("currentWaypoint", v);//Set the first seen one as current
                    bot.SetField("visibleWaypoints", new Parameter(v.GetField<List<Entity>>("visiblePoints")));
                    break;
                }
            }

            return true;
        }

        public static bool spawnBossBot(int SpawnLoc)
        {
            if (botSpawns.Count == 0)
            {
                Utilities.PrintToConsole("No bot spawns available! Please have at least one \"zombiespawn\" in your map file.");
                GSCFunctions.Announcement("^1No bot spawns available! Check console for details");
                return false;
            }

            Entity bot = GSCFunctions.Spawn("script_model", botSpawns[SpawnLoc]);
            spawnedBots++;
            bot.Angles = spawnAngles[SpawnLoc];
            bot.SetModel("mp_fullbody_opforce_juggernaut");
            playAnimOnBot(bot, anim_idle);
            bot.SetField("isAlive", true);
            botsInPlay.Add(bot);
            Entity botHitbox = GSCFunctions.Spawn("script_model", bot.Origin + new Vector3(0, 0, 30));
            botHitbox.SetModel("com_plasticcase_trap_friendly");
            //botHitbox.CloneBrushModelToScriptModel(_airdropCollision);
            botHitbox.Angles = new Vector3(90, bot.Angles.Y, 0);
            //botHitbox.Solid();
            botHitbox.SetCanDamage(true);
            botHitbox.SetCanRadiusDamage(true);
            botHitbox.LinkTo(bot, "j_mainroot");
            botHitbox.Hide();
            botHitbox.SetField("currentHealth", bossHealth);
            botHitbox.SetField("damageTaken", 0);
            botHitbox.SetField("parent", bot);
            botHitbox.SetField("isBoss", true);
            bot.SetField("hitbox", botHitbox);
            bot.SetField("state", "idle");
            bot.SetField("isAttacking", false);
            bot.SetField("currentWaypoint", bot);
            bot.SetField("isOnCompass", false);
            bot.SetField("primedForNuke", false);
            bot.SetField("lastActiveTime", 0);//Bosses won't die from time
            b_roundSystem.checkForCompass();//Check every spawn so when the last bot spawns, he activates the compass for all

            onBotUpdate();

            c_bonusDrops.onNuke += () => killBotOnNuke(bot, false, true);

            AfterDelay(50, () => OnInterval(100, () => botAI(bot, botHitbox, false, true)));//wait a frame to let the bot spawn

            //Delay to allow AI to intitalize
            AfterDelay(100, () => botHitbox.OnNotify("damage", (entity, damage, attacker, direction_vec, point, meansOfDeath, modelName, partName, tagName, iDFlags, weapon) => onBotDamage(entity, damage, attacker, direction_vec, point, meansOfDeath, modelName, partName, tagName, iDFlags, weapon, false, true)));

            //Check for waypoints on spawn once
            foreach (Entity v in h_mapEdit.waypoints)
            {
                Vector3 botHeadTag = bot.GetTagOrigin("j_head");
                bool waypointTrace = GSCFunctions.SightTracePassed(botHeadTag, v.Origin, false, botHitbox);//Check for waypoints
                if (waypointTrace)
                {
                    bot.SetField("currentWaypoint", v);//Set the first seen one as current
                    bot.SetField("visibleWaypoints", new Parameter(v.GetField<List<Entity>>("visiblePoints")));
                    break;
                }
            }

            return true;
        }

        public static bool botAI(Entity ai, Entity botHitbox, bool isCrawler, bool isBoss)
        {
            if (!ai.GetField<bool>("isAlive") || !botsInPlay.Contains(ai) || botHitbox.GetField<int>("currentHealth") <= botHitbox.GetField<int>("damageTaken")) return false;
            killBotIfUnderMap(ai);
            if (!ai.GetField<bool>("isAlive")) return false;//Do another check after height check

            //check time inactivity
            int currentTime = GSCFunctions.GetTime();
            int lastTime = ai.GetField<int>("lastActiveTime");
            if (currentTime > lastTime + 120000 && !isBoss && !freezerActivated)
            {
                killBotAndRespawn(ai);
                return false;
            }

            Entity target = null;
            Vector3 botOrigin = ai.Origin;
            Vector3 botHeadTag = ai.GetTagOrigin("j_head");// + new Vector3 (0, 0, 5);

            float Ground = GSCFunctions.GetGroundPosition(botOrigin).Z;

            #region targeting
            if (glowsticks.Count != 0)//Find a glowstick first
            {
                foreach (Entity g in glowsticks)
                {
                    if (freezerActivated) break;
                    if (g_AIZ.isGlowstick(ai.GetField<Entity>("currentWaypoint"))) { target = ai.GetField<Entity>("currentWaypoint"); continue; }
                    if (botOrigin.DistanceTo(g.Origin) > 500) continue;
                    bool tracePass = GSCFunctions.SightTracePassed(botHeadTag, g.Origin, false, botHitbox);
                    if (tracePass)
                    {
                        target = g;
                        //ai.ClearField("currentWaypoint");
                        ai.SetField("currentWaypoint", g);
                        ai.ClearField("visibleWaypoints");
                        break;
                    }
                    //else
                    //{
                        //Log.Write(LogLevel.All, "No trace available");
                    //}
                }
            }
            if (target == null && !freezerActivated)//If we haven't found a glowstick, find a real target
            {
                float dist;
                float tempDist = 999999999;
                foreach (Entity p in Players)//Find a player
                {
                    Vector3 playerOrigin = p.Origin;
                    dist = botOrigin.DistanceTo(playerOrigin);
                    if (p.SessionTeam != "allies" || !p.IsAlive || p.GetField<bool>("isDown") || p.GetField<bool>("isInHeliSniper") || dist > 600) continue;
                    Vector3 playerHeadTag = p.GetTagOrigin("j_head");
                    bool tracePass = GSCFunctions.SightTracePassed(botHeadTag, playerHeadTag, false, botHitbox);
                    if (tracePass)
                    {
                        //Log.Write(LogLevel.All, "Traced {0}", p.Name);
                        //if (target != null)
                        {
                            bool isCloser = playerOrigin.DistanceTo(botOrigin) < tempDist;//GSCFunctions.Closer(botHeadTag, playerHeadTag, targetHeadTag);
                            if (isCloser)
                            {
                                tempDist = playerOrigin.DistanceTo(botOrigin);
                                target = p;
                                //ai.ClearField("currentWaypoint");
                                ai.SetField("currentWaypoint", ai);
                                ai.ClearField("visibleWaypoints");
                                //Log.Write(LogLevel.All, "{0} is closer", target.Name);
                            }
                        }
                        /*
                        else
                        {
                            target = p;
                            //ai.ClearField("currentWaypoint");
                            ai.SetField("currentWaypoint", ai);
                            ai.ClearField("visibleWaypoints");
                            //Log.Write(LogLevel.All, "Target is null");
                        }
                        */
                    }
                    //Attacking players
                    float attackDist = botHitbox.Origin.DistanceTo(playerOrigin);

                    if (attackDist <= 50 && !ai.GetField<bool>("isAttacking"))
                    {
                        ai.SetField("isAttacking", true);

                        updateBotLastActiveTime(ai);

                        if (ai.GetField<bool>("primedForNuke")) playAnimOnBot(ai, anim_lose);
                        else if (isCrawler || ai.HasField("hasBeenCrippled")) playAnimOnBot(ai, crawlerAnim_attack);
                        else playAnimOnBot(ai, anim_attack);
                        AfterDelay(700, () =>
                        {
                            if ((isCrawler || ai.HasField("hasBeenCrippled")) && ai.GetField<bool>("isAlive"))
                                playAnimOnBot(ai, crawlerAnim_walk);
                            else if (isBoss && ai.GetField<bool>("isAlive"))
                                playAnimOnBot(ai, anim_run);
                            else
                            {
                                if (ai.GetField<bool>("isAlive"))
                                {
                                    if (isInPeril(botHitbox)) playAnimOnBot(ai, anim_run);
                                    else playAnimOnBot(ai, anim_walk);
                                }
                            }
                            if (ai.GetField<bool>("isAlive")) ai.SetField("isAttacking", false);
                        });

                        if (ai.GetField<bool>("primedForNuke")) continue;

                        Vector3 dir = GSCFunctions.VectorToAngles(ai.Origin - p.Origin);
                        dir.Normalize();
                        float hitDir =  dir.Y - p.GetPlayerAngles().Y;
                        //Log.Write(LogLevel.Debug, "Dir = {0}; Angle = {1}", dir.ToString(), angle);
                        AfterDelay(100, () =>
                        {
                            GSCFunctions.PlayFX(fx_blood, p.Origin + new Vector3(0, 0, 30));
                            if ((p.HasWeapon("riotshield_mp") || p.HasWeapon("iw5_riotshield_mp")) && ((p.CurrentWeapon != "riotshield_mp" && p.CurrentWeapon  != "iw5_riotshield_mp" && hitDir > -80 && hitDir < 80) || (p.CurrentWeapon == "riotshield_mp" || p.CurrentWeapon == "iw5_riotshield_mp")))
                            {
                                p.PlaySound("melee_hit");
                                p.FinishPlayerDamage(null, null, dmg/2, 0, "MOD_FALLING", "none", p.Origin, dir, "none", 0);
                            }
                            else
                            {
                                p.PlaySound("melee_punch_other");
                                p.FinishPlayerDamage(null, null, dmg, 0, "MOD_FALLING", "none", p.Origin, dir, "none", 0);
                            }
                            AfterDelay(8000, () => p.Health = p.MaxHealth);
                        });
                    }
                    //End attacking
                }
                if (target == null)//No players, find a waypoint
                {
                    if (ai.HasField("currentWaypoint") && ai.HasField("visibleWaypoints"))
                    {
                        //Entity currentWaypoint = ai.GetField<Entity>("currentWaypoint");
                        //if (currentWaypoint.HasField("visiblePoints") && !ai.HasField("visibleWaypoints")) ai.SetField("visibleWaypoints", new Parameter(currentWaypoint.GetField<List<Entity>>("visiblePoints")));
                        float waypointDist = botOrigin.DistanceTo(ai.GetField<Entity>("currentWaypoint").Origin);
                        if (ai.GetField<Entity>("currentWaypoint") == ai && ai.HasField("visibleWaypoints"))
                        {
                            List<Entity> visibleWaypoints = ai.GetField<List<Entity>>("visibleWaypoints");
                            int randomWaypoint = g_AIZ.rng.Next(visibleWaypoints.Count);
                            ai.SetField("currentWaypoint", visibleWaypoints[randomWaypoint]);
                        }
                        else if (waypointDist < 50)
                        {
                            ai.SetField("visibleWaypoints", new Parameter(ai.GetField<Entity>("currentWaypoint").GetField<List<Entity>>("visiblePoints")));
                            ai.SetField("currentWaypoint", ai);
                            //visibleWaypoints.Clear();
                            return true;
                        }
                    }
                    else if (!ai.HasField("currentWaypoint") || !ai.HasField("visibleWaypoints"))//Recalculate point
                    {
                        foreach (Entity v in h_mapEdit.waypoints)
                        {
                            bool waypointTrace = GSCFunctions.SightTracePassed(botHeadTag, v.Origin, false, botHitbox);//Check for waypoints
                            if (waypointTrace)
                            {
                                ai.SetField("currentWaypoint", v);//Set the first seen one as current
                                ai.SetField("visibleWaypoints", new Parameter(v.GetField<List<Entity>>("visiblePoints")));
                                break;
                            }
                        }
                    }
                    if (ai.HasField("currentWaypoint") && ai.GetField<Entity>("currentWaypoint") != ai) target = ai.GetField<Entity>("currentWaypoint");
                }
            }
            #endregion
            //Now we are done targeting, do the action for the target

            #region motion
            if (ai.GetField<bool>("isAttacking")) return true;//Stop moving to attack. Prevent bots getting stuck into players
            /*
            foreach (Entity bot in botsInPlay)//Prevent bots from combining into each other
            {
                if (ai == bot) continue;
                Vector3 closeOrigin = bot.Origin;
                if (botOrigin.DistanceTo(closeOrigin) < 10)//Move away from the bot and recalc
                {
                    Vector3 dir = GSCFunctions.VectorToAngles(closeOrigin - botOrigin);
                    Vector3 awayPos = botOrigin - dir * 100;
                    ai.MoveTo(awayPos, botOrigin.DistanceTo(awayPos) / 120);
                    ai.RotateTo(new Vector3(0, -dir.Y, 0), .3f, .1f, .1f);
                    return true;
                }
            }
            */

            if (target != null && glowsticks.Count == 0)//Move to our target if there are no glowsticks
            {
                Vector3 targetOrigin = target.Origin;
                //if (target.IsPlayer) targetHeadTag = target.GetTagOrigin("j_head");
                //else targetHeadTag = target.Origin;
                float angleY = GSCFunctions.VectorToAngles(targetOrigin - botOrigin).Y;
                ai.RotateTo(new Vector3(0, angleY, 0), .3f, .1f, .1f);

                if (botOrigin.DistanceTo2D(targetOrigin) < 100 || Ground == botOrigin.Z) Ground = targetOrigin.Z;

                int speed = 100;
                float distance = botOrigin.DistanceTo(targetOrigin);

                if (((isInPeril(botHitbox) && !ai.HasField("hasBeenCrippled")) && !ai.HasField("hasBeenCrippled")) || isBoss)
                    speed = 170;
                else if (ai.HasField("hasBeenCrippled"))
                    speed = 30;
                float groundDist = Ground - botOrigin.Z;
                groundDist *= 8;//Overcompansate to move faster and track along ground in a better way
                if (Ground == targetOrigin.Z) groundDist = 0;//Fix 'jumping bots'

                ai.MoveTo(new Vector3(targetOrigin.X, targetOrigin.Y, Ground + groundDist), distance / speed);

                string state = ai.GetField<string>("state");
                if ((state == "post_hurt" || state == "idle" || state == "dancing") && state != "hurt" && state != "attacking")
                {
                    if (isCrawler || ai.HasField("hasBeenCrippled")) playAnimOnBot(ai, crawlerAnim_walk);
                    else if (isBoss) playAnimOnBot(ai, anim_run);
                    else
                    {
                        if (isInPeril(botHitbox)) playAnimOnBot(ai, anim_run);
                        else playAnimOnBot(ai, anim_walk);
                    }
                    ai.SetField("state", "moving");
                }
            }
            else if (target != null && (glowsticks.Count > 0 && g_AIZ.isGlowstick(target)))//Move towards a glowstick and dance
            {
                Vector3 targetOrigin = target.Origin;
                if (Ground == botOrigin.Z) Ground = targetOrigin.Z;
                float angleY = GSCFunctions.VectorToAngles(targetOrigin - botOrigin).Y;
                ai.RotateTo(new Vector3(0, angleY, 0), .3f, .1f, .1f);
                float randomX = g_AIZ.rng.Next(-100, 100);
                float randomY = g_AIZ.rng.Next(-100, 100);
                string state = ai.GetField<string>("state");

                if (botOrigin.DistanceTo(targetOrigin) > 50)
                {
                    int speed = 100;
                    float distance = botOrigin.DistanceTo(targetOrigin);

                    if (((isInPeril(botHitbox) && !ai.HasField("hasBeenCrippled")) && !ai.HasField("hasBeenCrippled")) || isBoss)
                        speed = 170;
                    else if (ai.HasField("hasBeenCrippled"))
                        speed = 30;
                    float groundDist = Ground - botOrigin.Z;
                    groundDist *= 8;//Overcompansate to move faster and track along ground in a better way
                    if (Ground == targetOrigin.Z) groundDist = 0;//Fix 'jumping bots'

                    ai.MoveTo(new Vector3(targetOrigin.X + randomX, targetOrigin.Y + randomY, Ground + groundDist), distance / speed);
                }
                else if (state != "dancing")
                {
                    ai.Origin = botOrigin;
                    playAnimOnBot(ai, anim_lose);
                    ai.SetField("state", "dancing");
                    return true;
                }
                if ((state == "post_hurt" || state == "idle") && state != "hurt" && state != "attacking")
                {
                    if (isCrawler) playAnimOnBot(ai, crawlerAnim_walk);
                    else if (isBoss) playAnimOnBot(ai, anim_run);
                    else playAnimOnBot(ai, anim_walk);
                    ai.SetField("state", "moving");
                }
            }
            else if (target != null && (glowsticks.Count > 0 && !g_AIZ.isGlowstick(target)))//Move towards a player while a glowstick is out but not in sight
            {
                Vector3 targetOrigin = target.Origin;
                if (Ground == botOrigin.Z) Ground = targetOrigin.Z;
                float angleY = GSCFunctions.VectorToAngles(targetOrigin - botOrigin).Y;
                ai.RotateTo(new Vector3(0, angleY, 0), .3f, .1f, .1f);

                int speed = 100;
                float distance = botOrigin.DistanceTo(targetOrigin);

                if (((isInPeril(botHitbox) && !ai.HasField("hasBeenCrippled")) && !ai.HasField("hasBeenCrippled")) || isBoss)
                    speed = 170;
                else if (ai.HasField("hasBeenCrippled"))
                    speed = 30;
                float groundDist = Ground - botOrigin.Z;
                groundDist *= 8;//Overcompansate to move faster and track along ground in a better way
                if (Ground == targetOrigin.Z) groundDist = 0;//Fix 'jumping bots'

                ai.MoveTo(new Vector3(targetOrigin.X, targetOrigin.Y, Ground + groundDist), distance / speed);

                string state = ai.GetField<string>("state");
                if ((state == "post_hurt" || state == "idle" || state == "dancing") && state != "hurt" && state != "attacking")
                {
                    if (isCrawler || ai.HasField("hasBeenCrippled")) playAnimOnBot(ai, crawlerAnim_walk);
                    else if (isBoss) playAnimOnBot(ai, anim_run);
                    else
                    {
                        if (isInPeril(botHitbox)) playAnimOnBot(ai, anim_run);
                        else playAnimOnBot(ai, anim_walk);
                    }
                    ai.SetField("state", "moving");
                }
            }
            else//failsafe, just stand still if there is no other options
            {
                ai.MoveTo(new Vector3(botOrigin.X, botOrigin.Y, Ground), 1);
                string state = ai.GetField<string>("state");
                if (state != "idle" && state != "hurt" && state != "attacking")
                {
                    if (isCrawler || ai.HasField("hasBeenCrippled")) playAnimOnBot(ai, crawlerAnim_idle);
                    else playAnimOnBot(ai, anim_idle);
                    ai.SetField("state", "idle");
                }
            }
            #endregion

            GSCFunctions.ResetTimeout();
            return true;
        }
    }
}
