﻿using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using LeagueSharp.Common;
using Damage = LeagueSharp.Common.Damage;
using Spell = LeagueSharp.Common.Spell;

namespace ElVladimirReborn
{
    internal enum Spells
    {
        Q,

        W,

        E,

        R
    }


    internal static class Vladimir
    {
        #region Properties

        private static AIHeroClient Player
        {
            get { return ObjectManager.Player; }
        }

        #endregion

        public static bool getCheckBoxItem(Menu m, string item)
        {
            return m[item].Cast<CheckBox>().CurrentValue;
        }

        public static int getSliderItem(Menu m, string item)
        {
            return m[item].Cast<Slider>().CurrentValue;
        }

        public static bool getKeyBindItem(Menu m, string item)
        {
            return m[item].Cast<KeyBind>().CurrentValue;
        }

        public static int getBoxItem(Menu m, string item)
        {
            return m[item].Cast<ComboBox>().CurrentValue;
        }

        #region Public Methods and Operators

        public static void OnLoad()
        {
            if (ObjectManager.Player.CharData.BaseSkinName != "Vladimir")
            {
                return;
            }

            spells[Spells.Q].SetTargetted(0.25f, spells[Spells.Q].Instance.SData.MissileSpeed);
            spells[Spells.R].SetSkillshot(0.25f, 175, 700, false, SkillshotType.SkillshotCircle);

            ignite = Player.GetSpellSlot("summonerdot");

            ElVladimirMenu.Initialize();
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += Drawings.OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
        }

        #endregion

        #region Static Fields

        public static Dictionary<Spells, Spell> spells = new Dictionary<Spells, Spell>
        {
            {Spells.Q, new Spell(SpellSlot.Q, 600)},
            {Spells.W, new Spell(SpellSlot.W)},
            {Spells.E, new Spell(SpellSlot.E, 600)},
            {Spells.R, new Spell(SpellSlot.R, 700)}
        };

        private static SpellSlot ignite;

        #endregion

        #region Methods

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            var gapCloserActive = getCheckBoxItem(ElVladimirMenu.settingsMenu,
                "ElVladimir.Settings.AntiGapCloser.Active");

            if (gapCloserActive && spells[Spells.W].IsReady()
                && gapcloser.Sender.Distance(Player) < spells[Spells.W].Range
                && Player.CountEnemiesInRange(spells[Spells.Q].Range) >= 1)
            {
                spells[Spells.W].Cast();
            }
        }

        private static float GetComboDamage(Obj_AI_Base enemy)
        {
            var damage = 0d;

            if (spells[Spells.Q].IsReady())
            {
                damage += Player.GetSpellDamage(enemy, SpellSlot.Q);
            }

            if (spells[Spells.W].IsReady())
            {
                damage += Player.GetSpellDamage(enemy, SpellSlot.W);
            }

            if (spells[Spells.E].IsReady())
            {
                damage += Player.GetSpellDamage(enemy, SpellSlot.E);
            }

            if (spells[Spells.R].IsReady())
            {
                damage += Player.GetSpellDamage(enemy, SpellSlot.R);
            }
            else if (enemy.HasBuff("vladimirhemoplaguedebuff"))
            {
                damage += damage * 1.12;
            }

            return (float)(damage + Player.GetAutoAttackDamage(enemy));
        }

        private static float IgniteDamage(AIHeroClient target)
        {
            if (ignite == SpellSlot.Unknown || Player.Spellbook.CanUseSpell(ignite) != SpellState.Ready)
            {
                return 0f;
            }
            return (float)Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
        }

        private static void OnCombo()
        {
            var target = TargetSelector.GetTarget(1500, DamageType.Magical);
            if (target == null)
            {
                return;
            }

            var countEnemy = getSliderItem(ElVladimirMenu.comboMenu, "ElVladimir.Combo.Count.R");

            var comboDamage = GetComboDamage(target);

            if (getCheckBoxItem(ElVladimirMenu.comboMenu, "ElVladimir.Combo.Q") && spells[Spells.Q].IsReady()
                && target.IsValidTarget(spells[Spells.Q].Range))
            {
                spells[Spells.Q].CastOnUnit(target);
            }

            if (getCheckBoxItem(ElVladimirMenu.comboMenu, "ElVladimir.Combo.E") && spells[Spells.E].IsReady() && target.IsValidTarget(800))
            {
                if (Player.Distance(target) < 800)
                {
                    spells[Spells.E].StartCharging();
                }
            }

            if (getCheckBoxItem(ElVladimirMenu.comboMenu, "ElVladimir.Combo.W") && spells[Spells.W].IsReady()
                && target.IsValidTarget(spells[Spells.W].Range))
            {
                spells[Spells.W].Cast();
            }

            if (getCheckBoxItem(ElVladimirMenu.comboMenu, "ElVladimir.Combo.R.Killable"))
            {
                if (spells[Spells.R].IsReady() && Player.GetSpellDamage(target, SpellSlot.R) > target.Health && !target.IsDead)
                {
                    spells[Spells.R].Cast(target);
                }
                if (getCheckBoxItem(ElVladimirMenu.comboMenu, "ElVladimir.Combo.SmartUlt"))
                {
                    if (spells[Spells.Q].IsReady() && target.IsValidTarget(spells[Spells.Q].Range) && spells[Spells.Q].GetDamage(target) >= target.Health)
                    {
                        spells[Spells.Q].Cast();
                    }
                    else if (spells[Spells.R].IsReady() && GetComboDamage(target) >= target.Health && !target.IsDead)
                    {
                        spells[Spells.R].Cast(target);
                    }
                }
                else
                {
                    if (getCheckBoxItem(ElVladimirMenu.comboMenu, "ElVladimir.Combo.R") && comboDamage >= target.Health &&
                        !target.IsDead)
                    {
                        spells[Spells.R].Cast(target);
                    }
                }
            }
            else
            {
                if (getCheckBoxItem(ElVladimirMenu.comboMenu, "ElVladimir.Combo.R") && spells[Spells.R].IsReady())
                {
                    foreach (var x in HeroManager.Enemies.Where(hero => !hero.IsDead && hero.IsValidTarget(spells[Spells.R].Range)))
                    {
                        var pred = spells[Spells.R].GetPrediction(x);
                        if (pred.AoeTargetsHitCount >= countEnemy)
                        {
                            spells[Spells.R].Cast(x);
                        }
                    }
                }
            }

            if (getCheckBoxItem(ElVladimirMenu.comboMenu, "ElVladimir.Combo.Ignite") && Player.Distance(target) <= 600 && IgniteDamage(target) >= target.Health)
            {
                Player.Spellbook.CastSpell(ignite, target);
            }
        }

        private static void OnHarass()
        {
            var target = TargetSelector.GetTarget(spells[Spells.Q].Range, DamageType.Magical);
            if (target == null)
            {
                return;
            }

            if (getCheckBoxItem(ElVladimirMenu.harassMenu, "ElVladimir.Harass.Q") && spells[Spells.Q].IsReady()
                && target.IsValidTarget(spells[Spells.Q].Range))
            {
                spells[Spells.Q].CastOnUnit(target);
            }

            if (getCheckBoxItem(ElVladimirMenu.harassMenu, "ElVladimir.Harass.E") && spells[Spells.E].IsReady() && target.IsValidTarget(800))
            {
                if (Player.Distance(target) < 800)
                {
                    spells[Spells.E].StartCharging();
                }
            }
        }

        private static void OnJungleClear()
        {
            var useQ = getCheckBoxItem(ElVladimirMenu.clearMenu, "ElVladimir.JungleClear.Q");
            var useE = getCheckBoxItem(ElVladimirMenu.clearMenu, "ElVladimir.JungleClear.E");
            var playerHp = getSliderItem(ElVladimirMenu.clearMenu, "ElVladimir.WaveClear.Health.E");

            if (spells[Spells.Q].IsReady() && useQ)
            {
                var allMinions = MinionManager.GetMinions(
                    ObjectManager.Player.ServerPosition,
                    spells[Spells.W].Range,
                    MinionTypes.All,
                    MinionTeam.Neutral,
                    MinionOrderTypes.MaxHealth);
                {
                    foreach (var minion in
                        allMinions.Where(minion => minion.IsValidTarget()))
                    {
                        spells[Spells.Q].CastOnUnit(minion);
                        return;
                    }
                }
            }

            if (spells[Spells.E].IsReady() && Player.Health / Player.MaxHealth * 100 >= playerHp && useE)
            {
                var minions = MinionManager.GetMinions(
                    ObjectManager.Player.ServerPosition,
                    spells[Spells.W].Range,
                    MinionTypes.All,
                    MinionTeam.Neutral,
                    MinionOrderTypes.MaxHealth);
                if (minions.Count <= 0)
                {
                    return;
                }

                if (minions.Count > 1)
                {
                    spells[Spells.E].StartCharging();
                }
            }
        }

        private static void OnLaneClear()
        {
            var useQ = getCheckBoxItem(ElVladimirMenu.clearMenu, "ElVladimir.WaveClear.Q");
            var useE = getCheckBoxItem(ElVladimirMenu.clearMenu, "ElVladimir.WaveClear.E");
            var playerHp = getSliderItem(ElVladimirMenu.clearMenu, "ElVladimir.WaveClear.Health.E");

            if (spells[Spells.Q].IsReady() && useQ)
            {
                var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, spells[Spells.Q].Range);
                {
                    foreach (var minion in
                        allMinions.Where(
                            minion => minion.Health <= ObjectManager.Player.GetSpellDamage(minion, SpellSlot.Q)))
                    {
                        if (minion.IsValidTarget())
                        {
                            spells[Spells.Q].CastOnUnit(minion);
                            return;
                        }
                    }
                }
            }

            if (spells[Spells.E].IsReady() && Player.Health / Player.MaxHealth * 100 >= playerHp && useE)
            {
                var minions = MinionManager.GetMinions(Player.ServerPosition, spells[Spells.E].Range);
                if (minions.Count <= 0)
                {
                    return;
                }

                if (minions.Count > 1)
                {
                    spells[Spells.E].StartCharging();
                }
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                OnCombo();
            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
            {
                OnHarass();
            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear) ||
                Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                OnLaneClear();
                OnJungleClear();
            }
        }

        #endregion
    }
}