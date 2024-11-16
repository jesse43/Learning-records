using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class LightingController0 : MonoBehaviour
{
    public float metalDarken;//默认就是0.0f
    public Color mainLightCol = Color.white;
    public Vector4 specParams = new Vector4(10.0f, 1.0f, 30.0f, 1.0f);
    public float skylightInt = 1.0f;
    public Vector4 reflectParams = new Vector4(7.0f, 1.0f, 1.0f, 1.0f);
    public float fresnelPow = 5.0f;
    public Color emissionCol = Color.white;

    private void OnEnable()
    {
        UpdateGlobalProperties();
    }
    
    // 更新Shader全局参数
    public void UpdateGlobalProperties()
    {
        //注意: Shader的各Set接口都支持按参数名或按参数ID设置 后者性能较好 Runtime代码需先缓存ID 避免按名Set
        Shader.SetGlobalFloat("_G_MetalDarken", metalDarken);
        Shader.SetGlobalColor("_G_MainLightCol", mainLightCol);
        Shader.SetGlobalVector("_G_SpecParams", specParams);
        Shader.SetGlobalFloat("_G_SkyLightInt", skylightInt);
        Shader.SetGlobalVector("_G_ReflectParams", reflectParams);
        Shader.SetGlobalFloat("_G_FresnelPow", metalDarken);
        Shader.SetGlobalColor("_G_EmissionCol", emissionCol);
    }
    
    //结构：保存lightmap信息
    private struct LightmapsInfo
    {
        //数量
        public readonly int lightmapsCount;
        //信息<路径，纹理对象>
        public readonly Dictionary<string, Texture2D> lightmapInfo;
        //资产路径
        public readonly string assetPath;
        //构造方法
        public LightmapsInfo(LightmapData[] lightmapData)
        {
            //获取lightmap数量
            lightmapsCount = lightmapData.Length;
            //创建字典<路径，纹理>彬填充
            lightmapInfo = new Dictionary<string, Texture2D>();
            var path = "";
            foreach (var data in lightmapData)
            {
                var texture = data.lightmapColor;
                path = AssetDatabase.GetAssetPath(texture);
                lightmapInfo.Add(path, texture);
            }
            //获取lightmap资产路径
            assetPath = lightmapsCount < 0 ? "" : Path.GetDirectoryName(path);
            //打印日志
            if (lightmapsCount < 0)
            {
                Debug.Log("LightmapsInfo/l: lightmap信息为空");
            }
            else
            {
                Debug.Log("LightmapsInfo: lightmap数量: " + lightmapsCount);
                Debug.Log("LightmapsInfo: 资产路径: " + assetPath);
            }
        }
    }
    
    //结构： lightmaps缓存
    private struct LightmapsBuffer
    {
        //lightmap缓存类型
        public enum BufferType
        {
            MainLight,
            SkyLight,
            EmissionGI,
            Lightmap,
        }
        //lightmap缓存
        private Texture2D[] _bufferA;
        private Texture2D[] _bufferB;
        private Texture2D[] _bufferC;
        private Texture2D[] _lightmap;
        //清理缓存方法
        private void ClearBuffer(BufferType type)
        {
            switch (type)
            {
                case BufferType.MainLight:
                    if (_bufferA == null) return;
                    foreach (var texture in _bufferA)
                        DestroyImmediate(texture);
                    _bufferA = null;
                    Debug.Log("LightmapsBuffer :缓存A已清除");
                    return;
                case BufferType.SkyLight:
                    if (_bufferB == null) return;
                    foreach (var texture in _bufferB)
                        DestroyImmediate(texture);
                    _bufferB = null;
                    Debug.Log("LightmapsBuffer: 缓存B已清理.");
                    return;
                case BufferType.EmissionGI:
                    if (_bufferC == null) return;
                    foreach (var texture in _bufferC)
                        DestroyImmediate(texture);
                    _bufferC = null;
                    Debug.Log("LightmapsBuffer: 缓存C已清理.");
                    return;
                case BufferType.Lightmap:
                    if (_lightmap == null) return;
                    foreach (var texture in _lightmap)
                        DestroyImmediate(texture);
                    _lightmap = null;
                    Debug.Log("LightmapsBuffer: Lightmap已清理.");
                    return;
                default:
                    return;
            }
        }
        //清理所有缓存
        public void Clear()
        {
            if (_bufferA != null)
            {
                foreach(var texture in _bufferA)
                    DestroyImmediate(texture);
                _bufferA = null;
            }
            if (_bufferB != null)
            {
                foreach (var texture in _bufferB)
                    DestroyImmediate(texture);
                _bufferB = null;
            }
            if (_bufferC != null)
            {
                foreach (var texture in _bufferC)
                    DestroyImmediate(texture);
                _bufferC = null;
            }
            if (_lightmap != null)
            {
                foreach (var texture in _lightmap)
                    DestroyImmediate(texture);
                _lightmap = null;
            }
            Debug.Log("LightmapsBuffer: 缓存已全部清理.");
        }
        //从LightmapInfo写入缓存
        public void WriteBuffer(LightmapsInfo info, BufferType type)
        {
            //拒绝合成Lightmap缓存的写入
            if (type == BufferType.Lightmap) return;
            //  清理缓存
            ClearBuffer(type);
            //创建缓存并从Info中复制纹理
            var lightmapsCount = info.lightmapsCount;
            var buffer = new Texture2D[lightmapsCount];
            for (var i = 0; i < lightmapsCount; i++)
            {
                var lightmap = info.lightmapInfo.Values.ElementAt(i);
                buffer[i] = new Texture2D(lightmap.width, lightmap.height, lightmap.format, false);
                Graphics.CopyTexture(lightmap,0,0,buffer[i],0,0);
            }
            // 指定到目标缓存
            switch (type)
            {
                case BufferType.MainLight:
                    _bufferA = buffer;
                    Debug.Log("LightmapsBuffer: 缓存A已写入");
                    break;
                case BufferType.SkyLight:
                    _bufferB = buffer;
                    Debug.Log("LightmapsBuffer: 缓存B已写入.");
                    break;
                case BufferType.EmissionGI:
                    _bufferC = buffer;
                    Debug.Log("LightmapsBuffer: 缓存C已写入.");
                    break;
                default:
                    return;
            }
        }
        //从缓存创建lightmap前检查
        public void CreateLightmaps()
        {
            //检查个缓存是否为空
            if (_bufferA == null || _bufferB == null || _bufferC == null)
            {
                Debug.Log("LightmapsBuffer错误： 存在空缓存！");
                return;
            }
            //检查个缓存长度是否一致
            var lightmapsCount = _bufferA.Length;
            if (lightmapsCount < 1)
            {
                Debug.Log("LightmapsBuffer错误： 存在缓存长度为0！");
                return;
            }
            //检查各缓存长度是否一致
            if (_bufferB.Length != lightmapsCount || _bufferC.Length != lightmapsCount)
            {
                Debug.Log("LightmapsBuffer: 各缓存数量不一致！");
                return;
            }
            //检查各缓存纹理尺寸格式是否一致
            var lightmapsWidth = new int[lightmapsCount];
            var lightmapsHeight = new int[lightmapsCount];
            var lightmapsFormat = _bufferA[0].format;
            for (var i = 0; i < lightmapsCount; i++)
            {
                //获取各纹理
                var texA = _bufferA[i];
                var texB = _bufferB[i];
                var texC = _bufferC[i];
                //获取基准纹理尺寸
                lightmapsWidth[i] = texA.width;
                lightmapsHeight[i] = texA.height;
                //判断纹理尺寸是否合理
                if (texB.width != lightmapsWidth[i] || texB.height != lightmapsHeight[i] ||
                    texB.format != lightmapsFormat ||
                    texC.width != lightmapsWidth[i] || texC.height != lightmapsHeight[i] ||
                    texC.format != lightmapsFormat)
                {
                    Debug.Log("LightmapsBuffer: 各纹理缓存纹理尺寸格式不同！");
                    return;
                }
            }
            // 创建并写入lightmap 
            ClearBuffer(BufferType.Lightmap);
            _lightmap = new Texture2D[lightmapsCount];
            for (var i = 0; i < lightmapsCount; i++)
            {
                // 获取纹理尺寸
                var width = lightmapsWidth[i];
                var height = lightmapsHeight[i];
                // 创建纹理并写入颜色
                var lightmap = new Texture2D(width, height, lightmapsFormat, false);
                for (var x = 0; x < width; x++)
                {
                    for (var y = 0; y < height; y++)
                    {
                        var colA = _bufferA[i].GetPixel(x,y);
                        var colB = _bufferB[i].GetPixel(x,y);
                        var colC = _bufferC[i].GetPixel(x,y);
                        var newCol = new Color(colA.r, colB.r , colC.r, 1.0f);
                        lightmap.SetPixel(x, y, newCol);
                    }
                }
                // 应用纹理修改
                lightmap.Apply();
                // 指定给数组
                _lightmap[i] = lightmap;
            }
        }
        //覆盖场景lightmap
        public void OverrideLightmaps(LightmapsInfo info)
        {
            //判断lightmap缓存是否为空
            if (_lightmap == null)
            {
                Debug.Log("lightmapsOverrider： lightmap缓存为空 覆盖失败");
                return;
            }
            // 判断缓存纹理数与目标是否一致
            var lightmapsInfo = info.lightmapInfo;
            var lightmapsCount = lightmapsInfo.Count;
            if (_lightmap.Length != lightmapsCount)
            {
                Debug.Log("LightmapsOverrider: 缓存纹理数量与目标不一致，覆盖失败");
                return;
            }
            //覆盖并更新lightmap
            for (var i = 0; i < lightmapsCount; i++)
            {
                var bytes = _lightmap[i].EncodeToEXR(Texture2D.EXRFlags.CompressZIP);
                File.WriteAllBytes(lightmapsInfo.Keys.ElementAt(i), bytes);
                AssetDatabase.Refresh();
            }
        }
    }
    
    //烘焙方式枚举
    public enum BakeMode
    {
        Deflaut,
        BakeMainlight,
        BakeSkylight,
        BakeEmissionGI
    }
    
    //准备烘焙环境
    private void ArrangeBakeScene(BakeMode mode)
    {
        //获取主光
        var mainlight = RenderSettings.sun;
        if (mainlight == null)
        {
            Debug.Log("LightingmapsBaker: Lighting设置缺少主光，烘焙环境准备失败");
            return;
        }
        //按给定模式配置烘焙环境
        switch (mode)
        {
            case BakeMode.Deflaut:
                //开启主光
                mainlight.enabled = true;
                //设置环境
                RenderSettings.ambientMode = AmbientMode.Skybox;
                RenderSettings.ambientIntensity = 1.0f;
                //设置Shader全局分支
                Shader.DisableKeyword("_BakeMainLight");
                Shader.DisableKeyword("_Bake_SkyLight");
                Shader.DisableKeyword("_BakeEmissionGI");
                break;
            case BakeMode.BakeMainlight:
                //开启主光
                mainlight.enabled = true;
                //设置环境
                mainlight.color = Color.white;
                mainlight.intensity = 1.0f;
                mainlight.lightmapBakeType = LightmapBakeType.Baked;
                var staticFlags = StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic;
                GameObjectUtility.SetStaticEditorFlags(mainlight.gameObject, staticFlags);
                //设置环境
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientSkyColor = Color.black;
                //设置Shader全局分支
                Shader.EnableKeyword("_BakeMainLight");
                Shader.DisableKeyword("_Bake_SkyLight");
                Shader.DisableKeyword("_BakeEmissionGI");
                break;
            case BakeMode.BakeSkylight:
                // 关闭主光
                mainlight.enabled = false;
                // 设置环境
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientSkyColor = Color.white;
                //设置Shader全局分支
                Shader.DisableKeyword("_BakeMainLight");
                Shader.EnableKeyword("_Bake_SkyLight");
                Shader.DisableKeyword("_BakeEmissionGI");
                break;
            case BakeMode.BakeEmissionGI:
                //关闭主光
                mainlight.enabled = false;
                //设置环境
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientSkyColor = Color.black;
                //设置Shader全局分支
                Shader.DisableKeyword("_BakeMainLight");
                Shader.DisableKeyword("_Bake_SkyLight");
                Shader.EnableKeyword("_BakeEmissionGI");
                break;
        }
    }
    
    //烘焙方法
    public void Bake(BakeMode mode)
    {
        // 清理旧的烘焙信息
        Lightmapping.Clear();
        // 准备烘焙环境
        ArrangeBakeScene(mode);
        // 执行烘焙
        Lightmapping.Bake();
        // 打印日志
        switch (mode)
        {
            case BakeMode.BakeMainlight:
                Debug.Log("LightmapsBaker: 主光已烘焙");
                break;
            case BakeMode.BakeSkylight:
                Debug.Log("LightmapsBaker: 天光已烘焙");
                break;
            case BakeMode.BakeEmissionGI:
                Debug.Log("LightmapsBaker: 自发光GI已烘焙");
                break;
        }
    }
    
    //烘焙反射探头方法
    private void BakeReflectProbe()
    {
        var allProbe = FindObjectsOfType<ReflectionProbe>();
        foreach (var probe in allProbe)
        {
            var path = AssetDatabase.GetAssetPath(probe.texture);
            Lightmapping.BakeReflectionProbe(probe, path);
        }
        AssetDatabase.Refresh();
    }
    
    //多重烘焙方法
    public void MultiBake()
    {
        //创建lightmap缓存
        var buffer = new LightmapsBuffer();
        //烘焙主光并写入缓存
        Bake(BakeMode.BakeMainlight);
        var info = new LightmapsInfo(LightmapSettings.lightmaps);
        buffer.WriteBuffer(info, LightmapsBuffer.BufferType.MainLight);
        //烘焙天光并写入缓存
        Bake(BakeMode.BakeSkylight);
        buffer.WriteBuffer(info, LightmapsBuffer.BufferType.SkyLight);
        //烘焙自发光并写入缓存
        Bake(BakeMode.BakeEmissionGI);
        buffer.WriteBuffer(info, LightmapsBuffer.BufferType.EmissionGI);
        //从缓存创建lightmap
        buffer.CreateLightmaps();
        //覆盖场景lightmap
        buffer.OverrideLightmaps(info);
        //清空lightmap缓存
        buffer.Clear();
        //恢复场景光照环境
        ArrangeBakeScene(BakeMode.Deflaut);
        //更新全局变量
        UpdateGlobalProperties();
        //烘焙反射探头
        BakeReflectProbe();
    }
    
    

    [ContextMenu("教学·设置全局变量")]
    private void Test_SetGlobalParam()
    {
        // 获取当前值
        var origentCol = Shader.GetGlobalColor("_G_TestCol");
        // 当前不为红也不为绿时 上红色
        if (origentCol != Color.red && origentCol != Color.green)
        {
            Shader.SetGlobalColor("_G_TestCol", Color.red);
            return;
        }
        // 当前为红绿时 来回切
        if (origentCol == Color.red)
            Shader.SetGlobalColor("_G_TestCol", Color.green);
        if (origentCol == Color.green)
            Shader.SetGlobalColor("_G_TestCol", Color.red);
    }

    [ContextMenu("教学·设置全局分支")]
    private void Test_SetGlobalKeyword()
    {
        // 三个keyword都未激活时 激活A
        if (Shader.IsKeywordEnabled("_TESTA") == false &&
            Shader.IsKeywordEnabled("_TESTB") == false &&
            Shader.IsKeywordEnabled("_TESTC") == false)
        {
            Shader.EnableKeyword("_TESTA");
            return;
        }
        // 存在激活时 轮着切
        if (Shader.IsKeywordEnabled("_TESTA"))
        {
            Shader.DisableKeyword("_TESTA");
            Shader.EnableKeyword("_TESTB");
            return;
        }
        if (Shader.IsKeywordEnabled("_TESTB"))
        {
            Shader.DisableKeyword("_TESTB");
            Shader.EnableKeyword("_TESTC");
            return;
        }
        if (Shader.IsKeywordEnabled("_TESTC"))
        {
            Shader.DisableKeyword("_TESTC");
            Shader.EnableKeyword("_TESTA");
        }
    }

}
