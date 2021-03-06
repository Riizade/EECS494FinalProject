﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public enum Direction {
	Up, Down, Left, Right, None
};

public enum Element
{
    Fire, Ice, None
}

//Create a Level GO to act as a parent to be deleted at the end of each level in order to clean up memory

public class GameManager : MonoBehaviour {

	static public GameManager S;
    
    // Sprites/Colors
    public Sprite[] statusEffectSprites;
    public Sprite[] liquidTileSprites;
    public Sprite[] volcanoSprites;
    public Color[] elementColors;
	public Sprite[] floorTileSprites; //Sprites to be used for placing any floor tiles on the fly
    public Sprite playerSprite;
    public Sprite tombstoneIcon;
    public Sprite[] playerIndicatorSprites;
    public Sprite[] elementIcons;

    // Random Prefabs
    public GameObject[] enemyTypes;
    public GameObject[] weaponDrops;
    public GameObject weaponPickupPrefab;
    public GameObject manaPotionPrefab;
    public GameObject iceShardPrefab;
    public GameObject floorTile; //Floor tile prefab used to place floor tiles on the fly
    public GameObject wallTile; //Wall tile prefab ^^
	public GameObject room;
    public GameObject door;
    public GameObject coinPrefab;
	public GameObject wallFixture; //Used to place walls where doors aren't needed
	public GameObject hallway;
    public GameObject floatingTextPrefab;
    public GameObject pricetagPrefab;
    public GameObject merchantStandPrefab;

	//Minimap Prefabs
	public GameObject currRoomBorder;
	public GameObject roomBlocker; //Covers the room in the minimap
	public GameObject hallwayBlocker; //covers the hallway in the minimap
	public GameObject miniMapDoor;

	//Players
	public GameObject playerPrefab;
	public GameObject[] players;
	public bool playersInitialized = false;

	//Room data
	public TextAsset[] easyLayouts;
	public TextAsset[] medLayouts;
	public TextAsset[] hardLayouts;
	public TextAsset[] roomFiles;
	public TextAsset endRoom;
	public TextAsset bossRoomFile;
	private List<TextAsset> easyList;
	private List<TextAsset> medList;
	private List<TextAsset> hardList;
	private List<TextAsset> roomList;

	public TextAsset[] bossRoomFiles; //Indexed by Element enum
	public int roomWidth = 24;
	public int roomHeight = 16;
	public int hallLength = 8;
	public int hallWidth = 6;

	//Door offsets
	public int h_UpAndDown = 13;
	public int v_LeftAndRight = 6;

    //Game meta data
    public GameObject HUDCanvas;
    public GameObject LoadingScreenCanvas;
	public GameObject EndGameCanvasLose;
    public GameObject EndGameCanvasWin;
    public GameObject instructionalCanvas;
    public float floatingTextInterval = 0.5f;
	public int numPlayers = 0;
    public bool gameOver = false;
    public int numRounds = 50;
	public Room currentRoom;
	public Element currentLevelElement;
    [HideInInspector]
    public int round = 1;
    public int goldAmount = 0;
    GameObject goldAmountText;

	public bool created = false;

	//GameObjects that need to be deleted after the level is finished
	List<GameObject> levelGOs;

	void Awake(){
		S = this;
		EndGameCanvasLose.SetActive (false);
        goldAmountText = HUDCanvas.transform.FindChild("GoldAmount").gameObject;
        Invoke("TurnOffInstructionalCanvas", 7.5f);
	}

    void TurnOffInstructionalCanvas()
    {
        instructionalCanvas.SetActive(false);
    }

	//Use this function to set up the initial game level
	//TODO Eventually create a start screen instead of just launching the game
	//     in order to keep players from being shocked
	void Start(){
		Setup (); //Only called when the game actually starts up

        CreateDungeonLevel();

        //Create Players and set their position
        numPlayers = PlayerPrefs.GetInt("numPlayers");
        if (numPlayers == 0) numPlayers = 1;
        
        if (numPlayers == 1)
        {
            HUDCanvas.transform.FindChild("P2HUD").gameObject.SetActive(false);
        }

        players = new GameObject[numPlayers];
		for (int i = 1; i <= numPlayers; ++i) {
			GameObject p = Instantiate (playerPrefab);
			Player player = p.GetComponent<Player> ();
			player.playerNum = i;
			player.controllerNum = i;
			//player.controllerNum = 0;
			player.PlacePlayer();
			players [i - 1] = p;
		}
        
		playersInitialized = true;
	}


	//Setup functions for the GameManager

	//Does a lot of book keeping set up for the game
	//Also initialized structures for the GameManager
	void Setup(){
		//init structures
		levelGOs = new List<GameObject>();
		easyList = new List<TextAsset> ();
		medList = new List<TextAsset> ();
		hardList = new List<TextAsset> ();
		roomList = new List<TextAsset> ();

		currRoomBorder = Instantiate (currRoomBorder);
		currRoomBorder.transform.position = new Vector3 (-100, -100, 0);

		//Fill structures
		LoadTextAssets (); //load text files
	}

	void LoadTextAssets(){
		// string path = Application.dataPath;
		foreach(TextAsset ta in Resources.LoadAll("LayoutEasy", typeof(TextAsset))){
			easyList.Add(ta);
		}
		GameManager.S.easyLayouts = easyList.ToArray ();

		foreach(TextAsset ta in Resources.LoadAll("LayoutMedium", typeof(TextAsset))){
			medList.Add(ta);
		}
		GameManager.S.medLayouts = medList.ToArray ();

		foreach(TextAsset ta in Resources.LoadAll("LayoutHard", typeof(TextAsset))){
			hardList.Add(ta);
		}
		GameManager.S.hardLayouts = hardList.ToArray ();

		foreach (TextAsset ta in Resources.LoadAll("RoomFiles", typeof(TextAsset))) {
			roomList.Add (ta);
		}
		GameManager.S.roomFiles = roomList.ToArray ();
	}



	//Game Helpers

	//Adds child to the level game object to be deleted at the end of the level
	public void AddObject(GameObject go){
		levelGOs.Add (go);
	}

	//Returns a random element from the enum to be used in each dungeon level
	public Element GetNextElement(){
		Element newElt = Element.None;

        switch (currentLevelElement)
        {
            case Element.Fire:
                newElt = Element.Ice;
                break;
            case Element.Ice:
                newElt = Element.Fire;
                break;
        }

		return newElt;
	}

	//Returns a random room file to be placed in the level
	public TextAsset GetRandomRoomFile(){
		return roomFiles[UnityEngine.Random.Range(0, roomFiles.Length)];
	}

    public void AddGold(int amount)
    {
        goldAmount += amount;
        goldAmountText.GetComponent<UnityEngine.UI.Text>().text = goldAmount.ToString();
    }

	//Level Creation Methods

	//Picks an element for the Level, Creates the map, Creates a Dungeon Layout, and sets the initial game state for that level
	void CreateDungeonLevel(){
		currentLevelElement = GetNextElement(); //set the initial element for this dungeon level
        HUDCanvas.transform.FindChild("CurrentLevel").GetComponent<UnityEngine.UI.Text>().text = "Level " + round;
		DungeonLayoutGenerator.S.CreateLevelMap();
		DungeonLayout DL = DungeonLayoutGenerator.S.levelLayout.GetComponent<DungeonLayout> ();
        LoadingScreenCanvas.SetActive(false);

		created = true;
	}

    //Level Destruction
    public void CleanUpGame() {
		created = false;
        ++round;

        LoadingScreenCanvas.SetActive(true);
        Invoke("CleanUpHelper", 0.2f);
    }
    
    void CleanUpHelper() { 
		currRoomBorder.transform.position = new Vector3 (-100, -100, 0); //Put the border off screen
        foreach (GameObject go in levelGOs) {
			Destroy (go);
		}

		if (round <= numRounds) {
			CreateDungeonLevel ();
			foreach (GameObject p in players) {
				Player player = p.GetComponent<Player> ();
				player.PlacePlayer ();
			}
		} else {
            //GG show an end screen or something here
            LoadingScreenCanvas.SetActive(false);
            EndGameCanvasWin.SetActive(true);
		}
	}


	//End Game Functions

	//Function that checks to see if all the players died
	public void CheckPlayers(){
		bool allDead = true;
		foreach (GameObject go in players) {
			Player p = go.GetComponent<Player> ();
			if (!p.dead) {
				allDead = false;
				break;
			}
		}

		if (allDead)
        {
            EndGameCanvasLose.transform.FindChild("promptText").GetComponent<UnityEngine.UI.Text>().text = "You made it to Level " + round;
            EndGameCanvasLose.SetActive(true);
            playersInitialized = false;
            gameOver = true;
		}
	}
}
