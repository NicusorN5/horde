using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;
using InfinityScript;
using static InfinityScript.GSCFunctions;
using static horde.hordeUtils;

//Fix revive pistol code (error on weapon pickup with p99), fix model for special cases, fix GLs instakilling

namespace horde
{
    public class horde : BaseScript
    {
        public static string _mapname;
        public static bool gameStarted = false;
        public static bool gameEnded = false;
        public static string gameState = "intermission";
        public static string bodyModel;
        public static string headModel;
        private static int fx_carePackageSmoke;
        public static int fx_sentryExplode;
        public static int fx_sentrySmoke;
        public static int fx_sentryDeath;
        public static int fx_tracer_single;
        public static int fx_blood;
        public static int fx_headshotBlood;
        public static int fx_crateCollectSmoke;
        private static Entity _airdropCollision;
        public static int mapHeight = 0;
        public static int maxWeaponLevelValue = 320;
        public static int maxSupportDropValue = 600;
        public static string thermal_vision = "thermal_mp";

        public static HudElem roundCounter;
        public static HudElem supportDropMeter;

        public static Dictionary<string, string> botAnims = new Dictionary<string, string>();
        public static readonly string[] botAnims_death_explode = { "pb_explosion_death_B1", "pb_stand_death_kickup", "pb_explosion_death_B3", "pb_explosion_death_B2" };
        public static readonly string[] botAnims_deaths = { "pb_stand_death_frontspin", "pb_stand_death_shoulderback", "pb_shotgun_death_legs", "pb_stand_death_leg_kickup", "pb_stand_death_tumbleback", "pb_stand_death_head_collapse", "pb_stand_death_nervedeath", "pb_stand_death_leg", "pb_stand_death_chest_spin", "pb_stand_death_legs", "pb_stand_death_chest_blowback", "pb_stand_death_lowerback" };

        public static int[] rankTable = new int[81];

        public static Dictionary<string, string> weaponNames = new Dictionary<string, string>();
        public static Dictionary<string, string> weaponIcons = new Dictionary<string, string>();
        public static Dictionary<string, string> perkNames = new Dictionary<string, string>();
        public static Dictionary<string, string> perkIcons = new Dictionary<string, string>();
        public static Dictionary<string, string> killstreakNames = new Dictionary<string, string>();
        public static Dictionary<string, string> killstreakIcons = new Dictionary<string, string>();
        public static List<string> supportDropTable = new List<string>();
        public static List<string> killstreakDropTable = new List<string>();
        public static Dictionary<int, Dictionary<string, int>> weaponLevels = new Dictionary<int, Dictionary<string, int>>();
        public static Dictionary<int, Dictionary<string, int>> weaponLevelValues = new Dictionary<int, Dictionary<string, int>>();
        public static int supportDropMeterValue = 0;

        public static List<Entity> lootPackages = new List<Entity>();

        public static readonly string dev = "Slvr99";

        public horde()
        {
            if (GetDvar("g_gametype") != "war")
            {
                Utilities.PrintToConsole("You must be running Safeguard on Team Deathmatch!");
                SetDvar("g_gametype", "war");
                Utilities.ExecuteCommand("map_restart");
                return;
            }
            if (GetDvarInt("sv_maxclients") > 4)
            {
                Utilities.PrintToConsole(string.Format("The current max players for Safeguard can only be 4 or below. The current setting is {0}. It has been set to 4.", GetDvarInt("sv_maxclients")));
                Marshal.WriteInt32(new IntPtr(0x0585AE0C), 4);//Set maxclients directly to avoid map_restart
                Marshal.WriteInt32(new IntPtr(0x0585AE1C), 4);//Latched value
                Marshal.WriteInt32(new IntPtr(0x049EB68C), 4);//Raw maxclients value, this controls the real number of maxclients
                MakeDvarServerInfo("sv_maxclients", 4);
            }

            _mapname = GetDvar("mapname");
            if (_mapname == "mp_radar") thermal_vision = "thermal_snowlevel_mp";

            //Setup our friendly player models
            switch (_mapname)
            {
                case "mp_plaza2":
                case "mp_seatown":
                case "mp_underground":
                case "mp_aground_ss":
                case "mp_italy":
                case "mp_courtyard_ss":
                case "mp_meteora":
                    bodyModel = "mp_body_sas_urban_smg";
                    headModel = "head_sas_a";
                    break;
                case "mp_paris":
                    bodyModel = "mp_body_gign_paris_assault";
                    headModel = "head_gign_a";
                    break;
                case "mp_mogadishu":
                case "mp_bootleg":
                case "mp_carbon":
                case "mp_village":
                case "mp_bravo":
                case "mp_shipbreaker":
                    bodyModel = "mp_body_pmc_africa_assault_a";
                    headModel = "head_pmc_africa_a";
                    break;
                default:
                    bodyModel = "mp_body_delta_elite_smg_a";
                    headModel = "head_delta_elite_a";
                    break;
            }
            switch (_mapname)
            {
                case "mp_dome":
                    mapHeight = -600;
                    break;
                case "mp_alpha":
                case "mp_mogadishu":
                case "mp_park":
                    mapHeight = -100;
                    break;
                case "mp_bootleg":
                    mapHeight = -150;
                    break;
                case "mp_bravo":
                    mapHeight = 900;
                    break;
                case "mp_exchange":
                    mapHeight = -200;
                    break;
                case "mp_interchange":
                case "mp_nola":
                case "mp_six_ss":
                    mapHeight = -15;
                    break;
                case "mp_lambeth":
                    mapHeight = -375;
                    break;
                case "mp_paris":
                    mapHeight = -75;
                    break;
                case "mp_seatown":
                    mapHeight = 100;
                    break;
                case "mp_underground":
                    mapHeight = -300;
                    break;
                case "mp_village":
                case "mp_qadeem":
                    mapHeight = 100;
                    break;
                case "mp_aground_ss":
                case "mp_boardwalk":
                case "mp_courtyard_ss":
                case "mp_roughneck":
                    mapHeight = -50;
                    break;
                case "mp_burn_ss":
                    mapHeight = -70;
                    break;
                case "mp_cement":
                    mapHeight = 250;
                    break;
                case "mp_crosswalk_ss":
                    mapHeight = 1760;
                    break;
                case "mp_hillside_ss":
                    mapHeight = 1930;
                    break;
                case "mp_meteora":
                case "mp_restrepo_ss":
                    mapHeight = 1500;
                    break;
                case "mp_overwatch":
                    mapHeight = 12500;
                    break;
                case "mp_italy":
                    mapHeight = 650;
                    break;
                case "mp_moab":
                case "mp_shipbreaker":
                    mapHeight = 350;
                    break;
                case "mp_morningwood":
                    mapHeight = 1100;
                    break;
                default:
                    mapHeight = 0;
                    break;
            }

            //set up gametype tables
            //P99
            weaponNames.Add("iw5_p99_mp", "P99");
            weaponIcons.Add("iw5_p99_mp", "hud_icon_p99");
            //Wild Widow
            weaponNames.Add("iw5_44magnum_mp", "Wild Widow");
            weaponIcons.Add("iw5_44magnum_mp", "weapon_magnum");
            //Striker
            weaponNames.Add("iw5_striker_mp", "Striker");
            weaponIcons.Add("iw5_striker_mp", "hud_icon_striker");
            //SPAS
            weaponNames.Add("iw5_spas12_mp", "SPAS-12");
            weaponIcons.Add("iw5_spas12_mp", "hud_icon_spas12");
            //MP7
            weaponNames.Add("iw5_mp7_mp", "MP7");
            weaponIcons.Add("iw5_mp7_mp", "hud_icon_mp7");
            //FAD
            weaponNames.Add("iw5_fad_mp", "FAD");
            weaponIcons.Add("iw5_fad_mp", "hud_icon_fad");
            //AK47
            weaponNames.Add("iw5_ak47_mp", "AK-47");
            weaponIcons.Add("iw5_ak47_mp", "hud_icon_ak47");
            //ACR
            weaponNames.Add("iw5_acr_mp", "ACR");
            weaponIcons.Add("iw5_acr_mp", "hud_icon_acr");
            //MG
            weaponNames.Add("iw5_mg36_mp", "MG36");
            weaponIcons.Add("iw5_mg36_mp", "hud_icon_mg36");
            //MK46
            weaponNames.Add("iw5_mk46_mp", "MK46");
            weaponIcons.Add("iw5_mk46_mp", "hud_icon_mk46");
            //Minigun
            weaponNames.Add("iw5_m60jugg_mp", "Minigun");
            weaponIcons.Add("iw5_m60jugg_mp", "hud_icon_m60e4");//hud_icon_minigun
            //Heli Sniper
            weaponNames.Add("iw5_as50_mp", "Heli Sniper");
            weaponIcons.Add("iw5_as50_mp", "hud_icon_as50");
            //MSR
            //weaponNames.Add("iw5_msr_mp", "MSR");
            //weaponIcons.Add("iw5_msr_mp", "hud_icon_msr");
            //throwing knife
            weaponNames.Add("throwingknife_mp", "Throwing Knives");
            weaponIcons.Add("throwingknife_mp", "killiconimpale");
            //speed
            //perkNames.Add("specialty_lightweight", "Speed");
            //perkIcons.Add("specialty_lightweight", "specialty_lightweight");
            //fastreload
            perkNames.Add("specialty_fastreload", "Sleight of Hand");
            perkIcons.Add("specialty_fastreload", "specialty_fastreload_upgrade");
            //quickdraw
            perkNames.Add("specialty_quickdraw", "Quickdraw");
            perkIcons.Add("specialty_quickdraw", "specialty_quickdraw");
            //Marathon
            perkNames.Add("specialty_marathon", "Marathon");
            perkIcons.Add("specialty_marathon", "specialty_longersprint_upgrade");
            //quickswap
            perkNames.Add("specialty_quickswap", "Reflex");
            perkIcons.Add("specialty_quickswap", "specialty_twoprimaries_upgrade");
            //steady aim
            perkNames.Add("specialty_bulletaccuracy", "Steady Aim");
            perkIcons.Add("specialty_bulletaccuracy", "specialty_steadyaim");
            //ready up
            perkNames.Add("specialty_fastsprintrecovery", "Ready Up");
            perkIcons.Add("specialty_fastsprintrecovery", "specialty_quickdraw_upgrade");
            //blast shield
            perkNames.Add("_specialty_blastshield", "Blast Shield");
            perkIcons.Add("_specialty_blastshield", "specialty_blastshield_upgrade");
            //stalker
            perkNames.Add("specialty_stalker", "Stalker");
            perkIcons.Add("specialty_stalker", "specialty_stalker");
            //focus
            //perkNames.Add("specialty_sharp_focus", "Focus");
            //perkIcons.Add("specialty_sharp_focus", "specialty_sharp_focus");
            //regen
            perkNames.Add("specialty_regenspeed", "Faster Health Regen");
            perkIcons.Add("specialty_regenspeed", "specialty_hardline");
            //sprintreload
            //perkNames.Add("specialty_sprintreload", "Sprint Reload");
            //perkIcons.Add("specialty_sprintreload", "demo_forward_fast");
            //trigger happy
            perkNames.Add("specialty_triggerhappy", "Trigger Happy");
            perkIcons.Add("specialty_triggerhappy", "weapon_attachment_xmags");
            //explosivebullets
            perkNames.Add("specialty_explosivebullets", "Explosive Bullets");
            perkIcons.Add("specialty_explosivebullets", "weapon_attachment_fmj");

            //Killstreaks
            //ims
            killstreakNames.Add("ims", "I.M.S.");
            killstreakIcons.Add("ims", "specialty_ims_crate");
            //sentry
            killstreakNames.Add("sentry", "Sentry Gun");
            killstreakIcons.Add("sentry", "specialty_sentry_gun_crate");
            //missile
            killstreakNames.Add("missile", "Trinity Rocket");
            killstreakIcons.Add("missile", "specialty_predator_missile_crate");
            //helicopter
            killstreakNames.Add("helicopter", "Battle Hind");
            killstreakIcons.Add("helicopter", "specialty_attack_helicopter_crate");
            //dragonfly
            killstreakNames.Add("dragonfly", "Vulture");
            killstreakIcons.Add("dragonfly", "specialty_helicopter_guard_crate");
            //scout
            killstreakNames.Add("heloscout", "Helo Scout");
            killstreakIcons.Add("heloscout", "headicon_heli_extract_point");

            //populate the drop tables
            foreach (string drop in weaponNames.Keys)
            {
                if (drop == "iw5_p99_mp") continue;
                if (drop == "iw5_m60jugg_mp") continue;
                if (drop == "iw5_as50_mp") continue;
                string dropName = drop;

                switch (drop)
                {
                    case "iw5_striker_mp":
                        dropName += "_reflex_silencer03";
                        break;
                    case "iw5_spas12_mp":
                        dropName += "_reflex_silencer03";
                        break;
                    case "iw5_fad_mp":
                        dropName += "_eotech";
                        break;
                    case "iw5_ak47_mp":
                        dropName += "_silencer";
                        break;
                    case "iw5_acr_mp":
                        dropName += "_hybrid_silencer";
                        break;
                    case "iw5_mg36_mp":
                        dropName += "_acog_silencer";
                        break;
                    case "iw5_mk46_mp":
                        dropName += "_silencer";
                        break;
                }
                supportDropTable.Add(dropName);
            }
            foreach (string drop in perkNames.Keys)
            {
                if (drop == "specialty_explosivebullets") continue;

                supportDropTable.Add(drop);
            }

            foreach (string drop in killstreakNames.Keys)
                killstreakDropTable.Add(drop);

            //set player weapon levels
            for (int i = 0; i < 4; i++)
            {
                Dictionary<string, int> defaultLevels = new Dictionary<string, int>();
                foreach (string weapon in weaponNames.Keys)
                    defaultLevels.Add(weapon, 1);
                weaponLevels.Add(i, defaultLevels);
            }
            for (int i = 0; i < 4; i++)
            {
                Dictionary<string, int> defaultLevelValues = new Dictionary<string, int>();
                foreach (string weapon in weaponNames.Keys)
                    defaultLevelValues.Add(weapon, 0);
                weaponLevelValues.Add(i, defaultLevelValues);
            }

            //Init bot anims
            botAnims.Add("idle", "pb_stand_alert");
            botAnims.Add("idle_rpg", "pb_stand_alert_RPG");
            botAnims.Add("idle_mg", "pb_stand_alert_mg");
            botAnims.Add("idle_pistol", "pb_stand_alert_pistol");
            botAnims.Add("run", "pb_sprint_assault");
            botAnims.Add("run_smg", "pb_sprint_smg");
            botAnims.Add("run_mg", "pb_sprint_lmg");
            botAnims.Add("run_pistol", "pb_pistol_run_fast");
            botAnims.Add("run_sniper", "pb_sprint_sniper");
            botAnims.Add("run_shotgun", "pb_sprint_shotgun");
            botAnims.Add("run_rpg", "pb_sprint_RPG");
            botAnims.Add("shoot", "pt_stand_shoot");
            botAnims.Add("shoot_rpg", "pt_stand_shoot_RPG");
            botAnims.Add("shoot_mg", "pt_stand_shoot_mg");
            botAnims.Add("shoot_pistol", "pt_stand_shoot_pistol");
            botAnims.Add("reload", "pt_reload_stand_auto");
            botAnims.Add("reload_rpg", "pt_reload_stand_RPG");
            botAnims.Add("reload_pistol", "pt_reload_stand_pistol");
            botAnims.Add("reload_mg", "pt_reload_stand_mg");
            botAnims.Add("run_hurt", "pb_stumble_forward");
            botAnims.Add("melee", "pt_melee_left2right");
            //botAnims.Add("walk_hurt", "pb_stumble_walk_forward");
            botAnims.Add("tower_idle", "pb_crouch_hold_idle");
            botAnims.Add("tower_run", "pb_crouch_walk_forward_unarmed");
            botAnims.Add("tower_run_hurt", "pb_crouch_pain_holdStomach");
            botAnims.Add("tower_melee", "pb_crouch_bombplant");
            botAnims.Add("tower_death", "pb_crouch_death_fetal");

            //Populate bot health table
            Dictionary<string, int> health = new Dictionary<string, int>();
            health.Add("ravager", 100);
            health.Add("enforcer", 250);
            health.Add("tower", 125);
            health.Add("striker", 200);
            health.Add("destructor", 500);
            health.Add("hammer", 250);
            bots.botHealth = health;

            lootCrateLocations.initlootLocations();

            precacheGametype();

            Marshal.WriteInt32(new IntPtr(0x05866E04), 1);//Patch dvar below to accept 1 as a value
            SetDvar("g_maxDroppedWeapons", 1);//Fix for SetItemDropEnabled crash

            SetDvar("cg_drawCrosshair", 1);
            SetDvar("cl_demo_enabled", 0);//Disable recording to prevent overflow
            SetDvar("cl_demo_recordPrivateMatches", 0);
            AfterDelay(50, () =>
            {
                MakeDvarServerInfo("ui_allow_teamchange", 0);
                MakeDvarServerInfo("ui_allow_classchange", 0);
            });
            MakeDvarServerInfo("ui_netGametypeName", "Safeguard");
            MakeDvarServerInfo("party_gametype", "^Safeguard");
            MakeDvarServerInfo("ui_customModeName", "Safeguard");
            MakeDvarServerInfo("ui_gametype", "Safeguard");
            MakeDvarServerInfo("didyouknow", "Safeguard Made by ^2Slvr99");
            MakeDvarServerInfo("g_motd", "Safeguard Made by ^2Slvr99");
            MakeDvarServerInfo("ui_connectScreenTextGlowColor", "0 1 0");
            MakeDvarServerInfo("g_hardcore", 1);

            //Server netcode adjustments//
            SetDvar("com_maxfps", 0);
            //-IW5 server update rate-
            SetDevDvar("sv_network_fps", 200);
            //-Remove ping degradation-
            SetDvar("sv_pingDegradation", 0);
            SetDvar("sv_pingDegradationLimit", 9999);
            //-Tweak ping clamps-
            SetDvar("sv_minPingClamp", 50);
            //-Increase server think rate per frame-
            SetDvar("sv_cumulThinkTime", 1000);
            //-Lock CPU threads-
            SetDvar("sys_lockThreads", "all");

            AfterDelay(50, () => SetDynamicDvar("scr_war_promode", 1));//Pro for lulz
            AfterDelay(10000, () => SetDynamicDvar("scr_war_timelimit", 0));//Hardcode unlimited time
            //Set high quality voice chat audio
            SetDvar("sv_voiceQuality", 9);
            SetDvar("maxVoicePacketsPerSec", 2000);
            SetDvar("maxVoicePacketsPerSecForServer", 1000);
            //Ensure all players are heard regardless of any settings
            SetDvar("cg_everyoneHearsEveryone", 1);
            SetDvar("perk_extendedMagsMGAmmo", 899);
            //SetDvar("perk_weapRateMultiplier", "0.5");

            PlayerConnected += onPlayerConnected;

            Notified += onGlobalNotify;

            createServerHud();

            //init rank table for stat tracking
            for (int i = 0; i < 80; i++)
            {
                rankTable[i] = int.Parse(TableLookup("mp/rankTable.csv", 0, i, 2));
            }
            rankTable[80] = 1746200;

            Entity cp = GetEnt("care_package", "targetname");
            _airdropCollision = GetEnt(cp.Target, "targetname");

            pathfinding.initPathNodes();//Initialize all our path nodes
            pathfinding.bakeAllPathNodes();//Bake path nodes

            for (int i = 0; i < 30; i++)//init botPool. Can be changed to higher number of offhand bots
                bots.spawnBot();
        }

        private static void precacheGametype()
        {
            foreach (string drop in supportDropTable)
            {
                if (perkIcons.Keys.Contains(drop))
                    PreCacheShader(perkIcons[drop]);
                else if (weaponIcons.Keys.Contains(getBaseWeaponName(drop)))
                    PreCacheShader(weaponIcons[getBaseWeaponName(drop)]);
            }
            //foreach (string icon in killstreakIcons.Values)
            //PreCacheShader(icon);
            PreCacheShader("headicon_heli_extract_point");

            PreCacheShader("clanlvl_box");
            //PreCacheShader("progress_bar_fill");
            PreCacheShader("waypoint_ammo_friendly");

            fx_carePackageSmoke = LoadFX("explosions/bouncing_betty_explosion");
            fx_sentryExplode = LoadFX("explosions/sentry_gun_explosion");
            fx_sentrySmoke = LoadFX("smoke/car_damage_blacksmoke");
            fx_sentryDeath = LoadFX("explosions/killstreak_explosion_quick");
            fx_blood = LoadFX("impacts/flesh_hit_body_fatal_exit");
            fx_headshotBlood = LoadFX("impacts/flesh_hit_head_fatal_exit");
            fx_crateCollectSmoke = LoadFX("props/crateexp_dust");
            fx_tracer_single = LoadFX("impacts/exit_tracer");
            //fx_tracer_shotgun = LoadFX("impacts/shotgun_default");

            PreCacheItem("remote_uav_weapon_mp");
            //PreCacheItem("iw5_mk12spr_mp");
            PreCacheVehicle("remote_uav_mp");
            PreCacheStatusIcon("cardicon_iwlogo");

            foreach (string anim in botAnims.Values) PreCacheMpAnim(anim);
            foreach (string anim in botAnims_deaths) PreCacheMpAnim(anim);
            foreach (string anim in botAnims_death_explode) PreCacheMpAnim(anim);
        }

        private static void onPlayerConnected(Entity player)
        {
            //Disable RCon for clients because sad day
            player.SetClientDvar("cl_enableRCon", 0);

            player.SetClientDvar("cl_maxPackets", 100);

            player.SetField("isViewingScoreboard", false);
            refreshScoreboard(player);

            player.SetClientDvar("g_hardcore", "1");
            player.SetClientDvar("g_teamname_axis", "".PadRight(350));
            player.SetClientDvar("g_teamicon_axis", "weapon_missing_image");
            player.SetClientDvar("g_teamicon_MyAxis", "weapon_missing_image");
            player.SetClientDvar("g_teamicon_EnemyAxis", "weapon_missing_image");
            player.SetClientDvar("cl_demo_recordPrivateMatch", "0");
            player.SetClientDvar("cl_demo_enabled", "0");
            //player.SetClientDvars("cg_scoreboardWidth", 700, "cg_scoreboardFont", 0);
            player.SetClientDvar("perk_extendedMagsMGAmmo", 899);
            //player.SetClientDvar("cg_weaponCycleDelay", 750);//Add weapon swap delay to fix hud issues
            player.SetClientDvar("useRelativeTeamColors", 1);
            player.SetField("hasSecondGun", true);
            player.SetField("hasDeathMachine", false);
            player.SetField("isDown", false);
            player.SetField("lastDroppableWeapon", "iw5_p99_mp_xmags");
            player.SetField("weaponsList", new Parameter(new string[] {"iw5_p99_mp_xmags", ""}));
            player.SetField("perksList", new Parameter(new List<string>()));
            player.SetField("killstreaks", new Parameter(new string[] { "", "", "", "" }));
            player.SetField("streakSlotText", new Parameter(new string[] { "", "", "", "" }));
            player.SetField("isCarryingSentry", false);

            //Reset certain dvars that some servers may have set and not restored
            player.SetClientDvar("waypointIconHeight", "36");
            player.SetClientDvar("waypointIconWidth", "36");

            player.SetClientDvar("ui_gametype", "Safeguard");
            player.SetClientDvar("ui_customModeName", "Safeguard");
            player.SetClientDvar("party_gametype", "Safeguard");
            player.SetClientDvar("didyouknow", "^1Safeguard Made by ^2Slvr99");
            player.SetClientDvar("motd", "^1Safeguard Made by ^2Slvr99");
            player.SetClientDvar("g_motd", "^1Safeguard Made by ^2Slvr99");
            player.SetClientDvar("cg_objectiveText", "Face increasingly difficult odds as hordes of enemies assault your team.");
            player.NotifyOnPlayerCommand("use_button_pressed:" + player.EntRef, "+activate");
            player.NotifyOnPlayerCommand("aim_button_pressed", "+toggleads_throw");
            player.NotifyOnPlayerCommand("streakUsed_0:" + player.EntRef, "+actionslot 4");
            player.NotifyOnPlayerCommand("streakUsed_1:" + player.EntRef, "+actionslot 5");
            player.NotifyOnPlayerCommand("streakUsed_2:" + player.EntRef, "+actionslot 6");
            player.NotifyOnPlayerCommand("streakUsed_3:" + player.EntRef, "+actionslot 7");

            AfterDelay(100, () =>
                player.Notify("menuresponse", "team_marinesopfor", "allies"));

            //Init class selection for game
            //player.OnNotify("predicting_about_to_spawn_player", (p) => player.Notify("menuresponse", "changeclass", "allies_recipe1"));

            //trackUsablesForPlayer(player);

            player.SpawnedPlayer += () => onPlayerSpawned(player);
            player.SetViewKickScale(.1f);

            //Rank tracking
            int lastRank = getRankForXP(player);
            lastRank++;
            player.SetField("lastRank", lastRank);
            player.SetField("nextRankXP", rankTable[lastRank]);

            createPlayerHud(player);
        }

        public static void spawnPlayer(Entity player)
        {
            if (player.Classname != "player") return;

            Entity randomSpawn = getRandomSpawnpoint();
            player.Spawn(randomSpawn.Origin, randomSpawn.Angles);
            player.SessionState = "playing";
            player.SessionTeam = "allies";
            //player.TakeAllWeapons();
            //player.ClearPerks();
            player.Notify("spawned_player");
            player.SetModel(bodyModel);
            if (player.GetAttachSize() == 0)
                player.Attach(headModel, "j_spine4");
        }

        private static void onPlayerSpawned(Entity player)
        {
            AfterDelay(50, () =>
            {
                if (gameState != "intermission")
                {
                    player.SessionState = "spectator";
                    player.SessionTeam = "spectator";
                    return;
                }
                player.SessionTeam = "allies";
                player.MaxHealth = 500;
                player.Health = 500;
                player.GiveWeapon("iw5_p99_mp_xmags");
                player.SetSpawnWeapon("iw5_p99_mp_xmags");
                player.GiveMaxAmmo("iw5_p99_mp_xmags");
                OnInterval(2000, () => regeneratePistolAmmo(player));
                player.SetField("hasSecondGun", false);
                player.VisionSetThermalForPlayer(_mapname, 0);

                player.SetClientDvar("g_hardcore", "1");
                player.SetClientDvar("cg_drawCrosshair", "1");
                player.SetClientDvar("ui_drawCrosshair", "1");
                player.SetClientDvar("cg_crosshairDynamic", "1");
                player.SetClientDvar("cg_objectiveText", "Face increasingly difficult odds as hordes of enemies assault your team.");
                player.SetClientDvar("ui_gametype", "Safeguard");
                player.SetClientDvar("party_gametype", "Safeguard");

                player.OpenMenu("perk_hide");

                if (gameEnded) { player.SessionState = "spectator"; return; }

                player.SetPerk("specialty_pistoldeath", true, true);
                player.SetPerk("specialty_finalstand", true, false);

                player.DisableWeaponPickup();

                player.SetField("isDown", false);

                updateAmmoHud(player, true);

                updatePlayerCountForScoreboard();

                trackUsablesForPlayer(player);

                player.Kills = 0;
                player.Deaths = 0;
                player.Assists = 0;
            });
        }

        public override void OnPlayerConnecting(Entity player)
        {
            player.SetClientDvar("ui_gametype", "Safeguard");
            player.SetClientDvar("g_gametype", "Safeguard");
            player.SetClientDvar("ui_customModeName", "Safeguard");
            player.SetClientDvar("party_gametype", "Safeguard");
            player.SetClientDvar("didyouknow", "^1Safeguard Made by ^2Slvr99");
            player.SetClientDvar("motd", "^1Safeguard Made by ^2Slvr99");
            player.SetClientDvar("g_motd", "^1Safeguard Made by ^2Slvr99");
            player.SetClientDvar("ui_connectScreenTextGlowColor", new Vector3(0, 1, 0));
        }
        public override void OnPlayerDamage(Entity player, Entity inflictor, Entity attacker, int damage, int dFlags, string mod, string weapon, Vector3 point, Vector3 dir, string hitLoc)
        {
            if (player == null) return;

            if (player.IsAlive)
            {
                int time = GetTime();
                player.SetField("lastDamageTime", time);

                //Utilities.PrintToConsole(player.HasField("specialty_regenspeed").ToString());
                int regenTime = player.HasField("specialty_regenspeed") ? 5000 : 7000;

                AfterDelay(regenTime, () =>
                {
                    //Utilities.PrintToConsole("Healing player" + player.Name);
                    if (player.GetField<int>("lastDamageTime") == time && player.SessionState == "playing")
                    {
                        player.Health = player.MaxHealth;
                        //Utilities.PrintToConsole("Healed to " + player.MaxHealth);
                    }
                });
            }
        }
        public override void OnPlayerKilled(Entity player, Entity inflictor, Entity attacker, int damage, string mod, string weapon, Vector3 dir, string hitLoc)
        {
            player.TakeAllWeapons();
            player.GetField<HudElem>("hud_killstreakList").SetText("");
            player.GetField<HudElem>("hud_perks").SetText("");
            player.GetField<HudElem>("hud_perks").SetField("text", "");
            player.GetField<List<string>>("perksList").Clear();

            if (gameEnded) return;

            AfterDelay(200, () =>
                onPlayerDeath(player));
        }
        public override EventEat OnSay2(Entity player, string name, string message)
        {
            if (!player.HasField("isDev")) return EventEat.EatNone;

            if (message == "viewpos")
            {
                printToConsole("({0}, {1}, {2})", player.Origin.X, player.Origin.Y, player.Origin.Z);
                Vector3 angles = player.GetPlayerAngles();
                printToConsole("({0}, {1}, {2})", angles.X, angles.Y, angles.Z);
                return EventEat.EatGame;
            }
            if (message.StartsWith("test"))
            {
                doSupportDrop();
                return EventEat.EatGame;
            }
            return EventEat.EatNone;
        }
        public override void OnPlayerDisconnect(Entity player)
        {
            //AfterDelay(500, () => { if (gameState == "ingame") checkForEndGame(); });
            destroyPlayerHud(player);

            updatePlayerCountForScoreboard();
        }
        public override void OnPlayerLastStand(Entity player, Entity inflictor, Entity attacker, int damage, string mod, string weapon, Vector3 dir, string hitLoc, int timeOffset, int deathAnimDuration)
        {
            if (player.GetField<bool>("isDown") || player.Health < -200) return;
            player.SetField("isDown", true);
            AfterDelay(100, () => player.Notify("death"));//Remove the GSC counter
            player.PlaySound("freefall_death");
            player.Health = 500;

            //player.DisableOffhandWeapons();
            //player.DisableWeaponSwitch();
            player.FreezeControls(false);
            player.Deaths++;

            //takePlayerPerks(player);

            HudElem reviveIcon = createReviveHeadIcon(player);

            IPrintLn(string.Format("^1{0} ^1needs to be revived!", player.Name));

            Entity reviver = Spawn("script_origin", player.Origin);
            reviver.LinkTo(player);
            reviver.SetField("usableType", "revive");
            reviver.SetField("range", 60);
            reviver.SetField("player", player);
            reviver.SetField("icon", reviveIcon);
            reviver.SetField("user", reviver);
            player.SetField("reviveTrigger", reviver);
            //makeUsable(reviver, "revive", 50);

            OnInterval(10000, () =>
            {
                takeLastPerk(player);
                if (player.GetField<bool>("isDown")) return true;
                return false;
            });

            int deathCount = 0;
            float red = 1f;
            OnInterval(1000, () =>
            {
                if (gameEnded) return false;

                if (reviver.GetField<Entity>("user") != reviver) return true;

                if (player.IsPlayer) player.PingPlayer();
                deathCount++;

                if (!player.GetField<bool>("isDown")) return false;

                if (deathCount == 15 && player.GetField<bool>("isDown")) player.VisionSetNakedForPlayer("cheat_bw", 15);

                if (deathCount > 20)//Tint icon red
                {
                    if (red >= .05f) red -= .05f;
                    reviveIcon.Color = new Vector3(1, red, red);
                }

                if (deathCount == 40)
                    player.Suicide();

                if (!player.IsAlive)//Check for death after suicide
                {
                    reviver.Delete();
                    player.ClearField("reviveTrigger");
                    reviveIcon.Destroy();
                    return false;
                }
                if (player.GetField<bool>("isDown") && player.IsPlayer) return true;
                return false;
            });
        }
        private static void onPlayerDeath(Entity player)
        {
            if (player.HasField("hud_created"))
            {
                HudElem ksList = player.GetField<HudElem>("hud_killstreakList");
                ksList.SetText("");
                ksList.SetField("text", "");
                HudElem message = player.GetField<HudElem>("hud_message");
                message.SetText("");
                updateAmmoHud(player, false);
            }

            updatePlayerCountForScoreboard();

            clearPlayerWeaponsList(player);

            player.SetField("isDown", true);//Just in case it doesn't get set prior to this stage

            IPrintLn(string.Format("^1{0} ^1has been killed.", player.Name));

            AfterDelay(250, () =>
                StartAsync(setPlayerAsSpectator(player)));

            AfterDelay(1000, () =>
                checkForPlayerRespawn(player));
        }

        private static void onGlobalNotify(int entRef, string message, params Parameter[] parameters)
        {
            //if (entRef > 2046) return;
            Entity player = Entity.GetEntity(entRef);

            #region match timer
            if (message == "fontPulse" && !gameStarted)
            {
                HudElem countdownTimer = HudElem.GetHudElem(entRef);
                HudElem countdownText = HudElem.GetHudElem(entRef - 1);

                countdownText.SetText("Get ready for the attack in:");
                countdownTimer.GlowAlpha = 1;
                countdownTimer.GlowColor = new Vector3(RandomFloatRange(0, 1), RandomFloatRange(0, 1), RandomFloatRange(0, 1));
            }
            #endregion

            #region match start
            else if (message == "prematch_done")
            {
                gameStarted = true;
                AfterDelay(5000, roundUtil.startIntermission);
            }
            #endregion

            #region grenade_fire
            else if (message == "grenade_fire")
            {
                foreach (Entity players in Players)//For grenade HUD
                {
                    if (!players.IsPlayer || !players.IsAlive) continue;
                    updateAmmoHud(players, false);
                    //watchEquipmentUsage
                }
            }
            #endregion

            #region reload
            else if (message == "reload")
            {
                foreach (Entity players in Players)
                {
                    if (!players.IsPlayer || !players.IsAlive || !players.HasField("isDown")) continue;
                    //if (!players.IsReloading()) continue;

                    updateAmmoHud(players, false);
                }
            }
            #endregion

            #region weapon_switch
            else if (message == "weapon_switch_started")
            {
                string newWeap = (string)parameters[0];
                if (newWeap == "c4_mp" || newWeap == "flash_grenade_mp") return;

                if (player.EntRef < 18)
                {
                    if (!newWeap.StartsWith("killstreak_"))
                    {
                        onWeaponSwitch(player, newWeap);
                        StartAsync(watchWeaponChange(player, newWeap));
                    }
                }
                else
                {
                    foreach (Entity players in Players)
                    {
                        if (!newWeap.StartsWith("killstreak_"))
                        {
                            if (!player.IsPlayer || !player.IsAlive || !player.HasField("isDown")) continue;
                            if (!player.IsSwitchingWeapon()) continue;
                            onWeaponSwitch(players, newWeap);
                            StartAsync(watchWeaponChange(players, newWeap));
                        }
                        //break;
                    }
                }
            }
            #endregion

            #region weapon_change
            else if (message == "weapon_change")
            {
                string weapon = (string)parameters[0];
                if (player.EntRef < 18)
                {
                    onWeaponChange(player, weapon);
                }
                foreach (Entity players in Players)
                {
                    if (players.CurrentWeapon != weapon) continue;
                    onWeaponChange(players, weapon);
                    //break;
                }
            }
            #endregion

            #region killstreaks
            if (message.StartsWith("streakUsed"))
            {
                if (!message.Contains("_")) return;

                string data = message.Split('_')[1];
                int slot = int.Parse(data.Split(':')[0]);
                Entity entity = Entity.GetEntity(int.Parse(data.Split(':')[1]));

                StartAsync(killstreaks.executeKillstreak(player, slot));
            }
            #endregion

            #region weapon_fired
            else if (message == "weapon_fired")
            {
                foreach (Entity players in Players)
                {
                    if (!players.IsPlayer || !players.IsAlive || !players.HasField("isDown")) continue;
                    updateAmmoHud(players, false);
                }
            }
            #endregion

            #region player commands
            else if (message.StartsWith("use_button_pressed"))
            {
                Entity user = Entity.GetEntity(int.Parse(message.Split(':')[1]));
                checkPlayerUsables(player);
                player.SetField("isDoubleTapping", true);
                AfterDelay(500, () => player.ClearField("isDoubleTapping"));
            }
            else if (message.StartsWith("-scoreboard:"))
            {
                Entity user = Entity.GetEntity(int.Parse(message.Split(':')[1]));
                user.SetField("isViewingScoreboard", false);
            }
            else if (message.StartsWith("+scoreboard:"))
            {
                Entity user = Entity.GetEntity(int.Parse(message.Split(':')[1]));
                user.SetField("isViewingScoreboard", true);
            }
            #endregion

            #region anticheat
            else if (message.StartsWith("menuresponse") && player.EntRef < 18)//Force class prevention
            {
                if (parameters[0].As<string>().StartsWith("changeclass"))
                {
                    if ((string)parameters[1] != "allies_recipe1" && (string)parameters[1] != "back")
                    {
                        Utilities.ExecuteCommand("kickclient " + player.EntRef + " MP_CHANGE_CLASS_NEXT_SPAWN");
                    }
                }
                else if ((string)parameters[0] == "team_marinesopfor")
                {
                    if ((string)parameters[1] != "allies")
                    {
                        Utilities.ExecuteCommand("kickclient " + player.EntRef + " MP_CANTJOINTEAM");
                    }
                }
            }
            #endregion
        }

        private static IEnumerator watchWeaponChange(Entity player, string expectedWeapon)
        {
            while (player.IsSwitchingWeapon())
                yield return WaitForFrame();

            if (player.CurrentWeapon != expectedWeapon)
                updateAmmoHud(player, true, player.CurrentWeapon);
        }
        private static void onWeaponSwitch(Entity player, string newWeapon)
        {
            if (!player.IsPlayer || !player.IsAlive || !player.HasField("isDown")) return;
            bool isSwitching = player.IsSwitchingWeapon();
            if (isSwitching && player.GetField<string>("lastDroppableWeapon") != newWeapon && player.HasWeapon(newWeapon))
            {
                updateAmmoHud(player, true, newWeapon);

                if (isWeaponMinigun(newWeapon))
                {
                    player.SetPerk("specialty_rof", true, false);
                    player.SetPerk("specialty_extendedmags", true, false);
                    player.SetWeaponAmmoClip(newWeapon, 999);
                    player.SetWeaponAmmoStock(newWeapon, 0);
                    player.UnSetPerk("specialty_extendedmags");
                }
                else
                {
                    if (player.HasPerk("specialty_rof")) player.UnSetPerk("specialty_rof", true);
                    //players.UnSetPerk("specialty_bulletaccuracy", true);
                }
            }
        }
        private static void onWeaponChange(Entity player, string weapon)
        {
            if (!player.IsPlayer || !player.IsAlive || !player.HasField("isDown")) return;

            if (mayDropWeapon(player.CurrentWeapon))
            {
                if (player.CurrentWeapon.StartsWith("alt_"))
                    player.SetField("lastDroppableWeapon", player.CurrentWeapon.Replace("alt_", ""));
                else player.SetField("lastDroppableWeapon", player.CurrentWeapon);
            }

            updateAmmoHud(player, false);

            if (player.CurrentWeapon != weapon) return;

            //checkForKillstreak(players, weapon);
        }
        public static bool trackLootForPlayer(Entity player)
        {
            if (gameEnded) return false;
            if (lootPackages.Count == 0) return false;

            foreach (Entity usable in lootPackages)
            {
                if (player.IsPlayer && player.IsAlive && player.Origin.DistanceTo(usable.Origin) < 75)
                {
                    displayLootCrateHintMessage(player, usable);
                    return false;//We found a usable close enough, get out of this loop
                }
            }

            return true;
        }
        public static bool trackUsablesForPlayer(Entity player)
        {
            if (gameEnded) return false;

            //Revives
            foreach (Entity players in Players)
            {
                if (players == player) continue;
                if (!players.IsAlive || players.Classname != "player") continue;
                if (!players.HasField("isDown")) continue;

                if (players.GetField<bool>("isDown"))
                {
                    if (players.HasField("reviveTrigger") && player.Origin.DistanceTo(players.Origin) < 75)
                    {
                        displayReviveHintMessage(player, players);
                        return false;
                    }
                }
            }

            return true;
        }
        public static void checkPlayerUsables(Entity player)
        {
            if (player.IsAlive)
            {
                foreach (Entity usable in lootPackages)
                {
                    if (player.Origin.DistanceTo(usable.Origin) < 75)
                    {
                        grabCarePackage(usable, player);
                        return;//We found a loot crate close enough, get out of this thread entirely
                    }
                }
                //Check for other usables such as sentries
                //Check for revivable players
                foreach (Entity players in Players)
                {
                    if (players == player) continue;
                    if (!players.IsAlive || players.Classname != "player") continue;
                    if (!players.HasField("isDown")) continue;

                    if (players.GetField<bool>("isDown"))
                    {
                        if (players.HasField("reviveTrigger") && player.Origin.DistanceTo(players.Origin) < 75)
                        {
                            revivePlayer(players.GetField<Entity>("reviveTrigger"), player);
                            return;
                        }
                    }
                }
            }
        }
        private static void displayReviveHintMessage(Entity player, Entity downedPlayer)
        {
            if (!player.HasField("hud_message")) return;
            player.SetField("hasMessageUp", true);
            HudElem message = player.GetField<HudElem>("hud_message");
            message.Alpha = .85f;
            message.SetText("Hold ^3[{+activate}] ^7to revive " + downedPlayer.Name);
            OnInterval(100, () => monitorUsableHintMessage(player, downedPlayer, message));
        }
        private static void displayLootCrateHintMessage(Entity player, Entity usable)
        {
            if (!player.HasField("hud_message")) return;
            player.SetField("hasMessageUp", true);
            HudElem message = player.GetField<HudElem>("hud_message");
            message.Alpha = .85f;
            message.SetText(getLootCrateText(usable, player));
            OnInterval(100, () => monitorUsableHintMessage(player, usable, message, true));
        }
        private static bool monitorUsableHintMessage(Entity player, Entity usable, HudElem message, bool isLoot = false)
        {
            if (gameEnded) return false;
            if (player.Classname != "player" || !player.IsAlive) return false;

            //message.SetText(getUsableText(usable, player));
            if (player.Origin.DistanceTo(usable.Origin) > 75 || usable.HasField("refresh"))
            {
                message.Alpha = 0;
                message.SetText("");
                player.SetField("hasMessageUp", false);
                if (isLoot && player.IsAlive && lootPackages.Count > 0) OnInterval(250, () => trackLootForPlayer(player));
                else if (!isLoot && player.IsAlive) OnInterval(250, () => trackUsablesForPlayer(player));
                return false;
            }

            else return true;
        }

        private static void grabCarePackage(Entity package, Entity player)
        {
            if (package.GetField<bool>("isBeingCaptured")) return;//Default so that there can't be multiple users
            if (player.GetField<bool>("isCarryingSentry")) return;
            if (player.SessionTeam != "allies") return;

            if (player.HasField("isDoubleTapping") && !package.GetField<bool>("isBeingCaptured") && package.HasField("swapCount") && package.GetField<int>("swapCount") < 2)
            {
                player.ClearField("isDoubleTapping");
                rerollPackage(package);
                return;
            }

            HudElem progressBar = createPrimaryProgressBar(player, 0, 0);
            progressBar.SetField("isScaling", false);

            string contents = package.GetField<string>("content");
            package.SetField("isBeingCaptured", true);

            OnInterval(50, () => trackCarePackageCapture(package, player, progressBar));
        }
        private static bool trackCarePackageCapture(Entity package, Entity player, HudElem progressBar)
        {
            if (gameEnded) return false;
            if (player.Classname != "player") return false;

            int percent = package.GetField<int>("percent");

            if (player.UseButtonPressed() && player.Origin.DistanceTo(package.Origin) < 75)
            {
                player.DisableWeapons();
                player.PlayerLinkTo(package);
                player.PlayerLinkedOffsetEnable();
                percent+=3;
                package.SetField("percent", percent);
                if (!(bool)progressBar.GetField("isScaling"))
                {
                    progressBar.SetField("isScaling", true);
                    updateBar(progressBar, 120, 2f);
                }

                if (percent >= 125)
                {
                    player.EnableWeapons();
                    player.Unlink();
                    progressBar.GetField("bar").As<HudElem>().Destroy();
                    progressBar.Destroy();
                    PlayFX(fx_crateCollectSmoke, package.Origin);
                    PlaySoundAtPos(package.Origin, "crate_impact");
                    package.SetField("isBeingCaptured", false);

                    string contents = package.GetField<string>("content");

                    if (weaponNames.Keys.Contains(getBaseWeaponName(contents)))
                        giveWeapon(player, contents);
                    else if (perkNames.Keys.Contains(contents))
                        givePerk(player, contents);
                    else if (killstreakNames.Keys.Contains(contents))
                        killstreaks.giveKillstreak(player, contents);
                    else if (contents == "ammo")
                        giveMaxAmmoToAllPlayers();

                    deleteLootCrate(package);
                    return false;
                }
                return true;
            }
            else
            {
                progressBar.GetField("bar").As<HudElem>().Destroy();
                progressBar.Destroy();
                package.SetField("percent", 0);
                player.EnableWeapons();
                player.Unlink();
                package.SetField("isBeingCaptured", false);
                if (!lootPackages.Contains(package)) deleteLootCrate(package);
                return false;
            }
        }
        private static void revivePlayer(Entity reviveTrigger, Entity reviver)
        {
            if (reviver.GetField<bool>("isCarryingSentry")) return;
            if (reviver.GetField<bool>("isDown")) return;
            if (reviver.SessionTeam != "allies") return;
            if (reviveTrigger.GetField<Entity>("player") == reviver) return;
            if (reviveTrigger.GetField<Entity>("user") != reviveTrigger) return;//To avoid multiple revivers at a time
            reviveTrigger.GetField<Entity>("player").IPrintLnBold("Being revived by " + reviver.Name + "...");
            HudElem progressBar = createPrimaryProgressBar(reviver, 0, 0);
            progressBar.SetField("isScaling", false);
            reviveTrigger.SetField("user", reviver);
            reviveTrigger.SetField("reviveCounter", 1);
            OnInterval(50, () => revivePlayer_logicLoop(reviver, reviveTrigger, progressBar));
        }
        private static bool revivePlayer_logicLoop(Entity reviver, Entity reviveTrigger, HudElem progressBar)
        {
            if (gameEnded) return false;
            if (reviver.UseButtonPressed() && reviveTrigger.GetField<Entity>("player").IsAlive && reviver.Origin.DistanceTo(reviveTrigger.Origin) < 75 && !reviver.GetField<bool>("isDown"))
            {
                int reviveCounter = reviveTrigger.GetField<int>("reviveCounter");
                reviver.DisableWeapons();
                reviveCounter++;
                reviveTrigger.SetField("reviveCounter", reviveCounter);

                if (!(bool)progressBar.GetField("isScaling"))
                {
                    progressBar.SetField("isScaling", true);
                    updateBar(progressBar, 120, 5);
                }
                if (reviveCounter >= 100)
                {
                    Entity downedPlayer = reviveTrigger.GetField<Entity>("player");
                    downedPlayer.LastStandRevive();
                    reviver.EnableWeapons();
                    downedPlayer.SetField("isDown", false);
                    downedPlayer.SetCardDisplaySlot(reviver, 5);
                    downedPlayer.ShowHudSplash("revived", 1);
                    downedPlayer.EnableWeaponSwitch();
                    downedPlayer.EnableOffhandWeapons();
                    string[] weaponList = downedPlayer.GetField<string[]>("weaponsList");
                    if (!weaponList.Contains("iw5_usp45_mp"))
                        downedPlayer.TakeWeapon("iw5_usp45_mp");
                    downedPlayer.SwitchToWeapon(downedPlayer.GetField<string>("lastDroppableWeapon"));

                    downedPlayer.Health = downedPlayer.MaxHealth;
                    reviveTrigger.GetField<HudElem>("icon").Destroy();
                    progressBar.GetField("bar").As<HudElem>().Destroy();
                    progressBar.Destroy();
                    //scorePopup(reviver, amount);
                    reviver.Assists++;
                    reviveTrigger.ClearField("reviveCounter");
                    reviveTrigger.Delete();
                    downedPlayer.ClearField("reviveTrigger");
                    downedPlayer.VisionSetNakedForPlayer("");
                    
                    return false;
                }
                return true;
            }
            else
            {
                Entity downedPlayer = reviveTrigger.GetField<Entity>("player");
                reviveTrigger.SetField("user", reviveTrigger);
                progressBar.GetField("bar").As<HudElem>().Destroy();
                progressBar.Destroy();
                reviveTrigger.SetField("reviveCounter", 1);
                reviver.EnableWeapons();
                if (!downedPlayer.IsAlive)
                {
                    reviveTrigger.Delete();
                    downedPlayer.ClearField("reviveTrigger");
                    return false;
                }
                else return false;
            }
        }

        private static bool regeneratePistolAmmo(Entity player)
        {
            if (!player.HasWeapon("iw5_p99_mp_xmags")) return false;

            int currentAmmo = player.GetWeaponAmmoStock("iw5_p99_mp_xmags");
            player.SetWeaponAmmoStock("iw5_p99_mp_xmags", currentAmmo + 1);
            updateAmmoHud(player, false);

            if (!player.IsAlive || player.Classname != "player") return false;
            return true;
        }

        public static void createServerHud()
        {
            HudElem roundBG = NewHudElem();
            //roundBG.SetPoint("BOTTOM LEFT", "BOTTOM LEFT", 10, -5);
            roundBG.AlignX = HudElem.XAlignments.Left;
            roundBG.AlignY = HudElem.YAlignments.Bottom;
            roundBG.Alpha = .5f;
            roundBG.Archived = true;
            roundBG.Font = HudElem.Fonts.Objective;
            roundBG.FontScale = 1.5f;
            roundBG.Foreground = false;
            roundBG.HideIn3rdPerson = false;
            roundBG.HideWhenDead = false;
            roundBG.HideWhenInDemo = false;
            roundBG.HideWhenInMenu = true;
            roundBG.Archived = true;
            roundBG.Color = Vector3.Zero;
            roundBG.Sort = 5;
            roundBG.LowResBackground = false;
            roundBG.HorzAlign = HudElem.HorzAlignments.Left_Adjustable;
            roundBG.VertAlign = HudElem.VertAlignments.Bottom_Adjustable;
            roundBG.X = 0;
            roundBG.Y = 0;
            roundBG.SetShader("clanlvl_box", 128, 48);
            HudElem roundText = HudElem.CreateServerFontString(HudElem.Fonts.Default, 1.2f);//NewHudElem();
            roundText.Parent = roundBG;
            roundText.SetPoint("top left", "top left", 13, 10);
            roundText.HideWhenInMenu = true;
            roundText.Archived = true;
            roundText.Font = HudElem.Fonts.Default;
            roundText.FontScale = 1.2f;
            roundText.SetText("ROUND");
            roundText.Sort = 6;
            roundText.LowResBackground = false;
            HudElem roundNum = HudElem.CreateServerFontString(HudElem.Fonts.Objective, 1.5f);//NewHudElem();
            roundNum.Parent = roundBG;
            roundNum.SetPoint("right", "right", -12, 0);
            roundNum.HideWhenInMenu = true;
            roundNum.Archived = true;
            roundNum.Font = HudElem.Fonts.Objective;
            roundNum.FontScale = 2;
            roundNum.SetValue(roundUtil.round);
            roundNum.Sort = 6;
            roundNum.LowResBackground = false;
            roundCounter = roundNum;

            HudElem supportBarBG = NewHudElem();
            supportBarBG.AlignX = HudElem.XAlignments.Left;
            supportBarBG.AlignY = HudElem.YAlignments.Top;
            supportBarBG.Alpha = .5f;
            supportBarBG.Color = new Vector3(.2f, .2f, .2f);
            supportBarBG.Foreground = false;
            supportBarBG.HideIn3rdPerson = false;
            supportBarBG.HideWhenInMenu = true;
            supportBarBG.HideWhenDead = false;
            supportBarBG.HideWhenInDemo = false;
            supportBarBG.Archived = true;
            supportBarBG.HorzAlign = HudElem.HorzAlignments.Left_Adjustable;
            supportBarBG.VertAlign = HudElem.VertAlignments.Top_Adjustable;
            supportBarBG.LowResBackground = false;
            supportBarBG.X = 0;
            supportBarBG.Y = 60;
            supportBarBG.Sort = 2;
            supportBarBG.SetShader("clanlvl_box", 196, 20);
            supportDropMeter = supportBarBG;

            HudElem supportBar = HudElem.CreateServerFontString(HudElem.Fonts.Objective, 1.5f);//NewHudElem();
            supportBar.Parent = supportBarBG;
            supportBar.SetPoint("left", "left", 23, 0);
            supportBar.Alpha = 1;
            supportBar.Foreground = true;
            supportBar.HideWhenInMenu = true;
            supportBar.Archived = true;
            supportBar.Sort = 5;
            supportBar.SetShader("progress_bar_fill", 0, 8);

            HudElem supportBarText = HudElem.CreateServerFontString(HudElem.Fonts.Default, 1.2f);//NewHudElem();
            supportBarText.Parent = supportBarBG;
            supportBarText.SetPoint("left", "left", 23, 10);
            supportBarText.Alpha = 1;
            supportBarText.Foreground = true;
            supportBarText.HideWhenInMenu = true;
            supportBarText.Archived = true;
            supportBarText.Sort = 5;
            supportBarText.SetText("SUPPORT DROP");

            updateSupportDropValue(supportDropMeterValue);
        }

        public static void createPlayerHud(Entity player)
        {
            if (player.HasField("hud_created")) return;

            //Ammo counters
            HudElem equipment = HudElem.CreateFontString(player, HudElem.Fonts.HudSmall, .95f);
            equipment.SetPoint("bottom right", "bottom right", -150, -5);
            equipment.HideWhenInMenu = true;
            equipment.Archived = true;
            equipment.LowResBackground = true;
            equipment.AlignX = HudElem.XAlignments.Left;
            equipment.Alpha = 1;
            equipment.SetText("");
            equipment.Sort = 2;

            HudElem ammoStock = HudElem.CreateFontString(player, HudElem.Fonts.Objective, 1.6f);
            ammoStock.SetPoint("bottom right", "bottom right", -30, -20);
            ammoStock.HideWhenInMenu = true;
            ammoStock.Archived = true;
            ammoStock.SetValue(48);
            ammoStock.Sort = 2;

            HudElem ammoClip = HudElem.CreateFontString(player, HudElem.Fonts.Objective, 1.25f);
            ammoClip.Parent = ammoStock;
            ammoClip.SetPoint("right", "right", -45, 0);
            ammoClip.HideWhenInMenu = true;
            ammoClip.Archived = true;
            ammoClip.SetValue(12);
            ammoClip.Color = new Vector3(0, 0, 0);
            ammoClip.Sort = 2;

            HudElem weaponName = HudElem.CreateFontString(player, HudElem.Fonts.HudSmall, 1f);
            weaponName.SetPoint("bottom right", "bottom right", -180, -20);
            weaponName.HideWhenInMenu = true;
            weaponName.Archived = true;
            weaponName.Alpha = 1;
            weaponName.SetText("");
            weaponName.Sort = 2;

            HudElem weaponBarBG = HudElem.CreateIcon(player, "clanlvl_box", 164, 29);
            weaponBarBG.Parent = ammoStock;
            weaponBarBG.SetPoint("right", "right", 10, 0);
            weaponBarBG.Alpha = .4f;
            weaponBarBG.Foreground = false;
            weaponBarBG.HideWhenInMenu = true;
            weaponBarBG.Archived = true;
            weaponBarBG.Color = new Vector3(.1f, .1f, .1f);
            weaponBarBG.Sort = 0;
            HudElem weaponBar = HudElem.CreateIcon(player, "clanlvl_box", 108, 24);
            weaponBar.Parent = weaponBarBG;
            weaponBar.SetPoint("right", "right", -45);
            weaponBar.Alpha = 1;
            weaponBar.Foreground = false;
            weaponBar.HideWhenInMenu = true;
            weaponBar.Archived = true;
            weaponBar.Color = new Vector3(1, 1, 1);
            weaponBar.Sort = 1;
            weaponBar.SetField("maxWidth", 108);

            //Set out player fields for ammo hud
            player.SetField("hud_equipment", equipment);
            player.SetField("hud_ammoStock", ammoStock);
            player.SetField("hud_ammoClip", ammoClip);
            player.SetField("hud_weaponName", weaponName);
            player.SetField("hud_weaponBar", weaponBar);

            HudElem divider = NewClientHudElem(player);
            divider.X = 14;
            divider.Y = 15;
            divider.AlignX = HudElem.XAlignments.Left;
            divider.AlignY = HudElem.YAlignments.Top;
            divider.HorzAlign = HudElem.HorzAlignments.Left_Adjustable;
            divider.VertAlign = HudElem.VertAlignments.Top_Adjustable;
            divider.Alpha = 0.5f;
            divider.HideWhenInMenu = true;
            divider.Foreground = false;
            divider.LowResBackground = true;
            divider.Sort = 4;
            divider.Font = HudElem.Fonts.HudBig;
            divider.FontScale = 3;
            divider.Color = Vector3.Zero;
            divider.SetText(createHudShaderString("clanlvl_box", true, 20, 36));
            player.SetField("hud_divider", divider);

            //Weapon Icon
            HudElem weaponIcon = HudElem.CreateFontString(player, HudElem.Fonts.HudBig, 3);//HudElem.CreateIcon(player, "hud_icon_ak47", 96, 32);
            weaponIcon.Parent = divider;
            weaponIcon.SetPoint("left", "left", 55, 35);
            weaponIcon.Alpha = .9f;
            weaponIcon.Foreground = true;
            weaponIcon.HideWhenInMenu = true;
            weaponIcon.Archived = true;
            weaponIcon.Sort = 5;
            weaponIcon.SetText(createHudShaderString("hud_icon_ak47", true, 64, 32));
            player.SetField("hud_weaponIcon", weaponIcon);

            HudElem weaponLvl = HudElem.CreateFontString(player, HudElem.Fonts.Objective, 2);
            weaponLvl.Parent = weaponIcon;
            weaponLvl.SetPoint("left", "left", -10);
            weaponLvl.Alpha = 1;
            weaponLvl.Foreground = true;
            weaponLvl.HideWhenInMenu = true;
            weaponLvl.Archived = true;
            weaponLvl.Sort = 6;
            weaponLvl.SetValue(1);

            HudElem weaponLvlBarBG = HudElem.CreateIcon(player, "clanlvl_box", 142, 24);
            weaponLvlBarBG.Parent = weaponIcon;
            weaponLvlBarBG.SetPoint("left", "left", -5, 6);
            weaponLvlBarBG.Alpha = .5f;
            weaponLvlBarBG.Foreground = false;
            weaponLvlBarBG.HideWhenInMenu = true;
            weaponLvlBarBG.Archived = true;
            weaponLvlBarBG.Color = Vector3.Zero;
            weaponLvlBarBG.Sort = 2;

            HudElem weaponLvlBar = HudElem.CreateIcon(player, "clanlvl_box", 125, 18);
            weaponLvlBar.Parent = weaponLvlBarBG;
            weaponLvlBar.SetPoint("left", "left", 10);
            weaponLvlBar.Alpha = 1;
            weaponLvlBar.Foreground = false;
            weaponLvlBar.HideWhenInMenu = true;
            weaponLvlBar.Archived = true;
            weaponLvlBar.Color = new Vector3(.9f, .9f, 1);
            weaponLvlBar.Sort = 3;
            weaponLvlBar.SetField("maxWidth", 125);
            player.SetField("hud_weaponLevelBar", weaponLvlBar);

            //Hitmarker
            HudElem hitFeedback = NewClientHudElem(player);
            hitFeedback.HorzAlign = HudElem.HorzAlignments.Center;
            hitFeedback.VertAlign = HudElem.VertAlignments.Middle;
            hitFeedback.X = -12;
            hitFeedback.Y = -12;
            hitFeedback.Alpha = 0;
            hitFeedback.Archived = true;
            hitFeedback.SetShader("damage_feedback", 24, 48);
            hitFeedback.Sort = 2;
            player.SetField("hud_damageFeedback", hitFeedback);

            //Perk hud
            HudElem perks = HudElem.CreateFontString(player, HudElem.Fonts.HudSmall, 1f);
            perks.SetPoint("BOTTOM LEFT", "BOTTOM LEFT", 125, -12);
            perks.HideWhenInMenu = true;
            perks.Foreground = true;
            perks.Archived = true;
            perks.Alpha = 1;
            perks.Sort = 3;
            perks.SetText("");
            perks.SetField("text", "");

            //Score popups are handled individually in it's own function

            //Streaklist
            HudElem killstreakList = HudElem.CreateFontString(player, HudElem.Fonts.HudBig, .60f);
            killstreakList.SetPoint("BOTTOM RIGHT", "BOTTOM RIGHT", -70, -150);
            killstreakList.AlignX = HudElem.XAlignments.Left;
            killstreakList.HideWhenInMenu = true;
            killstreakList.HideWhenDead = true;
            killstreakList.Archived = true;
            killstreakList.Alpha = 1;
            killstreakList.SetText("");
            killstreakList.SetField("text", "");
            killstreakList.Sort = 16;

            //usables message
            HudElem message = HudElem.CreateFontString(player, HudElem.Fonts.Default, 1.6f);
            message.SetPoint("CENTER", "CENTER", 0, 110);
            message.HideWhenInMenu = true;
            //message.Foreground = true;
            message.Alpha = 0;
            message.Archived = true;
            message.Sort = 20;
            player.SetField("hud_message", message);

            //Finish out player fields
            player.SetField("hud_perks", perks);
            player.SetField("hud_killstreakList", killstreakList);
            player.SetField("hud_created", true);

            //Update our ammo counters
            updateAmmoHud(player, true);
        }
        public static void destroyPlayerHud(Entity player)
        {
            if (!player.HasField("hud_created")) return;
            HudElem[] gameHUD = new HudElem[10] {
                player.GetField<HudElem>("hud_equipment"),
                player.GetField<HudElem>("hud_ammoStock"),
                player.GetField<HudElem>("hud_ammoClip"),
                player.GetField<HudElem>("hud_weaponName"), 
                player.GetField<HudElem>("hud_divider"),
                player.GetField<HudElem>("hud_weaponLevelBar"),
                player.GetField<HudElem>("hud_perks"),
                player.GetField<HudElem>("hud_killstreakList"),
                player.GetField<HudElem>("hud_damageFeedback"),
                player.GetField<HudElem>("hud_message") };

            HudElem weaponBar = player.GetField<HudElem>("hud_weaponBar");
            HudElem weaponIcon = player.GetField<HudElem>("hud_weaponIcon");

            foreach (HudElem hud in gameHUD)
            {
                //hud.Reset();
                if (hud == null) continue;
                hud.Destroy();
            }

            foreach (HudElem hud in weaponBar.Children)
                hud.Destroy();
            weaponBar.Destroy();

            foreach (HudElem hud in weaponIcon.Children)
                hud.Destroy();
            weaponIcon.Destroy();

            player.ClearField("hud_equipment");
            player.ClearField("hud_ammoStock");
            player.ClearField("hud_ammoClip");
            player.ClearField("hud_weaponName");
            player.ClearField("hud_divider");
            player.ClearField("hud_weaponBar");
            player.ClearField("hud_weaponLevelBar");
            player.ClearField("hud_weaponIcon");
            player.ClearField("hud_perks");
            player.ClearField("hud_killstreakList");
            player.ClearField("hud_damageFeedback");
            player.ClearField("hud_message");
            player.ClearField("hud_created");
        }
        public static void scorePopup(Entity player, int amount)
        {
            if (!player.HasField("hud_created")) return;

            HudElem score = NewClientHudElem(player);
            score.AlignX = HudElem.XAlignments.Left;
            score.AlignY = HudElem.YAlignments.Top;
            score.Alpha = 1;
            score.Archived = false;
            score.Font = HudElem.Fonts.Default;
            score.FontScale = 1.2f;
            score.Foreground = true;
            score.HideIn3rdPerson = false;
            score.HideWhenDead = false;
            score.HideWhenInDemo = false;
            score.HideWhenInMenu = true;
            score.HorzAlign = HudElem.HorzAlignments.Left;
            score.VertAlign = HudElem.VertAlignments.Top_Adjustable;
            score.X = 80 + RandomIntRange(-30, 30);
            score.Y = 120 + RandomIntRange(-10, 10);
            score.SetText("+" + amount);

            score.FadeOverTime(.7f);
            score.Alpha = 0;
            score.MoveOverTime(.7f);
            score.X = 90;
            score.Y = 65;

            AfterDelay(700, () => score.Destroy());

            player.Score += amount;

            //Add score to supportDrop bar
            updateSupportDropValue(amount);

            //Add score to player weapon level
            addLevelToCurrentWeapon(player, amount / 2);
        }

        public static void doSupportDrop()
        {
            foreach (Entity players in Players)
            {
                if (players.Classname != "player")
                    continue;

                players.ShowHudSplash("caused_defcon", 0);//caused_defcon
                AfterDelay(11500, () => OnInterval(250, () => trackLootForPlayer(players)));
            }

            for (int i = 0; i < 3; i++)
                callAirdrop(lootCrateLocations.getRandomLootSpawn());

            AfterDelay(1000, lootCrateLocations.clearLootLocationFlags);
        }

        public static void callAirdrop(Vector3 location)
        {
            Entity owner = Players.First((p) => p.IsAlive);
            if (owner == null) return;

            Vector3 yaw = VectorToAngles(location - location.Around(50));
            Vector3 direction = new Vector3(0, yaw.Y, 0);
            Vector3 forward = AnglesToForward(direction);
            Vector3 start = location + (forward * -15000);
            Vector3 end = location + (forward * 20000);
            Vector3 pathStart = new Vector3(start.X, start.Y, location.Z + 1800);
            Vector3 pathEnd = new Vector3(end.X, end.Y, location.Z + 1800);
            Entity lb = SpawnHelicopter(owner, pathStart, forward, "littlebird_mp", "vehicle_little_bird_armed");

            lb.SetVehicleTeam(owner.SessionTeam);
            lb.EnableLinkTo();
            lb.SetSpeed(375, 225, 75);
            lb.SetTurningAbility(.3f);
            StartAsync(airdropFly(owner, lb, location, pathEnd));
        }
        private static IEnumerator airdropFly(Entity owner, Entity lb, Vector3 dropLocation, Vector3 pathEnd)
        {
            lb.SetVehGoalPos(dropLocation + new Vector3(0, 0, 1800), true);
            Vector3 crateTag = lb.GetTagOrigin("tag_ground");
            Entity crate = Spawn("script_model", crateTag);
            crate.SetModel("com_plasticcase_friendly");
            crate.Angles = new Vector3(0, RandomInt(360), 0);
            crate.LinkTo(lb, "tag_ground");
            yield return Wait(6f);

            crate.Unlink();
            crate.CloneBrushModelToScriptModel(_airdropCollision);
            crate.SetContents(1);
            Vector3 dropImpulse = new Vector3(RandomInt(5), RandomInt(5), RandomInt(5));
            crate.PhysicsLaunchServer(Vector3.Zero, dropImpulse);
            yield return Wait(1);

            lb.SetVehGoalPos(pathEnd, true);

            yield return Wait(4);
    
            crate.SetField("user", crate);
            crate.SetField("content", getRandomSupportCrateContent());
            crate.SetField("isBeingCaptured", false);
            crate.SetField("percent", 0);
            crate.SetField("swapCount", 0);
            lootPackages.Add(crate);
            HudElem icon = createPackageIcon(crate, true);
            crate.SetField("icon", icon);
            //StartAsync(watchCrateUsage(crate));
            yield return Wait(4);

            lb.FreeHelicopter();
            lb.Delete();
        }
        private static void rerollPackage(Entity package)
        {
            package.SetField("swapCount", package.GetField<int>("swapCount") + 1);
            if (package.GetField<int>("swapCount") > 1)
                package.SetField("content", "ammo");
            else
            {
                string newContents = getRandomSupportCrateContent();
                if (newContents == package.GetField<string>("content")) newContents = getRandomSupportCrateContent();
                package.SetField("content", newContents);
            }
            package.SetField("isBeingCaptured", false);
            package.SetField("percent", 0);
            package.SetField("refresh", true);
            AfterDelay(200, () => package.ClearField("refresh"));
            //lootPackages.Add(package);
            package.GetField<HudElem>("icon").Destroy();
            HudElem icon = createPackageIcon(package, true);
            package.SetField("icon", icon);
        }

        public static void spawnLoot()
        {
            for (int i = 0; i < 7; i++)
            {
                Vector3 lootLocation = lootCrateLocations.getRandomLootSpawn();
                Vector3 ground = GetGroundPosition(lootLocation, 1);

                Entity package = Spawn("script_model", lootLocation);
                package.SetModel("com_plasticcase_friendly");
                package.Angles = new Vector3(0, RandomInt(360), 0);
                package.CloneBrushModelToScriptModel(_airdropCollision);
                package.SetContents(1);
                Entity packageOutline = Spawn("script_model", package.Origin);
                packageOutline.Angles = package.Angles;
                packageOutline.SetModel("com_plasticcase_trap_bombsquad");
                packageOutline.LinkTo(package, "tag_origin", Vector3.Zero, Vector3.Zero);
                package.SetField("outline", packageOutline);

                package.PhysicsLaunchServer(Vector3.Zero, Vector3.Zero);
                string contents = getRandomKillstreak();
                package.SetField("content", contents);
                package.SetField("isBeingCaptured", false);
                package.SetField("percent", 0);

                HudElem icon = createPackageIcon(package);
                package.SetField("icon", icon);
                lootPackages.Add(package);

                Entity fx = SpawnFX(fx_carePackageSmoke, ground);
                AfterDelay(4000, () => fx.Delete());
                TriggerFX(fx);
                AfterDelay(500, () => PlaySoundAtPos(ground, "physics_weapon_container_default"));
            }
        }
    }
}
