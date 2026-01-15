using Game.Core;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "NewActionSetImmunityEffectData", menuName = "GameData/SkillData/ActionEffectData/SetImmunity")]
    public class ActionSetImmunityEffectData : ActionEffectData
    {
        public override SkillLogicType LogicType => SkillLogicType.ActionSetImmunity;

        [Tooltip("면역을 부여할 공격 타입 (Flag 조합 가능)")]
        public AttackType immunityAttackType;
        [Tooltip("면역을 부여할 거리 타입")]
        public RangeType immunityRangeType;
    }
}
