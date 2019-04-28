using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InfinityScript;
using static InfinityScript.GSCFunctions;

namespace horde
{
    public class hordeUtils : BaseScript
    {
        public static IEnumerator setPlayerAsSpectator(Entity player)
        {
            yield return Wait(.5f);
            player.SessionState = "spectator";
        }
        public static void checkForPlayerRespawn(Entity player)
        {
            if (horde.gameState == "intermission")
                horde.spawnPlayer(player);
            else roundUtil.checkForEndGame();
        }


        public static void deleteAllLootCrates()
        {
            List<Entity> deletedCrates = new List<Entity>();
            for (int i = 0; i < horde.lootPackages.Count; i++)
            {
                Entity crate = horde.lootPackages[i];
                if (crate.GetField<bool>("isBeingCaptured"))
                {
                    deletedCrates.Add(crate);
                    continue;
                }
                if (crate.HasField("swapCount")) continue;

                HudElem icon = crate.GetField<HudElem>("icon");
                icon.Destroy();
                if (crate.HasField("outline"))
                {
                    crate.GetField<Entity>("outline").Delete();
                    crate.ClearField("outline");
                }
                deletedCrates.Add(crate);
                crate.Delete();
            }

            foreach (Entity crate in deletedCrates)
                horde.lootPackages.Remove(crate);
            //horde.lootPackages.Clear();
        }
        public static void deleteLootCrate(Entity crate)
        {
            HudElem icon = crate.GetField<HudElem>("icon");
            icon.Destroy();
            crate.ClearField("icon");
            if (crate.HasField("outline"))
            {
                crate.GetField<Entity>("outline").Delete();
                crate.ClearField("outline");
            }
            if (horde.lootPackages.Contains(crate))
            {
                //Utilities.PrintToConsole("Removing a loot crate");
                horde.lootPackages.Remove(crate);
                //Utilities.PrintToConsole("Removed successfully");
            }
            crate.ClearField("user");
            crate.ClearField("content");
            crate.ClearField("isBeingCaptured");
            crate.ClearField("percent");
            crate.ClearField("swapCount");
            crate.Delete();
        }

        public static string getRandomKillstreak()
        {
            int random = RandomInt(horde.killstreakDropTable.Count);

            return horde.killstreakDropTable[random];
        }

        public static string getRandomSupportCrateContent()
        {
            int random = RandomInt(horde.supportDropTable.Count);
            string content = "";
            content = horde.supportDropTable[random];

            if (content == "iw5_44magnum_mp" && roundUtil.round < 10) return getRandomSupportCrateContent();

            //Utilities.PrintToConsole("Rolled crate content " + content);
            return content;
        }
        public static string getUsableString(Entity usable, Entity player)
        {
            switch (usable.GetField<string>(""))
            {
                case "revive":
                    Entity downed = usable.GetField<Entity>("player");
                    if (player == downed || usable.GetField<Entity>("user") == player) return "";
                    else if (usable.GetField<Entity>("user") != usable) return downed.Name + " is already being revived!";
                    else return "Hold ^3[{+activate}] ^7to revive " + downed.Name;
                default:
                    return "";
            }
        }
        public static string getLootCrateText(Entity usable, Entity player)
        {
            if (player.SessionTeam != "allies") return string.Empty;
            if (!usable.HasField("content")) return string.Empty;

            string content = usable.GetField<string>("content");
            string text = "";

            if (horde.killstreakNames.Keys.Contains(content)) text = "Press and Hold ^3[{+activate}] ^7for " + horde.killstreakNames[content];
            else if (horde.perkNames.Keys.Contains(content)) text = "Press and Hold ^3[{+activate}] ^7for " + horde.perkNames[content] + ".";
            else if (horde.weaponNames.Keys.Contains(getBaseWeaponName(content))) text = "Press and Hold ^3[{+activate}] ^7for " + horde.weaponNames[getBaseWeaponName(content)] + ".";
            else text = string.Empty;

            if (usable.HasField("swapCount") && usable.GetField<int>("swapCount") < 2) text += "\nDouble Tap ^3[{+activate}] ^7to change.";
            else if (usable.HasField("swapCount") && usable.GetField<int>("swapCount") >= 2) text = "Press and Hold ^3[{+activate}] ^7for ammo.";

            return text;
        }

        public static bool isWeaponMinigun(string weapon)
            => weapon == "iw5_m60jugg_mp_rof";

        public static void printToConsole(string format, params object[] p)
        {
            if (p.Length > 0)
                Utilities.PrintToConsole(string.Format(format, p));
            else Utilities.PrintToConsole(format);
        }
        public static Vector3 parseVec3(string vec3)
        {
            vec3 = vec3.Replace(" ", string.Empty);
            if (!vec3.StartsWith("(") && !vec3.EndsWith(")")) printToConsole("Vector was not formatted correctly! Vector: " + vec3);
            vec3 = vec3.Replace("(", string.Empty);
            vec3 = vec3.Replace(")", string.Empty);
            string[] split = vec3.Split(',');
            if (split.Length < 3) printToConsole("Vector was not formatted correctly! Vector: " + vec3);
            Vector3 ret = new Vector3(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]));
            return ret;
        }
        public static void teamSplash(string splash, Entity player)
        {
            foreach (Entity players in Players)
            {
                if (!players.IsPlayer) continue;
                players.SetCardDisplaySlot(player, 5);
                players.ShowHudSplash(splash, 1);
            }
        }
        public static void refreshScoreboard(Entity player)
        {
            player.NotifyOnPlayerCommand("+scoreboard:" + player.EntRef, "+scores");
            player.NotifyOnPlayerCommand("-scoreboard:" + player.EntRef, "-scores");
            OnInterval(50, () =>
            {
                if (!player.IsPlayer || !player.HasField("isViewingScoreboard"))
                {
                    player.ClearField("isViewingScoreboard");
                    return false;
                }
                if (!player.GetField<bool>("isViewingScoreboard")) return true;
                player.ShowScoreBoard();
                return true;
            });
        }
        public static void updatePlayerCountForScoreboard()
        {
            int playerCount = GetTeamPlayersAlive("allies");
            SetTeamScore("allies", playerCount);
        }
        public static bool mayDropWeapon(string weapon)
        {
            if (weapon == "none")
                return false;

            if (weapon == "iw5_as50_mp_as50scope" || isWeaponMinigun(weapon) || weapon == "deployable_vest_marker_mp")
                return false;

            if (weapon.Contains("killstreak") || weapon.Contains("airdrop"))
                return false;

            if (weapon == "frag_grenade_mp")
                return false;

            if (weapon == "trophy_mp")
                return false;

            return true;
        }
        public static Entity getRandomSpawnpoint()
        {
            Entity ret = null;
            Entity[] spawns = getAllEntitiesWithClassname("mp_tdm_spawn");

            if (spawns.Length > 0)
                ret = spawns[RandomInt(spawns.Length)];
            else ret = GetEnt("flag_descriptor", "targetname");//Should never get to this point

            return ret;
        }
        public static Entity getRandomBotSpawnpoint()
        {
            Entity ret = null;
            Entity[] spawns = getAllEntitiesWithClassname("mp_dm_spawn");

            if (spawns.Length > 0)
                ret = spawns[RandomInt(spawns.Length)];
            else ret = getRandomSpawnpoint(); 

            return ret;
        }
        public static Entity[] getAllEntitiesWithClassname(string classname)
        {
            int entCount = GetEntArray(classname, "classname").GetHashCode();
            Entity[] ret = new Entity[entCount];
            int count = 0;
            for (int i = 0; i < 2000; i++)
            {
                Entity e = Entity.GetEntity(i);
                string c = e.Classname;
                if (c == classname) ret[count] = e;
                else continue;
                count++;
                if (count == entCount) break;
            }
            return ret;
        }

        public static int getRankForXP(Entity player)
        {
            int playerXP = (int)player.GetPlayerData("experience");
            int rank = 0;

            for (int i = 0; i < 80; i++)
            {
                int rankXp = horde.rankTable[i];

                if (playerXP < rankXp) break;
                else if (playerXP >= rankXp) rank = i;
            }

            return rank;
        }

        public static void addRank(Entity player, int exp)
        {
            int XP = (int)player.GetPlayerData("experience");
            int newXP = XP + exp;

            if (newXP <= 1746200)
                player.SetPlayerData("experience", newXP);

            else player.SetPlayerData("experience", 1746200);

            int nextXp = player.GetField<int>("nextRankXP");

            if (newXP > nextXp && player.GetField<int>("lastRank") < 80)
            {
                int lastRank = player.GetField<int>("lastRank");
                lastRank++;
                player.SetRank(lastRank - 1);//-1 because it uses array ints
                player.SetField("lastRank", lastRank);
                int rankXp = horde.rankTable[lastRank];
                player.SetField("nextRankXP", rankXp);

                player.SetClientDvar("ui_promotion", 1);
                player.PlayLocalSound("mp_level_up");
                AfterDelay(50, () => player.ShowHudSplash("promotion", 1));//After a frame to show correct rank
            }
        }

        public static void giveWeapon(Entity player, string weapon)
        {
            if (player.HasWeapon(weapon))
            {
                //player.SwitchToWeaponImmediate(weapon);
                promoteWeapon(player, weapon);
                player.GiveMaxAmmo(weapon);
                return;
            }
            if (weapon == "throwingknife_mp")
            {
                player.TakeWeapon("c4_mp");
                player.GiveWeapon("throwingknife_mp");
                player.SetOffhandPrimaryClass("throwingknife");
                updateAmmoHud(player, false);
                return;
            }

            updatePlayerWeaponsList(player, weapon);
            player.GiveWeapon(weapon);
            player.GiveMaxAmmo(weapon);
            player.SwitchToWeaponImmediate(weapon);
            updateAmmoHud(player, true, weapon);
        }
        public static void givePerk(Entity player, string perk)
        {
            if (player.HasPerk(perk) || player.HasField(perk)) return;

            if (perk == "_specialty_blastshield" || perk == "specialty_triggerhappy" || perk == "specialty_regenspeed") player.SetField(perk, true);
            else player.SetPerk(perk, true, true);

            List<string> perksList = player.GetField<List<string>>("perksList");
            perksList.Add(perk);
            player.SetField("perksList", new Parameter(perksList));
            player.PlayLocalSound("earn_perk");

            HudElem perks = player.GetField<HudElem>("hud_perks");
            string text = (string)perks.GetField("text");
            text += createHudShaderString(horde.perkIcons[perk], false, 64, 64);
            perks.SetText(text);
            perks.SetField("text", text);
        }
        public static void takeLastPerk(Entity player)
        {
            string perk = player.GetField<List<string>>("perksList").Last();
            if (player.HasField(perk)) player.ClearField(perk);
            else player.UnSetPerk(perk, true);

            HudElem perks = player.GetField<HudElem>("hud_perks");
            string text = (string)perks.GetField("text");
            string[] perkTokens = text.Split('^');
            string lastPerk = perkTokens.Last();
            int perkIndex = Array.IndexOf(perkTokens, lastPerk);
            text.Remove(perkIndex, text.Length - perkIndex);
            perks.SetText(text);
            perks.SetField("text", text);
        }

        public static void updatePlayerWeaponsList(Entity player, string newWeapon)
        {
            if (!player.HasField("isDown")) return;

            string[] weaponsList = player.GetField<string[]>("weaponsList");

            if (weaponsList[1] == "")
                weaponsList[1] = newWeapon;
            else
            {
                int currentWeaponIndex = Array.IndexOf(weaponsList, player.GetField<string>("lastDroppableWeapon"));
                weaponsList[currentWeaponIndex] = newWeapon;
                player.TakeWeapon(player.GetField<string>("lastDroppableWeapon"));
            }

            //else Log.Write(LogLevel.Info, "Tried to add a weapon to a player's weapon list that the player already has!");

            player.SetField("weaponsList", new Parameter(weaponsList));
        }

        public static void clearPlayerWeaponsList(Entity player)
        {
            if (!player.HasField("isDown")) return;

            string[] weaponsList = player.GetField<string[]>("weaponsList");
            weaponsList[0] = "";
            weaponsList[1] = "";

            player.SetField("weaponsList", new Parameter(weaponsList));
        }

        public static void giveMaxAmmo(Entity player)
        {
            if (!player.HasField("isDown")) return;

            string[] weaponsList = player.GetField<string[]>("weaponsList");
            foreach (string weapon in weaponsList)
                player.GiveMaxAmmo(weapon);

            player.PlayLocalSound("ammo_crate_use");
        }
        public static void giveMaxAmmoToAllPlayers()
        {
            foreach (Entity player in Players)
            {
                if (!player.HasField("isDown")) continue;

                player.ShowHudSplash("team_ammo_refill", 0, 0);
                giveMaxAmmo(player);
            }
        }

        public static void promoteCurrentWeapon(Entity player)
        {
            string weapon = player.CurrentWeapon;
            weapon = getBaseWeaponName(weapon);
            HudElem weaponLevelBar = player.GetField<HudElem>("hud_weaponLevelBar");
            HudElem weaponLevelCounter = player.GetField<HudElem>("hud_weaponIcon").Children[0];
            int weaponLevel = horde.weaponLevels[player.EntRef][weapon];

            if (weaponLevel >= 100) return;
            weaponLevel = ++horde.weaponLevels[player.EntRef][weapon];
            horde.weaponLevelValues[player.EntRef][weapon] = 0;

            updateWeaponLevel(player, weapon, true);

            player.ShowHudSplash("promotion_weapon", 1);
            //player.PlayLocalSound("");
        }
        public static void promoteWeapon(Entity player, string weapon)
        {
            string baseWeapon = getBaseWeaponName(weapon);
            HudElem weaponLevelBar = player.GetField<HudElem>("hud_weaponLevelBar");
            HudElem weaponLevelCounter = player.GetField<HudElem>("hud_weaponIcon").Children[0];
            int weaponLevel = horde.weaponLevels[player.EntRef][baseWeapon];

            if (weaponLevel >= 100) return;
            weaponLevel = ++horde.weaponLevels[player.EntRef][baseWeapon];
            horde.weaponLevelValues[player.EntRef][baseWeapon] = 0;

            updateWeaponLevel(player, baseWeapon, true);

            player.ShowHudSplash("promotion_weapon", 1);
            //player.PlayLocalSound("");
        }

        public static void addLevelToCurrentWeapon(Entity player, int points)
        {
            string weapon = player.CurrentWeapon;
            if (!horde.weaponNames.Keys.Contains(getBaseWeaponName(weapon))) return;

            weapon = getBaseWeaponName(weapon);
            HudElem weaponLevelBar = player.GetField<HudElem>("hud_weaponLevelBar");
            horde.weaponLevelValues[player.EntRef][weapon] += points;

            if (horde.weaponLevelValues[player.EntRef][weapon] >= horde.maxWeaponLevelValue)
            {
                promoteCurrentWeapon(player);
                return;
            }

            updateWeaponLevel(player, weapon);
        }

        #region HUD Utils
        public static string createHudShaderString(string shader, bool flipped = false, int width = 64, int height = 64)
        {
            byte[] str;
            byte flip;
            flip = (byte)(flipped ? 2 : 1);
            byte w = (byte)width;
            byte h = (byte)height;
            byte length = (byte)shader.Length;
            str = new byte[4] { flip, w, h, length };
            string ret = "^" + Encoding.UTF8.GetString(str);
            return ret + shader;
        }
        public static HudElem createTimer(int time, string label)
        {
            HudElem timerBG = NewHudElem();
            //roundBG.SetPoint("BOTTOM LEFT", "BOTTOM LEFT", 10, -5);
            timerBG.AlignX = HudElem.XAlignments.Center;
            timerBG.AlignY = HudElem.YAlignments.Middle;
            timerBG.Alpha = 0;
            timerBG.Archived = false;
            timerBG.Foreground = false;
            timerBG.HideIn3rdPerson = false;
            timerBG.HideWhenDead = false;
            timerBG.HideWhenInDemo = false;
            timerBG.HideWhenInMenu = false;
            timerBG.Color = Vector3.Zero;
            timerBG.LowResBackground = false;
            timerBG.HorzAlign = HudElem.HorzAlignments.Center_Adjustable;
            timerBG.VertAlign = HudElem.VertAlignments.Middle;
            timerBG.X = -165;
            timerBG.Y = 75;
            timerBG.SetShader("clanlvl_box", 128, 48);
            timerBG.FadeOverTime(.5f);
            timerBG.Alpha = .5f;

            HudElem timer = NewHudElem();
            //timer.SetPoint("CENTER", "CENTER", -150, 50);
            timer.Parent = timerBG;
            timer.Alpha = 0;
            timer.Archived = false;
            timer.Font = HudElem.Fonts.Objective;
            timer.FontScale = 2f;
            timer.Foreground = true;
            timer.HideIn3rdPerson = false;
            timer.HideWhenDead = false;
            timer.HideWhenInDemo = false;
            timer.HideWhenInMenu = false;
            timer.X = 0;
            timer.Y = 0;
            timer.SetPoint("left", "left", 20);
            timer.SetTenthsTimer(time);
            timer.FadeOverTime(.5f);
            timer.Alpha = 1f;

            if (label != "" || !string.IsNullOrEmpty(label))
            {
                HudElem timerText = HudElem.CreateServerFontString(HudElem.Fonts.Default, 1);
                timerText.Parent = timer;
                timerText.Alpha = 0;
                timerText.SetPoint("left", "left", 45, 12);
                timerText.Archived = false;
                timerText.Foreground = true;
                timerText.HideIn3rdPerson = false;
                timerText.HideWhenDead = false;
                timerText.HideWhenInDemo = false;
                timerText.HideWhenInMenu = false;
                timerText.Color = new Vector3(1, 1, 0.2f);
                timerText.SetText(label + createHudShaderString("hud_killstreak_dpad_arrow_down", false, 38, 48));
                timerText.FadeOverTime(.5f);
                timerText.Alpha = 1;
            }

            return timer;
        }
        public static void destroyTimer(HudElem timer)
        {
            foreach (HudElem child in timer.Children)
                child.Destroy();
            timer.Parent.Destroy();
            timer.Destroy();
        }
        public static HudElem createPackageIcon(Entity package, bool isSupport = false)
        {
            HudElem icon = NewHudElem();
            icon.Alpha = .7f;
            icon.Archived = false;
            icon.HideIn3rdPerson = false;
            icon.HideWhenDead = false;
            icon.HideWhenInDemo = false;
            icon.HideWhenInMenu = false;
            if (isSupport)
            {
                string contents = package.GetField<string>("content");
                string shader = "";
                if (package.HasField("swapCount") && package.GetField<int>("swapCount") > 1)
                    shader = "waypoint_ammo_friendly";
                else if (horde.weaponNames.Keys.Contains(getBaseWeaponName(contents)))
                    shader = horde.weaponIcons[getBaseWeaponName(contents)];
                else if (horde.perkNames.Keys.Contains(contents))
                    shader = horde.perkIcons[contents];

                icon.SetShader(shader, 10, 10);
            }
            else icon.SetShader(horde.killstreakIcons[package.GetField<string>("content")], 10, 10);
            icon.X = package.Origin.X;
            icon.Y = package.Origin.Y;
            icon.Z = package.Origin.Z + 15;
            icon.SetWaypoint(true, true, false, false);
            package.SetField("icon", icon);
            return icon;
        }
        public static HudElem createReviveHeadIcon(Entity player)
        {
            HudElem icon = NewTeamHudElem("allies");
            icon.SetShader("waypoint_revive", 8, 8);
            icon.Alpha = .85f;
            icon.SetWaypoint(true, true);
            icon.SetTargetEnt(player);
            return icon;
        }
        public static HudElem createPrimaryProgressBar(Entity player, int xOffset, int yOffset)
        {
            HudElem progressBar = HudElem.CreateIcon(player, "progress_bar_fill", 0, 9);//NewClientHudElem(player);
            progressBar.SetField("frac", 0);
            progressBar.Color = new Vector3(1, 1, 1);
            progressBar.Sort = -2;
            progressBar.Shader = "progress_bar_fill";
            progressBar.SetShader("progress_bar_fill", 1, 9);
            progressBar.Alpha = 1;
            progressBar.SetPoint("center", "", 0, -61);
            progressBar.AlignX = HudElem.XAlignments.Left;
            progressBar.X = -60;

            HudElem progressBarBG = HudElem.CreateIcon(player, "progress_bar_bg", 124, 13);//NewClientHudElem(player);
            progressBarBG.SetPoint("center", "", 0, -61);
            progressBarBG.SetField("bar", progressBar);
            progressBarBG.Sort = -3;
            progressBarBG.Color = new Vector3(0, 0, 0);
            progressBarBG.Alpha = .5f;

            return progressBarBG;
        }

        public static void updateBar(HudElem barBG, int barFrac, float rateOfChange)
        {

            HudElem bar = (HudElem)barBG.GetField("bar");
            bar.SetField("frac", barFrac);

            if (rateOfChange > 0)
                bar.ScaleOverTime(rateOfChange, barFrac, bar.Height);
            else if (rateOfChange < 0)
                bar.ScaleOverTime(-1 * rateOfChange, barFrac, bar.Height);
        }

        public static void updateAmmoHud(Entity player, bool updateName, string newWeapon = "")
        {
            if (!player.HasField("hud_created") || (player.HasField("hud_created") && !player.GetField<bool>("hud_created")))
                return;

            HudElem ammoStock = player.GetField<HudElem>("hud_ammoStock");
            HudElem ammoClip = player.GetField<HudElem>("hud_ammoClip");
            HudElem equipment = player.GetField<HudElem>("hud_equipment");
            HudElem ammoBar = player.GetField<HudElem>("hud_weaponBar");
            string weapon = player.CurrentWeapon;
            if (newWeapon != "") weapon = newWeapon;

            //build grenades hud
            int lethalAmmoCount = 0;
            string grenade = "  ";
            if (player.HasWeapon("c4_mp"))
            {
                lethalAmmoCount = player.GetAmmoCount("c4_mp");
                string extraLethal = lethalAmmoCount > 1 ? createHudShaderString("hud_icon_c4", false, 48, 48) : "";
                grenade = lethalAmmoCount > 0 ? createHudShaderString("hud_icon_c4", false, 48, 48) + extraLethal + "" : "";
            }
            else if (player.HasWeapon("throwingknife_mp"))
            {
                lethalAmmoCount = player.GetAmmoCount("throwingknife_mp");
                grenade = lethalAmmoCount > 0 ? createHudShaderString("killiconimpale", false, 48, 48) + "" : "";
            }

            int tacticalAmmoCount = player.GetAmmoCount("flash_grenade_mp");
            string special = "";
            string extraTactical = tacticalAmmoCount > 1 ? createHudShaderString("hud_us_flashgrenade", false, 48, 48) : "   ";
            special = (player.HasWeapon("flash_grenade_mp") && tacticalAmmoCount > 0) ? createHudShaderString("hud_us_flashgrenade", false, 48, 48) + extraTactical + "    " : "         ";

            int ammoClipValue = player.GetWeaponAmmoClip(weapon);
            int ammoStockValue = player.GetWeaponAmmoStock(weapon);

            ammoStock.SetValue(ammoStockValue);
            ammoClip.SetValue(ammoClipValue);
            ammoStock.Alpha = 1;
            ammoClip.Alpha = 1;
            equipment.SetText(special + grenade);

            float ammoPercent = ammoClipValue / (float)WeaponClipSize(weapon);
            if (ammoPercent < .02f) ammoPercent = .02f;
            int maxWidth = (int)ammoBar.GetField("maxWidth");

            ammoBar.SetShader("clanlvl_box", (int)(ammoPercent * maxWidth), 24);

            if (updateName)
            {
                StartAsync(updateWeaponName(player, weapon));
                updateWeaponImage(player, weapon);
                updateWeaponLevel(player, weapon, true);
            }
        }
        public static IEnumerator updateWeaponName(Entity player, string weapon)
        {
            if (!player.HasField("hud_created")) yield break;
            HudElem weaponName = player.GetField<HudElem>("hud_weaponName");
            weaponName.FadeOverTime(0);
            weaponName.Alpha = 0;
            weaponName.SetText(getWeaponName(weapon));
            weaponName.MoveOverTime(0);
            weaponName.X = -240;
            weaponName.FadeOverTime(.5f);
            weaponName.Alpha = 1;
            weaponName.MoveOverTime(.5f);
            weaponName.X = -180;
            yield return Wait(1);
            weaponName.FadeOverTime(1);
            weaponName.Alpha = 0;
        }
        public static void updateWeaponImage(Entity player, string weapon)
        {
            HudElem weaponIcon = player.GetField<HudElem>("hud_weaponIcon");

            string[] tokens = weapon.Split('_');
            string baseWeapon = weapon;
            if (tokens[0] == "iw5") baseWeapon = tokens[0] + "_" + tokens[1] + "_" + tokens[2];
            else if (tokens[0] == "alt") baseWeapon = tokens[1] + "_" + tokens[2] + "_" + tokens[3];

            if (horde.weaponIcons.ContainsKey(baseWeapon))
            {
                if (baseWeapon != "iw5_p99_mp" && baseWeapon != "iw5_44magnum_mp") weaponIcon.SetText(createHudShaderString(horde.weaponIcons[baseWeapon], true, 64, 32));
                else weaponIcon.SetText(createHudShaderString(horde.weaponIcons[baseWeapon], true, 32, 32));
            }
            else weaponIcon.SetText("");
        }
        public static void updateWeaponLevel(Entity player, string weapon, bool updateLevelNumber = false)
        {
            string baseWeapon = getBaseWeaponName(weapon);
            HudElem weaponLevelBar = player.GetField<HudElem>("hud_weaponLevelBar");
            HudElem weaponLevelCounter = player.GetField<HudElem>("hud_weaponIcon").Children[0];

            if (!horde.weaponNames.ContainsKey(baseWeapon))
            {
                weaponLevelBar.ScaleOverTime(.3f, 0, 18);
                weaponLevelCounter.SetValue(0);
                return;
            }

            int weaponLevelValue = horde.weaponLevelValues[player.EntRef][baseWeapon];
            float percent = weaponLevelValue / (float)horde.maxWeaponLevelValue;
            if (percent < .05f) percent = .05f;
            weaponLevelBar.ScaleOverTime(.3f, (int)(percent * (int)weaponLevelBar.GetField("maxWidth")), 18);
            if (updateLevelNumber)
            {
                int weaponLevel = horde.weaponLevels[player.EntRef][baseWeapon];
                weaponLevelCounter.SetValue(weaponLevel);
            }
        }
        public static void updateSupportDropMeter()
        {
            HudElem supportBar = horde.supportDropMeter.Children[0];

            float percent = horde.supportDropMeterValue / (float)horde.maxSupportDropValue;
            if (percent < .03f) percent = .03f;
            supportBar.ScaleOverTime(.3f, (int)(percent * 152), 8);
        }
        public static void updateSupportDropValue(int value)
        {
            horde.supportDropMeterValue += value;
            updateSupportDropMeter();

            if (horde.supportDropMeterValue >= horde.maxSupportDropValue)
            {
                horde.doSupportDrop();
                horde.supportDropMeterValue = 0;
                if (horde.maxSupportDropValue < 1000)
                    horde.maxSupportDropValue += 20;
                updateSupportDropMeter();
            }
        }
        public static string getWeaponName(string weapon)
        {
            string baseWeapon = getBaseWeaponName(weapon);
            string attachments = getWeaponAttachments(weapon);

            if (horde.weaponNames.ContainsKey(baseWeapon))
                return horde.weaponNames[baseWeapon] + attachments;
            else if (baseWeapon.Split('_').Length > 1)
                return baseWeapon.Split('_')[1];
            else return weapon;
        }
        public static string getBaseWeaponName(string weapon)
        {
            if (!weapon.StartsWith("iw5_") && !weapon.StartsWith("alt_iw5_")) return weapon;

            string[] tokens = weapon.Split('_');
            string baseWeapon = tokens[0] + "_" + tokens[1] + "_" + tokens[2];
            if (tokens[0] == "alt") baseWeapon = tokens[1] + "_" + tokens[2] + "_" + tokens[3];

            return baseWeapon;
        }
        public static string getWeaponAttachments(string weapon)
        {
            //Utilities.PrintToConsole("Checking attachments for " + weapon);

            if (weapon.Split('_').Length < 4) return "";

            int attachmentIndex = weapon.IndexOf("_mp") + 4;
            string attachments = weapon.Substring(attachmentIndex);
            string attachmentName = "";

            if (attachments.Contains("scope_"))
            {
                string baseName = weapon.Split('_')[1];
                int scopeIndex = attachments.IndexOf(baseName + "scope_");
                int scopeLength = (baseName + "scope_").Length;
                attachments.Remove(scopeIndex, scopeLength);
            }

            if (attachments.Contains("_"))
                return " Custom";
            else
            {
                switch (attachments)
                {
                    case "reflex":
                    case "reflexsmg":
                    case "reflexlmg":
                        attachmentName = " Red Dot Sight";
                        break;
                    case "acog":
                    case "acoglmg":
                    case "acogsmg":
                        attachmentName = " ACOG Sight";
                        break;
                    case "grip":
                        attachmentName = " Foregrip";
                        break;
                    case "akimbo":
                        attachmentName = " Akimbo";
                        break;
                    case "shotgun":
                        attachmentName = " w/ Shotgun";
                        break;
                    case "heartbeat":
                        attachmentName = " Heartbeat Sensor";
                        break;
                    case "xmags":
                        attachmentName = " Extended Mags";
                        break;
                    case "rof":
                        attachmentName = " Rapid Fire";
                        break;
                    case "eotech":
                    case "eotechsmg":
                    case "eotechlmg":
                        attachmentName = " Holographic";
                        break;
                    case "tactical":
                        attachmentName = " Tactical Knife";
                        break;
                    case "gl":
                    case "gp25":
                    case "m320":
                        attachmentName = " Grenade Launcher";
                        break;
                    case "silencer":
                    case "silencer02":
                    case "silencer03":
                        attachmentName = " Silenced";
                        break;
                    default:
                        attachmentName = "";
                        break;
                }
                return attachmentName;
            }
        }
        #endregion

        private static bool slvrImposter(Entity player)
        {
            Utilities.ExecuteCommand("kickclient " + player.EntRef + " Please do not impersonate the developer.");
            return false;
        }
    }
}
