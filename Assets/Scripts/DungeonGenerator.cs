using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class DungeonGenerator : MonoBehaviour
{
	[Header("Prefabs")]
	public GameObject[] m_prefabTiles;
	public GameObject[] m_startPrefabs;
	public GameObject[] m_exitPrefabs;
	public GameObject[] m_blockedPrefabs;
	public GameObject[] m_doorPrefabs;
	[Header("Generation Tweeks")]
	[SerializeField] [Range(0, 1f)] float m_constructionDelay = 0f;
	[SerializeField] [Range(1, 1000)] int m_mainLength = 10;
	[SerializeField] [Range(0, 250)] int m_branchLength = 10;
	[SerializeField] [Range(0, 100)] int m_numbersOfBranches = 25;
	[SerializeField] [Range(0, 100)] int m_doorPercentage = 25;
	Transform m_from, m_to, m_tileRoot, m_container;
	public List<Tile> m_generatedTiles;
	public List<Connector> m_availableConnectors;
	YieldInstruction waitForSeconds;
	int attempts = 0;
	int maxAttempts = 50;
	public DungeonStates m_state = DungeonStates.INACTIVE;

	public bool m_useBoxColliders;

	void Start()
	{
		waitForSeconds = new WaitForSeconds(m_constructionDelay);
		m_generatedTiles = new List<Tile>();
		m_availableConnectors = new List<Connector>();
		GenerateDungeon();
		Setup();
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.R))
		{
			UnityEngine.SceneManagement.SceneManager.LoadScene(0);
		}
	}

	void Setup()
	{
		// DungeonGeneratorEvents.OnFinishGeneratingNewDungeon += (() => m_generatedTiles.Clear());
		// DungeonGeneratorEvents.OnFinishGeneratingNewDungeon += (() =>
		// {
		// 	for (int i = 0; i < transform.childCount; i++)
		// 	{
		// 		DestroyImmediate(transform.GetChild(0).gameObject);
		// 	}
		// });
		DungeonGeneratorEvents.OnGettingNewGenerator += GettinThis;
	}

	void Clean()
	{
		// DungeonGeneratorEvents.OnFinishGeneratingNewDungeon -= (() => m_generatedTiles.Clear());
		DungeonGeneratorEvents.OnGettingNewGenerator -= GettinThis;
	}

	IEnumerator DungeonBuild()
	{
		GameObject goContainer = new GameObject("Main Path");
		m_container = goContainer.transform;
		m_container.SetParent(transform);
		m_tileRoot = GenerateStartingTile();
		m_to = m_tileRoot;
		m_state = DungeonStates.GENERATING_MAIN;
		while (m_generatedTiles.Count < m_mainLength)
		{
			yield return new WaitForSeconds(m_constructionDelay);
			m_from = m_to;
			if (m_generatedTiles.Count != m_mainLength - 1)
			{
				m_to = CreateTile();
			}
			else
			{
				m_to = GenerateEndingTile();
			}
			ConnectTiles();
			CollisionCheck();
		}
		foreach (Connector conn in m_container.GetComponentsInChildren<Connector>())
		{
			if (!conn.isConnected)
			{
				if (!m_availableConnectors.Contains(conn))
					m_availableConnectors.Add(conn);
			}
		}
		m_state = DungeonStates.GENERATING_BRANCHES;
		for (int i = 0; i < m_numbersOfBranches - 1; i++)
		{
#if UNITY_EDITOR
			Debug.Log($"Rodando a branch ${i}");
#endif
			if (m_availableConnectors.Count > 0)
			{
				goContainer = new GameObject($"Branch { i + 1 }");
				m_container = goContainer.transform;
				m_container.SetParent(transform);
				int availableIndex = UnityEngine.Random.Range(0, m_availableConnectors.Count);
				m_tileRoot = m_availableConnectors[availableIndex].transform.parent.parent;
				m_availableConnectors.RemoveAt(availableIndex);
				m_to = m_tileRoot;
				for (int b = 0; b < m_branchLength; b++)
				{
					attempts = 0;
					yield return new WaitForSeconds(m_constructionDelay);
					m_from = m_to;
					m_to = CreateTile();
					ConnectTiles();
					CollisionCheck();
					if (attempts >= maxAttempts) break;
				}
			}
			else break;
		}
		m_state = DungeonStates.CLEANING_UP;
		CleanUpBoxColliders();
		BlockedPassages();
		SpawnDoors();
		m_state = DungeonStates.COMPLETED;
	}

	void SpawnDoors()
	{
		if (m_doorPercentage > 0)
		{
			Connector[] allConnectors = transform.GetComponentsInChildren<Connector>();
			for (int i = 0; i < allConnectors.Length; i++)
			{
				Connector connector = allConnectors[i];
				if (connector.isConnected)
				{
					int roll = UnityEngine.Random.Range(0, 100);
					if (roll <= m_doorPercentage)
					{
						Vector3 halfExtents = new Vector3(connector.size.x, 1f, connector.size.x);
						Vector3 pos = connector.transform.position;
						Vector3 offset = Vector3.up * .5f;
						Collider[] hits = Physics.OverlapBox(pos + offset, halfExtents, Quaternion.identity, LayerMask.GetMask("Door"));
						if (hits.Length == 0)
						{
							int doorIndex = UnityEngine.Random.Range(0, m_doorPrefabs.Length);
							GameObject door = Instantiate(m_doorPrefabs[doorIndex], pos, connector.transform.rotation, connector.transform) as GameObject;
							door.name = m_doorPrefabs[doorIndex].name;
						}

					}
				}

			}
		}
	}

	void BlockedPassages()
	{
		foreach (Connector connector in transform.GetComponentsInChildren<Connector>())
		{
			if (!connector.isConnected)
			{
				Vector3 pos = connector.transform.position;
				int wallIndex = UnityEngine.Random.Range(0, m_blockedPrefabs.Length);
				GameObject go = Instantiate(m_blockedPrefabs[wallIndex], pos, connector.transform.rotation, connector.transform) as GameObject;
				go.name = m_blockedPrefabs[wallIndex].name;
			}

		}
	}

	private void CollisionCheck()
	{
		BoxCollider box = m_to.GetComponent<BoxCollider>();
		if (box == null)
		{
			box = m_to.gameObject.AddComponent<BoxCollider>();
			box.isTrigger = true;
		}
		Vector3 offset = (m_to.right * box.center.x) + (m_to.up * box.center.y) + (m_to.forward * box.center.z);
		Vector3 halfExtents = box.bounds.extents;
		List<Collider> hits = Physics.OverlapBox(m_to.position + offset, halfExtents, Quaternion.identity, LayerMask.GetMask("Tile")).ToList();
		if (hits.Count > 0)
		{
			if (hits.Exists(h => h.transform != m_to && h.transform != m_from))
			{
				attempts++;
				int toIndex = m_generatedTiles.FindIndex(t => t.tile == m_to);
				if (m_generatedTiles[toIndex].connector != null)
				{
					m_generatedTiles[toIndex].connector.isConnected = false;
				}
				m_generatedTiles.RemoveAt(toIndex);
				DestroyImmediate(m_to.gameObject);
				//backtracking
				if (attempts >= maxAttempts)
				{
					int fromIndex = m_generatedTiles.FindIndex(t => t.tile == m_from);
					Tile tileFrom = m_generatedTiles[fromIndex];
					if (m_from != m_tileRoot)
					{
						if (tileFrom.connector != null)
						{
							tileFrom.connector.isConnected = true;
						}
						m_availableConnectors.RemoveAll(t => t.transform.parent.parent == m_from);
						m_generatedTiles.RemoveAt(fromIndex);
						DestroyImmediate(m_from.gameObject);
						if (tileFrom.origin != m_tileRoot)
						{
							m_from = tileFrom.origin;
						}
						else if (m_container.name.Contains("Main"))
						{
							if (tileFrom.origin != null)
							{
								m_tileRoot = tileFrom.origin;
								m_from = m_tileRoot;
							}
						}
						else if (m_availableConnectors.Count > 0)
						{
							int avIndex = UnityEngine.Random.Range(0, m_availableConnectors.Count);
							m_tileRoot = m_availableConnectors[avIndex].transform.parent.parent;
							m_availableConnectors.RemoveAt(avIndex);
							m_from = m_tileRoot;
						}
						else return;
					}
					else if (m_container.name.Contains("Main"))
					{
						if (tileFrom.origin != null)
						{
							m_tileRoot = tileFrom.origin;
							m_from = m_tileRoot;
						}
					}
					else if (m_availableConnectors.Count > 0)
					{
						int avIndex = UnityEngine.Random.Range(0, m_availableConnectors.Count);
						m_tileRoot = m_availableConnectors[avIndex].transform.parent.parent;
						m_availableConnectors.RemoveAt(avIndex);
						m_from = m_tileRoot;
					}
					else return;
				}
				//retry
				if (m_from != null)
				{
					if (m_generatedTiles.Count != m_mainLength - 1)
					{
						m_to = CreateTile();
					}
					else
					{
						m_to = GenerateEndingTile();
					}
					ConnectTiles();
					CollisionCheck();
				}
			}
			else
			{
				attempts = 0;
			}
		}
	}

	[ContextMenu("Generate Dungeon")]
	public void GenerateInEditor()
	{
		GenerateDungeon();
	}

	public void GenerateDungeon()
	{
		DungeonGeneratorEvents.CallOnGenerateNewDungeon();
		waitForSeconds = new WaitForSeconds(m_constructionDelay);
		StartCoroutine(DungeonBuild());
		// GenerateFinishingTile();
		DungeonGeneratorEvents.CallOnFinishGeneratingDungeon();
	}


	private void ConnectTiles()
	{
		Transform connectFrom = GetRandomConnector(m_from);
		if (connectFrom != null)
		{
			Transform connectTo = GetRandomConnector(m_to);
			if (connectTo != null)
			{
				if (connectTo != null && connectFrom != null)
				{
					connectTo.SetParent(connectFrom);
					m_to.SetParent(connectTo);
					connectTo.localPosition = Vector3.zero;
					connectTo.localRotation = Quaternion.identity;
					connectTo.Rotate(0, 180f, 0);
					m_to.SetParent(m_container);
					connectTo.SetParent(m_to.Find("Connectors"));
					m_generatedTiles.Last().connector = connectFrom.GetComponent<Connector>();
				}
			}
		}
	}

	private Transform GetRandomConnector(Transform p_tile)
	{
		if (p_tile != null)
		{
			List<Connector> connectors = p_tile.GetComponentsInChildren<Connector>().ToList().FindAll(c => !c.isConnected);
			if (connectors.Count > 0)
			{
				int connectorIndex = UnityEngine.Random.Range(0, connectors.Count);
				connectors[connectorIndex].isConnected = true;
				if (p_tile != m_from)
				{
					BoxCollider box = p_tile.GetComponent<BoxCollider>();
					if (box == null)
					{
						box = p_tile.gameObject.AddComponent<BoxCollider>();
						box.isTrigger = true;
					}
				}
				return connectors[connectorIndex].transform;
			}
			else
				return null;
		}
		else
			return null;
	}

	Transform CreateTile()
	{
		int index = UnityEngine.Random.Range(0, m_prefabTiles.Length);
		GameObject tile = Instantiate(m_prefabTiles[index], Vector3.zero, Quaternion.identity, m_container) as GameObject;
		tile.name = m_prefabTiles[index].name;
		Transform origin = m_generatedTiles[m_generatedTiles.FindIndex(t => t.tile == m_from)].tile;
		m_generatedTiles.Add(new Tile(tile.transform, origin));
		return tile.transform;
	}

	private Transform GenerateStartingTile()
	{
		//DestroyPreviousDungeon();
		int index = UnityEngine.Random.Range(0, m_startPrefabs.Length);
		GameObject startingTile = Instantiate(m_startPrefabs[index], Vector3.zero, Quaternion.identity, m_container) as GameObject;
		float yRotation = UnityEngine.Random.Range(0, 4) * 90f;
		startingTile.transform.Rotate(0, yRotation, 0);
		//startingTile.transform.SetParent(this.transform);
		startingTile.name = "startingTile";
		m_generatedTiles.Add(new Tile(startingTile.transform, null));
		return startingTile.transform;
	}

	private Transform GenerateEndingTile()
	{
		int index = UnityEngine.Random.Range(0, m_exitPrefabs.Length);
		GameObject startingTile = Instantiate(m_exitPrefabs[index], Vector3.zero, Quaternion.identity, m_container) as GameObject;
		startingTile.name = "Ending Tile";
		m_generatedTiles.Add(new Tile(startingTile.transform, null));
		return startingTile.transform;
	}

	private void GenerateFinishingTile()
	{
		// CleanUpBoxColliders();
	}

	private void CleanUpBoxColliders()
	{
		if (!m_useBoxColliders)
		{
			foreach (Tile tile in m_generatedTiles)
			{
				BoxCollider box = tile.tile.GetComponent<BoxCollider>();
				if (box != null) Destroy(box);

			}
		}
	}

	[ContextMenu("Destroy Previous Dungeon")]
	private void DestroyPreviousDungeon()
	{
		try
		{
			if (transform.childCount > 0)
			{
				m_generatedTiles.Clear();
				DestroyImmediate(transform.GetChild(0).gameObject);
			}
		}
		catch (Exception e)
		{
			Debug.LogError(e);
		}
	}

	private DungeonGenerator GettinThis() => this;

}

public class DungeonGeneratorEvents
{
	public static Action OnGenerateNewDungeon;
	public static Action OnFinishGeneratingNewDungeon;
	public static Func<DungeonGenerator> OnGettingNewGenerator;
	public static void CallOnGenerateNewDungeon() => OnGenerateNewDungeon?.Invoke();
	public static void CallOnFinishGeneratingDungeon() => OnFinishGeneratingNewDungeon?.Invoke();
	public static DungeonGenerator CallOnGerttingNewGenerator() => OnGettingNewGenerator?.Invoke();
}
