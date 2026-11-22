using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;

public class Detect : Editor 
{
    private void OnSceneGUI()
    {
        DetectRangeTest player =  (DetectRangeTest)target;
        Handles.color = Color.cyan;
        
        Handles.DrawLine(player.transform.position, player.transform.forward * 2f,5f);
        
        /*
호의 중심은 Player 위치이고
player.transform.up을 법선 벡터로 가졌으며
player의 로컬 벡터 (0,0,1) 기준으로
_radius(DetectRangeTest class의 변수) 반지름을 가진 원을 그리는데
Arc(호)는 원의 360도 부분이다.
*/
        Handles.color = Color.white;
        Handles.DrawWireArc(player.transform.position,player.transform.up,Vector3.forward,360f,player._radius);

        Vector3 AngleToDir(float angle)
        {
            float rad = angle * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(rad), Mathf.Cos(rad));
        }

        Handles.color = Color.green;

        Vector3 rightDir = AngleToDir(player._angle * 0.5f);
        Vector3 leftDir = AngleToDir(player._angle * -1 * 0.5f);
        
        Handles.DrawLine(Vector3.zero, rightDir * player._radius);
        Handles.DrawLine(Vector3.zero, leftDir * player._radius);
        
        Handles.color = Color.red;
        
        float sqrDistance = Vector3.SqrMagnitude(player._enemy.transform.position - player.transform.position);

        if (sqrDistance < player._radius * player._radius)
        {
            Handles.DrawLine(player.transform.position, player._enemy.transform.position ,5f);
        }
    }
    
}
