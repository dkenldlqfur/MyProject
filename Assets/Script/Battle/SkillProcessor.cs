using Game.Core;
using Game.Data;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Game.Battle
{
    /// <summary>
    /// 스킬 효과를 실제 게임 로직으로 변환하여 실행하는 프로세서 클래스입니다.
    /// </summary>
    public static class SkillProcessor
    {
        static readonly Dictionary<SkillLogicType, ISkillEffectLogic> logics = new();

        static SkillProcessor()
        {
            // 리플렉션을 사용하여 SkillLogicAttribute가 붙은 모든 ISkillEffectLogic 구현체를 찾아 등록합니다.
            var types = typeof(SkillProcessor).Assembly.GetTypes();
            foreach (var type in types)
            {
                if (type.IsAbstract || type.IsInterface)
                    continue;

                var attr = type.GetCustomAttribute<SkillLogicAttribute>();
                if (null != attr && typeof(ISkillEffectLogic).IsAssignableFrom(type))
                {
                    try
                    {
                        var instance = (ISkillEffectLogic)Activator.CreateInstance(type);
                        logics[attr.LogicType] = instance;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[SkillProcessor] Failed to instantiate logic for {type.Name}: {e.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 입력받은 스킬 효과 데이터와 컨텍스트를 사용하여 로직을 실행합니다.
        /// </summary>
        public static void Process(SkillEffectData data, SkillContext context)
        {
            if (logics.TryGetValue(data.LogicType, out var logic))
                logic.Execute(data, context);
            else
                Debug.LogWarning($"[SkillProcessor] No logic found for SkillLogicType: {data.LogicType}");
        }
    }
}