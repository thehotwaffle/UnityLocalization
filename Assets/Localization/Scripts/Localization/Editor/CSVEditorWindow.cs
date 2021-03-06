﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class CSVEditorWindow : EditorWindow
{
    private static CSVEditorWindow mWindow;
    private static TextAsset mEditedFile;
    private static Dictionary<string, Dictionary<string, string>> mCopiedValues;
    private static Vector2 mScrollPosition;
    private static Vector2 mSecondScrollPosition;
    private static FileSystemWatcher mFileSystemWatcher;
    private static string mResourcesPath;
    private static string mAssetPath;
    private static string mKeyName;
    private static List<string> mLanguages;
    private static List<string> mKeys;
    private static Texture mIcon;
    private static bool mHasChanged;
    private static bool mIsDirty;
    private static bool mFileLoaded;

    private static Dictionary<string, Dictionary<string, string>> mAddedKeys;

    public static void LoadFile(TextAsset pFile)
    {
        mEditedFile = pFile;
    }

    [MenuItem("Window/CSV Editor")]
    public static void ShowWindow()
    {
        mIcon = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Localization/Icon/CSVEditorIcon.png");
        mResourcesPath = Application.dataPath + "/Resources/";
        PrepareWindow("CSV Editor");
        if (mEditedFile != null)
        {
            LoadAsset(mEditedFile);
        }
    }

    private static void LoadAsset(TextAsset pAsset)
    {
        SetTitle("CSV Editor");
        if (pAsset == null)
        {
            return;
        }
        mAssetPath = AssetDatabase.GetAssetPath(pAsset);
        LocalizationService.LoadCSV(mAssetPath);
        mLanguages = LocalizationService.GetLanguages();
        mKeys = LocalizationService.GetKeys();
        mCopiedValues = LocalizationService.GetValues();
        mFileSystemWatcher = new FileSystemWatcher();
        mFileSystemWatcher.Path = mResourcesPath;
        mFileSystemWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
        mFileSystemWatcher.Filter = mEditedFile.name + ".csv";
        mFileSystemWatcher.Changed += OnFileChanged;
        mFileSystemWatcher.EnableRaisingEvents = true;
        mKeyName = string.Empty;
        mAddedKeys = new Dictionary<string, Dictionary<string, string>>();
        mHasChanged = false;
        mIsDirty = false;
        mFileLoaded = true;
    }

    private static void PrepareWindow(string pTitle)
    {
        mWindow = GetWindow<CSVEditorWindow>(false, pTitle, true);
    }

    private static void SetTitle(string pTitle)
    {
        if (mWindow != null)
        {
            GUIContent lTitleContent = new GUIContent(pTitle, mIcon);
            mWindow.titleContent = lTitleContent;
        }
    }

    private static void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        WaitUntilWritable();
        LocalizationService.LoadCSV(mAssetPath);
        mLanguages = LocalizationService.GetLanguages();
        mKeys = LocalizationService.GetKeys();
        mCopiedValues = LocalizationService.GetValues();
        if (mWindow != null)
        {
            mWindow.Repaint();
        }
    }

    void OnGUI()
    {
        if (mFileLoaded == false)
        {
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            mEditedFile = EditorGUILayout.ObjectField(mEditedFile, typeof(TextAsset), false, GUILayout.MinWidth(150), GUILayout.MaxWidth(300), GUILayout.ExpandWidth(false)) as TextAsset;
            if (GUILayout.Button("Load", GUILayout.Width(200)) == true)
            {
                LoadAsset(mEditedFile);
            }
            GUILayout.EndHorizontal();
            return;
        }
        string lPoppedTempKey = string.Empty;
        string lPoppedKey = string.Empty;

        // Changes the top section horizontal scrollbar style to none so it doesn't show if keys were added.
        GUIStyle scrollStyle = mAddedKeys.Count > 0 ? GUIStyle.none : GUI.skin.horizontalScrollbar;
        mScrollPosition = EditorGUILayout.BeginScrollView(mScrollPosition, scrollStyle, GUI.skin.verticalScrollbar);
        GUILayout.BeginHorizontal();
        EditorGUILayout.SelectableLabel(string.Empty, GUILayout.Width(200));
        foreach (string lLanguage in mLanguages)
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.SelectableLabel(lLanguage);
            GUILayout.FlexibleSpace();
        }
        GUILayout.EndHorizontal();
        GUILayout.BeginVertical();
        GUILayout.EndVertical();
        foreach (string lKey in mKeys)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-", GUILayout.Width(15)) == true)
            {
                lPoppedKey = lKey;
                SetTitle("CSV Editor*");
                mHasChanged = true;
            }
            EditorGUILayout.SelectableLabel(lKey, GUILayout.Width(200));

            foreach (string lLanguage in mLanguages)
            {
                GUILayout.FlexibleSpace();
                mCopiedValues[lLanguage][lKey] = EditorGUILayout.TextField(mCopiedValues[lLanguage][lKey], GUILayout.MinWidth(300), GUILayout.MaxWidth(300));
                GUILayout.FlexibleSpace();

            }
            GUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        if (lPoppedKey != string.Empty)
        {
            foreach (string lLanguage in mLanguages)
            {
                mCopiedValues[lLanguage].Remove(lPoppedKey);
            }
            mKeys.Remove(lPoppedKey);
        }
        // If temporary keys were created.
        if (mAddedKeys.Count > 0)
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            mSecondScrollPosition = EditorGUILayout.BeginScrollView(mSecondScrollPosition);
            mScrollPosition.x = mSecondScrollPosition.x;
            foreach (string lKey in mAddedKeys.Keys)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("-", GUILayout.Width(15)) == true)
                {
                    lPoppedTempKey = lKey;
                }
                EditorGUILayout.SelectableLabel(lKey, GUILayout.Width(200));
                foreach (string lLanguage in mLanguages)
                {
                    GUILayout.FlexibleSpace();
                    mAddedKeys[lKey][lLanguage] = EditorGUILayout.TextField(mAddedKeys[lKey][lLanguage], GUILayout.MinWidth(300), GUILayout.MaxWidth(300));
                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        if (lPoppedTempKey != string.Empty)
        {
            mAddedKeys.Remove(lPoppedTempKey);
        }
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        GUILayout.BeginHorizontal();
        if (GUI.changed == true)
        {
            mHasChanged = true;
        }

        mKeyName = EditorGUILayout.TextField(mKeyName, GUILayout.Width(200));

        if (GUILayout.Button("Add", GUILayout.Width(100)) == true)
        {
            if (CreateKey() == true)
            {
                SetTitle("CSV Editor*");
                mHasChanged = true;
            }
        }

        if (mHasChanged == true)
        {
            if (GUILayout.Button("Save CSV", GUILayout.Width(200)) == true)
            {
                RefreshCSV();
                WriteNewValues();
                SetTitle("CSV Editor");
                mHasChanged = false;
            }
        }
        GUILayout.EndHorizontal();
        if (mIsDirty == true)
        {
            AssetDatabase.Refresh();
            mIsDirty = false;
        }
        GUILayout.BeginVertical();
        GUILayout.Space(5);
        GUILayout.EndVertical();
        GUI.changed = false;
        GUILayout.BeginHorizontal();
        mEditedFile = EditorGUILayout.ObjectField(mEditedFile, typeof(TextAsset), false, GUILayout.MinWidth(150), GUILayout.MaxWidth(300), GUILayout.ExpandWidth(false)) as TextAsset;
        if (GUILayout.Button("Load", GUILayout.Width(200)) == true)
        {
            LoadAsset(mEditedFile);
        }
        GUILayout.EndHorizontal();
        GUILayout.BeginVertical();
        GUILayout.Space(5);
        GUILayout.EndVertical();
    }

    // Write the existing keys (and remove the deleted ones) to the CSV file.
    private void RefreshCSV()
    {
        WaitUntilWritable();
        using (StreamWriter sw = new StreamWriter(mAssetPath))
        {
            string lFullText = "Key;";
            foreach (string lLanguage in mLanguages)
            {
                lFullText += lLanguage;
                lFullText += ";";
            }
            lFullText = lFullText.Substring(0, lFullText.Length - 1);
            lFullText += "\n";
            foreach (string lKey in mKeys)
            {
                string lText = string.Empty;
                lText += lKey;
                lText += ";";
                foreach (string lLanguage in mLanguages)
                {
                    if (mCopiedValues[lLanguage][lKey] != string.Empty)
                    {
                        lText += mCopiedValues[lLanguage][lKey];
                    }
                    lText += ";";
                }
                lText = lText.Substring(0, lText.Length - 1);
                lFullText += lText + "\n";
            }
            lFullText = lFullText.Substring(0, lFullText.Length - 1);
            sw.Write(lFullText);
        }
    }

    // Write the new values to the CSV file.
    private void WriteNewValues()
    {
        WaitUntilWritable();
        using (StreamWriter sw = File.AppendText(mAssetPath))
        {
            bool lHasEmptyKey = false;
            string lFullText = string.Empty;
            foreach (string lKey in mAddedKeys.Keys)
            {
                string lText = string.Empty;
                lText += "\n" + lKey;
                lText += ";";
                foreach (string lLanguage in mLanguages)
                {
                    if (mAddedKeys[lKey][lLanguage] != string.Empty)
                    {
                        lText += mAddedKeys[lKey][lLanguage];
                    }
                    else
                    {
                        lHasEmptyKey = true;
                        break;
                    }
                    lText += ";";
                }
                if (lHasEmptyKey == true)
                {
                    break;
                }
                lText = lText.Substring(0, lText.Length - 1);
                lFullText += lText;
            }
            if (lHasEmptyKey == false && lFullText.Length > 0)
            {
                sw.Write(lFullText);
                mAddedKeys.Clear();
            }
        }
        mIsDirty = true;
    }

    // Create new key
    private bool CreateKey()
    {
        if (mKeyName.Equals(string.Empty) || mKeys.Contains(mKeyName) || mAddedKeys.Keys.Contains(mKeyName))
        {
            return false;
        }
        Dictionary<string, string> lDictionary = new Dictionary<string, string>();
        foreach (string lLanguage in mLanguages)
        {
            lDictionary.Add(lLanguage, "Enter value");
        }
        mAddedKeys.Add(mKeyName, lDictionary);
        mKeyName = string.Empty;
        return true;
    }

    private static void WaitUntilWritable()
    {
        bool lFileIsLocked = true;
        while (lFileIsLocked == true)
        {
            try
            {
                // Attempt to open the file exclusively.
                using (FileStream fs = new FileStream(mAssetPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 100))
                {
                    fs.ReadByte();
                    lFileIsLocked = false;
                }
            }
            finally
            {
                System.Threading.Thread.Sleep(50);
            }
        }
    }
}