using System;
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

    private readonly List<ParsingQuery> queries = new();
    private List<string> matchedWords = new();

    [MenuItem("Avataris/Sentence parser")]
    public static void ShowWindow()
    {
        GetWindow<AvatarisSentenceParserEditor>("Sentence Parser");
    }

    #region WindowDrawing
    private void OnGUI()
    {
        DrawInputSection();
        DrawQueriesSection();
        DrawSentenceInput();
        DrawExecuteButton();
    }

    private void DrawInputSection()
    {
        float inputSectionWidth = WINDOW_WIDTH * 0.4f;

        GUILayout.BeginHorizontal(GUILayout.Width(WINDOW_WIDTH));
        GUILayout.BeginVertical(GUILayout.Width(inputSectionWidth));

        GUILayout.Label("New Query", EditorStyles.boldLabel);
        newQueryContent = EditorGUILayout.TextField("Query:", newQueryContent);
        newQueryPriority = EditorGUILayout.FloatField("Priority:", newQueryPriority);

        EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(newQueryContent));
        if (GUILayout.Button("Add Query"))
        {
            AddQuery();
        }
        EditorGUI.EndDisabledGroup();

        GUILayout.EndVertical();
    }

    private void DrawQueriesSection()
    {
        float queriesSectionWidth = WINDOW_WIDTH * 0.6f;

        GUILayout.BeginVertical(GUILayout.Width(queriesSectionWidth));
        GUILayout.Label("Existing Queries", EditorStyles.boldLabel);

        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        List<int> indicesToRemove = new();
        for (int i = 0; i < queries.Count; i++)
        {
            DrawQueryRow(queries[i], queriesSectionWidth, indicesToRemove, i);
        }
        RemoveQueries(indicesToRemove);

        GUILayout.EndScrollView();

        EditorGUI.BeginDisabledGroup(queries.Count <= 0);
        if (GUILayout.Button("Delete All Queries"))
        {
            queries.Clear();
        }
        EditorGUI.EndDisabledGroup();

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }

    private void DrawQueryRow(ParsingQuery query, float width, List<int> indicesToRemove, int index)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(query.Query, GUILayout.Width(width * 0.6f));
        GUILayout.Label(query.Priority.ToString("F1"), GUILayout.Width(width * 0.1f));

        if (GUILayout.Button("Remove Query", GUILayout.Width(width * 0.2f)))
        {
            indicesToRemove.Add(index);
        }

        GUILayout.EndHorizontal();
    }

    private void DrawSentenceInput()
    {
        GUILayout.Label("Input Sentence", EditorStyles.boldLabel);
        inputSentence = EditorGUILayout.TextField("Enter an input:", inputSentence, GUILayout.Height(100f));
    }

    private void DrawExecuteButton()
    {
        EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(inputSentence) || queries.Count <= 0);

        if (GUILayout.Button("Release the Kraken!", GUILayout.Width(WINDOW_WIDTH)))
        {
            ExecuteQueries();
        }

        EditorGUI.EndDisabledGroup();
    }
    #endregion

    private void RemoveQueries(List<int> indicesToRemove)
    {
        for (int i = indicesToRemove.Count - 1; i >= 0; i--)
        {
            queries.RemoveAt(indicesToRemove[i]);
        }
    }

    private void AddQuery()
    {
        if (!string.IsNullOrEmpty(newQueryContent))
        {
            queries.Add(new ParsingQuery(newQueryContent, newQueryPriority));
            queries.Sort((lhsQuery, rhsQuery) => rhsQuery.Priority.CompareTo(lhsQuery.Priority));
            EditorUtility.DisplayDialog("Success", $"Query \n{newQueryContent} \nwith priority {newQueryPriority} has been added successfully!", "OK");
        }
    }

    private void ExecuteQueries()
    {
        matchedWords.Clear();
        var matchingQuery = FindMatchingQuery(inputSentence, queries);

        if (matchingQuery != null)
        {
            string matchedWordsStr = string.Join(", ", matchedWords);
            EditorUtility.DisplayDialog("Result",
                $"The best matching query is:\n{matchingQuery.Query}\nwith priority {matchingQuery.Priority}\nMatched words: {matchedWordsStr}", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Result", "No matching query found.", "OK");
        }

        Debug.Log("Query execution complete!");
    }

    private ParsingQuery FindMatchingQuery(string sentence, List<ParsingQuery> queries)
    {
        foreach (var query in queries)
        {
            matchedWords.Clear();

            if (DoesSentenceMatchQuery(sentence, query.Query))
            {
                return query;
            }
        }
        return null;
    }

    private bool DoesSentenceMatchQuery(string sentence, string query)
    {
        var words = new HashSet<string>(sentence.ToLower().Split(new[] { ' ', ',', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries));
        var queryTree = TokenizeQuery(query.ToLower());

        return EvaluateQuery(words, queryTree);
    }

    private QueryNode TokenizeQuery(string query)
    {
        var rootNode = new QueryNode
        {
            Type = NodeType.Or
        };

        var stack = new Stack<QueryNode>();

        stack.Push(rootNode);
        var currentNode = rootNode;
        int position = 0;

        while (position < query.Length)
        {
            char currentCharacter = query[position];

            if (currentCharacter == '(')
            {
                var newNode = new QueryNode
                {
                    Type = NodeType.Or
                };

                currentNode.Children.Add(newNode);
                stack.Push(currentNode);
                currentNode = newNode;
            }
            else if (currentCharacter == ')')
            {
                currentNode = stack.Pop();
            }
            else if (currentCharacter == '|')
            {
                var newNode = new QueryNode
                {
                    Type = NodeType.Or
                };

                stack.Peek().Children.Add(newNode);
                currentNode = newNode;
            }
            else if (currentCharacter == '&')
            {
                var newNode = new QueryNode
                {
                    Type = NodeType.And
                };

                currentNode.Children.Add(newNode);
                currentNode = newNode;
            }
            else if (currentCharacter == '!')
            {
                var notNode = new QueryNode
                {
                    Type = NodeType.Not
                };

                notNode.Children.Add(new QueryNode
                {
                    Type = NodeType.Condition,
                    Value = ExtractCondition(query, ref position)
                });

                currentNode.Children.Add(notNode);
            }
            else
            {
                var condition = ExtractCondition(query, ref position);

                currentNode.Children.Add(new QueryNode
                {
                    Type = NodeType.Condition,
                    Value = condition
                });
            }

            position++;
        }

        return rootNode;
    }

    private bool EvaluateQuery(HashSet<string> words, QueryNode node)
    {
        switch (node.Type)
        {
            case NodeType.Or:
                return EvaluateOrNode(words, node);

            case NodeType.And:
                return EvaluateAndNode(words, node);

            case NodeType.Not:
                return EvaluateNotNode(words, node);

            case NodeType.Condition:
                return EvaluateCondition(words, node.Value);

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private string ExtractCondition(string query, ref int position)
    {
        int start = position;

        while (position < query.Length && !IsDelimiter(query[position]))
        {
            position++;
        }

        return query.Substring(start, position - start).Trim();
    }

    private bool IsDelimiter(char character)
    {
        return char.IsWhiteSpace(character) || character == '&' || character == '|' || character == '!' || character == '(' || character == ')';
    }

    private bool EvaluateOrNode(HashSet<string> words, QueryNode node)
    {
        foreach (var child in node.Children)
        {
            if (EvaluateQuery(words, child))
            {
                return true;
            }
        }
        return false;
    }

    private bool EvaluateAndNode(HashSet<string> words, QueryNode node)
    {
        foreach (var child in node.Children)
        {
            if (!EvaluateQuery(words, child))
            {
                return false;
            }
        }
        return true;
    }

    private bool EvaluateNotNode(HashSet<string> words, QueryNode node)
    {
        return node.Children.Count > 0 && !EvaluateQuery(words, node.Children[0]);
    }

    private bool EvaluateCondition(HashSet<string> words, string condition)
    {
        if (condition.Contains("["))
        {
            return EvaluateOptionalCondition(words, condition);
        }
        else
        {
            return EvaluateSimpleCondition(words, condition);
        }
    }

    private bool EvaluateOptionalCondition(HashSet<string> words, string condition)
    {
        int bracketStart = condition.IndexOf('[');
        if (bracketStart == -1)
        {
            return false;
        }

        var baseWord = condition.Substring(0, bracketStart).Trim();
        var options = condition.Substring(bracketStart + 1, condition.Length - bracketStart - 2).Split('/');

        foreach (var option in options)
        {
            var word = baseWord + option;

            if (words.Contains(word))
            {
                matchedWords.Add(word);
                return true;
            }
        }

        if (words.Contains(baseWord))
        {
            matchedWords.Add(baseWord);
            return true;
        }
        return false;
    }

    private bool EvaluateSimpleCondition(HashSet<string> words, string condition)
    {
        if (words.Contains(condition))
        {
            matchedWords.Add(condition);
            return true;
        }
        return false;
    }
}