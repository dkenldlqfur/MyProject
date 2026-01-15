using Game.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "NewActionChainDamageEffectData", menuName = "GameData/SkillData/ActionEffectData/ChainDamage")]
    public class ActionChainDamageEffectData : ActionDamageEffectData
    {
        public enum ChainMode
        {
            Decay,        // 거리가 멀어질수록 감쇠
            Distributed   // 피해량을 대상 수만큼 나눔
        }

        public override SkillLogicType LogicType => SkillLogicType.ActionChainDamage;
        
        public ChainMode chainMode;
        public int maxTargets = 0;
        public int decayRate = 0; // 감쇠율 (%)
        public List<int> decayRates = new(); // 수동 감쇠 리스트
    }
}
