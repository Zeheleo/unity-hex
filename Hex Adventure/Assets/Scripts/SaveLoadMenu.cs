using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;

public class SaveLoadMenu : MonoBehaviour
{
    public HexGrid hexGrid;
    bool saveMode;
    public Text menuLabel, actionButtonLabel;
    public InputField nameInput;

	public RectTransform listContent;
	public SaveLoadItem itemPrefab;

    public void Open(bool saveMode)
    {
        this.saveMode = saveMode;
        if(saveMode)
        {
            menuLabel.text = "Save";
            actionButtonLabel.text = "Save";
        }
        else
        {
            menuLabel.text = "Load";
            actionButtonLabel.text = "Load";
        }

		FillList();

        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    string GetSelectedPath()
    {
        string mapID = nameInput.text;
        if (mapID.Length == 0)
        {
            return null;
        }
        return Path.Combine(Application.persistentDataPath, mapID + ".map");
    }

    void Save(string path)
    {  
        using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create)))
        {
            // Debug.Log(Application.persistentDataPath);            
            writer.Write(1);
            hexGrid.Save(writer);
        }
    }

    void Load(string path)
    {
		if (!File.Exists(path))
        {
            Debug.LogError("File does not exist " + path);
            return;
        }

        using (BinaryReader reader = new BinaryReader(File.OpenRead(path)))
        {
            int header = reader.ReadInt32();
            if(header <= 1)
            {
                hexGrid.Load(reader, header);
            }
            else
            {
                Debug.LogWarning("Unknown format " + header);
            }
        }

    } 

    public void Action()
    {
        string path = GetSelectedPath();
        if(path == null)
        {
            return;
        }

        if(saveMode)
        {
            Save(path);
        }
        else
        {
            Load(path);
        }
        Close();
    }

	public void Delete()
	{
		string path = GetSelectedPath();
		if (path == null)
		{
			return;
		}
		if (File.Exists(path))
		{
			File.Delete(path);
		}
		nameInput.text = "";
		FillList();
	}

    public void SelectItem(string name)
    {
	    nameInput.text = name;
    }

	void FillList()
	{
		for (int i =0; i < listContent.childCount; i++)
		{
			Destroy(listContent.GetChild(i).gameObject);
		}

		string[] paths = Directory.GetFiles(Application.persistentDataPath, "*.map");
		Array.Sort(paths);

		for (int i = 0; i < paths.Length; i++)
		{
			SaveLoadItem item = Instantiate(itemPrefab);
			item.menu = this;
			item.MapID = Path.GetFileNameWithoutExtension(paths[i]);
			item.transform.SetParent(listContent, false);
		}
	}
}
