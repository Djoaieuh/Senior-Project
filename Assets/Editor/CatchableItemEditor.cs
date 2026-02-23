#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(CatchableItem))]
public class CatchableItemEditor : Editor
{
    private SerializedProperty reelSequenceProp;
    
    private void OnEnable()
    {
        reelSequenceProp = serializedObject.FindProperty("reelSequence");
    }
    
    public override void OnInspectorGUI()
    {
        // Draw default inspector for everything except reel sequence
        serializedObject.Update();
        
        // Draw all default properties
        DrawPropertiesExcluding(serializedObject, "reelSequence");
        
        // Custom reel sequence section
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Reel Sequence", EditorStyles.boldLabel);
        
        int sequenceSize = reelSequenceProp.arraySize;
        
        // Size field with buttons
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Sequence Length: {sequenceSize}", GUILayout.Width(150));
        
        if (GUILayout.Button("+", GUILayout.Width(30)))
        {
            reelSequenceProp.InsertArrayElementAtIndex(sequenceSize);
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.EndHorizontal();
            serializedObject.ApplyModifiedProperties();
            return; // Exit early to avoid accessing stale data
        }
        
        if (GUILayout.Button("-", GUILayout.Width(30)) && sequenceSize > 0)
        {
            reelSequenceProp.DeleteArrayElementAtIndex(sequenceSize - 1);
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.EndHorizontal();
            serializedObject.ApplyModifiedProperties();
            return; // Exit early
        }
        
        if (GUILayout.Button("Clear", GUILayout.Width(50)))
        {
            reelSequenceProp.ClearArray();
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.EndHorizontal();
            serializedObject.ApplyModifiedProperties();
            return; // Exit early
        }
        
        EditorGUILayout.EndHorizontal();
        
        // Draw sequence buttons in a grid
        if (sequenceSize > 0)
        {
            EditorGUILayout.Space(5);
            
            int buttonsPerRow = 3;
            int rows = Mathf.CeilToInt(sequenceSize / (float)buttonsPerRow);
            
            int indexToDelete = -1; // Track which index to delete
            
            for (int row = 0; row < rows; row++)
            {
                EditorGUILayout.BeginHorizontal();
                
                for (int col = 0; col < buttonsPerRow; col++)
                {
                    int index = row * buttonsPerRow + col;
                    if (index >= sequenceSize) break;
                    
                    SerializedProperty element = reelSequenceProp.GetArrayElementAtIndex(index);
                    
                    // Label with index
                    EditorGUILayout.LabelField($"[{index}]", GUILayout.Width(30));
                    
                    // Button dropdown
                    element.enumValueIndex = (int)(ReelButton)EditorGUILayout.EnumPopup(
                        (ReelButton)element.enumValueIndex, 
                        GUILayout.Width(100)
                    );
                    
                    // Quick remove button
                    if (GUILayout.Button("×", GUILayout.Width(20)))
                    {
                        indexToDelete = index;
                    }
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            // Delete after the loop and exit early
            if (indexToDelete >= 0)
            {
                reelSequenceProp.DeleteArrayElementAtIndex(indexToDelete);
                serializedObject.ApplyModifiedProperties();
                return; // Exit early to let Unity repaint with updated data
            }
        }
        
        // Quick presets section
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Quick Presets", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Simple (1-2-3-4)"))
        {
            SetSequence(new ReelButton[] { 
                ReelButton.Button1, 
                ReelButton.Button2, 
                ReelButton.Button3, 
                ReelButton.Button4 
            });
            EditorGUILayout.EndHorizontal();
            serializedObject.ApplyModifiedProperties();
            return; // Exit early
        }
        
        if (GUILayout.Button("Alternating (1-3-1-3)"))
        {
            SetSequence(new ReelButton[] { 
                ReelButton.Button1, 
                ReelButton.Button3, 
                ReelButton.Button1, 
                ReelButton.Button3 
            });
            EditorGUILayout.EndHorizontal();
            serializedObject.ApplyModifiedProperties();
            return; // Exit early
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Cross (2-4-2-4)"))
        {
            SetSequence(new ReelButton[] { 
                ReelButton.Button2, 
                ReelButton.Button4, 
                ReelButton.Button2, 
                ReelButton.Button4 
            });
            EditorGUILayout.EndHorizontal();
            serializedObject.ApplyModifiedProperties();
            return; // Exit early
        }
        
        if (GUILayout.Button("Random x6"))
        {
            ReelButton[] random = new ReelButton[6];
            for (int i = 0; i < 6; i++)
            {
                random[i] = (ReelButton)Random.Range(0, 4);
            }
            SetSequence(random);
            EditorGUILayout.EndHorizontal();
            serializedObject.ApplyModifiedProperties();
            return; // Exit early
        }
        
        EditorGUILayout.EndHorizontal();
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void SetSequence(ReelButton[] buttons)
    {
        reelSequenceProp.ClearArray();
        for (int i = 0; i < buttons.Length; i++)
        {
            reelSequenceProp.InsertArrayElementAtIndex(i);
            reelSequenceProp.GetArrayElementAtIndex(i).enumValueIndex = (int)buttons[i];
        }
    }
}
#endif