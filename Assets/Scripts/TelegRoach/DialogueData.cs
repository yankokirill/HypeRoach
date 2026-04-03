using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct MessageEntry
{
    public Messenger.Sender sender;
    [TextArea(3, 10)] public string text;
    public float delayAfter;
}

[CreateAssetMenu(fileName = "NewDialogue", menuName = "TelegRoach/Dialogue")]
public class DialogueData : ScriptableObject
{
    public List<MessageEntry> messages;
}
