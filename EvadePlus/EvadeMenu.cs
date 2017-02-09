using System.Collections.Generic;
using System.Linq;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;

namespace EvadePlus
{
    internal class EvadeMenu
    {
        public static Menu MainMenu { get; private set; }
        public static Menu SkillshotMenu { get; private set; }
        public static Menu SpellMenu { get; private set; }
        public static Menu DrawMenu { get; private set; }
        public static Menu HotkeysMenu { get; private set; }

        public static readonly Dictionary<string, EvadeSkillshot> MenuSkillshots = new Dictionary<string, EvadeSkillshot>();

        public static void CreateMenu()
        {
            if (MainMenu != null)
            {
                return;
            }

            MainMenu = EloBuddy.SDK.Menu.MainMenu.AddMenu("Evade+", "EvadePlus");

            // Set up main menu
            MainMenu.AddGroupLabel("Çeviri: Hesapup \nGenel Ayarlar");
            MainMenu.Add("fowDetection", new CheckBox("Enable FOW detection"));
            MainMenu.AddLabel("On: for dodging through fog of war, Off: for more human behaviour");
            MainMenu.AddSeparator(3);

            MainMenu.Add("processSpellDetection", new CheckBox("İşlem Yazımının Saptanmasını Etkinleştir"));
            MainMenu.AddLabel("skillshot detection before the missile is created, recommended: On");
            MainMenu.AddSeparator(3);

            MainMenu.Add("limitDetectionRange", new CheckBox("Mesafedki Skill Algılama oranı"));
            MainMenu.AddLabel("sadece yakınınızdaki skilleri algılar, önerilen: On/açık");
            MainMenu.AddSeparator(3);

            MainMenu.Add("recalculatePosition", new CheckBox("Kaçma pozisyonunun yeniden hesaplanmasına izin ver", false));
            MainMenu.AddLabel("kaçma yolunun değiştirilmesine izin ver, tavsiye edilen: Off/Kapalı");
            MainMenu.AddSeparator(3);

            MainMenu.Add("moveToInitialPosition", new CheckBox("Kaçma Sonrası İstenilen Bölgeye Git", false));
            MainMenu.AddLabel("kaçtıktan sonra istediğiniz yere haraket eder");
            MainMenu.AddSeparator(3);

            MainMenu.Add("serverTimeBuffer", new Slider("Sunucu Zamanı Arabelleği", 0, 0, 200));
            MainMenu.AddLabel("the extra time it is included during evade calculation");
            MainMenu.AddSeparator();

            MainMenu.AddGroupLabel("İnsancıllaştırma");
            MainMenu.Add("skillshotActivationDelay", new Slider("Kaçma Gecikmesi", 0, 0, 400));
            MainMenu.AddSeparator(10);

            MainMenu.Add("extraEvadeRange", new Slider("Ekstra Kaçma Mesafesi", 0, 0, 300));
            MainMenu.Add("randomizeExtraEvadeRange", new CheckBox("Ekstra Kaçma Mesafesini Rastgele Ayarla", false));

            // Set up skillshot menu
            var heroes = Program.DeveloperMode ? EntityManager.Heroes.AllHeroes : EntityManager.Heroes.Enemies;
            var heroNames = heroes.Select(obj => obj.ChampionName).ToArray();
            var skillshots =
                SkillshotDatabase.Database.Where(s => heroNames.Contains(s.SpellData.ChampionName)).ToList();
            skillshots.AddRange(
                SkillshotDatabase.Database.Where(
                    s =>
                        s.SpellData.ChampionName == "AllChampions" &&
                        heroes.Any(obj => obj.Spellbook.Spells.Select(c => c.Name).Contains(s.SpellData.SpellName))));

            SkillshotMenu = MainMenu.AddSubMenu("Skillshots");
            SkillshotMenu.AddLabel(string.Format("Skillshots yüklendi {0}", skillshots.Count));
            SkillshotMenu.AddSeparator();

            foreach (var c in skillshots)
            {
                var skillshotString = c.ToString().ToLower();

                if (MenuSkillshots.ContainsKey(skillshotString))
                    continue;

                MenuSkillshots.Add(skillshotString, c);

                SkillshotMenu.AddGroupLabel(c.DisplayText);
                SkillshotMenu.Add(skillshotString + "/enable", new CheckBox("Kaç"));
                SkillshotMenu.Add(skillshotString + "/draw", new CheckBox("Çiz"));

                var dangerous = new CheckBox("Tehlikeli", c.SpellData.IsDangerous);
                dangerous.OnValueChange += delegate(ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
                {
                    GetSkillshot(sender.SerializationId).SpellData.IsDangerous = args.NewValue;
                };
                SkillshotMenu.Add(skillshotString + "/dangerous", dangerous);

                var dangerValue = new Slider("Danger Value", c.SpellData.DangerValue, 1, 5);
                dangerValue.OnValueChange += delegate(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args)
                {
                    GetSkillshot(sender.SerializationId).SpellData.DangerValue = args.NewValue;
                };
                SkillshotMenu.Add(skillshotString + "/dangervalue", dangerValue);

                SkillshotMenu.AddSeparator();
            }

            // Set up spell menu
            SpellMenu = MainMenu.AddSubMenu("Kaçma Büyüleri");
            SpellMenu.AddGroupLabel("Flash");
            SpellMenu.Add("flash", new Slider("Tehlike Değeri", 5, 0, 5));

            // Set up draw menu
            DrawMenu = MainMenu.AddSubMenu("Çizimler");
            DrawMenu.AddGroupLabel("Kaçma Çizimleri");
            DrawMenu.Add("disableAllDrawings", new CheckBox("Tüm Çizimleri Devredışı Bırak", false));
            DrawMenu.Add("drawEvadePoint", new CheckBox("Kaçma Noktasını Çiz"));
            DrawMenu.Add("drawEvadeStatus", new CheckBox("Kaçma Durumunu Çiz"));
            DrawMenu.Add("drawDangerPolygon", new CheckBox("Tehlike Poligonu Çiz", false));
            DrawMenu.AddSeparator();
            DrawMenu.Add("drawPath", new CheckBox("Yürüme yolunu çiz"));

            // Set up controls menu
            HotkeysMenu = MainMenu.AddSubMenu("Kısayollar");
            HotkeysMenu.AddGroupLabel("Kısayollar");
            HotkeysMenu.Add("enableEvade", new KeyBind("Kaçmayı Etkinleştir", true, KeyBind.BindTypes.PressToggle, 'M'));
            HotkeysMenu.Add("dodgeOnlyDangerous", new KeyBind("Sadece Tehlikelilerden Kaç", false, KeyBind.BindTypes.HoldActive));
            HotkeysMenu.Add("dodgeOnlyDangeroustoggle", new KeyBind("Dodge Only Dangerous Toggle", false, KeyBind.BindTypes.PressToggle));
        }

        private static EvadeSkillshot GetSkillshot(string s)
        {
            return MenuSkillshots[s.ToLower().Split('/')[0]];
        }

        public static bool IsSkillshotEnabled(EvadeSkillshot skillshot)
        {
            var valueBase = SkillshotMenu[skillshot + "/enable"];
            return valueBase != null && valueBase.Cast<CheckBox>().CurrentValue;
        }

        public static bool IsSkillshotDrawingEnabled(EvadeSkillshot skillshot)
        {
            var valueBase = SkillshotMenu[skillshot + "/draw"];
            return valueBase != null && valueBase.Cast<CheckBox>().CurrentValue;
        }
    }
}