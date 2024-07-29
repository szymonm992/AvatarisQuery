using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AvatarisSentenceParserEditor : EditorWindow
{
    private const float WINDOW_WIDTH = 1000f;

    private string inputSentence = string.Empty;
    private string newQueryContent = string.Empty;

    private float newQueryPriority;
    private Vector2 scrollPosition;

    private readonly List<ParsingQuery> allParsingQueries = new();

    [MenuItem("Avataris/Sentence parser")]
    public static void ShowWindow()
    {
        GetWindow<AvatarisSentenceParserEditor>("Sentence parser");
    }

    private void OnGUI()
    {
        float inputSectionWidth = WINDOW_WIDTH * 0.4f;
        float currentQueriesSectionWidth = WINDOW_WIDTH * 0.6f;

        GUILayout.BeginHorizontal(GUILayout.Width(WINDOW_WIDTH));
        GUILayout.BeginVertical(GUILayout.Width(inputSectionWidth));

        GUILayout.Label("New query", EditorStyles.boldLabel);
        newQueryContent = EditorGUILayout.TextField("Query:", newQueryContent);
        newQueryPriority = EditorGUILayout.FloatField("Priority:", newQueryPriority);

        EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(newQueryContent));

        if (GUILayout.Button("Add Query"))
        {
            if (!string.IsNullOrEmpty(newQueryContent))
            {
                allParsingQueries.Add(new ParsingQuery(newQueryContent, newQueryPriority));
                EditorUtility.DisplayDialog("Success", $"Query \n{newQueryContent} \nwith priority {newQueryPriority} has been added successfully!", "OK");
            }
            else
            {
                Debug.LogError("New query cannot be null or empty!");
            }
        }

        EditorGUI.EndDisabledGroup();
        GUILayout.EndVertical();

        GUILayout.BeginVertical(GUILayout.Width(currentQueriesSectionWidth));
        GUILayout.Label("Existing queries", EditorStyles.boldLabel);

        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        List<int> indicesToRemove = new ();

        for (int i = 0; i < allParsingQueries.Count; i++)
        {
            GUILayout.BeginHorizontal();
            var currentQuery = allParsingQueries[i];
            GUILayout.Label(currentQuery.Query, GUILayout.Width(currentQueriesSectionWidth * 0.6f));
            GUILayout.Label(currentQuery.Priority.ToString("F1"), GUILayout.Width(currentQueriesSectionWidth * 0.1f));

            if (GUILayout.Button("Remove Query", GUILayout.Width(currentQueriesSectionWidth * 0.2f)))
            {
                indicesToRemove.Add(i);
            }

            GUILayout.EndHorizontal();
        }

        for (int i = indicesToRemove.Count - 1; i >= 0; i--)
        {
            allParsingQueries.RemoveAt(indicesToRemove[i]);
        }

        GUILayout.EndScrollView();
        EditorGUI.BeginDisabledGroup(allParsingQueries.Count <= 0);

        if (GUILayout.Button("Delete all Queries"))
        {
            allParsingQueries.Clear();
        }

        EditorGUI.EndDisabledGroup();

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();

        GUILayout.Label("Input sentence", EditorStyles.boldLabel);
        inputSentence = EditorGUILayout.TextField("Enter an input:", inputSentence, GUILayout.Height(100f));

        EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(inputSentence));

        if (GUILayout.Button("Release the kraken!", GUILayout.Width(WINDOW_WIDTH)))
        {
            Debug.Log("Launch!");
        }

        EditorGUI.EndDisabledGroup();
    }
}