using UnityEngine;

public class Menu
{
    public readonly MenuEntry entry;
    public readonly GameObject instance;

    public Menu(MenuEntry entry, GameObject instance)
    {
        this.entry = entry;
        this.instance = instance;
    }
}
