﻿using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using Heroes.ReplayParser;
using Foole.Mpq;
using System.Text;

/*
 * Edited base hotslogs c# example application for parsing hots replays by outputting to my own json structure
 * Maxim Tiourin
 */
namespace ConsoleApplication1
{
    class Program
    {
        private static StringBuilder sb = new StringBuilder();

        //Json QoL vars
        private static char _o = '{';
        private static char o_ = '}';
        private static char _a = '[';
        private static char a_ = ']';
        private static char c = ',';
        private static char e = '"';
        private static char k = ':';

        /*
         * Args: executionPath filepathToReplay
         */
        static void Main(string[] args)
        {
            int argc = args.Length;
            //int argexpect = (isWindowsOS()) ? (1) : (2);
            int argexpect = 1;

            if (argc != argexpect) {
                Console.WriteLine("{\"error\": \"incorrect amount of arguments expect " + argexpect + "\"}");
                return;
            }

            string filepath = args[argexpect - 1];

            if (File.Exists(filepath)) {
                //var heroesAccountsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"Heroes of the Storm\Accounts");
                //var randomReplayFileName = Directory.GetFiles(heroesAccountsFolder, "*.StormReplay", SearchOption.AllDirectories).OrderBy(i => Guid.NewGuid()).First();

                // Use temp directory for MpqLib directory permissions requirements
                var tmpPath = Path.GetTempFileName();
                File.Copy(filepath, tmpPath, overwrite: true);

                try {
                    // Attempt to parse the replay
                    // Ignore errors can be set to true if you want to attempt to parse currently unsupported replays, such as 'VS AI' or 'PTR Region' replays
                    var replayParseResult = DataParser.ParseReplay(tmpPath, ignoreErrors: false, deleteFile: false, allowPTRRegion: false, detailedBattleLobbyParsing: true);

                    // If successful, the Replay object now has all currently available information
                    if (replayParseResult.Item1 == DataParser.ReplayParseResult.Success) {
                        var replay = replayParseResult.Item2;

                        const int TEAMS = 2;

                        bool isRegionSet = false;
                        bool isWinnerSet = false;
                        int region = 1;
                        int winner = 0;

                        //Begin JSON
                        s(startObj());

                        //Game Mode
                        string mode = getGameMode(replay.GameMode);
                        if (!ensureValidGameMode(mode, replay.GameMode)) return;
                        s(keystr("type", mode));

                        //Date "2017-08-27 13:43:09" "yyyy-mm-dd hh:mm:ss"
                        s(keystr("date", getDateFormat(replay.Timestamp)));

                        //Match Length
                        s(keynum("match_length", (int) Math.Ceiling(replay.ReplayLength.TotalSeconds)));

                        //Map
                        s(keystr("map", replay.Map));

                        //Version
                        s(keystr("version", getVersionFormat(replay.ReplayVersion, replay.ReplayBuild)));

                        //Players
                        //Players party determination
                        Dictionary<long, Tuple<int, List<Player>>> party = new Dictionary<long, Tuple<int, List<Player>>>();
                        int partyIndex = 1;
                        foreach (var player in replay.Players.OrderByDescending(i => i.IsWinner)) {
                            long partyval = player.PartyValue;

                            if (partyval != 0) {
                                if (!party.ContainsKey(partyval)) {
                                    party[partyval] = new Tuple<int, List<Player>>(partyIndex++, new List<Player>());
                                }

                                party[partyval].Item2.Add(player);
                            }
                        }

                        //Actual players object construction
                        int playerCount = replay.Players.Length;
                        int p = 0;
                        s(startArr("players"));
                        foreach (var player in replay.Players.OrderByDescending(i => i.IsWinner)) {
                            ScoreResult stats = player.ScoreResult;

                            //Start Player
                            s(startObj());

                            if (!isRegionSet) {
                                region = player.BattleNetRegionId;
                                if (!ensureValidRegion(region)) return;
                            }
                            if (!isWinnerSet) {
                                if (player.IsWinner) {
                                    winner = player.Team;
                                }
                            }

                            //Blizz_id
                            s(keynum("id", player.BattleNetId));

                            //Blizz_name
                            s(keystr("name", player.Name));

                            //Blizz_tag
                            s(keynum("tag", player.BattleTag));

                            //Team
                            s(keynum("team", player.Team));

                            //Hero
                            s(keystr("hero", player.Character));

                            //Hero Level
                            s(keynum("hero_level", player.CharacterLevel));

                            //Account level
                            s(keynum("account_level", player.AccountLevel));

                            //Silenced status
                            s(keynum("silenced", (player.IsSilenced) ? (1) : (0)));

                            //Party array
                            s(startArr("party"));

                            if (player.PartyValue != 0) {
                                List<Player> partyplayers = party[player.PartyValue].Item2;
                                int currPartyIndex = party[player.PartyValue].Item1;
                                if (partyplayers != null) {
                                    List<Player> otherpartyplayers = partyplayers.Where(i => i.BattleNetId != player.BattleNetId).ToList();
                                    int othercount = otherpartyplayers.Count;
                                    int oc = 0;
                                    foreach (var partyplayer in otherpartyplayers) {
                                        s(startObj());

                                        s(keynum("party_id", currPartyIndex));

                                        s(keystr("name", partyplayer.Name, false));

                                        s(endObj(oc < othercount - 1));

                                        oc++;
                                    }
                                }
                            }

                            s(endArr());

                            //Stats object
                            s(startObj("stats"));

                            //In-game level
                            //s(keynum("level", stats.Level));

                            //Kills
                            s(keynum("kills", stats.SoloKills));

                            //Assists
                            s(keynum("assists", stats.Assists));

                            //Deaths
                            s(keynum("deaths", stats.Deaths));

                            //Siege Damage
                            s(keynum("siege_damage", stats.SiegeDamage));

                            //Hero Damage
                            s(keynum("hero_damage", stats.HeroDamage));

                            //Creep Damage
                            //s(keynum("creep_damage", stats.CreepDamage));

                            //Structure Damage
                            s(keynum("structure_damage", stats.StructureDamage));

                            //Summon Damage
                            //s(keynum("summon_damage", stats.SummonDamage));

                            //Minion Damage
                            //s(keynum("minion_damage", stats.MinionDamage));

                            //Healing
                            s(keynum("healing", (stats.Healing.HasValue) ? (stats.Healing.Value) : (0)));

                            //Self Healing
                            //s(keynum("self_healing", stats.SelfHealing));

                            //Damage Taken
                            s(keynum("damage_taken", (stats.DamageTaken.HasValue) ? (stats.DamageTaken.Value) : (0)));

                            //Merc Camps
                            s(keynum("merc_camps", stats.MercCampCaptures));

                            //Experience Contribution
                            s(keynum("exp_contrib", stats.ExperienceContribution));

                            //Meta Experience
                            //s(keynum("exp_meta", stats.MetaExperience));

                            //Best Killstreak
                            s(keynum("best_killstreak", stats.HighestKillStreak));

                            //Timed CCd enemies
                            /*s(keynum("time_ccd_enemies", 
                                (stats.TimeCCdEnemyHeroes.HasValue) 
                                ? ((int) Math.Ceiling(stats.TimeCCdEnemyHeroes.Value.TotalSeconds)) 
                                : (0)));*/

                            //Time Spent Dead
                            s(keynum("time_spent_dead", (int) Math.Ceiling(stats.TimeSpentDead.TotalSeconds)));

                            //Town Kills
                            //s(keynum("town_kills", stats.TownKills));

                            //Watchtowers Captured
                            //s(keynum("watchtowers_captured", stats.WatchTowerCaptures));

                            //Accolades
                            s(startArr("medals"));

                            int macount = stats.MatchAwards.Count;
                            int ma = 0;
                            foreach (var medal in stats.MatchAwards) {
                                s(str(medal + ""));

                                s(seperate(ma < macount - 1));

                                ma++;
                            }

                            s(endArr(false));

                            s(endObj());

                            //Talents
                            int talentCount = player.Talents.Length;
                            int t = 0;
                            s(startArr("talents"));
                            foreach (var talent in player.Talents.OrderBy(j => j.TalentID).Where(n => n.TalentName != null && n.TalentName.Length > 0)) {
                                //Id : Indexing for a hero starts at 0, and then increments top down, 
                                //     left right, as moving through talents
                                //s(talent.TalentID + "");
                                s(str(talent.TalentName));

                                s(seperate(t < talentCount - 1));

                                t++;
                            }
                            s(endArr(false)); //No comma termination for talents object

                            //End Player (Make sure not to add comma after last player object)
                            s(endObj(p < playerCount - 1));

                            p++;
                        }
                        s(endArr());

                        //Hero bans
                        s(startObj("bans"));

                        for (int team = 0; team < TEAMS; team++) {
                            s(startArr(team + ""));

                            if (replay.TeamHeroBans[team].Length > 0) {
                                List<string> bans = replay.TeamHeroBans[team].Where(j => j != null && j.Length > 0).ToList();

                                int banCount = bans.Count;
                                int bx = 0;
                                foreach (var b in bans) {
                                    s(str(b));

                                    s(seperate(bx < banCount - 1));

                                    bx++;
                                }
                            }

                            s(endArr(team < TEAMS - 1));
                        }

                        s(endObj());

                        //Team Xp sample points
                        //s(startObj("exp_samples"));

                        int[] team_level = new int[2];

                        for (int team = 0; team < TEAMS; team++) {
                            //s(startArr(team + ""));

                            int xpCount = replay.TeamPeriodicXPBreakdown[team].Count;
                            int x = 0;
                            foreach (var exp in replay.TeamPeriodicXPBreakdown[team]) {
                                //s(startObj());

                                //Time
                                //s(keynum("time", (int) Math.Ceiling(exp.TimeSpan.TotalSeconds)));

                                //Team Level
                                //s(keynum("level", exp.TeamLevel));

                                //Hero Exp
                                //s(keynum("hero_exp", exp.HeroXP));

                                //Soak Exp (Camps, minions, trickling)
                                //s(keynum("soak_exp", exp.CreepXP + exp.MinionXP + exp.TrickleXP));

                                //Structure Exp
                                //s(keynum("structure_exp", exp.StructureXP, false));

                                //Total Exp
                                //s(keynum("total_exp", exp.TotalXP, false));

                                //Set team level
                                if (x == xpCount - 1) {
                                    team_level[team] = exp.TeamLevel;
                                }

                                //s(endObj(x < xpCount - 1));

                                x++;
                            }

                            //s(endArr(team < TEAMS - 1));
                        }

                        //s(endObj());

                        //Region
                        s(keynum("region", region));

                        //Team level
                        s(startObj("team_level"));

                        s(keynum("0", team_level[0]));

                        s(keynum("1", team_level[1], false));

                        s(endObj());

                        //Winner
                        s(keynum("winner", winner, false));

                        s(endObj(false));

                        //TODO DEBUG file output
                        //System.IO.File.WriteAllText(@"test/output.json", sb.ToString());

                        //Output JSON
                        Console.OutputEncoding = Encoding.UTF8;
                        Console.WriteLine(sb);

                        //Console.Read(); // TODO DEBUG just to halt closing of cmdline window, use only for testing
                    }
                    else {
                        Console.WriteLine("{\"error\": \"replay can't be parsed\"}");
                    }
                }
                finally {
                    if (File.Exists(tmpPath))
                        File.Delete(tmpPath);
                }
            }
            else {
                Console.WriteLine("{\"error\": \"no file at given path\"}");
            }
        }

        /*
         * If the supplied region is not valid, signals to halt execution and writes a json structure with an error field
         * 1=US, 2=EU, 3=KR, 5=CN
         */
        private static bool ensureValidRegion(int region) {
            int[] validRegions = new int[] { 1, 2, 3, 5 };
            if (!validRegions.Contains(region)) {
                Console.WriteLine("{\"error\": \"Invalid region: " + region + "\"}");
                return false;
            }

            return true;
        }

        /*
         * If the supplied game mode is not valid, signals to halt execution and writes a json structure with an error field
         */
        private static bool ensureValidGameMode(string mode, GameMode type) {
            if (mode.Equals("Irrelevant")) {
                Console.WriteLine("{\"error\": \"Invalid game mode: " + type.ToString() + "\"}");
                return false;
            }

            return true;
        }

        private static void s(char v) {
            sb.Append(v);
        }

        private static void s(string v) {
            sb.Append(v);
        }

        private static string str(string v) {
            return e + v + e;
        }

        private static string seperate(bool append = true) {
            if (append) return c + "";
            else return "";
        }

        private static string keystr(string key, string v, bool commaTerminate = true) {
            return str(key) + k + str(v) + seperate(commaTerminate);
        }

        private static string keynum(string key, int v, bool commaTerminate = true) {
            return str(key) + k + v + seperate(commaTerminate);
        }

        private static string keynum(string key, long v, bool commaTerminate = true) {
            return str(key) + k + v + seperate(commaTerminate);
        }

        private static string keynum(string key, float v, bool commaTerminate = true) {
            return str(key) + k + v + seperate(commaTerminate);
        }

        private static string startObj() {
            return _o + "";
        }

        private static string startObj(string key) {
            return str(key) + k + _o;
        }

        private static string endObj(bool commaTerminate = true) {
            return o_ + seperate(commaTerminate);
        }

        private static string startArr(string key) {
            return str(key) + k + _a;
        }

        private static string endArr(bool commaTerminate = true) {
            return a_ + seperate(commaTerminate);
        }

        private static string getGameMode(GameMode v) {
            switch (v) {
                case GameMode.HeroLeague:
                    return "Hero League";
                case GameMode.TeamLeague:
                    return "Team League";
                case GameMode.UnrankedDraft:
                    return "Unranked Draft";
                case GameMode.QuickMatch:
                    return "Quick Match";
                default:
                    return "Irrelevant";
            }
        }

        private static float getDeathValueForHero(string hero) {
            if (hero.Equals("Murky")) {
                return .25f;
            }
            else {
                return 1f;
            }
        }

        private static List<T> filterToList<T>(IEnumerable<T> e, Func<T, bool> predicate) {
            List<T> list = new List<T>();
            foreach (var n in e) {
                if (predicate(n)) list.Add(n);
            }
            return list;
        }

        // Pads Left 0's onto number after converting to string, for use with date formatting
        private static string pad(int v, int c) {
            return (v + "").PadLeft(c, '0');
        }

        private static string getDateFormat(DateTime m) {
            return pad(m.Year, 4) + "-" + pad(m.Month, 2) + "-" + pad(m.Day, 2) 
                + " " + pad(m.Hour, 2) + ":" + pad(m.Minute, 2) + ":" + pad(m.Second, 2);
        }

        private static string getVersionFormat(string v, int build) {
            return v.Substring(2) + "." + build;
        }

        private static bool isWindowsOS() {
            switch (Environment.OSVersion.Platform) {
                case PlatformID.WinCE:
                    return true;
                case PlatformID.Win32Windows:
                    return true;
                case PlatformID.Win32S:
                    return true;
                case PlatformID.Win32NT:
                    return true;
                default:
                    return false;
            }
        }

        private static byte[] GetMpqArchiveFileBytes(MpqArchive archive, string fileName)
        {
            using (var mpqStream = archive.OpenFile(archive.Single(i => i.Filename == fileName)))
            {
                var buffer = new byte[mpqStream.Length];
                mpqStream.Read(buffer, 0, buffer.Length);
                return buffer;
            }
        }
    }
}
