using Game.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// 스킬 실행 중 데이터 공유를 위한 키 정의 클래스입니다.
    /// </summary>
    public static class ContextKeys
    {
        public const string PhysDamage = "PhysDamage";
        public const string MagicDamage = "MagicDamage";
        public const string IsConditionCheck = "IsConditionCheck";
        public const string LastHitResult = "LastHitResult";
        public const string LastAttackType = "LastAttackType";
        public const string LastRangeType = "LastRangeType";
        public const string AbortProcessing = "AbortProcessing";

        public const string HpCost = "HpCost";
        public const string SpCost = "SpCost";
        public const string MpCost = "MpCost";

        public const string HpRestore = "HpRestore";
        public const string SpRestore = "SpRestore";
        public const string MpRestore = "MpRestore";
    }

    /// <summary>
    /// 스킬 실행 프로세스 중 정보를 유지하고 전달하는 데이터 버스 클래스입니다.
    /// </summary>
    public class SkillContext
    {
        public Character caster;
        public Character target;

        readonly Dictionary<string, object> dataBus = new();

        public SkillContext(Character caster, Character target)
        {
            this.caster = caster;
            this.target = target;

            // 기본값 초기화
            Set(ContextKeys.LastHitResult, HitResult.Hit);
            Set(ContextKeys.IsConditionCheck, false);
            Set(ContextKeys.AbortProcessing, false);
        }

        #region 데이터 버스 제어
        public void Set<T>(string key, T value)
        {
            dataBus[key] = value;
        }

        public T Get<T>(string key, T defaultValue = default)
        {
            if (dataBus.TryGetValue(key, out var value))
                return (T)value;

            return defaultValue;
        }

        public void AddInt(string key, int amount)
        {
            int current = Get<int>(key);
            Set(key, current + amount);
        }
        #endregion

        #region 편의용 프로퍼티
        public int PhysDamage { get => Get<int>(ContextKeys.PhysDamage); set => Set(ContextKeys.PhysDamage, value); }
        public int MagicDamage { get => Get<int>(ContextKeys.MagicDamage); set => Set(ContextKeys.MagicDamage, value); }
        public bool IsConditionCheck { get => Get<bool>(ContextKeys.IsConditionCheck); set => Set(ContextKeys.IsConditionCheck, value); }
        public HitResult LastHitResult { get => Get<HitResult>(ContextKeys.LastHitResult); set => Set(ContextKeys.LastHitResult, value); }
        public AttackType LastAttackType { get => Get<AttackType>(ContextKeys.LastAttackType); set => Set(ContextKeys.LastAttackType, value); }
        public RangeType LastRangeType { get => Get<RangeType>(ContextKeys.LastRangeType); set => Set(ContextKeys.LastRangeType, value); }
        public bool AbortProcessing { get => Get<bool>(ContextKeys.AbortProcessing); set => Set(ContextKeys.AbortProcessing, value); }

        public int HpCost { get => Get<int>(ContextKeys.HpCost); set => Set(ContextKeys.HpCost, value); }
        public int SpCost { get => Get<int>(ContextKeys.SpCost); set => Set(ContextKeys.SpCost, value); }
        public int MpCost { get => Get<int>(ContextKeys.MpCost); set => Set(ContextKeys.MpCost, value); }

        public int HpRestore { get => Get<int>(ContextKeys.HpRestore); set => Set(ContextKeys.HpRestore, value); }
        public int SpRestore { get => Get<int>(ContextKeys.SpRestore); set => Set(ContextKeys.SpRestore, value); }
        public int MpRestore { get => Get<int>(ContextKeys.MpRestore); set => Set(ContextKeys.MpRestore, value); }
        #endregion
    }

    /// <summary>
    /// 모든 스킬 효과 데이터의 베이스가 되는 가상 클래스입니다.
    /// </summary>
    [Serializable]
    public abstract class SkillEffectData : ScriptableObject
    {
        public abstract SkillEffectType EffectType { get; }
        public abstract SkillLogicType LogicType { get; } // 연결될 로직 클래스를 결정하는 타입
 
        public string effectDescription; // 기획용 설명

        [Tooltip("애니메이션 이벤트 키 (설정 시 해당 키가 호출될 때까지 대기 후 실행)")]
        public string triggerKey;

        [Tooltip("이펙트 시작 시 재생할 애니메이션 트리거 이름 (조립형 스킬용)")]
        public string animationTriggerName;

        public int hpCost = 0; // 효과 추가 소모 HP
        public int spCost = 0; // 효과 추가 소모 SP
        public int mpCost = 0; // 효과 추가 소모 MP

        [Header("Animation Settings")]
        public RuntimeAnimatorController animatorOverride; // 스킬 전용 애니메이터 컨트롤러 (Attack, Return 스테이트 포함 필수)
    }

    public abstract class ActionEffectData : SkillEffectData
    {
        public override SkillEffectType EffectType => SkillEffectType.Action;
    }

    public abstract class CheckEffectData : SkillEffectData
    {
        public override SkillEffectType EffectType => SkillEffectType.Check;

        [Tooltip("조건 체크 실패 시 이후의 체인 효과들을 실행하지 않을지 여부")]
        public bool breakChainOnFail = false;
    }

    public abstract class ConditionalEffectData : SkillEffectData
    {
        public override SkillEffectType EffectType => SkillEffectType.Conditional;
    }
}
