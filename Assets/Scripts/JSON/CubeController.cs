using System.IO;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class EnemyStat
{
    public int maxHp;
}

public class CubeController : MonoBehaviour
{
    private string folderName = "Data";
    private string jsonFileName = "EnemyData.json";

    private int currentHp;
    private int maxHp;

    [Header("UI Settings")]
    public Slider hpBar;
    public Image hpBarFill; // 체력바의 내부 Fill 이미지 참조 추가
    public Gradient hpGradient; // 체력별 색상 지정을 위한 그라디언트 추가

    void Start()
    {
        LoadStatFromJson();
        UpdateHpBar(); // 초기 UI 및 색상 반영
    }

    private void LoadStatFromJson()
    {
        string filePath = "";
#if UNITY_EDITOR
        filePath = Path.Combine(Application.dataPath, folderName, jsonFileName);
#else
        filePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, folderName, jsonFileName);
#endif

        if (File.Exists(filePath))
        {
            string jsonText = File.ReadAllText(filePath);
            EnemyStat stat = JsonUtility.FromJson<EnemyStat>(jsonText);

            maxHp = stat.maxHp;
            currentHp = maxHp;
            Debug.Log($"<color=green>[JSON 로드 성공]</color> 초기 체력 설정: {currentHp}");
        }
        else
        {
            Debug.LogError($"[오류] JSON 파일을 찾을 수 없습니다: {filePath}");
            maxHp = 300;
            currentHp = maxHp;
        }
    }

    public void TakeDamage(int damage)
    {
        currentHp -= damage;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);

        Debug.Log($"<color=yellow>[피격]</color> 구체와 접촉! 현재 체력: {currentHp}");

        UpdateHpBar(); // 데미지 적용 후 UI 및 색상 갱신

        if (currentHp <= 0)
        {
            Die();
        }
    }

    private void UpdateHpBar()
    {
        if (hpBar != null)
        {
            hpBar.maxValue = maxHp;
            hpBar.value = currentHp;

            // 핵심 로직: 현재 슬라이더의 비율(0.0 ~ 1.0)에 따라 그라디언트 색상을 평가하여 Fill에 적용합니다.
            if (hpBarFill != null && hpGradient != null)
            {
                hpBarFill.color = hpGradient.Evaluate(hpBar.normalizedValue);
            }
        }
        else
        {
            Debug.LogWarning($"[경고] {gameObject.name}의 hpBar가 유니티 인스펙터에 할당되지 않았습니다.");
        }
    }

    private void Die()
    {
        Debug.Log("<color=red>[사망]</color> 정육면체가 파괴되었습니다.");
        Destroy(gameObject);
    }
}