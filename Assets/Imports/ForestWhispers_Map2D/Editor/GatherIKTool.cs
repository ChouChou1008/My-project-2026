using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ForestWhispers.Map2D.EditorTools
{
    /// <summary>
    /// 메뉴: Forest Whispers > Enable IK Pass On Selected Controller.
    /// Project 창에서 고른 Animator Controller의 모든 레이어에 IK Pass를 켠다.
    /// </summary>
    public static class GatherIKTool
    {
        [MenuItem("Forest Whispers/Enable IK Pass On Selected Controller")]
        public static void EnableIKPass()
        {
            var controller = Selection.activeObject as AnimatorController;
            if (controller == null)
            {
                EditorUtility.DisplayDialog("Forest Whispers", "Project 창에서 Animator Controller를 먼저 선택해 주세요.", "확인");
                return;
            }

            AnimatorControllerLayer[] layers = controller.layers;
            foreach (AnimatorControllerLayer layer in layers)
            {
                layer.iKPass = true;
            }

            controller.layers = layers;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("IK Pass 켜짐: " + controller.name);
        }
    }
}
