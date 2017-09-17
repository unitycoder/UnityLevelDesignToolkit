using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UnityCommunity
{
    public class LevelDesignToolkit : EditorWindow
    {

        static LevelDesignToolkit instance;
        static bool isEnabled = true;


        [MenuItem("Tools/LevelDesignToolkit/Level Design Toolkit", false, 0)]
        public static void Init()
        {
            var window = (LevelDesignToolkit)GetWindow(typeof(LevelDesignToolkit));
            window.Show();
            window.titleContent.text = "LevelEditTools";
            window.minSize = new Vector2(248, 28);
            //window.maxSize = new Vector2(250, 190);
            instance = window;
        }

        void OnEnable()
        {
            SceneView.onSceneGUIDelegate += OnSceneGUI;
        }
        void OnDisable()
        {
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
        }

        void OnGUI()
        {
            isEnabled = EditorGUILayout.Toggle("Enabled", isEnabled);
            EditorGUILayout.LabelField("Press O-key to move object under mouse cursor");

            // TODO: activate scene view if not active when trying to place objects
            //            if (focusedWindow != SceneView.currentDrawingSceneView)
            //            {
            //            }
        }

        // main loop
        static void OnSceneGUI(SceneView sceneview)
        {
            // poll keyboards
            if (isEnabled == false) return;

            Event e = Event.current;

            if (e.alt == true) // alt key is down
            {
                return;
            }

            if (e.type == EventType.keyDown)
            {
                switch (e.character)
                {
                    case 'o':
                        if (Selection.activeGameObject != null)
                        {
                            // TODO: move multiple
                            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                            RaycastHit hit;
                            if (Physics.Raycast(ray, out hit, Mathf.Infinity))
                            {
                                Selection.activeGameObject.transform.position = hit.point;
                            }
                        }
                        break;
                    default:
                        break;
                }
            }

            /*
            // stops selecting other objects
            if (Event.current.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(0);
            }*/

        }
    }
}
