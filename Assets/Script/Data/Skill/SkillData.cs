using System.Collections.Generic;
using UnityEngine;
using Game.Core;

namespace Game.Data
{
    /// <summary>
    /// 스킬의 종류(액티브, 패시브)를 구분하는 열거형입니다.
    /// </summary>
    public enum SkillType
    {
        Active, // 자신의 턴에 사용하는 스킬
        Passive // 특정 조건 만족 시 자동으로 발동하는 패시브/리액션 스킬
    }

    /// <summary>
    /// 스킬의 대상 범위를 정의하는 비트 플래그 열거형입니다.
    /// </summary>
    [System.Flags]
    public enum ScopeType
    {
        None = 0,
        [InspectorName("진영/자신")] Self = 1 << 0,
        [InspectorName("진영/아군(자신 제외)")] Ally = 1 << 1,
        [InspectorName("진영/아군 전체")] Party = Self | Ally,
        [InspectorName("진영/적군")] Enemy = 1 << 3,
        [InspectorName("진영/모두")] AnyFaction = Party | Enemy,
        [InspectorName("범위에 따른 대상/단일")] Single = 1 << 5,
        [InspectorName("범위에 따른 대상/다중")] Multi = 1 << 6,
        [InspectorName("범위에 따른 대상/가로열")] Row = 1 << 7,
        [InspectorName("범위에 따른 대상/세로열")] Column = 1 << 8,
        [InspectorName("범위에 따른 대상/전열")] Frontal = 1 << 9,
        [InspectorName("범위에 따른 대상/전체")] All = 1 << 10
    }

    /// <summary>
    /// 스킬 데이터를 정의하는 클래스입니다.
    /// </summary>
    public abstract class SkillData : ScriptableObject
    {
        public abstract SkillType SkillType { get; }

        public string skillName; // 스킬 이름
        [TextArea] public string description; // 스킬 설명
        public SkillType skillType; // 스킬 타입
        public RangeType rangeType; // 사거리 타입
        public ScopeType scopeType; // 대상 범위 타입
        public int targetCount; // scopeType이 Multi일 때만 유효한 대상 수
        public List<SkillEffectData> effects = new(); // 스킬이 가진 효과 목록
    }

    [CreateAssetMenu(fileName = "NewActiveSkillData", menuName = "GameData/SkillData/ActiveSkillData")]
    public class ActiveSkillData : SkillData
    {
        public override SkillType SkillType => SkillType.Active;
    }

    [CreateAssetMenu(fileName = "NewPassiveSkillData", menuName = "GameData/SkillData/PassiveSkillData")]
    public class PassiveSkillData : SkillData
    {
        public override SkillType SkillType => SkillType.Passive;

        public ReactionTiming reactionTiming = ReactionTiming.OnHit;
    }
}