
using Bg.DirectoryDuplicator.Editor;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    /// <summary>
    /// テンプレートから新しいページを生成するクラス
    /// </summary>
    public class PageCreateWindow : EditorWindow
    {
        private static readonly string ProjectPath = @"Assets\Demo";
        private static readonly string RenameText = "TemplatePage";

        private static readonly string TemplateSourcePath = @$"{ProjectPath}\Template\Page";
        private static readonly string TemplateDestPath = @$"{ProjectPath}\temp";

        private static readonly string ScriptSourcePath = @$"{TemplateDestPath}\Scripts";
        private static readonly string ScriptDestPath = @$"{ProjectPath}\Scripts";
        private static readonly string PrefabSourcePath = @$"{TemplateDestPath}\Prefabs";
        private static readonly string PrefabDestPath = @$"{ProjectPath}\Resources\Prefabs";
        private static readonly string PlayablesSourcePath = @$"{TemplateDestPath}\Playables";
        private static readonly string PlayablesDestPath = @$"{ProjectPath}\Playables";

        private string inputName;

        /// <summary>
        /// メニューからの呼び出し
        /// </summary>
        [MenuItem("Window/新規ページ作製")]
        public static void Execute()
        {
            var window = GetWindow<PageCreateWindow>();
            window.titleContent = new GUIContent("新規ページ作成");
        }

        /// <summary>
        /// GUI設定
        /// </summary>
        private void OnGUI()
        {
            // EditorGUILayoutの使用例.
            EditorGUILayout.LabelField("作製するページ名");
            inputName = EditorGUILayout.TextField("", inputName);

            // GUIの使用例.
            if (GUI.Button(new Rect(0f, 40f, 120.0f, 20.0f), "作製"))
            {
                this.OnClickGenerate();
            }
        }

        /// <summary>
        /// 作製ボタンの押し込み
        /// </summary>
        private async void OnClickGenerate()
        {
            // エラーチェック
            if (string.IsNullOrEmpty(inputName))
            {
                EditorUtility.DisplayDialog("エラー", "ページ名を入力してください。", "Yes");
                return;
            }

            // 正規表現を利用して半角英数チェック
            if (!Regex.IsMatch(inputName, @"^[0-9a-zA-Z]+$"))
            {
                EditorUtility.DisplayDialog("エラー", "半角英数で入力してください。", "Yes");
                return;
            }

            // 問題なかったので最終確認
            if (EditorUtility.DisplayDialog("確認", $"「{inputName}」というページを新規で作成してもよろしいですか？", "Yes", "No"))
            {
                await this.GenerateNewPage();
            }
        }

        /// <summary>
        /// テンプレートから新しいページの作成
        /// </summary>
        /// <returns></returns>
        private async Task GenerateNewPage()
        {
            // リンクを保持したままフォルダごと別名コピーする
            var srcFullPath = Path.GetFullPath(TemplateSourcePath);
            var destFullPath = Path.GetFullPath(TemplateDestPath);
            Debug.LogError("aaaaaaaaaaaaaaaaaaaa");
            await DuplicateDirectoryWithDependencies(srcFullPath, destFullPath);
            Debug.LogError("vvvvvvvvvvvvvvvvvvvvvvv");

            // csファイルのクラス名をリネーム
            ReNameClassName(ScriptSourcePath, RenameText, inputName);
            Debug.LogError("cccccccccccccccccccccccc");

            // 複製されたテンプレートから、各フォルダへ移動
            AssetDatabase.MoveAsset(ScriptSourcePath, Path.Combine(ScriptDestPath, inputName));
            AssetDatabase.MoveAsset(PrefabSourcePath, Path.Combine(PrefabDestPath, inputName));
            AssetDatabase.MoveAsset(PlayablesSourcePath, Path.Combine(PlayablesDestPath, inputName));

            // テンプレートファイル名を、入力されたテキストに置き換え
            ReNameFile(Path.Combine(ScriptDestPath, inputName), RenameText, inputName);
            ReNameFile(Path.Combine(PrefabDestPath, inputName), RenameText, inputName);
            ReNameFile(Path.Combine(PlayablesDestPath, inputName), RenameText, inputName);

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }

        /// <summary>
        /// ファイル名のリネーム
        /// </summary>
        /// <param name="srcPath">対象のパス</param>
        /// <param name="oldName">置き換える対象の名前</param>
        /// <param name="newName">新しい名前</param>
        private static void ReNameFile(string srcPath, string oldName, string newName)
        {
            var fullPath = Path.GetFullPath(srcPath);
            var files = Directory.GetFiles(fullPath, "*.*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                if (files[i].Contains(RenameText))
                {
                    var assetPath = GetAssetPath(files[i]);

                    // ファイル名変更
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                    AssetDatabase.RenameAsset(assetPath, fileName.Replace(oldName, newName));
                }
            }
        }

        /// <summary>
        /// csファイル内のクラス名をリネーム
        /// </summary>
        /// <param name="srcPath">対象のパス</param>
        /// <param name="oldName">置き換える対象の名前</param>
        /// <param name="newName">新しい名前</param>
        private static void ReNameClassName(string srcPath, string oldName, string newName)
        {
            var fullPath = Path.GetFullPath(srcPath);
            var files = Directory.GetFiles(fullPath, "*.cs", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                var assetPath = GetAssetPath(files[i]);

                // 読み込み
                string text = File.ReadAllText(assetPath);
                text = text.Replace(@$"public class {oldName}", $"public class {newName}");

                // 書き込み
                File.WriteAllText(assetPath, text);
            }
        }

        /// <summary>
        /// 絶対パスからプロジェクトの相対パスを取得する
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static string GetAssetPath(string path)
        {
            var index = path.IndexOf(@"Assets\");
            return path.Substring(index);
        }

        /// <summary>
        /// DirectoryDuplicatorの実行
        /// </summary>
        /// <param name="srcPath">入力パス</param>
        /// <param name="destPath">出力パス</param>
        /// <returns>タスク</returns>
        static async Task DuplicateDirectoryWithDependencies(string srcPath, string destPath)
        {
            try
            {
                EditorUtility.DisplayProgressBar("DirectoryDuplicator", "Executing directory copy and reference migration", 0);
                await DirectoryDuplicator.CopyDirectoryWithDependencies(
                    srcPath,
                    destPath,
                    null,
                    ret =>
                    {
                        EditorUtility.DisplayProgressBar("DirectoryDuplicator",
                            $"Executing directory copy and reference migration {ret.progress}/{ret.total}",
                            ret.progress / (float)ret.total);
                    });
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}
