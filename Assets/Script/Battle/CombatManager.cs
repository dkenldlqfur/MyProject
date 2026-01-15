using UnityEngine;
using Game.Core;
using Game.Data;
using System;
using System.Collections.Generic;

namespace Game.Battle
{
    public class CombatManager
    {
        readonly UnitManager unitManager;
        readonly Action<Character, SkillData, List<Character>, HitResult, AttackType, RangeType> executeSkillCallback;

        public CombatManager(UnitManager unitManager, Action<Character, SkillData, List<Character>, HitResult, AttackType, RangeType> executeSkillCallback)
        {
            this.unitManager = unitManager;
            this.executeSkillCallback = executeSkillCallback;
        }

        /// <summary>
        /// 물리/마법 데미지 공격을 처리합니다.
        /// 방어력 및 면역 계산, 회피/블럭/크리티컬 판정 후 최종 데미지를 적용합니다.
        /// </summary>
        /// <param name="physDamage">방어력이 적용되지 않은 순수 물리 공격력 (Raw Damage)</param>
        /// <param name="magicDamage">방어력이 적용되지 않은 순수 마법 공격력 (Raw Damage)</param>
        public HitResult ProcessAttack(Character attacker, Character target, int physDamage, int magicDamage, SkillEffectData sourceEffect, AttackType attackType, RangeType rangeType)
        {
            if (null == target)
                return HitResult.None;

            // 1. PreHit 반응 스킬 (방어 버프 등 선적용)
            ProcessReactionSkills(target, attacker, ReactionTiming.PreHit, HitResult.Hit, attackType, rangeType);

            // 2. 면역 체크
            if (target.IsImmune(attackType, rangeType))
            {
                physDamage = 0;
                magicDamage = 0;
                Debug.Log($"[Combat] {target.name} blocked damage due to immunity ({attackType} + {rangeType})");
            }
            
            // 3. 방어력 적용 (PreHit 이후 계산)
            int reducedPhys = (physDamage > 0) ? Mathf.Max(1, physDamage - target.CurrentStats.physDefense) : 0;
            int reducedMagic = (magicDamage > 0) ? Mathf.Max(1, magicDamage - target.CurrentStats.magicDefense) : 0;

            if (physDamage == 0 && magicDamage == 0)
            {
                reducedPhys = 0;
                reducedMagic = 0;
            }

            bool isTrueStrike = false;
            if (sourceEffect is ActionDamageEffectData damageEffect)
                isTrueStrike = damageEffect.isTrueStrike;

            // 4. 회피 판정 (Avoidance)
            if (CheckAvoidance(attacker, target))
            {
                if (!isTrueStrike)
                {
                    if (null != attacker)
                        ProcessReactionSkills(attacker, target, ReactionTiming.OnAttack, HitResult.Miss, attackType, rangeType);
                    
                    ProcessReactionSkills(target, attacker, ReactionTiming.OnHit, HitResult.Miss, attackType, rangeType);
                    return HitResult.Miss;
                }
            }

            // 5. 블럭 판정 (Block)
            bool isBlocked = CheckBlock(target);
            float damageMultiplier = isBlocked ? 0.5f : 1.0f; // 기본 블럭 감소율 50%

            // 6. 크리티컬 판정 (Critical)
            bool isCritical = false;
            if (!isBlocked) // 블럭 시 크리티컬 무효화
            {
                isCritical = CheckCritical(attacker, target);
                if (isCritical)
                    damageMultiplier *= 1.5f; // 기본 크리티컬 계수 150%
            }

            // 7. 최종 데미지 적용
            int finalDamage = (int)((reducedPhys + reducedMagic) * damageMultiplier);
            target.ApplyDamage(finalDamage);

            HitResult hitResult = isCritical ? HitResult.Critical : (isBlocked ? HitResult.Block : HitResult.Hit);

            // 8. OnAttack / OnHit 반응 스킬
            if (null != attacker)
                ProcessReactionSkills(attacker, target, ReactionTiming.OnAttack, hitResult, attackType, rangeType);
            
            ProcessReactionSkills(target, attacker, ReactionTiming.OnHit, hitResult, attackType, rangeType);

            // 9. 피격 상태 (인터럽트 등)
            if (hitResult == HitResult.Hit || hitResult == HitResult.Critical)
                target.IsInterruptRequested = true;

            return hitResult;
        }

        public void ProcessRestoreResource(Character target, ResourceType resourceType, int value, bool isOverHeal)
        {
            if (null == target)
                return;

            target.RestoreResource(resourceType, value, isOverHeal); // 추후 OverHeal 로직 분리 가능
        }

        public void ProcessCrowdControlState(Character target, Character source, CrowdControlType ccType)
        {
            if (null == target)
                return;

            target.SetCrowdControlState(ccType, source);
        }

        /// <summary>
        /// 반응형 스킬(패시브)을 체크하고 실행합니다.
        /// </summary>
        /// <param name="timing">발동 타이밍 필터 (OnHit: 공격 순간, PostAttack: 공격 완료 후)</param>
        /// <param name="hitResult">피격 결과 (Hit, Miss, Block, Critical)</param>
        public void ProcessReactionSkills(Character owner, Character triggerSource, ReactionTiming timing, HitResult hitResult = HitResult.Hit, AttackType attackType = AttackType.None, RangeType rangeType = RangeType.None)
        {
            if (null == owner)
                return;

            foreach (var skill in owner.equippedSkills)
            {
                if (null == skill)
                    continue;

                // PassiveSkillData만 처리
                if (skill is not PassiveSkillData passiveSkill)
                    continue;

                // 타이밍 필터링
                if (passiveSkill.reactionTiming != timing)
                    continue;

                // 사망 시에는 자신 대상 스킬만 가능하도록 제한
                if (0 >= owner.CurrentStats.hp && !skill.scopeType.HasFlag(ScopeType.Self))
                    continue;

                var targetList = new List<Character>();
                if (skill.scopeType.HasFlag(ScopeType.Self))
                    targetList.Add(owner);
                else if (null != triggerSource && skill.scopeType.HasFlag(ScopeType.Enemy))
                    targetList.Add(triggerSource);

                if (0 < targetList.Count)
                    executeSkillCallback?.Invoke(owner, skill, targetList, hitResult, attackType, rangeType); // HitResult, AttackType, RangeType 전달
            }
        }

        // --- 내부 계산 로직 ---

        bool CheckAvoidance(Character attacker, Character target)
        {
            if (null == target) return false;
            
            // 단순화된 명중/회피 공식:
            // 명중률 = (공격자 명중 - 방어자 회피) + 기본 85%
            // 실제 구현에서는 Random.Range 등을 사용
            int hitChance = attacker.CurrentStats.accuracyRate - target.CurrentStats.dodgeRate;
            int roll = UnityEngine.Random.Range(0, 100);
            
            return roll >= hitChance;
        }

        bool CheckBlock(Character target)
        {
            if (null == target) return false;

            // 방어율 = 방어자 블럭 확률
            int blockChance = target.CurrentStats.blockRate;
            int roll = UnityEngine.Random.Range(0, 100);

            return roll < blockChance;
        }

        bool CheckCritical(Character attacker, Character target)
        {
            if (null == attacker) return false;

            // 치명타율 = 공격자 치명타 (저항 없음)
            // CharacterStats에 critResist가 없으므로 공격자 치명타 확률만 사용하거나, 
            // 방어 스탯을 활용하는 방식으로 기획 변경 필요. 현재는 단순히 공격자 치명타율만 사용.
            int critChance = attacker.CurrentStats.criticalRate;
            int roll = UnityEngine.Random.Range(0, 100);

            return roll < critChance;
        }
    }
}
