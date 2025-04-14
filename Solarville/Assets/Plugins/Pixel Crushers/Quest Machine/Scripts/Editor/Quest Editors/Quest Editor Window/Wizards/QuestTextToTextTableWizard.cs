// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace PixelCrushers.QuestMachine
{

    public class QuestTextToTextTableWizard : ScriptableWizard
    {

        public TextTable textTable;

        [Tooltip("Abbreviate field names to 32 characters instead of using full text as field name.")]
        public bool abbreviateFieldNames = false;

        [Tooltip("Prepend quest IDs to field names. Useful to prevent multiple quests from using the same field name.")]
        public bool prependQuestIDs = false;

        private static int MaxAbbreviationLength = 32;

        private static bool s_abbreviateFieldNames = false;
        private static bool s_prependQuestIDs = false;
        private static Quest s_quest = null;

        public static void Open()
        {
            ScriptableWizard.DisplayWizard<QuestTextToTextTableWizard>("Text To Text Table", "Move Text");
        }

        private void OnWizardUpdate()
        {
            helpString = "This wizard will REMOVE all text from this quest, put it in a text table, and link the quest to the text table.\n\nNOTE: You cannot undo this action!" +
               "\n\nQuest: " + ((QuestEditorWindow.selectedQuest != null) ? QuestEditorWindow.selectedQuest.name : "(none)");
            if (textTable == null) helpString += "\n\nPlease select a Text Table.";
        }

        private void OnWizardCreate()
        {
            var quest = QuestEditorWindow.selectedQuest;
            if (quest == null || textTable == null) return;
            if (EditorUtility.DisplayDialog("Move Text To Text Table", "This will remove all loose text from " + quest.name + " and move it to the text table " + textTable.name + ". Proceed?", "OK", "Cancel"))
            {
                Undo.RecordObject(quest, "Fill Text Table");
                MoveTextToTextTable(quest, textTable, abbreviateFieldNames, prependQuestIDs);
                EditorUtility.SetDirty(quest);
                QuestEditorWindow.RepaintNow();
                QuestEditorWindow.RepaintInspectorsNow();
            }
        }

        public static void MoveTextToTextTable(Quest quest, TextTable textTable, 
            bool abbreviateFieldNames, bool prependQuestIDs)
        {
            if (quest == null || textTable == null) return;
            s_abbreviateFieldNames = abbreviateFieldNames;
            s_prependQuestIDs = prependQuestIDs;
            MoveTextToTextTable(quest.offerContentList, textTable);
            MoveTextToTextTable(quest.stateInfoList, textTable);
            MoveTextToTextTable(quest.nodeList, textTable);
            Debug.Log("Quest Machine: Moved all loose text in " + quest.name + " to " + textTable.name + ".", textTable);
        }

        private static void MoveTextToTextTable(List<QuestNode> nodeList, TextTable textTable)
        {
            if (nodeList == null) return;
            for (int i = 0; i < nodeList.Count; i++)
            {
                MoveTextToTextTable(nodeList[i], textTable);
            }
        }

        private static void MoveTextToTextTable(QuestNode node, TextTable textTable)
        {
            if (node == null) return;
            MoveTextToTextTable(node.stateInfoList, textTable);
        }

        private static void MoveTextToTextTable(List<QuestStateInfo> stateInfoList, TextTable textTable)
        {
            if (stateInfoList == null) return;
            for (int i = 0; i < stateInfoList.Count; i++)
            {
                MoveTextToTextTable(stateInfoList[i], textTable);
            }
        }

        private static void MoveTextToTextTable(QuestStateInfo stateInfo, TextTable textTable)
        {
            if (stateInfo == null || stateInfo.categorizedContentList == null) return;
            for (int i = 0; i < stateInfo.categorizedContentList.Count; i++)
            {
                MoveTextToTextTable(stateInfo.categorizedContentList[i].contentList, textTable);
            }
        }

        private static void MoveTextToTextTable(QuestContentSet contentSet, TextTable textTable)
        {
            if (contentSet == null) return;
            MoveTextToTextTable(contentSet.contentList, textTable);
        }

        private static void MoveTextToTextTable(List<QuestContent> contentList, TextTable textTable)
        {
            if (contentList == null) return;
            for (int i = 0; i < contentList.Count; i++)
            {
                MoveTextToTextTable(contentList[i], textTable);
            }
        }

        private static void MoveTextToTextTable(QuestContent content, TextTable textTable)
        {
            if (content == null) return;
            MoveTextToTextTable(content.originalText, textTable);
        }

        private static void MoveTextToTextTable(StringField stringField, TextTable textTable)
        {
            if (stringField == null || stringField.textTable != null || stringField.stringAsset != null) return;
            if (string.IsNullOrEmpty(stringField.text)) return;
            var fieldName = s_abbreviateFieldNames
                ? ((stringField.text.Length <= MaxAbbreviationLength) ? stringField.text : stringField.text.Substring(0, MaxAbbreviationLength) + "...")
                : stringField.text;
            if (s_prependQuestIDs && s_quest != null)
            {
                fieldName = $"{s_quest.id}.{fieldName}";
            }
            var fieldID = textTable.GetFieldID(fieldName);
            if (fieldID == 0)
            {
                textTable.AddField(fieldName);
                var field = textTable.GetField(fieldName);
                field.SetTextForLanguage(0, stringField.text);
                fieldID = textTable.GetFieldID(fieldName);
            }
            stringField.text = string.Empty;
            stringField.stringAsset = null;
            stringField.textTable = textTable;
            stringField.textTableFieldID = fieldID;
        }

    }
}