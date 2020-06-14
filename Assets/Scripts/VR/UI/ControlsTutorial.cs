using UnityEngine;
using System.Collections;
using Valve.VR;
using System.Collections.Generic;
using Valve.VR.InteractionSystem;
using System;
using static Valve.VR.InteractionSystem.ControllerButtonHints;

public class ControlsTutorial : MonoBehaviour
{
    private interface IActionEntry
    {
        ISteamVR_Action_In_Source GetAction();
        string GetText();
        bool IsAnchorOnRightSide();
    }

    private class ActionEntry<TAction> : IActionEntry
        where TAction : ISteamVR_Action_In_Source
    {
        [SerializeField] private TAction action;
        [SerializeField, Multiline] private string text;
        [SerializeField] private bool anchorOnRightSide;

        public ISteamVR_Action_In_Source GetAction()
        {
            return action;
        }

        public string GetText()
        {
            return text;
        }

        public bool IsAnchorOnRightSide()
        {
            return anchorOnRightSide;
        }
    }

    [SerializeField] private SteamVR_Action_Boolean toggleHints;

    [Serializable]
    private class BooleanActionEntry : ActionEntry<SteamVR_Action_Boolean> { }

    [Serializable]
    private class Vector2ActionEntry : ActionEntry<SteamVR_Action_Vector2> { }

    [SerializeField] private BooleanActionEntry[] booleanActions;
    [SerializeField] private Vector2ActionEntry[] vector2Actions;

    [SerializeField] private Hand[] hands;

    private List<IActionEntry> actions = new List<IActionEntry>();

    private bool showHints = true;

    private bool showTimer = true;

    private void Start()
    {
        actions.AddRange(booleanActions);
        actions.AddRange(vector2Actions);

        StartCoroutine(Timer());
    }

    private IEnumerator Timer()
    {
        yield return new WaitForSeconds(2.0f);

        if(showTimer)
        {
            ShowHints();
        }
    }

    private void ShowHints()
    {
        foreach (var action in actions)
        {
            foreach (var hand in hands)
            {
                ControllerButtonHints.ShowTextHint(action.IsAnchorOnRightSide() ? AnchorSide.RIGHT : AnchorSide.LEFT, hand, action.GetAction(), action.GetText(), true);
            }
        }
    }

    private void HideHints()
    {
        foreach (var action in actions)
        {
            foreach (var hand in hands)
            {
                ControllerButtonHints.HideTextHint(action.IsAnchorOnRightSide() ? AnchorSide.RIGHT : AnchorSide.LEFT, hand, action.GetAction());
            }
        }
    }

    private void Update()
    {
        if(toggleHints != null && toggleHints.active && toggleHints.stateDown)
        {
            showTimer = false;

            showHints = !showHints;

            if (!showHints)
            {
                HideHints();
            }
            else
            {
                ShowHints();
            }
        }
    }
}
