namespace Game.Data
{
    using UnityEngine;

    // 종족 (태생적 분류 - 캐릭터 고유)
    public enum RaceType
    {
        Human,      // 인간
        Undead,     // 언데드
    }

    /// <summary>
    /// 종족 데이터를 정의하는 클래스입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewRace", menuName = "GameData/RaceData")]
    public class RaceData : ScriptableObject
    {
        public RaceType raceType;
        public string raceName; // 종족 이름
        public SkillData racialSkill; // 이 종족이 가진 특성
    }
}