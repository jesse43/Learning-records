using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LightingController0))]
public class LightingControllerGUI0 : Editor
{
    // 重载GUI绘制方法
    public override void OnInspectorGUI()
    {
        // 获取控制器
        var controller = target as LightingController0;
        // 绘制功能区按钮
        DrawFunctionButtons(controller);
        // 绘制参数面板
        DrawGlobleProperties(controller);
    }
    // 绘制功能区按钮
    private void DrawFunctionButtons(LightingController0 controller)
    {
        // 第一行 多重烘焙
        if (GUILayout.Button("多重烘焙"))
            controller.MultiBake();
        //第二行
        EditorGUILayout.BeginHorizontal();
        {
            //实时光照按钮
            if (GUILayout.Button("实时光照"))
            {
                Lightmapping.Clear();
            }
            //烘焙并预览主光按钮
            if (GUILayout.Button("主光烘焙"))
                controller.Bake(LightingController0.BakeMode.BakeMainlight);
            //烘焙并预览天光按钮
            if (GUILayout.Button("天光烘焙"))
                controller.Bake(LightingController0.BakeMode.BakeSkylight);
            //烘焙并预览自发光GI按钮
            if (GUILayout.Button("自发光烘焙"))
                controller.Bake(LightingController0.BakeMode.BakeEmissionGI);
        }
        EditorGUILayout.EndHorizontal();
    }
    // 组开关变量
    private bool _groupAToggle;
    private bool _groupBToggle;
    private bool _groupCToggle;
    private bool _groupDToggle;
    private bool _groupEToggle;
    //绘制Shader全局参数GUI
    private void DrawGlobleProperties(LightingController0 controller)
    {
        // 开始参数修改检测
        EditorGUI.BeginChangeCheck();
        {
            //参数组A: 材质属性
            _groupAToggle = EditorGUILayout.BeginFoldoutHeaderGroup(_groupAToggle, "材质属性");
            if (_groupAToggle)
            {
                controller.metalDarken = EditorGUILayout.Slider(
                    "金属压暗",
                    controller.metalDarken,
                    0.0f, 5.0f);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            // 参数组B: 主光配置
            _groupBToggle = EditorGUILayout.BeginFoldoutHeaderGroup(_groupBToggle, "主光配置");
            if (_groupBToggle)
            {
                controller.mainLightCol = EditorGUILayout.ColorField(
                    "主光颜色",
                    controller.mainLightCol);
                controller.specParams = EditorGUILayout.Vector4Field(
                    "高光参数",
                    controller.specParams);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            // 参数组C: 天光配置
            _groupCToggle = EditorGUILayout.BeginFoldoutHeaderGroup(_groupCToggle, "天光配置");
            if (_groupCToggle)
            {
                controller.skylightInt = EditorGUILayout.Slider(
                    "天光强度",
                    controller.skylightInt,
                    0.0f, 5.0f);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            // 参数组D: 反射配置
            _groupDToggle = EditorGUILayout.BeginFoldoutHeaderGroup(_groupDToggle, "反射配置");
            if (_groupDToggle)
            {
                controller.reflectParams = EditorGUILayout.Vector4Field(
                    "反射参数",
                    controller.reflectParams);

                controller.fresnelPow = EditorGUILayout.Slider(
                    "菲涅尔次幂",
                    controller.fresnelPow,
                    0.1f, 50.0f);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            // 参数组E: 自发光GI配置
            _groupEToggle = EditorGUILayout.BeginFoldoutHeaderGroup(_groupEToggle, "自发光GI配置");
            if (_groupEToggle)
            {
                EditorGUILayout.BeginFadeGroup(1.0f);
                controller.emissionCol = EditorGUILayout.ColorField(
                    "自发光GI颜色",
                    controller.emissionCol);
                EditorGUILayout.EndFadeGroup();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        // 结束参数修改检测 变则设置shader全局参数
        if (EditorGUI.EndChangeCheck())
        {
            controller.UpdateGlobalProperties();
            EditorUtility.SetDirty(controller);
        }
    }
}
