namespace Game.Data
{
    using System.Collections.Generic;
    using UnityEngine;

    // 병과(이동 타입 & 지형 상성)
    public enum BranchType
    {
        [InspectorName("보병")]
        Infantry,   // 보병 (표준, 엄폐물 효과, 기병에 취약)
        [InspectorName("기병")]
        Cavalry,    // 기병 (도로 빠름, 숲 느림, 보병에 강함)
        [InspectorName("비병")]
        Flying      // 비병 (지형 무시, 궁수에 치명적)
    }

    // 병종
    public enum UnitType
    {
        None,
        [InspectorName("중장갑")]
        Armored,
        [InspectorName("술사")]
        Magic,
        [InspectorName("척후")]
        Scout,
    }

    /// <summary>
    /// 직업 데이터를 정의하는 클래스입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewJob", menuName = "GameData/JobData")]
    public class JobData : ScriptableObject
    {
        public string jobName; // 직업 이름
        public Sprite icon;

        // 1. 병과 (Branch) - 대분류
        public BranchType branch;

        // 2. 병종 (UnitTypes) - 소분류 (태그)
        public List<UnitType> unitTypes;

        public CharacterStats growthStats; // 레벨 1업 당 증가하는 능력치입니다

        public List<SkillData> skillData;
    }
}