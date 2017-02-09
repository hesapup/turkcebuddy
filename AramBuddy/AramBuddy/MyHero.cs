using EloBuddy;
using EloBuddy.SDK;

namespace AramBuddy
{
    public static class MyHero
    {
        public static AIHeroClient Instance => Player.Instance;
        public static float LastTurretAttack;
        public static bool TurretAttackingMe => Core.GameTickCount - LastTurretAttack < 2750;
    }
}
