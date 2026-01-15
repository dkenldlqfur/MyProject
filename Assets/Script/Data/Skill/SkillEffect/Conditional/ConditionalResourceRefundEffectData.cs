using Game.Core;
using System;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "NewConditionalResourceRefundEffectData", menuName = "GameData/SkillData/ConditionalEffectData/ResourceRefund")]
    public class ConditionalResourceRefundEffectData : ConditionalEffectData
    {
        public override SkillLogicType LogicType => SkillLogicType.ConditionalResourceRefund;

        [Range(0, 100)] public int refundPercent = 50; // 자원 환급 비율 (%)
    }
}
