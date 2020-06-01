﻿using UnityEngine;
using System.Collections;
using Valve.VR;
using System.Collections.Generic;
using Valve.VR.InteractionSystem;
using System;

public class ControlsTutorial : MonoBehaviour
{
    private interface IActionEntry
    {
        ISteamVR_Action_In_Source GetAction();
        string GetText();
    }

    private class ActionEntry<TAction> : IActionEntry
        where TAction : ISteamVR_Action_In_Source
    {
        [SerializeField] private TAction action;
        [SerializeField] private string text;

        public ISteamVR_Action_In_Source GetAction()
        {
            return action;
        }

        public string GetText()
        {
            return text;
        }
    }

    [Serializable]
    private class BooleanActionEntry : ActionEntry<SteamVR_Action_Boolean> { }

    [Serializable]
    private class Vector2ActionEntry : ActionEntry<SteamVR_Action_Boolean> { }

    [SerializeField] private BooleanActionEntry[] booleanActions;
    [SerializeField] private Vector2ActionEntry[] vector2Actions;

    [SerializeField] private Hand[] hands;

    private List<IActionEntry> actions = new List<IActionEntry>();

    private void Start()
    {
        actions.AddRange(booleanActions);
        actions.AddRange(vector2Actions);
    }

    private void Update()
    {
        foreach(var action in actions)
        {
            foreach (var hand in hands)
            {
                ControllerButtonHints.ShowButtonHint(hand, action.GetAction());
                ControllerButtonHints.ShowTextHint(hand, action.GetAction(), action.GetText(), true);
            }
        }
    }
}
