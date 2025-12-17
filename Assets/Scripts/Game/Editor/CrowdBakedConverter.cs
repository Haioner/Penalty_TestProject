#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class CrowdBakedConverter : EditorWindow
{
    private Transform crowdParent;
    private BakedMeshAnimationData idleAnimation;
    private BakedMeshAnimationData winAnimation;
    private bool removeSkinnedMesh = true;
    private bool removeAnimator = true;
    private bool removeAnimation = true;
    private bool removeOldBones = false;
    
    [MenuItem("Tools/Crowd/Convert to Baked Animation")]
    public static void ShowWindow()
    {
        GetWindow<CrowdBakedConverter>("Crowd Converter");
    }
    
    private void OnGUI()
    {
        EditorGUILayout.LabelField("🔄 Converter Multidão para Baked Animation", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "Este tool vai converter todos os personagens da multidão:\n\n" +
            "✓ Remove Animator + SkinnedMeshRenderer\n" +
            "✓ Adiciona MeshFilter + MeshRenderer\n" +
            "✓ Adiciona BakedMeshAnimator\n" +
            "✓ Configura animações bakadas\n\n" +
            "⚡ RESULTADO: Até 10x mais performance!",
            MessageType.Info);
        
        EditorGUILayout.Space(10);
        
        crowdParent = EditorGUILayout.ObjectField("Parent da Multidão", crowdParent, typeof(Transform), true) as Transform;
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Animações Bakadas:", EditorStyles.boldLabel);
        
        idleAnimation = EditorGUILayout.ObjectField("Idle Animation", idleAnimation, typeof(BakedMeshAnimationData), false) as BakedMeshAnimationData;
        winAnimation = EditorGUILayout.ObjectField("Win Animation", winAnimation, typeof(BakedMeshAnimationData), false) as BakedMeshAnimationData;
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Opções de Conversão:", EditorStyles.boldLabel);
        
        removeSkinnedMesh = EditorGUILayout.Toggle("Remover SkinnedMeshRenderer", removeSkinnedMesh);
        removeAnimator = EditorGUILayout.Toggle("Remover Animator", removeAnimator);
        removeAnimation = EditorGUILayout.Toggle("Remover Animation", removeAnimation);
        removeOldBones = EditorGUILayout.Toggle("Remover Bones Hierarchy", removeOldBones);
        
        EditorGUILayout.Space(10);
        
        GUI.enabled = crowdParent != null && idleAnimation != null;
        
        if (GUILayout.Button("🚀 CONVERTER TODOS OS PERSONAGENS", GUILayout.Height(40)))
        {
            ConvertCrowd();
        }
        
        GUI.enabled = true;
        
        if (crowdParent != null)
        {
            SkinnedMeshRenderer[] skinnedMeshes = crowdParent.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"SkinnedMeshRenderers encontrados: {skinnedMeshes.Length}");
        }
    }
    
    private void ConvertCrowd()
    {
        if (crowdParent == null || idleAnimation == null)
        {
            EditorUtility.DisplayDialog("Erro", "Configure o Parent e a animação Idle!", "OK");
            return;
        }
        
        if (!EditorUtility.DisplayDialog(
            "Confirmar Conversão",
            $"Isso vai modificar todos os personagens em '{crowdParent.name}'.\n\n" +
            "⚠️ RECOMENDO FAZER BACKUP DA CENA PRIMEIRO!\n\n" +
            "Continuar?",
            "Sim, Converter",
            "Cancelar"))
        {
            return;
        }
        
        SkinnedMeshRenderer[] allSkinnedMeshes = crowdParent.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        int convertedCount = 0;
        
        for (int i = 0; i < allSkinnedMeshes.Length; i++)
        {
            SkinnedMeshRenderer skinnedMesh = allSkinnedMeshes[i];
            if (skinnedMesh == null) continue;
            
            GameObject character = skinnedMesh.gameObject;
            
            EditorUtility.DisplayProgressBar("Convertendo Multidão", 
                $"Processando {character.name}...", 
                (float)i / allSkinnedMeshes.Length);
            
            ConvertCharacter(character, skinnedMesh);
            convertedCount++;
        }
        
        EditorUtility.ClearProgressBar();
        
        EditorUtility.DisplayDialog("Sucesso!", 
            $"✅ Convertidos {convertedCount} personagens para Baked Animation!\n\n" +
            "🔹 Não esqueça de SALVAR a cena!\n" +
            "🔹 Adicione o componente CrowdBakedManager no parent '{crowdParent.name}'", 
            "OK");
        
        Debug.Log($"✅ Conversão completa: {convertedCount} personagens convertidos");
    }
    
    private void ConvertCharacter(GameObject character, SkinnedMeshRenderer skinnedMesh)
    {
        Material[] materials = skinnedMesh.sharedMaterials;
        Transform rootBone = skinnedMesh.rootBone;
        
        MeshFilter meshFilter = character.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = character.AddComponent<MeshFilter>();
        }
        
        if (idleAnimation != null && idleAnimation.bakedFrames != null && idleAnimation.bakedFrames.Length > 0)
        {
            meshFilter.sharedMesh = idleAnimation.bakedFrames[0];
        }
        
        MeshRenderer meshRenderer = character.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = character.AddComponent<MeshRenderer>();
        }
        meshRenderer.sharedMaterials = materials;
        
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        meshRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        
        BakedMeshAnimator animator = character.GetComponent<BakedMeshAnimator>();
        if (animator == null)
        {
            animator = character.AddComponent<BakedMeshAnimator>();
        }
        
        SerializedObject so = new SerializedObject(animator);
        so.FindProperty("idleAnimation").objectReferenceValue = idleAnimation;
        so.FindProperty("winAnimation").objectReferenceValue = winAnimation;
        so.FindProperty("meshFilter").objectReferenceValue = meshFilter;
        so.FindProperty("randomizeStartTime").boolValue = true;
        so.FindProperty("speedVariation").floatValue = 0.2f;
        so.ApplyModifiedProperties();
        
        if (removeSkinnedMesh && skinnedMesh != null)
        {
            DestroyImmediate(skinnedMesh);
        }
        
        if (removeAnimator)
        {
            Animator oldAnimator = character.GetComponent<Animator>();
            if (oldAnimator != null) DestroyImmediate(oldAnimator);
        }
        
        if (removeAnimation)
        {
            Animation oldAnimation = character.GetComponent<Animation>();
            if (oldAnimation != null) DestroyImmediate(oldAnimation);
            
            Component[] allComponents = character.GetComponents<Component>();
            foreach (Component comp in allComponents)
            {
                if (comp != null && comp.GetType().Name == "CrowdAnimationController")
                {
                    DestroyImmediate(comp);
                }
            }
        }
        
        if (removeOldBones && rootBone != null)
        {
            Transform parent = character.transform;
            foreach (Transform child in parent)
            {
                if (child == rootBone || child.name.Contains("mixamorig") || child.name.Contains("Armature"))
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
        
        EditorUtility.SetDirty(character);
    }
}
#endif
