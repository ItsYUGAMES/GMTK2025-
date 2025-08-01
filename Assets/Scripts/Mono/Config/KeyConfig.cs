using UnityEngine;

[System.Serializable]
public class KeyConfig
{
    public KeyCode primaryKey = KeyCode.A;
    public KeyCode secondaryKey = KeyCode.D;

    public void Save(string prefix)
    {
        PlayerPrefs.SetInt(prefix + "_PrimaryKey", (int)primaryKey);
        PlayerPrefs.SetInt(prefix + "_SecondaryKey", (int)secondaryKey);
        PlayerPrefs.Save();
    }

    public void Load(string prefix)
    {
        if (PlayerPrefs.HasKey(prefix + "_PrimaryKey"))
            primaryKey = (KeyCode)PlayerPrefs.GetInt(prefix + "_PrimaryKey");
        if (PlayerPrefs.HasKey(prefix + "_SecondaryKey"))
            secondaryKey = (KeyCode)PlayerPrefs.GetInt(prefix + "_SecondaryKey");
    }
}
