#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.Playables;
using UnityEngine.Animations;

public class AnimationBakerAdvanced : EditorWindow
{
    private GameObject sourceCharacter;
    private AnimationClip animationClip;
    private int targetFrameRate = 20;
    private string outputPath = "Assets/BakedAnimations/";
    private bool bakeNormals = true;
    private bool usePlayableGraph = true;
    
    [MenuItem("Tools/Crowd/Animation Baker Advanced")]
    public static void ShowWindow()
    {
        GetWindow<AnimationBakerAdvanced>("Advanced Baker");
    }
    
    private void OnGUI()
    {
        EditorGUILayout.LabelField("🔥 Advanced Animation Baker", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "Versão avançada que usa PlayableGraph para garantir que a animação seja aplicada corretamente.\n" +
            "Funciona com qualquer hierarquia (Armature > Hips ou qualquer outra).",
            MessageType.Info);
        
        EditorGUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "COMO USAR:\n" +
            "1. Arraste um personagem da CENA (não o prefab)\n" +
            "2. Arraste o AnimationClip (ex: Sitting Idle.fbx)\n" +
            "3. Clique em BAKE",
            MessageType.Warning);
        
        EditorGUILayout.Space(5);
        
        sourceCharacter = EditorGUILayout.ObjectField("Personagem (da Cena)", sourceCharacter, typeof(GameObject), true) as GameObject;
        animationClip = EditorGUILayout.ObjectField("Animation Clip", animationClip, typeof(AnimationClip), false) as AnimationClip;
        
        if (sourceCharacter != null)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            Animator anim = sourceCharacter.GetComponent<Animator>();
            if (anim == null) anim = sourceCharacter.GetComponentInChildren<Animator>();
            
            SkinnedMeshRenderer smr = sourceCharacter.GetComponentInChildren<SkinnedMeshRenderer>();
            
            EditorGUILayout.LabelField("Diagnóstico:", EditorStyles.boldLabel);
            
            if (anim != null)
            {
                EditorGUILayout.LabelField($"✅ Animator encontrado");
                if (anim.avatar != null)
                {
                    EditorGUILayout.LabelField($"✅ Avatar: {anim.avatar.name}");
                    EditorGUILayout.LabelField($"   Humanoid: {anim.avatar.isHuman}");
                    EditorGUILayout.LabelField($"   Valid: {anim.avatar.isValid}");
                }
                else
                {
                    EditorGUILayout.LabelField($"❌ Avatar não configurado!");
                }
            }
            else
            {
                EditorGUILayout.LabelField($"❌ Animator NÃO encontrado!");
            }
            
            if (smr != null)
            {
                EditorGUILayout.LabelField($"✅ SkinnedMeshRenderer: {smr.name}");
                EditorGUILayout.LabelField($"   Bones: {smr.bones.Length}");
                EditorGUILayout.LabelField($"   Root Bone: {(smr.rootBone != null ? smr.rootBone.name : "NULL")}");
            }
            else
            {
                EditorGUILayout.LabelField($"❌ SkinnedMeshRenderer NÃO encontrado!");
            }
            
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.Space(5);
        
        targetFrameRate = EditorGUILayout.IntSlider("Frame Rate", targetFrameRate, 10, 60);
        bakeNormals = EditorGUILayout.Toggle("Bake Normals", bakeNormals);
        usePlayableGraph = EditorGUILayout.Toggle("Use Playable Graph", usePlayableGraph);
        
        EditorGUILayout.Space(5);
        
        outputPath = EditorGUILayout.TextField("Output Path", outputPath);
        
        EditorGUILayout.Space(10);
        
        GUI.enabled = sourceCharacter != null && animationClip != null;
        
        if (GUILayout.Button("🚀 BAKE ANIMATION", GUILayout.Height(40)))
        {
            if (usePlayableGraph)
            {
                BakeAnimationWithPlayable();
            }
            else
            {
                BakeAnimationLegacy();
            }
        }
        
        GUI.enabled = true;
        
        if (animationClip != null)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Preview:", EditorStyles.boldLabel);
            float duration = animationClip.length;
            int frameCount = Mathf.CeilToInt(duration * targetFrameRate);
            EditorGUILayout.LabelField($"Duração: {duration:F2}s");
            EditorGUILayout.LabelField($"Frames: {frameCount}");
            EditorGUILayout.LabelField($"Tamanho aprox: {(frameCount * 0.5f):F1} MB");
        }
    }
    
    private void BakeAnimationWithPlayable()
    {
        if (!ValidateInputs()) return;
        
        if (!System.IO.Directory.Exists(outputPath))
        {
            System.IO.Directory.CreateDirectory(outputPath);
        }
        
        GameObject tempObject = Instantiate(sourceCharacter);
        tempObject.name = "BakingTemp";
        tempObject.hideFlags = HideFlags.HideAndDontSave;
        
        Animator animator = tempObject.GetComponent<Animator>();
        if (animator == null)
        {
            animator = tempObject.GetComponentInChildren<Animator>();
        }
        
        if (animator == null)
        {
            DestroyImmediate(tempObject);
            EditorUtility.DisplayDialog("Erro", "Animator não encontrado!", "OK");
            return;
        }
        
        animator.enabled = true;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        
        SkinnedMeshRenderer tempSkinned = tempObject.GetComponentInChildren<SkinnedMeshRenderer>();
        if (tempSkinned == null)
        {
            DestroyImmediate(tempObject);
            EditorUtility.DisplayDialog("Erro", "SkinnedMeshRenderer não encontrado!", "OK");
            return;
        }
        
        var playableGraph = PlayableGraph.Create("BakerGraph");
        var playableOutput = AnimationPlayableOutput.Create(playableGraph, "Animation", animator);
        
        var clipPlayable = AnimationClipPlayable.Create(playableGraph, animationClip);
        playableOutput.SetSourcePlayable(clipPlayable);
        
        playableGraph.Play();
        clipPlayable.Pause();
        
        float duration = animationClip.length;
        int frameCount = Mathf.CeilToInt(duration * targetFrameRate);
        List<Mesh> bakedFrames = new List<Mesh>();
        
        Debug.Log($"Iniciando bake: {frameCount} frames de {animationClip.name}");
        
        for (int i = 0; i < frameCount; i++)
        {
            float time = i / (float)targetFrameRate;
            float normalizedTime = time / duration;
            
            clipPlayable.SetTime((double)time);
            playableGraph.Evaluate(0f);
            
            Mesh bakedMesh = new Mesh();
            bakedMesh.name = $"{animationClip.name}_Frame_{i:000}";
            tempSkinned.BakeMesh(bakedMesh, bakeNormals);
            
            if (bakedMesh.vertexCount == 0)
            {
                Debug.LogWarning($"Frame {i} está vazio!");
            }
            
            bakedFrames.Add(bakedMesh);
            
            EditorUtility.DisplayProgressBar("Baking Animation", 
                $"Frame {i + 1}/{frameCount} (time: {time:F2}s)", 
                (float)i / frameCount);
        }
        
        playableGraph.Destroy();
        DestroyImmediate(tempObject);
        
        SaveBakedAnimation(bakedFrames, animationClip);
    }
    
    private void BakeAnimationLegacy()
    {
        if (!ValidateInputs()) return;
        
        if (!System.IO.Directory.Exists(outputPath))
        {
            System.IO.Directory.CreateDirectory(outputPath);
        }
        
        GameObject tempObject = Instantiate(sourceCharacter);
        tempObject.name = "BakingTemp";
        
        Animator animator = tempObject.GetComponent<Animator>();
        if (animator == null) animator = tempObject.GetComponentInChildren<Animator>();
        
        SkinnedMeshRenderer tempSkinned = tempObject.GetComponentInChildren<SkinnedMeshRenderer>();
        
        float duration = animationClip.length;
        int frameCount = Mathf.CeilToInt(duration * targetFrameRate);
        List<Mesh> bakedFrames = new List<Mesh>();
        
        for (int i = 0; i < frameCount; i++)
        {
            float time = i / (float)targetFrameRate;
            
            animationClip.SampleAnimation(tempObject, time);
            
            if (animator != null)
            {
                animator.Update(0f);
            }
            
            Mesh bakedMesh = new Mesh();
            bakedMesh.name = $"{animationClip.name}_Frame_{i:000}";
            tempSkinned.BakeMesh(bakedMesh, bakeNormals);
            
            bakedFrames.Add(bakedMesh);
            
            EditorUtility.DisplayProgressBar("Baking Animation", 
                $"Frame {i + 1}/{frameCount}", 
                (float)i / frameCount);
        }
        
        DestroyImmediate(tempObject);
        
        SaveBakedAnimation(bakedFrames, animationClip);
    }
    
    private void SaveBakedAnimation(List<Mesh> bakedFrames, AnimationClip clip)
    {
        BakedMeshAnimationData data = ScriptableObject.CreateInstance<BakedMeshAnimationData>();
        data.animationName = clip.name;
        data.bakedFrames = bakedFrames.ToArray();
        data.frameRate = targetFrameRate;
        data.loop = clip.isLooping;
        
        string assetPath = $"{outputPath}{clip.name}_Baked.asset";
        AssetDatabase.CreateAsset(data, assetPath);
        
        for (int i = 0; i < bakedFrames.Count; i++)
        {
            AssetDatabase.AddObjectToAsset(bakedFrames[i], data);
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.ClearProgressBar();
        
        Debug.Log($"✅ Bake completo! {bakedFrames.Count} frames salvos em {assetPath}");
        
        EditorUtility.DisplayDialog("Sucesso!", 
            $"Animação bakada com sucesso!\n\n" +
            $"Frames: {bakedFrames.Count}\n" +
            $"Path: {assetPath}\n\n" +
            $"Verifique o primeiro frame no Inspector para confirmar que não está em T-pose.", 
            "OK");
        
        Selection.activeObject = data;
        EditorGUIUtility.PingObject(data);
    }
    
    private bool ValidateInputs()
    {
        if (sourceCharacter == null || animationClip == null)
        {
            EditorUtility.DisplayDialog("Erro", "Configure o personagem e a animação!", "OK");
            return false;
        }
        
        Animator anim = sourceCharacter.GetComponent<Animator>();
        if (anim == null) anim = sourceCharacter.GetComponentInChildren<Animator>();
        
        if (anim == null)
        {
            EditorUtility.DisplayDialog("Erro", 
                "Personagem não possui Animator!\n\n" +
                "Arraste o personagem da CENA, não o prefab.", 
                "OK");
            return false;
        }
        
        if (anim.avatar == null)
        {
            EditorUtility.DisplayDialog("Aviso", 
                "Animator não tem Avatar configurado!\n\n" +
                "Isso pode causar problemas. Configure o Avatar no Animator.", 
                "Continuar Mesmo Assim");
        }
        
        SkinnedMeshRenderer smr = sourceCharacter.GetComponentInChildren<SkinnedMeshRenderer>();
        if (smr == null)
        {
            EditorUtility.DisplayDialog("Erro", "SkinnedMeshRenderer não encontrado!", "OK");
            return false;
        }
        
        return true;
    }
}
#endif
