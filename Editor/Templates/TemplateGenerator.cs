// ----------------------------------------------------------------------------
// The MIT-Red License
// Copyright (c) 2012-2025 Leopotam <leopotam@yandex.ru>
// ----------------------------------------------------------------------------

using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace Leopotam.EcsLite.UnityEditor.Templates {
    sealed class TemplateGenerator : ScriptableObject {
        const string Title = "LeoECS Lite template generator";

        const string StartupTemplate = "Startup.cs.txt";
        const string InitSystemTemplate = "InitSystem.cs.txt";
        const string RunSystemTemplate = "RunSystem.cs.txt";
        const string ComponentTemplate = "Component.cs.txt";

        [MenuItem ("Assets/Create/LeoECS Lite/Create Startup from template", false, -200)]
        static void CreateStartupTpl () {
            var assetPath = GetAssetPath ();
            CreateAndRenameAsset ($"{assetPath}/EcsStartup.cs", GetIcon (), (name) => {
                if (CreateTemplateInternal (GetTemplateContent (StartupTemplate), name) == null) {
                    if (EditorUtility.DisplayDialog (Title, "Create data folders?", "Yes", "No")) {
                        CreateEmptyFolder ($"{assetPath}/Components");
                        CreateEmptyFolder ($"{assetPath}/Systems");
                        CreateEmptyFolder ($"{assetPath}/Views");
                        CreateEmptyFolder ($"{assetPath}/Services");
                        AssetDatabase.Refresh ();
                    }
                }
            });
        }

        static void CreateEmptyFolder (string folderPath) {
            if (!Directory.Exists (folderPath)) {
                try {
                    Directory.CreateDirectory (folderPath);
                    File.Create ($"{folderPath}/.gitkeep");
                } catch {
                    // ignored
                }
            }
        }

        [MenuItem ("Assets/Create/LeoECS Lite/Systems/Create InitSystem from template", false, -199)]
        static void CreateInitSystemTpl () {
            CreateAndRenameAsset ($"{GetAssetPath ()}/EcsInitSystem.cs", GetIcon (),
                (name) => CreateTemplateInternal (GetTemplateContent (InitSystemTemplate), name));
        }

        [MenuItem ("Assets/Create/LeoECS Lite/Systems/Create RunSystem from template", false, -198)]
        static void CreateRunSystemTpl () {
            CreateAndRenameAsset ($"{GetAssetPath ()}/EcsRunSystem.cs", GetIcon (),
                (name) => CreateTemplateInternal (GetTemplateContent (RunSystemTemplate), name));
        }

        [MenuItem ("Assets/Create/LeoECS Lite/Components/Create Component from template", false, -197)]
        static void CreateComponentTpl () {
            CreateAndRenameAsset ($"{GetAssetPath ()}/EcsComponent.cs", GetIcon (),
                (name) => CreateTemplateInternal (GetTemplateContent (ComponentTemplate), name));
        }

        public static string CreateTemplate (string proto, string fileName) {
            if (string.IsNullOrEmpty (fileName)) {
                return "Invalid filename";
            }
            var ns = EditorSettings.projectGenerationRootNamespace.Trim ();
            if (string.IsNullOrEmpty (EditorSettings.projectGenerationRootNamespace)) {
                ns = "Client";
            }
            proto = proto.Replace ("#NS#", ns);
            proto = proto.Replace ("#SCRIPTNAME#", SanitizeClassName (Path.GetFileNameWithoutExtension (fileName)));
            try {
                File.WriteAllText (AssetDatabase.GenerateUniqueAssetPath (fileName), proto);
            } catch (Exception ex) {
                return ex.Message;
            }
            AssetDatabase.Refresh ();
            return null;
        }

        static string SanitizeClassName (string className) {
            var sb = new StringBuilder ();
            var needUp = true;
            foreach (var c in className) {
                if (char.IsLetterOrDigit (c)) {
                    sb.Append (needUp ? char.ToUpperInvariant (c) : c);
                    needUp = false;
                } else {
                    needUp = true;
                }
            }
            return sb.ToString ();
        }

        static string CreateTemplateInternal (string proto, string fileName) {
            var res = CreateTemplate (proto, fileName);
            if (res != null) {
                EditorUtility.DisplayDialog (Title, res, "Close");
            }
            return res;
        }

        static string GetTemplateContent (string proto) {
            // hack: its only one way to get current editor script path. :(
            var pathHelper = CreateInstance<TemplateGenerator> ();
            var path = Path.GetDirectoryName (AssetDatabase.GetAssetPath (MonoScript.FromScriptableObject (pathHelper)));
            DestroyImmediate (pathHelper);
            try {
                return File.ReadAllText (Path.Combine (path ?? "", proto));
            } catch {
                return null;
            }
        }

        static string GetAssetPath () {
            var path = AssetDatabase.GetAssetPath (Selection.activeObject);
            if (!string.IsNullOrEmpty (path) && AssetDatabase.Contains (Selection.activeObject)) {
                if (!AssetDatabase.IsValidFolder (path)) {
                    path = Path.GetDirectoryName (path);
                }
            } else {
                path = "Assets";
            }
            return path;
        }

        static Texture2D GetIcon () {
            return EditorGUIUtility.IconContent ("cs Script Icon").image as Texture2D;
        }

        static void CreateAndRenameAsset (string fileName, Texture2D icon, Action<string> onSuccess) {
            var action = CreateInstance<CustomEndNameAction> ();
            action.Callback = onSuccess;
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists (0, action, fileName, icon, null);
        }

        sealed class CustomEndNameAction : EndNameEditAction {
            [NonSerialized] public Action<string> Callback;

            public override void Action (int instanceId, string pathName, string resourceFile) {
                Callback?.Invoke (pathName);
            }
        }
    }
}
