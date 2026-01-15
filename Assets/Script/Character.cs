using System.Collections.Generic;
using UnityEngine;
using Game.Data;
using Game.Battle;
using System;

namespace Game.Core
{
    [System.Flags]
    public enum CrowdControlType
    {
        None = 0,
        Airborne = 1 << 0,
        Stun = 1 << 1,
        Freeze = 1 << 2,
        Poison = 1 << 3,
        Burn = 1 << 4,
        Bleed = 1 << 5,
        Slow = 1 << 6,
        Blind = 1 << 7,
        Silence = 1 << 9
    }

    /// <summary>
    /// 캐릭터의 기본 정보 및 전투 로직을 담당하는 클래스입니다.
    /// </summary>
    public class Character : MonoBehaviour
    {
        [SerializeField] CharacterData characterData; // 캐릭터 기본 데이터
        [SerializeField] List<ItemData> equippedItemData; // 장착 보너스 아이템 데이터

        [SerializeField] private Animator animator; // 애니메이터 컴포넌트
        [SerializeField] private TextMesh actionText; // 행동 텍스트 (공격, 스킬명 등 표시)
        private RuntimeAnimatorController defaultController;

        public CharacterStats BaseStats { get; private set; } // characterData + equippedItemData (기본 스탯)
        public CharacterStats CurrentStats { get; private set; } // 현재 스탯
        public CrowdControlType CurrentCrowdControlType { get; private set; } // 현재 군중 제어 상태

        public SkillData[] equippedSkills = new SkillData[10]; // 장착된 스킬 목록

        [Tooltip("비용 가중치 파라미터 (1: 선형, 2: 이차, 3: 삼차...)")]
        [Range(1.0f, 5.0f)]
        public float steepness = 2.0f;

        [Tooltip("비용 계수 (스킬의 기본 소모량을 배가시키는 정도)")]
        public float costFactor = 1.0f;
        
        [Tooltip("배치된 그리드 인덱스 (1~6)")]
        [Range(1, 6)]
        public int gridIndex;

        public bool isPlayerFaction; // 플레이어 진영 여부

        public void SetCurrentStats(CharacterStats newStats)
        {
            CurrentStats = newStats;
        }

        public void SetCurrentCrowdControlType(CrowdControlType newType)
        {
            CurrentCrowdControlType = newType;
        }

        public bool IsInterruptRequested { get; set; }

        // 면역 로직
        struct ImmunityInfo
        {
            public AttackType attackType;
            public RangeType rangeType;
        }
        private List<ImmunityInfo> temporaryImmunities = new();

        public void AddImmunity(AttackType attackType, RangeType rangeType)
        {
            var info = new ImmunityInfo { attackType = attackType, rangeType = rangeType };
            if (!temporaryImmunities.Contains(info))
                temporaryImmunities.Add(info);
        }

        public bool IsImmune(AttackType incomingAttackType, RangeType incomingRangeType)
        {
            if (AttackType.None == incomingAttackType)
                return false;

            foreach (var immunity in temporaryImmunities)
            {
                // 공격 타입 확인 (플래그)
                bool attackTypeMatch = (immunity.attackType == AttackType.None) || ((incomingAttackType & immunity.attackType) != 0);
                
                // 사거리 타입 확인 (정확한 일치 또는 None이면 모두)
                bool rangeTypeMatch = (immunity.rangeType == RangeType.None) || (immunity.rangeType == incomingRangeType);

                if (attackTypeMatch && rangeTypeMatch)
                    return true;
            }
            return false;
        }

        public void ClearImmunities()
        {
            temporaryImmunities.Clear();
        }

        // 디커플링을 위한 이벤트 정의
        public delegate HitResult AttackRequestHandler(Character attacker, Character target, int physDamage, int magicDamage, SkillEffectData sourceEffect, AttackType attackType, RangeType rangeType);
        public event AttackRequestHandler OnAttackRequested;
        
        public event Action<Character, ResourceType, int, bool> OnRestoreRequested;
        public event Action<Character, Character, CrowdControlType> OnCrowdControlRequested;
        public event Action<string> OnAnimationTriggered;

        /// <summary>
        /// 애니메이션 이벤트에서 호출되어, 특정 키에 해당하는 로직을 트리거합니다.
        /// </summary>
        public void TriggerAnimationEvent(string key)
        {
            OnAnimationTriggered?.Invoke(key);
        }

        public event Action<string> OnPlayAnimationRequested;

        public void PlayAnimation(string triggerName)
        {
            if (animator != null)
                animator.SetTrigger(triggerName);
            
            OnPlayAnimationRequested?.Invoke(triggerName);
        }

        public void PlayDodgeAnimation()
        {
            if (animator != null)
                animator.SetTrigger("Dodge");
        }

        public void SetAnimatorOverride(RuntimeAnimatorController overrideController)
        {
            if (animator != null && overrideController != null)
                animator.runtimeAnimatorController = overrideController;
        }

        public void RestoreAnimatorController()
        {
            if (animator != null && defaultController != null)
                animator.runtimeAnimatorController = defaultController;
        }

        public void SetActionText(string text)
        {
            if (actionText != null)
                actionText.text = text;
        }

        public float GetAnimationClipLength(string clipName)
        {
            if (null == animator || null == animator.runtimeAnimatorController)
                return 0f;

            foreach (var clip in animator.runtimeAnimatorController.animationClips)
                if (clipName == clip.name)
                    return clip.length;
            }
            return 0f; // 찾지 못함
        }

        void Awake()
        {
            // 초기 스탯 계산
            CharacterStats initialStats = characterData.baseStats;
            if (equippedItemData != null)
            {
                foreach (var equippedItem in equippedItemData)
                    initialStats += equippedItem.stats;
            }
            BaseStats = initialStats;
            CurrentStats = BaseStats;

            if (null == animator)
                animator = GetComponent<Animator>();

            if (animator != null)
                defaultController = animator.runtimeAnimatorController;

            // 행동 텍스트(ActionText) 설정
            if (null == actionText)
            {
                var textObj = transform.Find("ActionText");
                if (null != textObj)
                    actionText = textObj.GetComponent<TextMesh>();
            }
        }

        void Start()
        {
            // 배틀 매니저에 캐릭터 등록
            if (null != BattleManager.Instance)
                BattleManager.Instance.RegisterCharacter(this, isPlayerFaction);
        }

        /// <summary>
        /// 캐릭터의 군중 제어 상태를 설정합니다.
        /// </summary>
        public void SetCrowdControlState(CrowdControlType newState, Character attacker)
        {
            OnCrowdControlRequested?.Invoke(this, attacker, newState);
        }

        /// <summary>
        /// 캐릭터 피격 요청을 처리합니다 (이벤트 발송).
        /// </summary>
        public HitResult TakeDamage(Character attacker, int physDamage, int magicDamage, SkillEffectData sourceEffect, AttackType attackType, RangeType rangeType)
        {
            return OnAttackRequested?.Invoke(attacker, this, physDamage, magicDamage, sourceEffect, attackType, rangeType) ?? HitResult.None;
        }

        public void ApplyDamage(int damage)
        {
            if (0 > damage)
                damage = 0;
            var newStats = CurrentStats;
            newStats.hp -= damage;
            CurrentStats = newStats;
        }

        /// <summary>
        /// 캐릭터의 자원(HP, SP, MP 등)을 회복합니다.
        /// </summary>
        public void RestoreResource(ResourceType resourceType, int value, bool isOverHeal)
        {
             OnRestoreRequested?.Invoke(this, resourceType, value, isOverHeal);
        }

        /// <summary>
        /// 스킬 사용에 필요한 자원을 소모합니다. 자원이 부족하면 false를 반환합니다.
        /// </summary>
        public bool ConsumeResourceForSkill(SkillData skillData)
        {
            int totalHpCost = 0;
            int totalSpCost = 0;
            int totalMpCost = 0;

            int index = 0;
            foreach (var effect in skillData.effects)
            {
                if (0 >= effect.hpCost + effect.spCost + effect.mpCost)
                    continue;

                totalHpCost += CalculateProgressiveCost(effect.hpCost, index, steepness, costFactor);
                totalSpCost += CalculateProgressiveCost(effect.spCost, index, steepness, costFactor);
                totalMpCost += CalculateProgressiveCost(effect.mpCost, index, steepness, costFactor);

                ++index;
            }

            // 자원 부족 체크
            if (totalHpCost > CurrentStats.hp || totalSpCost > CurrentStats.sp || totalMpCost > CurrentStats.mp)
                return false;

            // 자원 차감
            var newStats = CurrentStats;
            newStats.hp -= totalHpCost;
            newStats.sp -= totalSpCost;
            newStats.mp -= totalMpCost;
            CurrentStats = newStats;

            return true;
        }

        /// <summary>
        /// 연속 사용됨에 따라 증가하는 비용을 계산합니다.
        /// </summary>
        int CalculateProgressiveCost(int baseCost, int index, float steepness, float factor)
        {
            if (0 == baseCost)
                return 0;
            if (0 >= index)
                return baseCost;

            float multiplier = Mathf.Pow(index, steepness);
            int additionalCost = (int)(multiplier * factor);

            return baseCost + additionalCost;
        }

        /// <summary>
        /// 현재 자원으로 사용 가능한 액티브 스킬이 하나라도 있는지 확인합니다.
        /// (기본 비용 기준으로만 체크)
        /// </summary>
        public bool HasEnoughResourceForAnyActiveSkill()
        {
            foreach (var skill in equippedSkills)
            {
                if (null == skill || SkillType.Active != skill.skillType)
                    continue;

                int totalHpCost = 0;
                int totalSpCost = 0;
                int totalMpCost = 0;

                foreach (var effect in skill.effects)
                {
                    totalHpCost += effect.hpCost;
                    totalSpCost += effect.spCost;
                    totalMpCost += effect.mpCost;
                }

                if (CurrentStats.hp >= totalHpCost && CurrentStats.sp >= totalSpCost && CurrentStats.mp >= totalMpCost)
                    return true;
            }

            return false;
        }
    }
}