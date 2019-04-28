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
    public class roundUtil : BaseScript
    {
        private static bool isLootRound = false;
        public static int round = 1;
        private static int maxRounds = 20;

        public static void startIntermission()
        {
            horde.gameState = "intermission";
            foreach (Entity player in Players)
            {
                if (player.SessionState != "playing" || !player.IsAlive)
                {
                    horde.spawnPlayer(player);
                }
            }

            isLootRound = round == 1 || round % 5 == 0;
            if (isLootRound)
            {
                if (round != 1)
                {
                    round++;
                    horde.roundCounter.SetValue(round);
                }
                horde.spawnLoot();

                foreach (Entity players in Players)
                {
                    if (players.Classname != "player") continue;
                    if (players.SessionTeam != "allies") continue;

                    OnInterval(250, () => horde.trackLootForPlayer(players));
                }

                HudElem lootTimer = hordeUtils.createTimer(25, "LOOT ROUND");
                PlaySoundAtPos(Vector3.Zero, "mp_killstreak_carepackage");
                AfterDelay(25000, () =>
                {
                    lootTimer.FadeOverTime(.5f);
                    lootTimer.Alpha = 0;
                    AfterDelay(500, () =>
                    {
                        hordeUtils.destroyTimer(lootTimer);
                        hordeUtils.deleteAllLootCrates();
                    });
                    AfterDelay(5000, () => startNextRound());
                    horde.gameState = "ingame";
                    isLootRound = false;
                });
                return;
            }

            int time = 20;
            HudElem timer = hordeUtils.createTimer(time, "NEXT ROUND");

            AfterDelay(20000, () =>
            {
                timer.FadeOverTime(.5f);
                timer.Alpha = 0;
                AfterDelay(500, () => hordeUtils.destroyTimer(timer));
                AfterDelay(5000, () => startNextRound());
                horde.gameState = "ingame";
                return;
            });
        }
        public static void startNextRound()
        {
            checkForEndGame();//Before we start, make sure there are players to start
            round++;
            horde.roundCounter.SetValue(round);
            bots.spawnedBots = 0;
            bots.botsForWave += 1;

            string[] botTypes = bots.botHealth.Keys.ToArray();
            for (int i = 0; i < bots.botHealth.Count; i++)
                bots.botHealth[botTypes[i]] += 10;

            startBotSpawn();
            horde.gameState = "ingame";

            //Clear the loot locations
            lootCrateLocations.clearLootLocationFlags();

            //if (isLootRound) return;

            foreach (Entity players in Players)
            {
                if (players.IsPlayer && players.HasField("isDown"))
                {
                    players.PlayLocalSound("mp_bonus_end");
                    int randomStart = RandomInt(6);
                    switch (randomStart)
                    {
                        case 0:
                            players.PlayLocalSound("US_1mc_fightback");
                            break;
                        case 1:
                            players.PlayLocalSound("US_1mc_goodtogo");
                            break;
                        case 2:
                            players.PlayLocalSound("US_1mc_holddown");
                            break;
                        case 3:
                            players.PlayLocalSound("US_1mc_keepfighting");
                            break;
                        case 4:
                            players.PlayLocalSound("US_1mc_pushforward");
                            break;
                        case 5:
                            players.PlayLocalSound("US_1mc_readytomove");
                            break;
                    }
                }
            }
        }

        public static void startBotSpawn()
        {
            OnInterval(1000 + RandomIntRange(1000, 4000), () =>
            {
                if (horde.gameEnded) return false;

                if (bots.botsInPlay.Count >= 15)
                    return true;
                else if (bots.spawnedBots == bots.botsForWave)
                    return false;
                else
                    return bots.respawnBot(bots.getRandomBotType());

            });
        }

        public static void checkForEndGame()
        {
            int playersAlive = GetTeamPlayersAlive("allies");
            if (playersAlive == 0)
                StartAsync(endGame(false));
        }

        public static void checkForEndRound()
        {
            if (bots.botsInPlay.Count == 0 && bots.botsForWave == bots.spawnedBots)
            {
                if (round == maxRounds)
                {
                    horde.gameState = "ended";
                    StartAsync(endGame(true));
                    return;
                }
                AfterDelay(100, () => startIntermission());
                foreach (Entity players in Players)
                {
                    if (players.Classname == "player")
                    {
                        players.PlayLocalSound("mp_bonus_start");
                        players.PlayLocalSound("US_1mc_encourage_win");
                    }
                }
                AfterDelay(2000, bots.clearAllRagdolls);
            }
        }

        private static IEnumerator endGame(bool win)
        {
            horde.gameEnded = true;
            HudElem[] endGameScreen = createEndGameScreen(win, "");
            MakeDvarServerInfo("scr_gameended", "1");
            Notify("game_over");
            Notify("game_ended");
            if (!win)
                Notify("game_win", "axis");
            else
                Notify("game_win", "allies");

            foreach (Entity players in Players)
            {
                players.VisionSetNakedForPlayer("mpoutro", 1);
            }

            yield return Wait(1);

            foreach (Entity players in Players)
            {
                players.FreezeControls(true);
            }

            yield return Wait(6);

            foreach (Entity players in Players)
            {
                Entity intermission = GetEnt("mp_global_intermission", "classname");
                players.SessionState = "spectator";
                players.SetOrigin(intermission.Origin);
                players.SetPlayerAngles(intermission.Angles);
            }

            yield return Wait(8);

            Utilities.ExecuteCommand("map_rotate");
        }
        private static HudElem[] createEndGameScreen(bool win, string endText)
        {
            HudElem outcomeTitle = HudElem.CreateServerFontString(HudElem.Fonts.HudBig, 1.5f);
            outcomeTitle.SetPoint("CENTER", "", 0, -134);
            outcomeTitle.Foreground = true;
            outcomeTitle.GlowAlpha = 1;
            outcomeTitle.HideWhenInMenu = false;
            outcomeTitle.Archived = false;

            HudElem outcomeText = HudElem.CreateServerFontString(HudElem.Fonts.HudBig, 1);
            outcomeText.Parent = outcomeTitle;
            outcomeText.Foreground = true;
            outcomeText.SetPoint("TOP", "BOTTOM", 0, 18);
            outcomeText.GlowAlpha = 1;
            outcomeText.HideWhenInMenu = false;
            outcomeText.Archived = false;

            outcomeTitle.GlowColor = new Vector3(0, 0, 0);
            if (win)
            {
                outcomeTitle.SetText("Victory!");
                outcomeTitle.Color = new Vector3(.3f, .7f, .2f);
            }
            else
            {
                outcomeTitle.SetText("Defeat!");
                outcomeTitle.Color = new Vector3(.7f, .3f, .2f);
            }
            outcomeText.GlowColor = new Vector3(.2f, .3f, .7f);
            outcomeText.SetText(endText);
            outcomeTitle.SetPulseFX(100, 60000, 1000);
            outcomeText.SetPulseFX(100, 60000, 1000);

            HudElem leftIcon = NewHudElem();
            string alliesTeam = GetMapCustom("allieschar");
            string icon_allies = TableLookup("mp/factionTable.csv", 0, alliesTeam, 1);
            leftIcon.SetShader(icon_allies, 70, 70);
            leftIcon.Parent = outcomeText;
            leftIcon.SetPoint("TOP", "BOTTOM", -60, 45);
            //leftIcon.SetShader("cardicon_soap", 70, 70);
            leftIcon.Foreground = true;
            leftIcon.HideWhenInMenu = false;
            leftIcon.Archived = false;
            leftIcon.Alpha = 0;
            leftIcon.FadeOverTime(.5f);
            leftIcon.Alpha = 1;

            HudElem rightIcon = NewHudElem();
            string axisTeam = GetMapCustom("axischar");
            string icon_axis = TableLookup("mp/factionTable.csv", 0, axisTeam, 1);
            rightIcon.SetShader(icon_axis, 70, 70);
            rightIcon.Parent = outcomeText;
            rightIcon.SetPoint("TOP", "BOTTOM", 60, 45);
            //rightIcon.SetShader("cardicon_nuke", 70, 70);
            rightIcon.Foreground = true;
            rightIcon.HideWhenInMenu = false;
            rightIcon.Archived = false;
            rightIcon.Alpha = 0;
            rightIcon.FadeOverTime(.5f);
            rightIcon.Alpha = 1;

            HudElem leftScore = HudElem.CreateServerFontString(HudElem.Fonts.HudBig, 1.25f);
            leftScore.Parent = leftIcon;
            leftScore.SetPoint("TOP", "BOTTOM", 0, 0);
            if (win)
            {
                leftScore.GlowColor = new Vector3(.2f, .8f, .2f);
                leftScore.SetText("Win");
            }
            else
            {
                leftScore.GlowColor = new Vector3(.8f, .2f, .2f);
                leftScore.SetText("Lose");
            }
            leftScore.GlowAlpha = 1;
            leftScore.Foreground = true;
            leftScore.HideWhenInMenu = false;
            leftScore.Archived = false;
            leftScore.SetPulseFX(100, 60000, 1000);

            HudElem rightScore = HudElem.CreateServerFontString(HudElem.Fonts.HudBig, 1.25f);
            rightScore.Parent = rightIcon;
            rightScore.SetPoint("TOP", "BOTTOM", 0);
            rightScore.GlowAlpha = 1;
            if (!win)
            {
                rightScore.GlowColor = new Vector3(.2f, .8f, .2f);
                rightScore.SetText("Win");
            }
            else
            {
                rightScore.GlowColor = new Vector3(.8f, .2f, .2f);
                rightScore.SetText("Lose");
            }
            rightScore.Foreground = true;
            rightScore.HideWhenInMenu = false;
            rightScore.Archived = false;
            rightScore.SetPulseFX(100, 60000, 1000);

            return new HudElem[] { outcomeTitle, outcomeText, rightScore, leftScore, rightIcon, leftIcon };
        }
    }

    public static class lootCrateLocations
    {
        public static Dictionary<Vector3, bool> lootLocations = new Dictionary<Vector3, bool>();
        public static void initlootLocations()
        {
            switch (horde._mapname)
            {
                case "mp_dome":
                    lootLocations.Add(new Vector3(97, 898, -240), false);
                    lootLocations.Add(new Vector3(-226, 1464, -231), false);
                    lootLocations.Add(new Vector3(-603, 194, -358), false);
                    lootLocations.Add(new Vector3(814, -406, -335), false);
                    lootLocations.Add(new Vector3(5, 1975, -231), false);
                    lootLocations.Add(new Vector3(-673, 1100, -284), false);
                    lootLocations.Add(new Vector3(669, 1028, -255), false);
                    lootLocations.Add(new Vector3(1231, 807, -267), false);
                    lootLocations.Add(new Vector3(709, 210, -342), false);
                    lootLocations.Add(new Vector3(1223, 10, -336), false);
                    lootLocations.Add(new Vector3(-222, 418, -333), false);
                    lootLocations.Add(new Vector3(501, -183, -330), false);
                    break;
                case "mp_plaza2":
                    lootLocations.Add(new Vector3(221, 440, 754), false);
                    lootLocations.Add(new Vector3(155, 1763, 668), false);
                    lootLocations.Add(new Vector3(-430, 1871, 691), false);
                    lootLocations.Add(new Vector3(-1190, 1759, 668), false);
                    lootLocations.Add(new Vector3(-1273, 1279, 829), false);
                    lootLocations.Add(new Vector3(-593, 1274, 676), false);
                    lootLocations.Add(new Vector3(-251, 1006, 722), false);
                    lootLocations.Add(new Vector3(80, 1343, 676), false);
                    lootLocations.Add(new Vector3(397, -99, 708), false);
                    lootLocations.Add(new Vector3(-1109, 92, 741), false);
                    lootLocations.Add(new Vector3(-280, -195, 700), false);
                    lootLocations.Add(new Vector3(28, -1600, 668), false);
                    lootLocations.Add(new Vector3(764, -1752, 669), false);
                    break;
                case "mp_mogadishu":
                    lootLocations.Add(new Vector3(1448, 1945, 39), false);
                    lootLocations.Add(new Vector3(1499, -1193, 15), false);
                    lootLocations.Add(new Vector3(791, -880, 16), false);
                    lootLocations.Add(new Vector3(38, -1007, 16), false);
                    lootLocations.Add(new Vector3(-691, -260, 22), false);
                    lootLocations.Add(new Vector3(2, 52, 2), false);
                    lootLocations.Add(new Vector3(664, 69, 12), false);
                    lootLocations.Add(new Vector3(1676, 251, -1), false);
                    lootLocations.Add(new Vector3(2314, 1860, 63), false);
                    lootLocations.Add(new Vector3(73, 858, 3), false);
                    lootLocations.Add(new Vector3(710, 837, 16), false);
                    lootLocations.Add(new Vector3(-549, 829, 2), false);
                    lootLocations.Add(new Vector3(34, 1850, 84), false);
                    lootLocations.Add(new Vector3(-778, 2614, 157), false);
                    lootLocations.Add(new Vector3(-204, 3206, 152), false);
                    lootLocations.Add(new Vector3(752, 3189, 148), false);
                    lootLocations.Add(new Vector3(692, 2354, 95), false);
                    break;
                case "mp_paris":
                    lootLocations.Add(new Vector3(-931, -921, 110), false);
                    lootLocations.Add(new Vector3(1597, 1768, 47), false);
                    lootLocations.Add(new Vector3(716, 1809, 33), false);
                    lootLocations.Add(new Vector3(258, 2074, 36), false);
                    lootLocations.Add(new Vector3(459, 1067, 37), false);
                    lootLocations.Add(new Vector3(852, 1350, 118), false);
                    lootLocations.Add(new Vector3(1601, 897, 45), false);
                    lootLocations.Add(new Vector3(1286, 420, 41), false);
                    lootLocations.Add(new Vector3(1613, 181, 172), false);
                    lootLocations.Add(new Vector3(466, -752, 67), false);
                    lootLocations.Add(new Vector3(994, -625, 50), false);
                    lootLocations.Add(new Vector3(-211, -60, 63), false);
                    lootLocations.Add(new Vector3(-742, 177, 133), false);
                    lootLocations.Add(new Vector3(-1532, 100, 250), false);
                    lootLocations.Add(new Vector3(-343, 1922, 121), false);
                    lootLocations.Add(new Vector3(-1127, 1555, 284), false);
                    lootLocations.Add(new Vector3(-2025, 1327, 316), false);
                    lootLocations.Add(new Vector3(-1039, 841, 187), false);
                    break;
                case "mp_exchange":
                    lootLocations.Add(new Vector3(-614, 1286, 113), false);
                    lootLocations.Add(new Vector3(182, 1155, 148), false);
                    lootLocations.Add(new Vector3(1018, 1254, 120), false);
                    lootLocations.Add(new Vector3(2182, 1322, 145), false);
                    lootLocations.Add(new Vector3(655, 815, 13), false);
                    lootLocations.Add(new Vector3(761, -312, -18), false);
                    lootLocations.Add(new Vector3(761, -771, 112), false);
                    lootLocations.Add(new Vector3(635, -1450, 110), false);
                    lootLocations.Add(new Vector3(152, -1538, 96), false);
                    lootLocations.Add(new Vector3(303, -824, 88), false);
                    lootLocations.Add(new Vector3(-953, -768, 45), false);
                    lootLocations.Add(new Vector3(2392, 1305, 144), false);
                    lootLocations.Add(new Vector3(1634, 1329, 151), false);
                    lootLocations.Add(new Vector3(1315, 743, 159), false);
                    break;
                case "mp_hardhat":
                    lootLocations.Add(new Vector3(2035, -229, 246), false);
                    lootLocations.Add(new Vector3(1959, -772, 352), false);
                    lootLocations.Add(new Vector3(1883, -1384, 351), false);
                    lootLocations.Add(new Vector3(848, -1520, 334), false);
                    lootLocations.Add(new Vector3(1326, -1380, 342), false);
                    lootLocations.Add(new Vector3(-338, -1273, 348), false);
                    lootLocations.Add(new Vector3(-821, -884, 348), false);
                    lootLocations.Add(new Vector3(-920, -290, 230), false);
                    lootLocations.Add(new Vector3(-463, -250, 333), false);
                    lootLocations.Add(new Vector3(-741, 208, 245), false);
                    lootLocations.Add(new Vector3(-201, 806, 437), false);
                    lootLocations.Add(new Vector3(224, 980, 436), false);
                    lootLocations.Add(new Vector3(1125, 656, 255), false);
                    lootLocations.Add(new Vector3(1531, 1241, 364), false);
                    lootLocations.Add(new Vector3(1522, 542, 244), false);
                    break;
                case "mp_lambeth":
                    lootLocations.Add(new Vector3(-293, -1286, -180), false);
                    lootLocations.Add(new Vector3(-938, -785, -130), false);
                    lootLocations.Add(new Vector3(-375, -250, -187), false);
                    lootLocations.Add(new Vector3(-355, 409, -196), false);
                    lootLocations.Add(new Vector3(161, -5, -181), false);
                    lootLocations.Add(new Vector3(682, -407, -197), false);
                    lootLocations.Add(new Vector3(694, 263, -196), false);
                    lootLocations.Add(new Vector3(690, 1158, -243), false);
                    lootLocations.Add(new Vector3(1181, 801, -67), false);
                    lootLocations.Add(new Vector3(1281, 1248, -257), false);
                    lootLocations.Add(new Vector3(2057, 757, -249), false);
                    lootLocations.Add(new Vector3(1470, -1040, -109), false);
                    lootLocations.Add(new Vector3(1761, -258, -210), false);
                    lootLocations.Add(new Vector3(2800, -652, -186), false);
                    lootLocations.Add(new Vector3(2785, 445, -244), false);
                    lootLocations.Add(new Vector3(2751, 1090, -263), false);
                    lootLocations.Add(new Vector3(1535, 1980, -214), false);
                    lootLocations.Add(new Vector3(1262, 2602, -213), false);
                    lootLocations.Add(new Vector3(419, 2218, -183), false);
                    lootLocations.Add(new Vector3(170, 1631, -182), false);
                    lootLocations.Add(new Vector3(-606, 1549, -201), false);
                    lootLocations.Add(new Vector3(-1199, 1030, -196), false);
                    break;
                case "mp_radar":
                    lootLocations.Add(new Vector3(-3482, -498, 1222), false);
                    lootLocations.Add(new Vector3(-4263, -124, 1229), false);
                    lootLocations.Add(new Vector3(-4006, 827, 1238), false);
                    lootLocations.Add(new Vector3(-3375, 342, 1222), false);
                    lootLocations.Add(new Vector3(-4623, 531, 1298), false);
                    lootLocations.Add(new Vector3(-5157, 877, 1200), false);
                    lootLocations.Add(new Vector3(-5950, 1071, 1305), false);
                    lootLocations.Add(new Vector3(-6509, 1660, 1299), false);
                    lootLocations.Add(new Vector3(-7013, 2955, 1359), false);
                    lootLocations.Add(new Vector3(-6333, 3473, 1421), false);
                    lootLocations.Add(new Vector3(-5675, 2923, 1388), false);
                    lootLocations.Add(new Vector3(-7119, 4357, 1380), false);
                    lootLocations.Add(new Vector3(-5487, 4077, 1356), false);
                    lootLocations.Add(new Vector3(-5736, 2960, 1407), false);
                    lootLocations.Add(new Vector3(-4908, 3281, 1225), false);
                    lootLocations.Add(new Vector3(-4421, 4071, 1268), false);
                    lootLocations.Add(new Vector3(-4979, 1816, 1205), false);
                    lootLocations.Add(new Vector3(-4874, 2306, 1223), false);
                    break;
            }
        }

        public static Vector3 getRandomLootSpawn()
        {
            int random = RandomInt(lootLocations.Count);
            Vector3 ret;
            Vector3[] keys = lootLocations.Keys.ToArray();

            ret = keys[random];

            if (!lootLocations[ret])
            {
                lootLocations[ret] = true;
                return ret;
            }
            else return getRandomLootSpawn();
        }

        public static void clearLootLocationFlags()
        {
            Vector3[] keys = lootLocations.Keys.ToArray();
            for (int i = 0; i < lootLocations.Count; i++)
            {
                lootLocations[keys[i]] = false;
            }
        }
    }
}
