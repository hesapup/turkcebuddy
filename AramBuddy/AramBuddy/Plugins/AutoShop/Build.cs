// <summary>
//   The class containing the BuildData used by the interpreter to buy items in order
// </summary>
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using AramBuddy.MainCore.Common;
using EloBuddy;

namespace AramBuddy.Plugins.AutoShop
{
    /// <summary>
    ///     The class containing the BuildData used by the interpreter to buy items in order
    /// </summary>
    public class Build
    {
        /// <summary>
        ///     An array of the item names
        /// </summary>
        public string[] BuildData { get; set; }

        /// <summary>
        /// returns The build name.
        /// </summary>
        public static string BuildName()
        {
            var ChampionName = Player.Instance.CleanChampionName();
            
            if (ADC.Any(s => s.Equals(ChampionName, StringComparison.CurrentCultureIgnoreCase)))
            {
                return "ADC";
            }

            if (AD.Any(s => s.Equals(ChampionName, StringComparison.CurrentCultureIgnoreCase)))
            {
                return "AD";
            }

            if (AP.Any(s => s.Equals(ChampionName, StringComparison.CurrentCultureIgnoreCase)))
            {
                return "AP";
            }

            if (ManaAP.Any(s => s.Equals(ChampionName, StringComparison.CurrentCultureIgnoreCase)))
            {
                return "ManaAP";
            }

            if (Tank.Any(s => s.Equals(ChampionName, StringComparison.CurrentCultureIgnoreCase)))
            {
                return "Tank";
            }

            Logger.Send("Failed To Detect " + ChampionName, Logger.LogLevel.Warn);
            //Logger.Send("Using Default Build !");
            return "Default";
        }

        /// <summary>
        ///     Creates Builds
        /// </summary>
        public static void CreateDefualtBuild()
        {
            try
            {
                var filename = $"{BuildName()}.json";
                var buildUrl = $"https://raw.githubusercontent.com/plsfixrito/AramBuddy.Data/master/Default/{filename}";
                var result = Weeb.ReadString(buildUrl).Result;

                if (string.IsNullOrEmpty(result))
                {
                    Logger.Send("Wrong Response or was canceled, No Champion Build Created !", Logger.LogLevel.Warn);
                    Logger.Send("No Build is being used !", Logger.LogLevel.Warn);
                    return;
                }
                
                if (result.Contains("data"))
                {
                    File.WriteAllText(Setup.BuildPath + "\\" + filename, result);
                    Setup.Builds.Add(BuildName(), File.ReadAllText(Setup.BuildPath + "\\" + filename));
                    Logger.Send(BuildName() + " Build Created for " + Player.Instance.ChampionName + " - " + BuildName());
                    Setup.UseDefaultBuild();
                }
                else
                {
                    Logger.Send("Wrong Response, No Champion Build Created", Logger.LogLevel.Warn);
                    Console.WriteLine(result);
                }
            }
            catch (Exception ex)
            {
                // if faild to create build terminate the AutoShop
                Logger.Send("Failed to create default build for " + Player.Instance.ChampionName, ex, Logger.LogLevel.Error);
                Logger.Send("No build is currently being used!", Logger.LogLevel.Error);
            }
        }

        /// <summary>
        ///     Creates Builds
        /// </summary>
        public static void GetBuildFromService()
        {
            try
            {
                var filename = $"{Player.Instance.CleanChampionName()}.json";
                var buildUrl = $"https://raw.githubusercontent.com/plsfixrito/AramBuddy.Data/master/{Config.CurrentPatchUsed}/ItemBuilds/{filename}";
                var result = Weeb.ReadString(buildUrl).Result;
                if (string.IsNullOrEmpty(result))
                {
                    Logger.Send("Wrong Response or was canceled, No Champion Build Created !", Logger.LogLevel.Warn);
                    Logger.Send("Trying To Get Defualt Build !", Logger.LogLevel.Warn);
                    Setup.UseDefaultBuild();
                    return;
                }

                if (result.Contains("data"))
                {
                    var filepath = $"{Setup.BuildPath}/{filename}";
                    File.WriteAllText(filepath, result);
                    Setup.Builds.Add(Player.Instance.CleanChampionName(), File.ReadAllText(filepath));
                    Logger.Send("Created Build for " + Player.Instance.ChampionName);
                    Setup.CustomBuildService();
                }
                else
                {
                    Logger.Send("Wrong Response, No Champion Build Created !", Logger.LogLevel.Warn);
                    Logger.Send("Trying To Get Defualt Build !", Logger.LogLevel.Warn);
                    Setup.UseDefaultBuild();
                }
            }
            catch (Exception ex)
            {
                // if faild to create build terminate the AutoShop
                Logger.Send($"Failed to create Build from service {Config.CurrentPatchUsed} for {Player.Instance.ChampionName}", Logger.LogLevel.Error);
                Logger.Send(ex.InnerException?.Message, Logger.LogLevel.Error);
                Logger.Send("Trying To Get Defualt Build !", Logger.LogLevel.Warn);
                Setup.UseDefaultBuild();
            }
        }

        /// <summary>
        ///  ADC Champions.
        /// </summary>
        public static readonly string[] ADC =
            {
                "Ashe", "Caitlyn", "Corki", "Draven", "Ezreal", "Graves", "Jhin", "Jinx", "Kalista", "Kindred", "KogMaw", "Lucian", "MissFortune", "Sivir", "Quinn",
                "Tristana", "Twitch", "Urgot", "Varus", "Vayne"
            };

        /// <summary>
        ///  Mana AP Champions.
        /// </summary>
        public static readonly string[] ManaAP =
            {
                "Ahri", "Anivia", "Annie", "AurelionSol", "Azir", "Brand", "Cassiopeia", "Diana", "Elise", "Ekko", "Evelynn", "Fiddlesticks", "Fizz", "Galio",
                "Gragas", "Heimerdinger", "Janna", "Karma", "Karthus", "Kassadin", "Kayle", "Leblanc", "Lissandra", "Lulu", "Lux", "Malzahar", "Morgana", "Nami", "Nidalee", "Ryze", "Orianna", "Sona",
                "Soraka", "Swain", "Syndra", "Taliyah", "Teemo", "TwistedFate", "Veigar", "Viktor", "VelKoz", "Xerath", "Ziggs", "Zilean", "Zyra"
            };

        /// <summary>
        ///  AP no Mana Champions.
        /// </summary>
        public static readonly string[] AP = { "Akali", "Katarina", "Kennen", "Mordekaiser", "Rumble", "Vladimir" };

        /// <summary>
        ///  AD Champions.
        /// </summary>
        public static readonly string[] AD =
            {
                "Aatrox", "Fiora", "Gangplank", "Jax", "Jayce", "KhaZix", "LeeSin", "MasterYi", "Nocturne", "Olaf", "Pantheon", "Rengar", "Riven", "Talon", "Tryndamere",
                "Wukong", "XinZhao", "Yasuo", "Zed"
            };

        /// <summary>
        ///  Tank Champions.
        /// </summary>
        public static readonly string[] Tank =
            {
                "Alistar", "Amumu", "Blitzcrank", "Bard", "Braum", "ChoGath", "Darius", "DrMundo", "Garen", "Gnar", "Hecarim", "Kled", "Illaoi", "Irelia", "Ivern", "JarvanIV",
                "Leona", "Malphite", "Maokai", "Nasus", "Nautilus", "Nunu", "Poppy", "Rammus", "RekSai", "Renekton", "Sejuani", "Shaco", "Shen", "Shyvana", "Singed", "Sion", "Skarner", "TahmKench",
                "Taric", "Thresh", "Trundle", "Udyr", "Vi", "Volibear", "Warwick", "Yorick", "Zac"
            };
    }
}
