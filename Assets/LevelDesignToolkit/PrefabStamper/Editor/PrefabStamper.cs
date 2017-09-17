// PrefabStamper by http://www.eastshade.com/
// modified by unitycoder.com

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace EastshadeStudio
{
    public class PrefabStamper : EditorWindow
    {
        static LayerMask targetLayer = 0 << 8;

        const string stampDataFolder = "/LevelDesignToolkit/PrefabStamper/Text/Stamps/";

        StampData Stamp;
        List<Transform> stampedTransforms = new List<Transform>();

        Vector3 lastHitPoint;
        Vector3 lastHitNormal;
        bool alignToNormal = true;
        bool randomYRot = true;
        bool sizeVariation = true;
        float smallest = .85f;
        float largest = 1.15f;
        static PrefabStamper instance;

        static int undoGroupIncrementer = 0;
        private float radius;
        static bool isEnabled = true;
        static string currentStampName = "";

        [MenuItem("Assets/Stamp")]
        static void StartStamping()
        {
            Init();
            Object obj = Selection.activeObject;
            if (obj.GetType() == typeof(TextAsset))
            {
                // instance.Stamp = Serializer.Deserialize<StampData>((Selection.activeObject as TextAsset).text); // EasyJson plugin
                instance.Stamp = JsonUtility.FromJson<StampData>((Selection.activeObject as TextAsset).text);
            } else
            { // we will generate a stamp from this single prefab
                StampData singleStamp = new StampData();
                StampChild singleChild = new StampChild();
                singleChild.PrefabPath = AssetDatabase.GetAssetPath(obj);
                singleStamp.prefabs.Add(singleChild);
                instance.Stamp = singleStamp;
            }
            currentStampName = Selection.activeObject.name;
        }

        // check if menu can be enabled for this selection or not
        [MenuItem("Assets/Stamp", true)]
        static bool StampCheck()
        {
            Object candidate = Selection.activeObject;

            if (candidate == null)
            {
                return false;
            }

            if (Selection.activeObject.GetType() == typeof(GameObject))
            {
                return true;
            }

            if (Selection.activeObject.GetType() == typeof(TextAsset))
            {
                try
                {
                    // if (Serializer.Deserialize<StampData>((candidate as TextAsset).text) == null) // EasyJson plugin
                    if (JsonUtility.FromJson((candidate as TextAsset).text, typeof(StampData)) == null)
                    {
                        return false;
                    }
                }
                catch
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        [MenuItem("Tools/LevelEditTools/Prefab Stamper/Prefab Stamper Window", false, 0)]
        public static void Init()
        {
            if (LayerMask.GetMask("Terrain") > 0)
            {
                targetLayer = LayerMask.NameToLayer("Terrain");
            }

            // Get existing open window or if none, make a new one:
            PrefabStamper window = (PrefabStamper)GetWindow(typeof(PrefabStamper));
            window.Show();
            window.titleContent.text = "PrefabStamper";
            window.minSize = new Vector2(248, 198);
            window.maxSize = new Vector2(250, 200);
            instance = window;
        }

        void OnEnable()
        {
            SceneView.onSceneGUIDelegate += OnScene;
        }
        void OnDisable()
        {
            SceneView.onSceneGUIDelegate -= OnScene;
        }

        // editor gui window
        void OnGUI()
        {
            if (instance == null)
            {
                instance = (PrefabStamper)GetWindow(typeof(PrefabStamper));
            }

            isEnabled = EditorGUILayout.Toggle("Stamper Enabled", isEnabled);

            EditorGUILayout.LabelField("Current stamp", currentStampName);

            targetLayer = EditorGUILayout.LayerField("Target layer", targetLayer);

            alignToNormal = EditorGUILayout.Toggle("Align to Normal", alignToNormal);
            randomYRot = EditorGUILayout.Toggle("Randomize Y Rotation", randomYRot);

            sizeVariation = EditorGUILayout.Toggle("Randomize Scale", sizeVariation);

            GUI.enabled = sizeVariation;
            Rect r = EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Min " + smallest.ToString());
            GUILayout.FlexibleSpace();
            GUILayout.Label(largest.ToString() + " Max");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.MinMaxSlider(ref smallest, ref largest, 0.25f, 2.0f);

            GUI.enabled = true;

            if (GUILayout.Button("Drop Selected To Terrain"))
            {
                DropSelectedToTerrain();
            }


            if (GUILayout.Button("Close"))
            {
                instance.Close();
            }
        }

        // main loop
        static void OnScene(SceneView sceneview)
        {
            if (isEnabled == false) return;

            if (Event.current.alt) // alt key is down
            {
                return;
            }

            if (instance == null)
            {
                instance = (PrefabStamper)GetWindow(typeof(PrefabStamper));
            }

            if (instance.Stamp == null) return;

            // get raycast hit for preview circle
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, 1 << targetLayer))
            {
                Handles.color = Color.blue;
                // FIXME not accurate radius/area
                Handles.DrawWireDisc(hit.point, hit.normal, instance.Stamp.bounds.extents.magnitude * 0.5f);
                //Handles.DrawWireCube(hit.point, instance.Stamp.bounds.size);
                HandleUtility.Repaint();
            }

            // left mouse button is down
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                PaintStamp();
                Event.current.Use();
            }

            if (Event.current.type == EventType.MouseUp)
            {
                instance.stampedTransforms.Clear();
            }

            // move stamp with mouse
            if (instance.stampedTransforms.Count > 0)
            {
                PreviewStamp();
            }

            // stops selecting other objects
            if (Event.current.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(0);
            }
        }

        static void PreviewStamp()
        {
            // rotate with scrollwheel
            float scroll = (Event.current.type == EventType.scrollWheel) ? Event.current.delta.y * 4f : 0f;
            if (Event.current.type == EventType.scrollWheel)
            {
                Event.current.Use();
            }
            foreach (Transform t in instance.stampedTransforms)
            {
                t.RotateAround(instance.lastHitPoint, instance.lastHitNormal, scroll);
            }

            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, 1 << targetLayer))
            {
                {
                    foreach (Transform t in instance.stampedTransforms)
                    {
                        t.position += hit.point - instance.lastHitPoint;
                    }

                    instance.lastHitPoint = hit.point;
                    instance.lastHitNormal = hit.normal;
                }
                DropToTerrain(instance.stampedTransforms.ToArray());
            }
        }

        static void PaintStamp()
        {
            Undo.IncrementCurrentGroup();

            GameObject newStampsGroup = GameObject.Find("new stamps");
            if (newStampsGroup == null || newStampsGroup.transform.root != newStampsGroup.transform)
            {
                newStampsGroup = new GameObject("new stamps");
            }

            Vector2 guiPosition = Event.current.mousePosition;
            Ray ray = HandleUtility.GUIPointToWorldRay(guiPosition);

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, 1 << targetLayer) == true)
            {
                instance.lastHitPoint = hit.point;
                instance.lastHitNormal = hit.normal;
                instance.stampedTransforms.Clear();

                foreach (StampChild child in instance.Stamp.prefabs)
                {
                    var obj = AssetDatabase.LoadAssetAtPath(child.PrefabPath, typeof(GameObject));
                    GameObject newGo = PrefabUtility.InstantiatePrefab(obj) as GameObject;
                    //newGo.name = newGo.name.Split('(')[0];
                    newGo.transform.parent = newStampsGroup.transform;
                    newGo.transform.position = (child.position - instance.Stamp.center) + hit.point;
                    newGo.transform.eulerAngles = child.eulerAngles;
                    newGo.transform.localScale = child.scale;

                    if (instance.sizeVariation == true)
                    {
                        newGo.transform.localScale *= Random.Range(instance.smallest, instance.largest);
                    }

                    instance.stampedTransforms.Add(newGo.transform);
                    Undo.RegisterCreatedObjectUndo(newGo, "lay stamp" + undoGroupIncrementer.ToString());
                }

                undoGroupIncrementer++;
            }
        }

        [MenuItem("Tools/LevelEditTools/Drop Selection to Terrain", false, 2)]
        static void DropSelectedToTerrain()
        {
            Undo.RecordObjects(Selection.transforms, "Drop to Terrain");
            DropToTerrain(Selection.transforms);
        }

        static void DropToTerrain(Transform[] toDrop)
        {
            foreach (Transform t in toDrop)
            {
                RaycastHit hit;
                if (Physics.Raycast(t.position + (Vector3.up * 999), Vector3.down, out hit, Mathf.Infinity, 1 << targetLayer))
                {
                    t.position = hit.point;

                    if (t.tag != "Trees")
                    {
                        // align to normal
                        Vector3 left = Vector3.Cross(t.forward, hit.normal);
                        Vector3 newForward = Vector3.Cross(hit.normal, left);
                        Quaternion newRotation = Quaternion.LookRotation(newForward, hit.normal);
                        t.rotation = newRotation;
                    }
                }
            }
        }

        [MenuItem("Tools/LevelEditTools/Prefab Stamper/Create Stamp from Selection", false, 0)]
        static void CreateStamp()
        {
            if (Selection.gameObjects.Length == 0)
            {
                return;
            }


            // extra check: selected objects must be in scene
            if (Selection.activeGameObject.activeInHierarchy == true)
            {
                // In Hierarchy, ok
            } else
            {
                // In Project View, cannot use
                Debug.LogError("Select prefabbed objects from scene/hierarcy, not from the project folder..");
                return;
            }

            foreach (GameObject go in Selection.gameObjects)
            {
                if (!go.activeInHierarchy) continue;

                GameObject parentPrefab = PrefabUtility.GetPrefabParent(go) as GameObject;

                if (parentPrefab == null || string.IsNullOrEmpty(AssetDatabase.GetAssetPath(parentPrefab)))
                {
                    Debug.LogError("All selected GameObjects must be prefabs to make a stamp.");
                    return;
                }
            }

            StampData stampData = new StampData();
            Vector3 center = Vector3.zero;
            Bounds bounds = new Bounds();

            foreach (GameObject go in Selection.gameObjects)
            {
                if (!go.activeInHierarchy) continue;
                StampChild sc = new StampChild();
                sc.PrefabPath = AssetDatabase.GetAssetPath(PrefabUtility.GetPrefabParent(go));
                sc.position = go.transform.position;
                sc.eulerAngles = go.transform.eulerAngles;
                sc.scale = go.transform.localScale;
                center += sc.position;
                // get bounds from all children
                foreach (Transform child in go.transform)
                {
                    var mf = child.GetComponent<MeshFilter>();
                    bounds.Encapsulate(mf.sharedMesh.bounds);
                }
                stampData.prefabs.Add(sc);
            }

            stampData.center = center / stampData.prefabs.Count;
            stampData.bounds = bounds;

            // string jsonString = Serializer.Serialize(stampData, true); // EasyJSON plugin
            string jsonString = JsonUtility.ToJson(stampData, true);

            string assetPath = stampDataFolder + "new stamp.JSON";
            string fullPath = Application.dataPath + assetPath;

            if (Directory.Exists(Path.GetDirectoryName(fullPath)) == true)
            {
                StreamWriter stream = File.CreateText(fullPath);
                stream.WriteLine(jsonString);
                stream.Close();

                Debug.Log("Created new stamp at " + fullPath);

                AssetDatabase.Refresh();
            } else
            {
                Debug.LogError("Missing stamp data folder: " + fullPath);
            }
        }

    }
}
