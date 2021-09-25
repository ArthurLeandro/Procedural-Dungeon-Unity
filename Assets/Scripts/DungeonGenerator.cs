using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;


[ExecuteInEditMode]
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
	[SerializeField] [Range(1, 100)] int m_mainLength = 10;
	[SerializeField] [Range(0, 50)] int m_branchLength = 10;
	[SerializeField] [Range(0, 100)] int m_numbersOfBranches = 25;
	[SerializeField] [Range(0, 100)] int m_doorPercentage = 25;
	Transform m_from, m_to, m_tileRoot, m_container;
	public List<Tile> m_generatedTiles = new List<Tile>();
	List<Connector> m_availableConnectors = new List<Connector>();
	YieldInstruction waitForSeconds;

	void Start()
	{
		GenerateDungeon();
	}

	IEnumerator DungeonBuild()
	{
		GameObject goContainer = new GameObject("Main Path");
		m_container = goContainer.transform;
		m_container.SetParent(transform);
		m_tileRoot = m_to = GenerateStartingTile();
		for (int i = 0; i < m_mainLength; i++)
		{
			yield return waitForSeconds;
			m_from = m_to;
			m_to = CreateTile();
			ConnectTiles();
			m_mainLength--;
		}
		foreach (Connector conn in m_container.GetComponentsInChildren<Connector>())
		{
			if (!conn.isConnected)
			{
				if (!m_availableConnectors.Contains(conn))
					m_availableConnectors.Add(conn);
			}
		}
		for (int i = 0; i < m_numbersOfBranches; i++)
		{
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
					yield return waitForSeconds;
					m_from = m_to;
					m_to = CreateTile();
					ConnectTiles();
				}
			}
			else break;
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
		GenerateFinishingTile();
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
		m_generatedTiles.Add(new Tile(startingTile.transform, null));
		return startingTile.transform;
	}

	private void GenerateFinishingTile()
	{
	}

	[ContextMenu("Destroy Previous Dungeon")]
	private void DestroyPreviousDungeon()
	{
		try
		{
			if (transform.childCount > 0)
				DestroyImmediate(this.transform.GetChild(0).gameObject);
		}
		catch (Exception e)
		{
			Debug.LogError(e);
		}
	}

}

public class DungeonGeneratorEvents
{
	public static Action OnGenerateNewDungeon;
	public static Action OnFinishGeneratingNewDungeon;
	public static void CallOnGenerateNewDungeon() => OnGenerateNewDungeon?.Invoke();
	public static void CallOnFinishGeneratingDungeon() => OnFinishGeneratingNewDungeon?.Invoke();
}
