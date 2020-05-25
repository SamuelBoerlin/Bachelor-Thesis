using UnityEngine;
using System;
using Valve.VR;
using System.Collections.Generic;

[Serializable]
public struct MenuEntry
{
    [SerializeField] private SteamVR_Input_Sources _inputSource;
    public SteamVR_Input_Sources InputSource
    {
        get
        {
            return _inputSource;
        }
    }

    [SerializeField] private SteamVR_Action_Boolean _action;
    public SteamVR_Action_Boolean Action
    {
        get
        {
            return _action;
        }
    }

    [SerializeField] private GameObject _prefab;
    public GameObject Prefab
    {
        get
        {
            return _prefab;
        }
    }

    [SerializeField] private GameObject _spawner;
    public GameObject Spawner
    {
        get
        {
            return _spawner;
        }
    }

    [SerializeField] private bool _toggle;
    public bool Toggle
    {
        get
        {
            return _toggle;
        }
    }

    public override bool Equals(object obj)
    {
        if (!(obj is MenuEntry))
        {
            return false;
        }

        var entry = (MenuEntry)obj;
        return _inputSource == entry._inputSource &&
               EqualityComparer<SteamVR_Action_Boolean>.Default.Equals(_action, entry._action) &&
               EqualityComparer<GameObject>.Default.Equals(_prefab, entry._prefab) &&
               EqualityComparer<GameObject>.Default.Equals(_spawner, entry._spawner) &&
               _toggle == entry._toggle;
    }

    public override int GetHashCode()
    {
        var hashCode = 334337139;
        hashCode = hashCode * -1521134295 + _inputSource.GetHashCode();
        hashCode = hashCode * -1521134295 + EqualityComparer<SteamVR_Action_Boolean>.Default.GetHashCode(_action);
        hashCode = hashCode * -1521134295 + EqualityComparer<GameObject>.Default.GetHashCode(_prefab);
        hashCode = hashCode * -1521134295 + EqualityComparer<GameObject>.Default.GetHashCode(_spawner);
        hashCode = hashCode * -1521134295 + _toggle.GetHashCode();
        return hashCode;
    }
}
