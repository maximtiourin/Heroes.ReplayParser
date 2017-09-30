using System.IO;
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
    /*class HeroStats {
        public string Name;
        public float Kills;
        public float Deaths;

        public HeroStats(string name) {
            Name = name;
            Kills = 0f;
            Deaths = 0f;
        }

        public void addKill(float val) {
            Kills += val;
        }

        public void addDeath(float val) {
            Deaths += val;
        }
    }*/

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
            int argexpect = (isWindowsOS()) ? (1) : (2);

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
                    var replayParseResult = DataParser.ParseReplay(tmpPath, ignoreErrors: false, deleteFile: false);

                    // If successful, the Replay object now has all currently available information
                    if (replayParseResult.Item1 == DataParser.ReplayParseResult.Success) {
                        var replay = replayParseResult.Item2;

                        const int TEAMS = 2;

                        bool isRegionSet = false;
                        bool isWinnerSet = false;
                        int region = 1;
                        int winner = 0;

                        //Init hero stats
                        /*Dictionary<int, Dictionary<string, HeroStats>> herostats = new Dictionary<int, Dictionary<string, HeroStats>>();
                        herostats[0] = new Dictionary<string, HeroStats>();
                        herostats[1] = new Dictionary<string, HeroStats>();*/

                        //Begin JSON
                        s(startObj());

                        //Game Mode
                        s(keystr("type", getGameMode(replay.GameMode)));

                        //Date "2017-08-27 13:43:09" "yyyy-mm-dd hh:mm:ss"
                        s(keystr("date", getDateFormat(replay.Timestamp)));

                        //Match Length
                        s(keynum("match_length", (int) Math.Ceiling(replay.ReplayLength.TotalSeconds)));

                        //Map
                        s(keystr("map", replay.Map));

                        //Version
                        s(keystr("version", getVersionFormat(replay.ReplayVersion, replay.ReplayBuild)));

                        //Calculate HeroStats
                        /*List<Unit> heroUnits = replay.Units.Where(unit => unit.Group == Unit.UnitGroup.Hero).ToList();

                        foreach (var unit in heroUnits) {
                            string heroname = unit.PlayerControlledBy.Character;

                            if (!herostats[unit.PlayerControlledBy.Team].ContainsKey(heroname)) {
                                herostats[unit.PlayerControlledBy.Team][heroname] = new HeroStats(heroname);
                            }

                            HeroStats stats = herostats[unit.PlayerControlledBy.Team][heroname];

                            Point death = unit.PointDied;

                            if (death != null) {
                                Player killerplayer = unit.PlayerKilledBy;
                                if (killerplayer != null) {
                                    if (unit.PlayerControlledBy != killerplayer) {
                                        stats.addDeath(getDeathValueForHero(heroname));

                                        if (killerplayer.PlayerType == PlayerType.Human) {
                                            string killerhero = killerplayer.Character;

                                            if (!herostats[killerplayer.Team].ContainsKey(killerhero)) {
                                                herostats[killerplayer.Team][killerhero] = new HeroStats(killerhero);
                                            }

                                            HeroStats killerStats = herostats[killerplayer.Team][killerhero];

                                            killerStats.addKill(getDeathValueForHero(heroname));
                                        }
                                    }
                                }
                                else {
                                    stats.addDeath(getDeathValueForHero(heroname));
                                }
                            }
                        }

                        //Herostats debug
                        int heroCount = heroUnits.Count;
                        int u = 0;
                        s(startArr("herostats_debug"));
                        foreach (var unit in heroUnits) {
                            Player player = unit.PlayerControlledBy;
                            Player killerplayer = unit.PlayerKilledBy;

                            if (killerplayer != null) {
                                s(str(unit.PlayerControlledBy.Character + " => " + unit.PlayerKilledBy.Character));
                            }
                            else {
                                s(str(unit.PlayerControlledBy.Character + " => ?"));
                            }

                            s(seperate(u < heroCount - 1));

                            u++;
                        }
                        s(endArr());*/

                        //Players
                        int playerCount = replay.Players.Length;
                        int p = 0;
                        s(startArr("players"));
                        foreach (var player in replay.Players.OrderByDescending(i => i.IsWinner)) {
                            //Start Player
                            s(startObj());

                            if (!isRegionSet) {
                                region = player.BattleNetRegionId;
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

                            //Kills
                            //s(keynum("kills", herostats[player.Team][player.Character].Kills));

                            //Deaths
                            //s(keynum("deaths", herostats[player.Team][player.Character].Deaths));

                            //Talents
                            int talentCount = player.Talents.Length;
                            int t = 0;
                            s(startArr("talents"));
                            foreach (var talent in player.Talents.OrderBy(j => j.TalentID)) {
                                //Id : Indexing for a hero starts at 0, and then increments top down, 
                                //     left right, as moving through talents
                                s(talent.TalentID + "");

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
                        s(startObj("exp_samples"));

                        for (int team = 0; team < TEAMS; team++) {
                            s(startArr(team + ""));

                            int xpCount = replay.TeamPeriodicXPBreakdown[team].Count;
                            int x = 0;
                            foreach (var exp in replay.TeamPeriodicXPBreakdown[team]) {
                                s(startObj());

                                //Time
                                s(keynum("time", (int) Math.Ceiling(exp.TimeSpan.TotalSeconds)));

                                //Team Level
                                s(keynum("level", exp.TeamLevel));

                                //Team Exp
                                s(keynum("exp", exp.TotalXP, false));

                                s(endObj(x < xpCount - 1));

                                x++;
                            }

                            s(endArr(team < TEAMS - 1));
                        }

                        s(endObj());

                        //Region
                        s(keynum("region", region));

                        //Winner
                        s(keynum("winner", winner, false));

                        s(endObj(false));

                        //TODO DEBUG file output
                        //System.IO.File.WriteAllText(@"test/output.json", sb.ToString());

                        //Output JSON
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
                    return "Irrelevant Match Type";
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
