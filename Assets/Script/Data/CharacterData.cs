namespace Game.Data
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 캐릭터의 스탯 정보를 담는 구조체입니다.
    /// </summary>
    [Serializable]
    public struct CharacterStats
    {
        public int hp;
        public int sp;                      // 물리 스킬 사용 시 소모 자원
        public int mp;                      // 마법 스킬 사용 시 소모 자원
        public int physAttack;              // 물리 공격력
        public int physDefense;             // 물리 방어력
        public int magicAttack;             // 마법 공격력
        public int magicDefense;            // 마법 방어력
        public int speed;                   // 행동 속도
        public int dodgeRate;               // 회피율
        public int accuracyRate;            // 명중률
        public int criticalRate;            // 크리티컬 확률
        public int criticalDamageRate;      // 크리티컬 대미지 비율
        public int blockRate;               // 방어(블럭) 확률
        public int blockDamageReduceRate;   // 블럭 시 데미지 감소 비율

        public static CharacterStats operator +(CharacterStats a, CharacterStats b)
        {
            return new CharacterStats
            {
                hp = a.hp + b.hp,
                sp = a.sp + b.sp,
                mp = a.mp + b.mp,
                physAttack = a.physAttack + b.physAttack,
                physDefense = a.physDefense + b.physDefense,
                magicAttack = a.magicAttack + b.magicAttack,
                magicDefense = a.magicDefense + b.magicDefense,
                speed = a.speed + b.speed,
                dodgeRate = a.dodgeRate + b.dodgeRate,
                accuracyRate = a.accuracyRate + b.accuracyRate,
                criticalRate = a.criticalRate + b.criticalRate,
                criticalDamageRate = a.criticalDamageRate + b.criticalDamageRate,
                blockRate = a.blockRate + b.blockRate,
                blockDamageReduceRate = a.blockDamageReduceRate + b.blockDamageReduceRate
            };
        }

        public static CharacterStats operator -(CharacterStats a, CharacterStats b)
        {
            return new CharacterStats
            {
                hp = a.hp - b.hp,
                sp = a.sp - b.sp,
                mp = a.mp - b.mp,
                physAttack = a.physAttack - b.physAttack,
                physDefense = a.physDefense - b.physDefense,
                magicAttack = a.magicAttack - b.magicAttack,
                magicDefense = a.magicDefense - b.magicDefense,
                speed = a.speed - b.speed,
                dodgeRate = a.dodgeRate - b.dodgeRate,
                accuracyRate = a.accuracyRate - b.accuracyRate,
                criticalRate = a.criticalRate - b.criticalRate,
                criticalDamageRate = a.criticalDamageRate + b.criticalDamageRate,
                blockRate = a.blockRate - b.blockRate,
                blockDamageReduceRate = a.blockDamageReduceRate - b.blockDamageReduceRate
            };
        }
    }

    /// <summary>
    /// 캐릭터의 고유 어셋 데이터를 정의하는 클래스입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacter", menuName = "GameData/CharacterData")]
    public class CharacterData : ScriptableObject
    {
        public string characterName;        // 캐릭터 이름
        public Sprite icon;                 // UI용 아이콘
        public GameObject prefab;           // 인게임 소환 프리팹
        public JobData jobData;             // 직업 데이터
        public RaceData raceData;           // 종족 데이터
        public CharacterStats baseStats;    // 기본 스탯

        public List<SkillEffectData> skillsEffects; // 보유 스킬 효과들
        public List<SkillData> skills; // 보유 스킬 목록
    }
}