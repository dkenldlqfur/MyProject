using Game.Core;
using Game.Data;
using System;
using UnityEngine;

namespace Game.Battle
{
    /// <summary>
    /// 스킬 로직 클래스와 SkillLogicType을 매핑하기 위한 어트리뷰트입니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class SkillLogicAttribute : Attribute
    {
        public SkillLogicType LogicType { get; }
        public SkillLogicAttribute(SkillLogicType logicType) => LogicType = logicType;
    }    

    /// <summary>
    /// 개별 스킬 효과 로직의 인터페이스입니다.
    /// </summary>
    public interface ISkillEffectLogic
    {
        void Execute(SkillEffectData sourceData, SkillContext context);
    }

    public abstract class ActionEffectLogic<T> : ISkillEffectLogic where T : ActionEffectData
    {
        public void Execute(SkillEffectData sourceData, SkillContext context)
        {
            if (sourceData is T data)
                OnExecute(data, context);
        }

        protected abstract void OnExecute(T data, SkillContext context);
    }

    /// <summary>
    /// 데미지 관련 로직의 베이스 클래스입니다.
    /// </summary>
    public abstract class ActionDamageLogic : ActionEffectLogic<ActionDamageEffectData>
    {
        protected (int phys, int magic) CalculateFinalDamage(Character caster, Character target, int physRate, int magicRate, float multiplier = 1.0f)
        {
            int rawPhys = (int)(caster.CurrentStats.physAttack * physRate * 0.01f * multiplier);
            int rawMagic = (int)(caster.CurrentStats.magicAttack * magicRate * 0.01f * multiplier);

            int finalPhys = Mathf.Max(1, rawPhys - target.CurrentStats.physDefense);
            int finalMagic = Mathf.Max(1, rawMagic - target.CurrentStats.magicDefense);

            return (finalPhys, finalMagic);
        }
    }

    public abstract class CheckEffectLogic<T> : ISkillEffectLogic where T : CheckEffectData
    {
        public void Execute(SkillEffectData sourceData, SkillContext context)
        {
            if (sourceData is T data)
            {
                bool isCheck = OnCheck(data, context);
                context.IsConditionCheck = isCheck;

                if (!isCheck && data.breakChainOnFail)
                        context.AbortProcessing = true;
            }
        }

        protected abstract bool OnCheck(T data, SkillContext context);
    }

    public abstract class ConditionalEffectLogic<T> : ISkillEffectLogic where T : ConditionalEffectData
    {
        public void Execute(SkillEffectData sourceData, SkillContext context)
        {
            if (sourceData is T data)
                OnExecute(data, context);
        }

        protected abstract void OnExecute(T data, SkillContext context);
    }

    
}