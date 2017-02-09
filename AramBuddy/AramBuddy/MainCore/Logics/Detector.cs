using System;
using System.Collections.Generic;
using System.Linq;
using AramBuddy.MainCore.Common;
using AramBuddy.MainCore.Utility.GameObjects;
using EloBuddy;
using EloBuddy.SDK;
using SharpDX;

namespace AramBuddy.MainCore.Logics
{
    internal static class Detector
    {
        public static void Init()
        {
            foreach (var h in EntityManager.Heroes.AllHeroes)
                TrackedUnits.Add(new UnitTrack(h));

            Obj_AI_Base.OnBasicAttack += Obj_AI_Base_OnBasicAttack;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnBasicAttack;
            Game.OnTick += Game_OnTick;
            Obj_AI_Base.OnNewPath += Obj_AI_Base_OnNewPath;
        }

        private static void Obj_AI_Base_OnNewPath(Obj_AI_Base sender, GameObjectNewPathEventArgs args)
        {
            if(sender == null)
                return;

            var unit = TrackedUnits.FirstOrDefault(s => s.Unit.IdEquals(sender));

            if (unit == null)
            {
                TrackedUnits.Add(new UnitTrack(sender));
                return;
            }
            
            var validPath = args.Path.LastOrDefault().Distance(sender) > 75 + sender.BoundingRadius;

            if (validPath)
            {
                unit.LastCommandTick = Core.GameTickCount;
            }
        }

        private static void Game_OnTick(System.EventArgs args)
        {
            foreach (var unit in TrackedUnits.Where(u => u.Unit.IsChampion()))
            {
                if(unit != null && unit.Unit.IsValid && unit.Unit.IsHPBarRendered)
                    unit.LastVisibleTick = Core.GameTickCount;
            }
        }

        private static void Obj_AI_Base_OnBasicAttack(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if(sender == null || args.Target == null)
                return;

            var unit = TrackedUnits.FirstOrDefault(s => s.Unit.IdEquals(sender));

            if (unit == null)
            {
                TrackedUnits.Add(new UnitTrack(sender));
                return;
            }
            
            unit.Target = args.Target;
            unit.LastCommandTick = Core.GameTickCount;
            unit.AttackStartTick = Core.GameTickCount;
        }

        public static UnitTrack TrackedUnit(this Obj_AI_Base unit)
        {
            return TrackedUnits.FirstOrDefault(u => u.Unit.IdEquals(unit));
        }
        
        public static bool IsAFK(this AIHeroClient target)
        {
            var unit = TrackedUnits.FirstOrDefault(s => s.Unit.IdEquals(target));

            if (unit?.Unit == null)
                return false;

            if (unit.Unit.IsEnemy)
                return Core.GameTickCount - unit.LastVisibleTick > 180000f; // not visible for more than 3 minutes

            return Core.GameTickCount - unit.LastCommandTick > 5000f; // Last command more than 5 seconds
        }

        public static bool IsAttacking(this Obj_AI_Base caster)
        {
            return caster.TrackedUnit() != null && caster.TrackedUnit().IsAttacking;
        }
        public static GameObject Lasttarget(this Obj_AI_Base caster)
        {
            return caster.TrackedUnit()?.Target;
        }
        public static bool LastTarget(this Obj_AI_Base caster, GameObject target)
        {
            return caster.Lasttarget() != null && target != null && target.IsValid && caster.Lasttarget().IdEquals(target);
        }
        public static float LastAttack(this Obj_AI_Base caster)
        {
            var unit = caster.TrackedUnit();
            return unit?.AttackEndTick ?? int.MaxValue;
        }

        public static Vector3 LastAlliesTeamFightPos;
        public static Vector3 LastEnemiesTeamFightPos;
        private static float _lastTeamFightTick;
        private static bool _cachedTeamFight;
        public static bool TeamFightActive()
        {
            if (Core.GameTickCount - _lastTeamFightTick > 2000) // update the team fight every 2 seconds and cache it's value
            {
                
                _cachedTeamFight = false;

                var enemies = EntityManager.Heroes.Enemies.Where(a => a.IsValidTarget()).ToList();

                if (enemies.Any() && MyHero.Instance.PredictHealthPercent() > 10)
                {
                    var alliesFighting = EntityManager.Heroes.Allies.Where(a => !a.IsAFK() && a.IsValidTarget() && a.IsAttacking() && a.Lasttarget().IsChampion()).ToList();
                    var enemiesFighting = enemies.Where(a => a.IsValidTarget() && a.IsAttacking() && a.Lasttarget().IsChampion()).ToList();
                    
                    LastAlliesTeamFightPos = alliesFighting.Select(a => a.ServerPosition).ToList().CenterVectors();
                    LastEnemiesTeamFightPos = enemiesFighting.Select(a => a.ServerPosition).ToList().CenterVectors();

                    var nearestEnemy = ObjectsManager.NearestEnemy;

                    var allyScore = LastAlliesTeamFightPos.TeamTotal() > LastEnemiesTeamFightPos.TeamTotal(true) && nearestEnemy != null && MyHero.Instance.GetScore() > nearestEnemy.GetScore();

                    var myfight = MyHero.Instance.CountEnemyAlliesInRangeWithPrediction(Config.SafeValue) >= MyHero.Instance.CountEnemyHeroesInRangeWithPrediction(Config.SafeValue)
                                  && (alliesFighting.Count > 1 || allyScore);

                    var alliesalive = EntityManager.Heroes.Allies.Count(a => a.IsValidTarget() && a.IsActive());

                    _cachedTeamFight = alliesFighting.Count(a => a.CountAllyHeros(Config.SafeValue) > 1) >= alliesalive / 2 || myfight;
                    
                    _lastTeamFightTick = Core.GameTickCount;
                }
            }

            return _cachedTeamFight;
        }

        public static List<UnitTrack> TrackedUnits = new List<UnitTrack>();
        public class UnitTrack
        {
            public UnitTrack(Obj_AI_Base unit)
            {
                this.Unit = unit;
            }
            public Obj_AI_Base Unit;
            public GameObject Target;
            public float LastVisibleTick;
            public float LastCommandTick;
            public float AttackStartTick = Core.GameTickCount;
            public float AttackEndTick => Game.Ping + this.AttackStartTick + (this.Unit.AttackDelay + this.Unit.AttackCastDelay) * 1000f;
            public bool Ended => this.AttackEndTick - Core.GameTickCount < 0;
            public bool IsAttacking => !this.Ended;
        }
    }
}
