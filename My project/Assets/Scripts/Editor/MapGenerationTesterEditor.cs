using UnityEditor;
using UnityEngine;
using RPG.Map.Test;

namespace RPG.Map.Test.Editor
{
    /// <summary>
    /// 自定义 Inspector 面板编辑器类
    /// 职责：为 MapGenerationTester 组件在 Inspector 上提供一个一键触发的图形按钮，
    /// 支持无需进入游戏 Play Mode，即可在编辑器内瞬时运行全套集成测试用例。
    /// </summary>
    [CustomEditor(typeof(MapGenerationTester))]
    public class MapGenerationTesterEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // 绘制默认的公有变量 Inspector 面板
            DrawDefaultInspector();

            MapGenerationTester tester = (MapGenerationTester)target;

            GUILayout.Space(15);

            // 绘制绿色的高颜值一键运行测试按钮
            GUI.backgroundColor = new Color(0.18f, 0.8f, 0.44f); // 优雅的扁平化翠绿色
            if (GUILayout.Button("▶ Run Map Generation Tests", GUILayout.Height(38)))
            {
                tester.RunAllTests();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(10);
            EditorGUILayout.HelpBox("提示：点击上方绿色按钮可以在【非运行模式】或【运行模式】下，一键对地形气候层（Layer 0）与资源分布层（Layer 1）进行 8 项集成测试与断言校验。结果将输出在 Console 面板。", MessageType.Info);
        }
    }
}
