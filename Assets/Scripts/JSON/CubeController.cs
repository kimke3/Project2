using System.IO;
using UnityEngine;

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

    void Start()
    {
        LoadStatFromJson();
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
            currentHp = 300; 
        }
    }

    // 구체의 스크립트에서 호출해주는 데미지 연산 함수입니다.
    public void TakeDamage(int damage)
    {
        currentHp -= damage;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);

        Debug.Log($"<color=yellow>[피격]</color> 구체와 접촉! 현재 체력: {currentHp}");

        if (currentHp <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("<color=red>[사망]</color> 정육면체가 파괴되었습니다.");
        Destroy(gameObject);
    }
}