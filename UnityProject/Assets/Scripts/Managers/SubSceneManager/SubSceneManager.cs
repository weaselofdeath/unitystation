﻿using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class SubSceneManager : NetworkBehaviour
{
	private static SubSceneManager subSceneManager;

	public static SubSceneManager Instance
	{
		get
		{
			if (subSceneManager == null)
			{
				subSceneManager = FindObjectOfType<SubSceneManager>();
			}

			return subSceneManager;
		}
	}

	[SerializeField] private AwayWorldListSO awayWorldList;
	[SerializeField] private MainStationListSO mainStationList;
	[SerializeField] private AsteroidListSO asteroidList;

	readonly ScenesSyncList loadedScenesList = new ScenesSyncList();

	public bool AwaySiteLoaded { get; private set; }
	public bool MainStationLoaded { get; private set; }

	void Update()
	{
		MonitorServerSceneListOnClient();
	}

	/// <summary>
	/// General subscene loader
	/// </summary>
	/// <param name="sceneName"></param>
	/// <returns></returns>
	IEnumerator LoadSubScene(string sceneName)
	{
		AsyncOperation AO = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
		while (!AO.isDone)
		{
			yield return WaitFor.EndOfFrame;
		}

		yield return WaitFor.EndOfFrame;
		if (isServer)
		{
			NetworkServer.SpawnObjects();
		}
		else
		{
			yield return WaitFor.Seconds(1f);
			ClientScene.PrepareToSpawnSceneObjects();
			RequestObserverRefresh.Send(sceneName);
		}
	}

	public static void ProcessObserverRefreshReq(ConnectedPlayer connectedPlayer, Scene sceneContext)
	{
		if (connectedPlayer.Connection != null)
		{
			Instance.AddObserverToAllObjects(connectedPlayer.Connection, sceneContext);
		}
	}

	//FIXME TILE MANAGER NEEDS TO BE HANDLED PROPERLY!!!!!
	public void WaitForSubScene(string data, uint id)
	{
		StartCoroutine(WaitForMapToLoad(data, id));
	}
	IEnumerator WaitForMapToLoad(string data, uint managerId)
	{
		while (!NetworkIdentity.spawned.ContainsKey(managerId))
		{
			yield return WaitFor.EndOfFrame;
		}

		TileChangeManager tm = NetworkIdentity.spawned[managerId].GetComponent<TileChangeManager>();
		tm.InitServerSync(data);
	}
}

public enum SceneType
{
	MainStation,
	AwaySite,
	Asteroid
}

[System.Serializable]
public struct SceneInfo
{
	public string SceneName;
	public SceneType SceneType;
}

[System.Serializable]
public class ScenesSyncList : SyncList<SceneInfo>{}