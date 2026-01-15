using Game.Core;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "NewCheckHitResultEffectData", menuName = "GameData/SkillData/CheckEffectData/HitResult")]
    public class CheckHitResultEffectData : CheckEffectData
    {
        public override SkillLogicType LogicType => SkillLogicType.CheckHitResult;

        public HitResult hitResult = HitResult.Hit; // 직전 공격의 판정 결과와 비교
        public AttackType attackType = AttackType.None; // 공격 타입 조건 (None이면 무시)
        public RangeType checkRangeType = RangeType.None; // 사거리 타입 조건 (None이면 무시)
    }
}
