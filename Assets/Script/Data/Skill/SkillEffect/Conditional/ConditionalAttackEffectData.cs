using Game.Core;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "NewConditionalAttackEffectData", menuName = "GameData/SkillData/ConditionalEffectData/Attack")]
    public class ConditionalAttackEffectData : ConditionalEffectData
    {
        public override SkillLogicType LogicType => SkillLogicType.ConditionalAttack;

        public int physPowerPercent; // 물리 공격 비례 계수
        public int magicPowerPercent; // 마법 공격 비례 계수

        public bool isUnblockable; // 가드 무시 여부
        public bool isTrueStrike; // 절대 명중 여부
    }
}
