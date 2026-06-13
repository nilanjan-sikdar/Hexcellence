using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[InitializeOnLoad]
public class SetupRotateCanvas
{
    static SetupRotateCanvas()
    {
        EditorApplication.delayCall += DoSetup;
    }

    private static void DoSetup()
    {
        if (SessionState.GetBool("RotateCanvasCreated", false)) return;
        SessionState.SetBool("RotateCanvasCreated", true);

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "SampleScene") return;

        if (GameObject.Find("RotateCanvas") != null) return;

        // 1. Create Canvas
        GameObject canvasObj = new GameObject("RotateCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = -10; // Render behind pieces
        
        RectTransform canvasRt = canvasObj.GetComponent<RectTransform>();
        canvasRt.sizeDelta = new Vector2(3f, 3f);
        
        PieceSpawner spawner = Object.FindObjectOfType<PieceSpawner>();
        if (spawner != null)
        {
            canvasObj.transform.position = spawner.SpawnPosition;
            canvasObj.transform.SetParent(spawner.transform);
        }
        else
        {
            canvasObj.transform.position = new Vector3(0, -3f, 0); // fallback
        }
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // 2. Create Event System if not exists
        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }
        
        // 3. Create Button
        GameObject buttonObj = new GameObject("RotateButtonCanvas");
        buttonObj.transform.SetParent(canvasObj.transform, false);
        
        RectTransform rt = buttonObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero; 
        rt.sizeDelta = new Vector2(3f, 3f);
        rt.localScale = Vector3.one;
        
        // 4. Setup Image and assign the Sprite
        Image img = buttonObj.AddComponent<Image>();
        Sprite s = null;
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("Assets/_assets/Restart-removebg-preview.png");
        foreach (var asset in assets)
        {
            if (asset is Sprite sprite)
            {
                s = sprite;
                break;
            }
        }
        
        if (s != null)
        {
            img.sprite = s;
            img.preserveAspect = true;
        }
        else
        {
            Debug.LogError("Could not find the Sprite in Restart-removebg-preview.png! Ensure it is imported as a Sprite.");
        }
        
        Button btn = buttonObj.AddComponent<Button>();
        
        // 5. Add the linking script
        buttonObj.AddComponent<CanvasRotateButton>();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Successfully created RotateCanvas in SampleScene and saved.");
    }
}
