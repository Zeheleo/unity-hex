using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SaveLoadItem : MonoBehaviour
{
	public SaveLoadMenu menu;
	string _mapID;
	public string MapID
	{
		get
		{
			return _mapID;
		}
		set
		{
			_mapID = value;
			transform.GetChild(0).GetComponent<Text>().text = value;
		}
	}

	public void Select()
	{
		menu.SelectItem(_mapID);
	}
}
