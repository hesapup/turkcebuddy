using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using AramBuddy.MainCore;
using AramBuddy.MainCore.Common;
using AramBuddy.MainCore.Logics;
using AramBuddy.MainCore.Utility;
using AramBuddy.MainCore.Utility.GameObjects;
using AramBuddy.Plugins.AutoShop;
using AramBuddy.Plugins.AutoShop.Sequences;
using AramBuddy.Plugins.Champions;
using AramBuddy.Plugins.KappaEvade;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Notifications;
using EloBuddy.SDK.Rendering;
using SharpDX;
using static AramBuddy.Config;
using Color = SharpDX.Color;
using Version = System.Version;

namespace AramBuddy
{
    internal class Program
    {
        public static List<string> CurrentPatchs = new List<string> { "7.2.1", "7.1.1" };

        private static string Texturefile = Misc.AramBuddyFolder + "\\temp\\DisableTexture1.dat";

        public static bool CrashAIODetected;
        public static bool CustomChamp;
        public static bool Loaded;
        public static bool GameEnded;

        public static Version version = typeof(Program).Assembly.GetName().Version;
        public static int MoveToCommands;
        public static float Timer;
        private static float TimeToStart;

        public static string Moveto;

        public static Menu MenuIni, SpellsMenu, MiscMenu, BuildMenu, InfoMenu;

        private static float textsize;
        private static Text text;

        private static void Main()
        {
            if (File.Exists(Texturefile))
            {
                ManagedTexture.OnLoad += delegate(OnLoadTextureEventArgs texture)
                    {
                        if (Game.MapId == GameMapId.HowlingAbyss)
                        {
                            Hacks.DisableTextures = true;
                            texture.Process = false;
                        }
                    };
            }

            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            try
            {
                // Disable arambuddy if it's not running in HowlingAbyss
                if (Game.MapId != GameMapId.HowlingAbyss)
                {
                    Logger.Send(Game.MapId + " AramBuddy Tarafından Desteklenmiyor !", Logger.LogLevel.Warn);
                    Chat.Print(Game.MapId + " AramBuddy Tarafından Desteklenmiyor !");
                    return;
                }

                textsize = Drawing.Width <= 1280 || Drawing.Height <= 720 ? 10F : 40F;
                text = new Text("ORBWALKERI'NIZ ETKİN DEĞİL", new Font("Euphemia", textsize, FontStyle.Bold)) { Color = System.Drawing.Color.White, Position = new Vector2(Drawing.Width * 0.3f, Drawing.Height * 0.2f) };

                Chat.OnClientSideMessage += delegate (ChatClientSideMessageEventArgs eventArgs)
                {
                    if (eventArgs.Message.ToLower().Contains("portaio") && !CrashAIODetected)
                    {
                        var warnmsg = "CrashAIO AramBuddy İle Çalışmıyor \nAramBuddy'i Kullanmak İçin CrashAIO'yu Devredışı Bırak!";
                        Chat.Print(warnmsg);
                        Logger.Send(warnmsg, Logger.LogLevel.Warn);
                        Notifications.Show(new SimpleNotification("AramBuddy", warnmsg), 20000);
                        Drawing.OnEndScene += delegate
                            {
                                text.TextValue = warnmsg;
                                text.Position = new Vector2(Drawing.Width * 0.3f, Drawing.Height * 0.2f);
                                text.Draw();
                        };
                        CrashAIODetected = true;
                    }
                };
                
                // Creates The Menu
                CreateMenu();

                // Checks for updates
                CheckVersion.Init();

                // Inits KappaEvade
                KappaEvade.Init();
                
                // Initialize the AutoShop.
                Setup.Init();
                
                // The time Game loaded At.
                Timer = Game.Time;

                // Time in ms to start the bot
                TimeToStart = new Random().Next(7500, 30000) + Game.Ping;

                Game.OnTick += Game_OnTick;
                Events.OnGameEnd += Events_OnGameEnd;
                Player.OnPostIssueOrder += Player_OnPostIssueOrder;
                Drawing.OnEndScene += Drawing_OnEndScene;

                Logger.Send("Başladıktan Sonra: " + (TimeToStart / 1000).ToString("F1") + " Second/s", Logger.LogLevel.Event);
            }
            catch (Exception ex)
            {
                Logger.Send("Program Hatası Loading_OnLoadingComplete", ex, Logger.LogLevel.Error);
            }
        }
        
        private static void Player_OnPostIssueOrder(Obj_AI_Base sender, PlayerIssueOrderEventArgs args)
        {
            try
            {
                if (sender.IsMe && args.Order == GameObjectOrder.MoveTo)
                    MoveToCommands++;
            }
            catch (Exception ex)
            {
                Logger.Send("Program Hatası Player_OnPostIssueOrder", ex, Logger.LogLevel.Error);
            }
        }

        private static void Events_OnGameEnd(bool args)
        {
            try
            {
                GameEnded = true;
                if (QuitOnGameEnd)
                {
                    var rnd = new Random().Next(15000, 30000) + Game.Ping;
                    Logger.Send("Closing the Game in: " + (rnd / 1000).ToString("F1") + " Second/s", Logger.LogLevel.Event);
                    Core.DelayAction(() => Game.QuitGame(), rnd);
                }
            }
            catch (Exception ex)
            {
                Logger.Send("Program  Events_OnGameEnd", ex, Logger.LogLevel.Error);
            }
        }

        private static void Init()
        {
            try
            {
                if (Orbwalker.MovementDelay < 200)
                {
                    Orbwalker.MovementDelay += new Random().Next(200, 400);
                }
                
                if (Setup.CurrentChampionBuild.BuildData.Length > 0)
                {
                    var i = 0;
                    foreach (var item in Setup.CurrentChampionBuild.BuildData)
                    {
                        i++;
                        BuildMenu.AddLabel(i + " - " + item);
                    }
                }

                if (EnableCustomPlugins)
                {
                    try
                    {
                        if ((Base)Activator.CreateInstance(null, "AramBuddy.Plugins.Champions." + Player.Instance.Hero + "." + Player.Instance.Hero).Unwrap() != null)
                        {
                            CustomChamp = true;
                            Logger.Send("Özel Şampiyon Yüklendi " + Player.Instance.Hero);
                        }
                    }
                    catch (Exception)
                    {
                        CustomChamp = false;
                        Logger.Send("Hiçbir Özel Eklenti Yok:" + Player.Instance.Hero, Logger.LogLevel.Warn);
                    }
                }

                // Sends Start / End Msg
                Chatting.Init();

                // Initialize Bot Functions.
                Brain.Init();
                
                // Inits Activator
                if (EnableActivator)
                    Plugins.Activator.Load.Init();
                
                Chat.Print("AramBuddy Yüklendi \nÇeviri: Hesapup !");
                Chat.Print("AramBuddy Versiyon: " + version);
            }
            catch (Exception ex)
            {
                Logger.Send("Başlangıçta Program Hatası", ex, Logger.LogLevel.Error);
            }
        }

        private static void CreateMenu()
        {
            try
            {
                MenuIni = MainMenu.AddMenu("AramBuddy", "AramBuddy");
                SpellsMenu = MenuIni.AddSubMenu("Spells");
                MenuIni.AddGroupLabel("AramBuddy Versiyon: \nÇeviri:Hesapup " + version);
                MenuIni.AddGroupLabel("AramBuddy Ayarları");

                BuildMenu = MenuIni.AddSubMenu("Geçerli Build");
                InfoMenu = MenuIni.AddSubMenu("Extra Ayarlar");

                var lolversion = BuildMenu.Add("buildpatch", new ComboBox("Build seç: ", 0, CurrentPatchs.ToArray()));
                
                BuildMenu.AddLabel($"Geçerli Build: {lolversion.SelectedText}");

                var debug = MenuIni.CreateCheckBox("debug", "Hata Ayıklamayı Etkinleştir");
                var activator = MenuIni.CreateCheckBox("activator", "Dahili Etkinleştiriciyi Etkinleştir");
                var DisableSpells = MenuIni.CreateCheckBox("DisableSpells", "Yerleşik Döküm Mantığı'nı devre dışı bırak", false);
                var CustomPlugin = MenuIni.CreateCheckBox("CustomPlugin", "Özel Eklentileri Etkinleştir");
                var quit = MenuIni.CreateCheckBox("quit", "Oyun Sonu Çık");
                var stealhr = MenuIni.CreateCheckBox("stealhr", "Düşmanın Heal'ini Çalma", false);
                var chat = MenuIni.CreateCheckBox("chat", "Başlangıçta / Sonda Oyun İçin Sohbetten Mesaj Gönder", false);
                var texture = MenuIni.CreateCheckBox("texture1", "Dokuları Devredışı Bırak (Less RAM/CPU)", false);
                var evade = MenuIni.CreateCheckBox("evade", "Kaçma Entegresi[BETA]");
                var ff = MenuIni.CreateCheckBox("ff", "Her zaman Takımla Teslim olun");
                var cameralock = MenuIni.CreateCheckBox("cameralock", "Kamerayı Her zaman sabitle");

                MenuIni.AddSeparator(0);
                var Safe = MenuIni.CreateSlider("Safe1", "Güvenli Kaydırıcı (Önerilen 1250)", 1250, 0, 2500);
                //MenuIni.AddLabel("More Safe Value = more defensive playstyle");
                //MenuIni.AddSeparator(0);
                var HRHP = MenuIni.CreateSlider("HRHP", "Sağlık Al Altındaysa: (Önerilen 75%)", 75);
                var HRMP = MenuIni.CreateSlider("HRMP", "Mana Al Altındaysa: (Önerilen 15%)", 15);
                MenuIni.AddSeparator(0);
                var Reset = MenuIni.CreateCheckBox("reset", "Bütün Ayarları Resetle ve Varsayılana Döndür", false);

                // Misc Settings
                MiscMenu = MenuIni.AddSubMenu("Misc Settings");
                var autolvl = MiscMenu.CreateCheckBox("autolvl", "Etkinleştir AutoLvlUP");
                var autoshop = MiscMenu.CreateCheckBox("autoshop", "Etkinleştir AutoShop");
                var fixdive = MiscMenu.CreateCheckBox("fixdive", "Try to Fix Diving Towers");
                var kite = MiscMenu.CreateCheckBox("kite", "Düşmanların Yanında Kite Dene");
                var ping = MiscMenu.CreateCheckBox("ping", "Yüksek Ping'de Üsse Dön");
                var bardchime = MiscMenu.CreateCheckBox("bardchime", "Bardın Tınılarını Topla");
                var corkibomb = MiscMenu.CreateCheckBox("corkibomb", "Corki'nin Bombasını Topla");
                var dravenaxe = MiscMenu.CreateCheckBox("dravenaxe", "Draven'in Baltalarıı Tut");
                var olafaxe = MiscMenu.CreateCheckBox("olafaxe", "Olaf'ın Baltasını Al");
                var zacpassive = MiscMenu.CreateCheckBox("zacpassive", "Zac Parçalarını Topla");
                var azirtower = MiscMenu.CreateCheckBox("azirtower", "Azir'in Kule Yaratma Pasifini Kullan);
                var teleport = MiscMenu.CreateCheckBox("tp", "Teleport Kullanımı Etkinleştir");
                var logs = MiscMenu.CreateCheckBox("logs", "AramBuddy Kayıtlarını Kaydet", false);
                var savechat = MiscMenu.CreateCheckBox("savechat", "Oyun Sohbetini Kaydet", false);
                var tyler1 = MiscMenu.CreateCheckBox("bigbrother", "Run it down mid", false);
                var tyler1g = MiscMenu.CreateSlider("gold", "Run it down mid if my Gold >= {0}", 3000, 500, 17500);

                Reset.OnValueChange += delegate (ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
                {
                    if (args.NewValue)
                    {
                        Reset.CurrentValue = false;
                        debug.CurrentValue = true;
                        activator.CurrentValue = true;
                        DisableSpells.CurrentValue = false;
                        CustomPlugin.CurrentValue = true;
                        quit.CurrentValue = true;
                        stealhr.CurrentValue = false;
                        chat.CurrentValue = true;
                        texture.CurrentValue = false;
                        evade.CurrentValue = true;
                        ff.CurrentValue = true;
                        cameralock.CurrentValue = true;
                        Safe.CurrentValue = 1250;
                        HRHP.CurrentValue = 75;
                        HRMP.CurrentValue = 15;

                        // Misc
                        autolvl.CurrentValue = true;
                        autoshop.CurrentValue = true;
                        fixdive.CurrentValue = true;
                        kite.CurrentValue = true;
                        ping.CurrentValue = true;
                        bardchime.CurrentValue = true;
                        corkibomb.CurrentValue = true;
                        dravenaxe.CurrentValue = true;
                        olafaxe.CurrentValue = true;
                        zacpassive.CurrentValue = true;
                        azirtower.CurrentValue = true;
                        teleport.CurrentValue = true;
                        logs.CurrentValue = false;
                        savechat.CurrentValue = false;
                        tyler1.CurrentValue = false;
                        tyler1g.CurrentValue = 3000;
                    }
                };

                Safe.IsVisible = false; // disabled
                corkibomb.IsVisible = false; // disable for now
                logs.IsVisible = false; // disabled kappa

                SpellsMenu.AddGroupLabel("Built-In Casting Logic:");
                SpellsMenu.CreateCheckBox("combo", "Combo modunda spell kullan");
                SpellsMenu.CreateCheckBox("harass", "Harras modda spell kullan");
                SpellsMenu.CreateCheckBox("flee", "Flee modda spell kullan");
                SpellsMenu.CreateCheckBox("laneclear", "Lane Temizleme modunda spell kullan");
                SpellsMenu.AddSeparator(0);
                SpellsMenu.AddGroupLabel("SummonerSpells");
                SpellsMenu.Add("Heal", new CheckBox("Şifa Kullan"));
                SpellsMenu.Add("Barrier", new CheckBox("Bariyer Kullan"));
                SpellsMenu.Add("Clarity", new CheckBox("Berrak Zihin kullan"));
                SpellsMenu.Add("Ghost", new CheckBox("Hayalet Kullan"));
                SpellsMenu.Add("Flash", new CheckBox("Flash Kullan"));
                SpellsMenu.Add("Cleanse", new CheckBox("Arındır Kullan"));

                InfoMenu.AddGroupLabel("Oyuni çi dokuyu devre dışı bırakma");
                InfoMenu.AddLabel("Dokuları Devre Dışı Bırakmak için Sohbete: \"Disable Texture\"");
                InfoMenu.AddLabel("Dokuları Etkinleştirmek için Sohbete: \"Enable Texture\"");
                InfoMenu.AddLabel("1 veya 2 oyun sonra etkili olur");

                Console.Title = $"{Drawing.Width}x{Drawing.Height}";

                texture.IsVisible = false;

                if (DisableTexture)
                    Disabletexture();
                else
                    Enabletexture();

                Chat.OnInput += delegate(ChatInputEventArgs args)
                    {
                        var input = args.Input.ToLower();
                        if (input.Contains("disable texture"))
                        {
                            Disabletexture();
                            texture.CurrentValue = true;
                            args.Process = false;
                        }
                        if (input.Contains("enable texture"))
                        {
                            Enabletexture();
                            texture.CurrentValue = false;
                            args.Process = false;
                        }
                        if (input.Contains("force start"))
                        {
                            TimeToStart = 0;
                            args.Process = false;
                        }
                    };
            }
            catch (Exception ex)
            {
                Logger.Send("Oluşturma meünüsünüde prgram hatası", ex, Logger.LogLevel.Error);
            }
        }

        private static void Enabletexture()
        {
            if (File.Exists(Texturefile))
            {
                File.Delete(Texturefile);
                Logger.Send("Texture etkinleştirildi !", Logger.LogLevel.Event);
            }
        }
        private static void Disabletexture()
        {
            Misc.CreateAramBuddyFile("DisableTexture1.dat", Misc.AramBuddyDirectories.Temp);
            Logger.Send("Texture devre dışı bırakıldı !", Logger.LogLevel.Event);
        }
        
        private static void Drawing_OnEndScene(EventArgs args)
        {
            try
            {
                if(CrashAIODetected) return;

                if (!Loaded)
                {
                    text.TextValue = $"AramBuddy başlatılıyor \nÇeviri:Hesapup: {(int)((Timer*1000 + TimeToStart - Game.Time*1000)/1000) + 1}";
                    text.Position = new Vector2(Drawing.Width * 0.3f, Drawing.Height * 0.2f);
                    text.Draw();
                    return;
                }

                if (Orbwalker.DisableMovement && !MainCore.Logics.Casting.SpecialChamps.IsCastingImportantSpell)
                {
                    text.TextValue = "ORBWALKERINIZ ENGELLİYOR\nBOT ÇALIŞMIYOR\nTİKİ KALDIR\nFAERYLE DOLAŞMIYI DEVRE DIŞI YAP";
                    text.Position = new Vector2(Drawing.Width * 0.3f, Drawing.Height * 0.2f);
                    text.Draw();
                }

                if (!EnableDebug)
                    return;

                var AllyTeamTotal = " | AllyTeamTotal: " + (int)Player.Instance.PredictPosition().TeamTotal();
                var EnemyTeamTotal = " | EnemyTeamTotal: " + (int)Player.Instance.PredictPosition().TeamTotal(true);
                var MoveTo = " | MoveTo: " + Moveto;
                var ActiveMode = " | ActiveMode: " + ModesManager.CurrentMode;
                var Alone = " | Alone: " + Brain.Alone();
                var AttackObject = " | AttackObject: " + ModesManager.AttackObject;
                var LastTurretAttack = " | LastTurretAttack: " + (Core.GameTickCount - MyHero.LastTurretAttack);
                var SafeToDive = " | SafeToDive: " + Misc.SafeToDive;
                var castingimportantspell = " | IsCastingImportantSpell: " + MainCore.Logics.Casting.SpecialChamps.IsCastingImportantSpell;
                var lagging = " | Lagging: " + Brain.Lagging;
                var SafeToAttack = " | SafeToAttack: " + Misc.SafeToAttack;
                var IsSafe = /*" | IsSafe: " + (Player.Instance.IsSafe() && Pathing.Position.IsSafe())*/ "";
                var LastTeamFight = " | LastTeamFight: " + (int)(Core.GameTickCount - Brain.LastTeamFight);
                var MovementCommands = " | Movement Commands Issued: " + MoveToCommands;
                var nextitem = " | sONRAKİ Item: " + Buy.CurrentItemIndex + " - " + Buy.NextItem + " | Value: " + Buy.NextItemValue;
                var fullbuild = " | FullBuild: " + Buy.FullBuild;

                Drawing.DrawText(Drawing.Width * 0.2f, Drawing.Height * 0.025f, System.Drawing.Color.White,
                    AllyTeamTotal + EnemyTeamTotal + "\n"
                    + ActiveMode + Alone + AttackObject + "\n"
                    + SafeToDive + SafeToAttack + IsSafe + "\n"
                    + castingimportantspell + lagging + "\n"
                    + LastTurretAttack + LastTeamFight + "\n"
                    + MovementCommands + MoveTo + "\n"
                    + nextitem + fullbuild + "\n");

                Drawing.DrawText(
                    Game.CursorPos.WorldToScreen().X + 50,
                    Game.CursorPos.WorldToScreen().Y,
                    System.Drawing.Color.Goldenrod,
                    (Game.CursorPos.TeamTotal() - Game.CursorPos.TeamTotal(true)).ToString(CultureInfo.CurrentCulture) + "\n" 
                    /*+ "KDA: " + Player.Instance.KDA() +" [" + Player.Instance.ChampionsKilled + ", " + Player.Instance.Assists + ", " + Player.Instance.Deaths + "]"*/,
                    5);

                foreach (var hr in ObjectsManager.HealthRelics.Where(h => h.IsValid && !h.IsDead))
                {
                    Circle.Draw(Color.GreenYellow, hr.BoundingRadius + Player.Instance.BoundingRadius, hr.Position);
                }
                
                if (Pathing.Position != null && Pathing.Position != Vector3.Zero && Pathing.Position.IsValid())
                {
                    Circle.Draw(Color.White, 100, Pathing.Position);
                }

                if (!DisableSpellsCasting && ModesManager.Spelllist != null)
                {
                    foreach (var spell in ModesManager.Spelllist.Where(s => s != null))
                    {
                        Circle.Draw(spell.IsReady() ? Color.Chartreuse : Color.OrangeRed, (spell as Spell.Chargeable)?.MaximumRange ?? spell.Range, Player.Instance);
                    }
                }

                if (PickBardChimes)
                {
                    foreach (var chime in ObjectsManager.BardChimes.Where(c => Player.Instance.Hero == Champion.Bard && c.IsValid && !c.IsDead))
                    {
                        Circle.Draw(Color.Goldenrod, chime.BoundingRadius + Player.Instance.BoundingRadius, chime.Position);
                    }
                }

                if (EnableEvade)
                {
                    foreach (var trap in ObjectsManager.EnemyTraps)
                    {
                        Circle.Draw(Color.OrangeRed, trap.Trap.BoundingRadius * 3, trap.Trap.Position);
                    }
                    /*
                    foreach (var p in KappaEvade.dangerPolygons)
                    {
                        p.Draw(System.Drawing.Color.AliceBlue, 2);
                    }*/
                }

                if (Player.Instance.Hero == Champion.Zac)
                    ObjectsManager.ZacPassives.ForEach(p => Circle.Draw(Color.AliceBlue, 100, p));
            }
            catch (Exception ex)
            {
                Logger.Send("Program Error At Drawing_OnEndScene", ex, Logger.LogLevel.Error);
            }
        }

        private static void Game_OnTick(EventArgs args)
        {
            try
            {
                if (!Loaded)
                {
                    if ((Game.Time - Timer) * 1000 >= TimeToStart)
                    {
                        Loaded = true;

                        // Initialize The Bot.
                        Init();
                    }
                }
                else
                {
                    if ((!Player.Instance.IsZombie() && Player.Instance.IsDead) || GameEnded)
                    {
                        Orbwalker.ActiveModesFlags = Orbwalker.ActiveModes.None;
                        return;
                    }

                    Brain.Decisions();
                    
                    if (CameraLock && !Camera.Locked)
                    {
                        Camera.Locked = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Send("Program Error At Game_OnTick", ex, Logger.LogLevel.Error);
            }
        }
    }
}
