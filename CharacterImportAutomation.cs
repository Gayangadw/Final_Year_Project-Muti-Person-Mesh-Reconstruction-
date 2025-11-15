using System;
using System.Collections.Generic;
//using Component = UnityEngine.Component;
//using System.Diagnostics;
using Object = UnityEngine.Object;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
//using static System.Net.Mime.MediaTypeNames;

public class CharacterImportAutomation : AssetPostprocessor
{
    // --- Folder roots ---
    private static readonly string PlayerRoot = "Assets/Game/Characters/Player_Characters";
    private static readonly string OpponentRoot = "Assets/Game/Characters/AutomateOpponentCharacter";

    // --- Main Import Hook ---
    static void OnPostprocessAllAssets(string[] importedAssets, string[] _, string[] __, string[] ___)
    {
        foreach (var path in importedAssets)
        {
            if (path.EndsWith(".fbx") && (path.StartsWith(PlayerRoot) || path.StartsWith(OpponentRoot)))
            {
                string dir = Path.GetDirectoryName(path);
                string marker = Path.Combine(dir, "needs_configuration.txt");

                if (File.Exists(marker))
                {
                    ConfigureCharacter(path);
                    File.Delete(marker);
                    AssetDatabase.Refresh();

                    bool isOpponent = path.StartsWith(OpponentRoot);
                    InstantiateInAllScenes(path, isOpponent);
                }
            }
        }
    }

    // --- Configure FBX import settings ---
    static void ConfigureCharacter(string fbxPath)
    {
        var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null) return;

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        importer.materialLocation = ModelImporterMaterialLocation.External;
        importer.SaveAndReimport();

        ApplyRootTransformSettings(fbxPath);
    }

    static void ApplyRootTransformSettings(string fbxPath)
    {
        var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null) return;

        var clips = importer.defaultClipAnimations;
        if (clips != null && clips.Length > 0)
        {
            foreach (var clip in clips)
            {
                clip.keepOriginalOrientation = true;
                clip.keepOriginalPositionY = true;
                clip.keepOriginalPositionXZ = true;
            }
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }
    }

    /***************************************************************************************/
    // --- Instantiate character in scene ---
    static void InstantiateInHierarchy(string fbxPath, bool isOpponent)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (prefab == null) return;

        string parentName = isOpponent ? "OpponentCharacters" : "PlayerCharacters";
        GameObject parent = GameObject.Find(parentName);

        if (parent == null)
        {
            Debug.LogWarning($"[{(isOpponent ? "Opponent" : "Player")}] '{parentName}' object not found in hierarchy. Skipping placement for {prefab.name}.");
            return;
        }

        string characterName = Path.GetFileNameWithoutExtension(fbxPath);
        if (CharacterExistsInScene(parent, characterName))
        {
            Debug.Log($"[{(isOpponent ? "Opponent" : "Player")}] Character '{characterName}' already exists in scene. Skipping instantiation.");
            return;
        }

        string sampleName = isOpponent ? "Sample2" : "Sample1";
        GameObject referenceSample = FindSampleInScene(parent, sampleName);

        if (referenceSample == null)
            Debug.LogWarning($"[{(isOpponent ? "Opponent" : "Player")}] Reference sample '{sampleName}' not found. Using default transform.");

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null) return;

        instance.transform.SetParent(parent.transform);

        // Apply transform from reference sample if available
        if (referenceSample != null)
        {
            instance.transform.localPosition = referenceSample.transform.localPosition;
            instance.transform.localRotation = referenceSample.transform.localRotation;
            instance.transform.localScale = referenceSample.transform.localScale;
            Debug.Log($"[{(isOpponent ? "Opponent" : "Player")}] Applied transform from reference sample '{sampleName}' to {instance.name}");
        }
        else
        {
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
        }

        PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        string assetDir = Path.GetDirectoryName(fbxPath);
        string tmFolder = Path.Combine(assetDir, "TM").Replace("\\", "/");
        if (!Directory.Exists(tmFolder))
        {
            Directory.CreateDirectory(tmFolder);
            AssetDatabase.ImportAsset(tmFolder);
        }

        ExtractMaterialsAndTextures(prefab, tmFolder);
        AssignMaterials(instance, tmFolder);

        // Animator setup using the updated method
        SetupAnimatorController(instance, fbxPath);

        // Ensure serialization
        EditorUtility.SetDirty(instance);
        var animator = instance.GetComponent<Animator>();
        if (animator != null) EditorUtility.SetDirty(animator);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        // Copy missing components and scripts from sample prefab
        GameObject samplePrefab = GetSampleCharacter(isOpponent);
        if (samplePrefab != null)
        {
            CopyMissingComponentsRecursive(instance, samplePrefab);
            AssignScriptReferences(instance, samplePrefab);
            AssignHealthBars(instance, isOpponent);

            string sceneName = SceneManager.GetActiveScene().name;
            bool isPlayerSelectScene = sceneName == "selectCharacter";
            bool isOpponentSelectScene = sceneName == "selectCharacter 1";
            bool isSelectionScene = isPlayerSelectScene || isOpponentSelectScene;

            if (isSelectionScene)
            {
                if (!isOpponent && isPlayerSelectScene)
                {
                    if (animator != null) animator.enabled = false;

                    var fightingCharacter = FindComponentByName(instance, new[] { "FightingCharacter", "Fighting Character" });
                    if (fightingCharacter is MonoBehaviour mono) mono.enabled = false;

                    instance.SetActive(false);
                }
                else if (isOpponent && isOpponentSelectScene)
                {
                    if (animator != null) animator.enabled = false;

                    var opponentAI = FindComponentByName(instance, new[] { "OpponentAI", "opponentAI" });
                    if (opponentAI is MonoBehaviour mono) mono.enabled = false;

                    instance.SetActive(false);
                }
                else
                {
                    instance.SetActive(false);
                }
            }
            else
            {
                EnableFightingScripts(instance, isOpponent);
                instance.SetActive(false);
            }
            /**************************************************************************/
            AssignPlayerInputEvents(instance);
            AssignFightingCharacterPlayerInput(instance);


            EditorUtility.SetDirty(instance);
            /**********************************************/
        }

        // Add player to camera targets in non-selection scenes
        if (!isOpponent)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            bool isSelectionScene = sceneName == "selectCharacter" || sceneName == "selectCharacter 1";
            if (!isSelectionScene) AddToCameraTargets(instance);
        }

        Debug.Log($"[{(isOpponent ? "Opponent" : "Player")}] Character setup completed for {instance.name} in scene {SceneManager.GetActiveScene().name}");
    }


    // --- Check if character already exists in scene to prevent duplication ---
    static bool CharacterExistsInScene(GameObject parent, string characterName)
    {
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            Transform child = parent.transform.GetChild(i);
            if (child.name == characterName || child.name.StartsWith(characterName))
            {
                return true;
            }
        }
        return false;
    }

    // --- Find reference sample anywhere in the scene ---
    static GameObject FindSampleInScene(GameObject parent, string sampleName)
    {
        // Search entire scene, not just under parent
        GameObject found = GameObject.Find(sampleName);
        if (found != null)
            return found;

        // Fallback: search all transforms (including inactive)
        var allTransforms = Object.FindObjectsOfType<Transform>(true);
        foreach (var t in allTransforms)
        {
            if (t.name.Equals(sampleName, System.StringComparison.OrdinalIgnoreCase))
                return t.gameObject;
        }
        return null;
    }

    // --- Method to enable fighting scripts in gameplay scenes ---
    static void EnableFightingScripts(GameObject instance, bool isOpponent)
    {
        if (isOpponent)
        {
            // For opponent characters, enable OpponentAI script
            var opponentAI = FindComponentByName(instance, new[] { "opponentAI", "opponent AI", "OpponentAI", "Opponent AI" });
            if (opponentAI != null && opponentAI is MonoBehaviour monoBehaviour)
            {
                monoBehaviour.enabled = true;
                Debug.Log($"Enabled OpponentAI script on {instance.name}");
            }
        }
        else
        {
            // For player characters, enable FightingCharacter script
            var fightingCharacter = FindComponentByName(instance, new[] { "FightingCharacter", "Fighting Character", "fightingcharacter", "Fighting Character (Script)" });
            if (fightingCharacter != null && fightingCharacter is MonoBehaviour monoBehaviour)
            {
                monoBehaviour.enabled = true;
                Debug.Log($"Enabled FightingCharacter script on {instance.name}");
            }
        }

        // Ensure Animator is enabled
        var animator = instance.GetComponent<Animator>();
        if (animator != null)
            animator.enabled = true;
    }

    static void AssignHealthBars(GameObject instance, bool isOpponent)
    {
        // --- Find the correct target script (FightingCharacter for player, OpponentAI for opponent) ---
        Component targetScript = null;

        if (isOpponent)
        {
            targetScript = FindComponentByName(instance, new[] { "OpponentAI", "opponentAI", "Opponent AI" });
        }
        else
        {
            targetScript = FindComponentByName(instance, new[] { "FightingCharacter", "Fighting Character" });
        }

        if (targetScript == null)
        {
            Debug.LogWarning($"❌ No {(isOpponent ? "OpponentAI" : "FightingCharacter")} script found on {instance.name}");
            return;
        }

        SerializedObject so = new SerializedObject(targetScript);

        // --- Try multiple possible serialized field names for the Health Bar reference ---
        SerializedProperty healthBarProp =
            so.FindProperty("healthBar") ??
            so.FindProperty("HealthBar") ??
            so.FindProperty("playerHealthBar") ??
            so.FindProperty("opponentHealthBar");

        if (healthBarProp == null)
        {
            Debug.LogWarning($"❌ Health Bar field not found on {(isOpponent ? "OpponentAI" : "FightingCharacter")} in {instance.name}");
            return;
        }

        // --- Load correct prefab path ---
        string healthBarPath = isOpponent
            ? "Assets/Game/Characters/AutomateOpponentCharacter/opponent_health_bar.prefab"
            : "Assets/Game/Characters/Player_Characters/Player_Health_Bar.prefab";

        GameObject healthBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(healthBarPath);

        if (healthBarPrefab == null)
        {
            Debug.LogWarning($"❌ Health bar prefab missing at {healthBarPath}");
            return;
        }

        // --- Assign and apply ---
        healthBarProp.objectReferenceValue = healthBarPrefab;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(targetScript as UnityEngine.Object);

        Debug.Log($"✅ Assigned {(isOpponent ? "Opponent" : "Player")} Health Bar prefab to {instance.name}");
    }


    // --- Material & Texture Extraction ---
    static void ExtractMaterialsAndTextures(GameObject prefab, string tmFolder)
    {
        string prefabPath = AssetDatabase.GetAssetPath(prefab);
        var materials = AssetDatabase.LoadAllAssetsAtPath(prefabPath);
        foreach (var matObj in materials)
        {
            if (matObj is Material mat)
            {
                string matPath = Path.Combine(tmFolder, mat.name + ".mat").Replace("\\", "/");
                AssetDatabase.CreateAsset(Object.Instantiate(mat), matPath);

                Shader shader = mat.shader;
                int propCount = ShaderUtil.GetPropertyCount(shader);
                for (int i = 0; i < propCount; i++)
                {
                    if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                    {
                        string propName = ShaderUtil.GetPropertyName(shader, i);
                        Texture tex = mat.GetTexture(propName);
                        if (tex != null)
                        {
                            string texPath = AssetDatabase.GetAssetPath(tex);
                            string texFile = Path.GetFileName(texPath);
                            string texDest = Path.Combine(tmFolder, texFile).Replace("\\", "/");
                            if (!File.Exists(texDest))
                                AssetDatabase.CopyAsset(texPath, texDest);
                        }
                    }
                }
            }
        }
        AssetDatabase.Refresh();
    }

    static void AssignMaterials(GameObject obj, string tmFolder)
    {
        var renderers = obj.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            Material[] mats = renderer.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] != null)
                {
                    string matPath = Path.Combine(tmFolder, mats[i].name + ".mat").Replace("\\", "/");
                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (mat != null)
                        mats[i] = mat;
                }
            }
            renderer.sharedMaterials = mats;
        }
    }

    static void SetupAnimatorController(GameObject instance, string fbxPath)
    {
        Animator animator = instance.GetComponent<Animator>();
        if (!animator) animator = instance.AddComponent<Animator>();
        if (animator.runtimeAnimatorController != null) return;

        string characterName = Path.GetFileNameWithoutExtension(fbxPath);
        string fbxDir = Path.GetDirectoryName(fbxPath);
        string controllerFolder = Path.Combine(fbxDir, "Controllers").Replace("\\", "/");

        // Search folders for sample prefab
        string[] searchRoots =
        {
        "Assets/Game/Characters/Player_Characters",
        "Assets/Game/Characters/AutomateOpponentCharacter"
    };

        string samplePrefabPath = null;

        foreach (string root in searchRoots)
        {
            string[] prefabs = Directory.GetFiles(root, "sample.prefab", SearchOption.AllDirectories);
            if (prefabs.Length > 0)
            {
                samplePrefabPath = prefabs[0].Replace("\\", "/");
                break;
            }
        }

        if (samplePrefabPath == null)
        {
            Debug.LogError("❌ No sample prefab found!");
            return;
        }

        // Load the sample prefab
        GameObject samplePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(samplePrefabPath);
        Animator sampleAnimator = samplePrefab.GetComponent<Animator>();

        if (!sampleAnimator || sampleAnimator.runtimeAnimatorController == null)
        {
            Debug.LogError("❌ Sample prefab does not have AnimatorController!");
            return;
        }

        RuntimeAnimatorController sampleController = sampleAnimator.runtimeAnimatorController;

        // Create destination folder
        if (!Directory.Exists(controllerFolder))
        {
            Directory.CreateDirectory(controllerFolder);
            AssetDatabase.Refresh();
        }

        // Path for new controller
        string controllerPath = Path.Combine(controllerFolder, $"{characterName}_Animator.controller").Replace("\\", "/");

        // Copy the sample controller file
        string sampleControllerPath = AssetDatabase.GetAssetPath(sampleController);
        if (!File.Exists(controllerPath))
        {
            AssetDatabase.CopyAsset(sampleControllerPath, controllerPath);
            AssetDatabase.Refresh();
        }

        AnimatorController newController = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        // ✅ Force-assign controller and make sure Unity recognizes the change
        animator.runtimeAnimatorController = newController;

        // --- Ensure Unity properly serializes the link ---
        EditorUtility.SetDirty(animator);
        EditorUtility.SetDirty(instance);

        // --- Make sure the asset and scene are saved ---
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        // --- Force reimport controller in case Unity hasn't loaded it yet ---
        AssetDatabase.ImportAsset(controllerPath);

        // --- Verify assignment ---
        if (animator.runtimeAnimatorController == null)
            Debug.LogError($"❌ Animator Controller assignment failed for {instance.name}");
        else
            Debug.Log($"✅ Animator Controller '{newController.name}' assigned to '{instance.name}'");


        //.Log($"✅ Copied AnimatorController:\n{sampleController.name} ➜ {newController.name} and assigned to {instance.name}");
    }


    // --- Load correct sample character ---
    static GameObject GetSampleCharacter(bool isOpponent)
    {
        string root = isOpponent ? OpponentRoot : PlayerRoot;
        string samplePath = Path.Combine(root, "sample.prefab").Replace("\\", "/");
        return AssetDatabase.LoadAssetAtPath<GameObject>(samplePath);
    }

    // --- Copy missing components from sample prefab ---
    static void CopyMissingComponentsRecursive(GameObject instance, GameObject sample)
    {
        CopyMissingComponents(instance, sample);
        for (int i = 0; i < sample.transform.childCount; i++)
        {
            Transform sampleChild = sample.transform.GetChild(i);
            Transform instanceChild = instance.transform.Find(sampleChild.name);
            if (instanceChild != null)
            {
                CopyMissingComponentsRecursive(instanceChild.gameObject, sampleChild.gameObject);
            }
        }
    }

    static void CopyMissingComponents(GameObject instance, GameObject sample)
    {
        Component[] sampleComponents = sample.GetComponents<Component>();
        Component[] instanceComponents = instance.GetComponents<Component>();

        foreach (var sampleComp in sampleComponents)
        {
            if (sampleComp is Transform || sampleComp is Animator)
                continue;

            string typeName = sampleComp.GetType().FullName;
            bool hasComp = false;
            foreach (var instComp in instanceComponents)
            {
                if (instComp.GetType().FullName == typeName)
                {
                    hasComp = true;
                    break;
                }
            }

            if (!hasComp)
            {
                Component newComp = instance.AddComponent(sampleComp.GetType());
                CopySerializedFields(sampleComp, newComp);
                Debug.Log($"Added missing component {typeName} to {instance.name}");
            }
        }
    }
    /*****************************************************************/
    static void AssignPlayerInputEvents(GameObject instance)
    {
        string samplePath = "Assets/Prefabs/SampleNew.prefab";
        GameObject samplePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(samplePath);

        if (samplePrefab == null)
        {
            Debug.LogError("❌ SampleNew prefab not found at: " + samplePath);
            return;
        }

        var sampleInput = samplePrefab.GetComponent<UnityEngine.InputSystem.PlayerInput>();
        var instanceInput = instance.GetComponent<UnityEngine.InputSystem.PlayerInput>();

        if (sampleInput == null || instanceInput == null)
        {
            Debug.LogError("❌ PlayerInput missing on sample or instance!");
            return;
        }

        // Copy Action Asset + Settings
        instanceInput.actions = sampleInput.actions;
        instanceInput.defaultActionMap = sampleInput.defaultActionMap;
        instanceInput.notificationBehavior = sampleInput.notificationBehavior;

        // Copy Unity Events (VRGame Events)
        SerializedObject src = new SerializedObject(sampleInput);
        SerializedObject dst = new SerializedObject(instanceInput);

        SerializedProperty srcEvents = src.FindProperty("m_ActionEvents");
        SerializedProperty dstEvents = dst.FindProperty("m_ActionEvents");

        if (srcEvents == null || dstEvents == null)
        {
            Debug.LogError("❌ PlayerInput Unity Events not found!");
            return;
        }

        dstEvents.arraySize = srcEvents.arraySize;

        for (int i = 0; i < srcEvents.arraySize; i++)
        {
            SerializedProperty srcEvent = srcEvents.GetArrayElementAtIndex(i);
            SerializedProperty dstEvent = dstEvents.GetArrayElementAtIndex(i);

            dstEvent.FindPropertyRelative("m_ActionId").stringValue =
                srcEvent.FindPropertyRelative("m_ActionId").stringValue;

            dstEvent.FindPropertyRelative("m_ActionName").stringValue =
                srcEvent.FindPropertyRelative("m_ActionName").stringValue;

            // Copy the UnityEvent list
            SerializedProperty srcCallbacks = srcEvent.FindPropertyRelative("m_PersistentCalls.m_Calls");
            SerializedProperty dstCallbacks = dstEvent.FindPropertyRelative("m_PersistentCalls.m_Calls");

            dstCallbacks.arraySize = srcCallbacks.arraySize;

            for (int j = 0; j < srcCallbacks.arraySize; j++)
            {
                var srcCall = srcCallbacks.GetArrayElementAtIndex(j);
                var dstCall = dstCallbacks.GetArrayElementAtIndex(j);

                // Find the FightingCharacter component
                var targetComponent = instance.GetComponent<FightingCharacter>();
                if (targetComponent == null)
                    targetComponent = instance.GetComponentInChildren<FightingCharacter>();

                // Assign component as event target
                dstCall.FindPropertyRelative("m_Target").objectReferenceValue = targetComponent;

                // Copy method name
                dstCall.FindPropertyRelative("m_MethodName").stringValue =
                    srcCall.FindPropertyRelative("m_MethodName").stringValue;

                dstCall.FindPropertyRelative("m_Mode").intValue =
                    srcCall.FindPropertyRelative("m_Mode").intValue;

                // Copy arguments
                var srcArgs = srcCall.FindPropertyRelative("m_Arguments");
                var dstArgs = dstCall.FindPropertyRelative("m_Arguments");

                dstArgs.FindPropertyRelative("m_ObjectArgument").objectReferenceValue =
                    srcArgs.FindPropertyRelative("m_ObjectArgument").objectReferenceValue;

                dstArgs.FindPropertyRelative("m_ObjectArgumentAssemblyTypeName").stringValue =
                    srcArgs.FindPropertyRelative("m_ObjectArgumentAssemblyTypeName").stringValue;

                dstArgs.FindPropertyRelative("m_IntArgument").intValue =
                    srcArgs.FindPropertyRelative("m_IntArgument").intValue;
                dstArgs.FindPropertyRelative("m_FloatArgument").floatValue =
                    srcArgs.FindPropertyRelative("m_FloatArgument").floatValue;
                dstArgs.FindPropertyRelative("m_StringArgument").stringValue =
                    srcArgs.FindPropertyRelative("m_StringArgument").stringValue;
                dstArgs.FindPropertyRelative("m_BoolArgument").boolValue =
                    srcArgs.FindPropertyRelative("m_BoolArgument").boolValue;

                dstCall.FindPropertyRelative("m_CallState").intValue =
                    srcCall.FindPropertyRelative("m_CallState").intValue;
            }


        }

        dst.ApplyModifiedProperties();
        EditorUtility.SetDirty(instanceInput);

        Debug.Log($"✅ PlayerInput VRGame events copied to {instance.name}");
    }
    /***********************************************************/
    static void AssignFightingCharacterPlayerInput(GameObject instance)
    {
        FightingCharacter fightScript = instance.GetComponent<FightingCharacter>();
        if (fightScript == null)
        {
            Debug.LogError($"❌ FightingCharacter not found on {instance.name}");
            return;
        }

        PlayerInput playerInput = instance.GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            Debug.LogError($"❌ PlayerInput not found on {instance.name}");
            return;
        }

        fightScript.playerInput = playerInput;

        EditorUtility.SetDirty(fightScript);
        PrefabUtility.RecordPrefabInstancePropertyModifications(fightScript);

        Debug.Log($"✅ PlayerInput assigned in FightingCharacter for {instance.name}");
    }
    static void AssignScriptReferences(GameObject instance, GameObject sample)
    {
        var sampleBehaviours = sample.GetComponentsInChildren<MonoBehaviour>(true);
        var instanceBehaviours = instance.GetComponentsInChildren<MonoBehaviour>(true);

        foreach (var sampleComp in sampleBehaviours)
        {
            var sampleType = sampleComp.GetType();

            foreach (var instanceComp in instanceBehaviours)
            {
                if (instanceComp.GetType() == sampleType)
                {
                    SerializedObject srcObj = new SerializedObject(sampleComp);
                    SerializedObject tgtObj = new SerializedObject(instanceComp);

                    SerializedProperty prop = srcObj.GetIterator();
                    while (prop.NextVisible(true))
                    {
                        if (prop.name == "m_Script") continue;
                        tgtObj.CopyFromSerializedProperty(prop);
                    }

                    tgtObj.ApplyModifiedProperties();
                    EditorUtility.SetDirty(instanceComp);
                }
            }
        }
    }

    static void CopySerializedFields(Component source, Component target)
    {
        SerializedObject srcObj = new SerializedObject(source);
        SerializedObject tgtObj = new SerializedObject(target);

        SerializedProperty prop = srcObj.GetIterator();
        while (prop.NextVisible(true))
        {
            if (prop.name == "m_Script") continue;
            tgtObj.CopyFromSerializedProperty(prop);
        }

        tgtObj.ApplyModifiedProperties();
    }

    // --- Add Player characters to camera targets ---
    static void AddToCameraTargets(GameObject newCharacter)
    {
        GameObject mainCam = GameObject.FindWithTag("MainCamera") ?? GameObject.Find("Main Camera");
        if (mainCam == null)
        {
            Debug.LogWarning("Main Camera not found in scene!");
            return;
        }

        var camController = mainCam.GetComponent<CameraController>();
        if (camController == null)
        {
            Debug.LogWarning("CameraController script not found on Main Camera!");
            return;
        }

        SerializedObject so = new SerializedObject(camController);
        SerializedProperty targetsProp = so.FindProperty("targets");

        if (targetsProp != null && targetsProp.isArray)
        {
            bool alreadyExists = false;
            for (int i = 0; i < targetsProp.arraySize; i++)
            {
                if (targetsProp.GetArrayElementAtIndex(i).objectReferenceValue == newCharacter.transform)
                {
                    alreadyExists = true;
                    break;
                }
            }

            if (!alreadyExists)
            {
                targetsProp.InsertArrayElementAtIndex(targetsProp.arraySize);
                targetsProp.GetArrayElementAtIndex(targetsProp.arraySize - 1).objectReferenceValue = newCharacter.transform;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(camController);
                Debug.Log($"Added {newCharacter.name} to CameraController targets in Main Camera hierarchy.");
            }
        }
        else
        {
            Debug.LogWarning("CameraController does not have a serialized array named 'targets'.");
        }
    }

    // --- Auto-assign character references between scripts ---
    static void AutoAssignCharactersInScene()
    {
        // ✅ ONLY run auto-assignment in gameplay scenes, NOT in selection scenes
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "selectCharacter" || sceneName == "selectCharacter 1")
        {
            Debug.Log($"Skipping auto-assignment in selection scene: {sceneName}");
            return;
        }

        GameObject playersRoot = GameObject.Find("PlayerCharacters");
        GameObject opponentsRoot = GameObject.Find("OpponentCharacters");
        if (playersRoot == null || opponentsRoot == null)
            return;

        // Collect direct child character gameObjects (ignore the root itself)
        var playerObjects = playersRoot.GetComponentsInChildren<Transform>(true)
                            .Where(t => t != playersRoot.transform && t.parent == playersRoot.transform)
                            .Select(t => t.gameObject)
                            .Distinct()
                            .ToList();

        var opponentObjects = opponentsRoot.GetComponentsInChildren<Transform>(true)
                            .Where(t => t != opponentsRoot.transform && t.parent == opponentsRoot.transform)
                            .Select(t => t.gameObject)
                            .Distinct()
                            .ToList();

        if (playerObjects.Count == 0 || opponentObjects.Count == 0) return;

        Debug.Log($"Found {playerObjects.Count} players and {opponentObjects.Count} opponents for auto-assignment in gameplay scene");

        // --- Assign players to OpponentAI components ---
        foreach (var oppGO in opponentObjects)
        {
            Component oppAI = FindComponentByName(oppGO, new[] { "opponentAI", "opponent AI", "OpponentAI" });
            if (oppAI == null)
            {
                Debug.LogWarning($"OpponentAI component not found on {oppGO.name}");
                continue;
            }

            var so = new SerializedObject(oppAI);

            // Debug: List all properties
            Debug.Log($"Properties found on {oppGO.name}:");
            var iterator = so.GetIterator();
            while (iterator.NextVisible(true))
            {
                Debug.Log($"  - {iterator.name} (type: {iterator.propertyType})");
            }

            // Try multiple possible property names for Players array
            SerializedProperty playersProp = so.FindProperty("Players") ??
                                           so.FindProperty("players") ??
                                           so.FindProperty("m_Players");

            if (playersProp != null && playersProp.isArray)
            {
                playersProp.arraySize = playerObjects.Count;
                for (int i = 0; i < playerObjects.Count; i++)
                {
                    playersProp.GetArrayElementAtIndex(i).objectReferenceValue = playerObjects[i].transform;
                }
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(oppAI);
                Debug.Log($"Assigned {playerObjects.Count} players to OpponentAI in {oppGO.name}");
            }
            else
            {
                Debug.LogWarning($"Players array property not found on {oppGO.name}");
            }

            // Also assign FightingController reference if needed
            var fcProp = so.FindProperty("fightingController") ??
                         so.FindProperty("FightingController");

            if (fcProp != null && fcProp.isArray)
            {
                // Find all FightingController components on player objects
                var fightingControllers = playerObjects
                    .Select(p => FindComponentByName(p, new[] { "FightingCharacter", "Fighting Character", "FightingController" }))
                    .Where(c => c != null)
                    .ToArray();

                if (fightingControllers.Length > 0)
                {
                    fcProp.arraySize = fightingControllers.Length;
                    for (int i = 0; i < fightingControllers.Length; i++)
                    {
                        fcProp.GetArrayElementAtIndex(i).objectReferenceValue = fightingControllers[i] as UnityEngine.Object;
                    }
                    so.ApplyModifiedProperties();
                    Debug.Log($"Assigned {fightingControllers.Length} FightingController references to {oppGO.name}");
                }
            }
        }

        // --- Assign opponents to FightingCharacter components ---
        foreach (var playerGO in playerObjects)
        {
            Component fightingCharComp = FindComponentByName(playerGO, new[] { "FightingCharacter", "Fighting Character", "Flighting Character", "FightingController" });
            if (fightingCharComp == null)
            {
                Debug.LogWarning($"FightingCharacter component not found on {playerGO.name}");
                continue;
            }

            var so = new SerializedObject(fightingCharComp);

            // Try multiple possible property names for Opponents array
            SerializedProperty opponentsProp = so.FindProperty("Opponents") ??
                                             so.FindProperty("opponents") ??
                                             so.FindProperty("m_Opponents");

            if (opponentsProp != null && opponentsProp.isArray)
            {
                opponentsProp.arraySize = opponentObjects.Count;
                for (int i = 0; i < opponentObjects.Count; i++)
                {
                    opponentsProp.GetArrayElementAtIndex(i).objectReferenceValue = opponentObjects[i].transform;
                }
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(fightingCharComp);
                Debug.Log($"Assigned {opponentObjects.Count} opponents to FightingCharacter in {playerGO.name}");
            }
            else
            {
                Debug.LogWarning($"Opponents array property not found on {playerGO.name}");
            }
        }
    }

    // --- Automatically assign imported models to Manager scripts ---
    static void AutoAssignManagerReferences(bool isOpponent)
    {
        string sceneName = SceneManager.GetActiveScene().name;

        if (isOpponent)
        {
            // For Opponent scenes (e.g. map1, map2, map3)
            GameObject managerGO = GameObject.Find("OpponentCharacters");
            if (managerGO == null)
            {
                Debug.LogWarning($"OpponentCharacters root not found in {sceneName}");
                return;
            }

            var manager = managerGO.GetComponent("OpponentManager");
            if (manager == null)
            {
                Debug.LogWarning($"OpponentManager script not found on {managerGO.name} in {sceneName}");
                return;
            }

            var children = managerGO.GetComponentsInChildren<Transform>(true)
                                    .Where(t => t.parent == managerGO.transform)
                                    .Select(t => t.gameObject)
                                    .ToList();

            SerializedObject so = new SerializedObject(manager as UnityEngine.Object);
            SerializedProperty arrayProp = so.FindProperty("opponentCharacters");

            if (arrayProp != null && arrayProp.isArray)
            {
                arrayProp.arraySize = children.Count;
                for (int i = 0; i < children.Count; i++)
                {
                    arrayProp.GetArrayElementAtIndex(i).objectReferenceValue = children[i];
                }
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(manager as UnityEngine.Object);
                Debug.Log($"✅ Updated OpponentManager in {sceneName} with {children.Count} opponents");
            }
        }
        else
        {
            // For Player selection scene
            GameObject managerGO = GameObject.Find("PlayerCharacters");
            if (managerGO == null)
            {
                Debug.LogWarning($"PlayerCharacters root not found in {sceneName}");
                return;
            }

            var manager = managerGO.GetComponent("PlayerSelection");
            if (manager == null)
            {
                Debug.LogWarning($"PlayerSelection script not found on {managerGO.name} in {sceneName}");
                return;
            }

            var children = managerGO.GetComponentsInChildren<Transform>(true)
                                    .Where(t => t.parent == managerGO.transform)
                                    .Select(t => t.gameObject)
                                    .ToList();

            SerializedObject so = new SerializedObject(manager as UnityEngine.Object);
            SerializedProperty arrayProp = so.FindProperty("playerCharacters");

            if (arrayProp != null && arrayProp.isArray)
            {
                arrayProp.arraySize = children.Count;
                for (int i = 0; i < children.Count; i++)
                {
                    arrayProp.GetArrayElementAtIndex(i).objectReferenceValue = children[i];
                }
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(manager as UnityEngine.Object);
                Debug.Log($"✅ Updated PlayerSelection in {sceneName} with {children.Count} players");
            }
        }
    }


    // --- Helper method to find components by name variations ---
    static Component FindComponentByName(GameObject gameObject, string[] possibleNames)
    {
        var allComponents = gameObject.GetComponents<MonoBehaviour>();
        foreach (var comp in allComponents)
        {
            string typeName = comp.GetType().Name;
            if (possibleNames.Any(name => typeName.Contains(name.Replace(" (Script)", "").Replace(" ", ""))))
            {
                return comp;
            }
        }
        return null;
    }

    static void InstantiateInAllScenes(string fbxPath, bool isOpponent)
    {
        string originalScenePath = SceneManager.GetActiveScene().path;
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");

        foreach (string guid in sceneGuids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guid);

            // Skip package or non-project scenes
            if (!scenePath.StartsWith("Assets/") || scenePath.StartsWith("Packages/"))
                continue;

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            string sceneName = scene.name;

            GameObject playersRoot = GameObject.Find("PlayerCharacters");
            GameObject opponentsRoot = GameObject.Find("OpponentCharacters");

            bool hasPlayers = playersRoot != null;
            bool hasOpponents = opponentsRoot != null;

            // If neither root exists, skip
            if (!hasPlayers && !hasOpponents)
            {
                Debug.Log($"[CharacterImport] Skipping scene '{sceneName}' — no PlayerCharacters or OpponentCharacters found.");
                continue;
            }

            Debug.Log($"[CharacterImport] Processing scene '{sceneName}' (hasPlayers={hasPlayers}, hasOpponents={hasOpponents})");

            // Use exclusive checks per your requirement:
            // - If scene has ONLY players -> add players
            // - If scene has ONLY opponents -> add opponents
            // - If scene has BOTH -> add whichever type we are importing
            if (hasPlayers && !hasOpponents)
            {
                // Scene contains only PlayerCharacters
                if (!isOpponent) // only instantiate player models in this scene
                {
                    Debug.Log($"[CharacterImport] Adding PLAYER '{Path.GetFileNameWithoutExtension(fbxPath)}' into scene '{sceneName}' (players-only).");
                    InstantiateInHierarchy(fbxPath, false);
                }
                else
                {
                    Debug.Log($"[CharacterImport] Skipping opponent '{Path.GetFileNameWithoutExtension(fbxPath)}' for scene '{sceneName}' (players-only).");
                }
            }
            else if (hasOpponents && !hasPlayers)
            {
                // Scene contains only OpponentCharacters
                if (isOpponent) // only instantiate opponent models in this scene
                {
                    Debug.Log($"[CharacterImport] Adding OPPONENT '{Path.GetFileNameWithoutExtension(fbxPath)}' into scene '{sceneName}' (opponents-only).");
                    InstantiateInHierarchy(fbxPath, true);
                }
                else
                {
                    Debug.Log($"[CharacterImport] Skipping player '{Path.GetFileNameWithoutExtension(fbxPath)}' for scene '{sceneName}' (opponents-only).");
                }
            }
            else if (hasPlayers && hasOpponents)
            {
                // Scene contains both -> add whichever type we're importing, but not the other
                if (!isOpponent)
                {
                    Debug.Log($"[CharacterImport] Scene '{sceneName}' has both roots — adding PLAYER.");
                    InstantiateInHierarchy(fbxPath, false);
                }
                else
                {
                    Debug.Log($"[CharacterImport] Scene '{sceneName}' has both roots — adding OPPONENT.");
                    InstantiateInHierarchy(fbxPath, true);
                }

                // After adding both types, auto-assign cross references (only for gameplay scenes)
                if (sceneName != "selectCharacter" && sceneName != "selectCharacter 1")
                {
                    GameObject pRoot = GameObject.Find("PlayerCharacters");
                    GameObject oRoot = GameObject.Find("OpponentCharacters");
                    if (pRoot != null && oRoot != null)
                    {
                        AutoAssignCharactersInScene();
                    }
                }
            }

            // ✅ New: Assign Manager References + Disable Animators
            AutoAssignManagerReferences(isOpponent);


            // Save the current scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"[CharacterImport] Finished processing scene '{sceneName}'.");
        }

        // ✅ Restore the previously active scene at the end
        if (!Application.isPlaying && !string.IsNullOrEmpty(originalScenePath))
        {
            EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
        }
    }
}