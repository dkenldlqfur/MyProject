using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// 두 값을 비교할 때 사용하는 비교 타입입니다.
    /// </summary>
    public enum CompareType
    {
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        Equal,
        NotEqual
    }

    /// <summary>
    /// 캐릭터의 자원 타입을 정의합니다.
    /// </summary>
    [System.Flags]
    public enum ResourceType
    {
        HP = 1 << 0,
        MP = 1 << 1,
        SP = 1 << 2,
    }

    public enum TargetType
    {
        Caster,
        Target,
    }

    [System.Flags]
    public enum AttackType
    {
        None = 0,
        Physical = 1 << 0,
        Magic = 1 << 1,
        Mixed = Physical | Magic
    }

    public enum RangeType
    {
        None,
        Melee,
        Ranged,
    }

    /// <summary>
    /// 공격의 적중 결과를 나타내는 열거형입니다.
    /// </summary>
    public enum HitResult
    {
        None,
        Hit,
        Miss,
        Block,
        Critical
    }

    /// <summary>
    /// 패시브/반응 스킬의 발동 타이밍을 정의합니다.
    /// </summary>
    public enum ReactionTiming
    {
        None,

        // 공격자(Caster) 기준 트리거
        PreAttack,      // 공격을 시도하기 직전 (1회)
        OnAttack,       // 공격 적중/시도 시 (타겟마다 발동)
        PostAttack,     // 모든 공격 행위가 끝난 후 (1회)

        // 피격자(Target) 기준 트리거
        PreHit,         // 데미지 계산 전 (회피/블럭 전)
        OnHit,          // 데미지 판정 후 (블럭/회피/데미지 적용 시)
        PostHit         // 모든 공격 처리가 끝난 후 (반격 등)
    }

    /// <summary>
    /// 스킬의 효과 대상을 필터링하는 모드입니다.
    /// </summary>
    public enum TargetFilterMode
    {
        All,
        Ally,
        Enemy
    }

    /// <summary>
    /// 스킬 효과의 대분류 타입입니다.
    /// </summary>
    public enum SkillEffectType
    {
        Action,
        Check,
        Conditional
    }

    /// <summary>
    /// 스킬 효과의 세부 로직 타입입니다.
    /// </summary>
    public enum SkillLogicType
    {
        None,
        ActionDamage,
        ActionChainDamage,
        ActionResourceRestore,
        CheckHitResult,
        CheckResource,
        CheckCrowdControl,
        ConditionalResourceRefund,
        ConditionalSetCrowdControl,
        ConditionalSetResource,
        ConditionalAttack,
        ActionSetImmunity,
    }

    /// <summary>
    /// 비교 연산을 도와주는 확장 클래스입니다.
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// 정수값에 대한 비교 연산을 수행합니다.
        /// </summary>
        public static bool Evaluate(this CompareType compareType, int sourceValue, int targetValue)
        {
            return compareType switch
            {
                CompareType.GreaterThan => sourceValue > targetValue,
                CompareType.GreaterThanOrEqual => sourceValue >= targetValue,
                CompareType.LessThan => sourceValue < targetValue,
                CompareType.LessThanOrEqual => sourceValue <= targetValue,
                CompareType.Equal => sourceValue == targetValue,
                CompareType.NotEqual => sourceValue != targetValue,
                _ => false
            };
        }

        /// <summary>
        /// 부동 소수점값에 대한 비교 연산을 수행합니다 (근사치 비교 포함).
        /// </summary>
        public static bool Evaluate(this CompareType compareType, float sourceValue, float targetValue)
        {
            return compareType switch
            {
                CompareType.GreaterThan => sourceValue > targetValue,
                CompareType.GreaterThanOrEqual => sourceValue >= targetValue,
                CompareType.LessThan => sourceValue < targetValue,
                CompareType.LessThanOrEqual => sourceValue <= targetValue,
                CompareType.Equal => Mathf.Approximately(sourceValue, targetValue),
                CompareType.NotEqual => !Mathf.Approximately(sourceValue, targetValue),
                _ => false
            };
        }
    }
}