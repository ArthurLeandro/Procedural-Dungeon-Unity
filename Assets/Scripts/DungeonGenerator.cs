using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;


[ExecuteInEditMode]
public class DungeonGenerator : MonoBehaviour
{
  public GameObject[] m_startPrefabs;
  public GameObject[] m_prefabTiles;

  Transform m_from, m_to;

  void Start(){
  }

  [ContextMenu("Generate Dungeon")]
  public void GenerateInEditor(){
    GenerateDungeon();
  }

  public void GenerateDungeon(){
    DungeonGeneratorEvents.CallOnGenerateNewDungeon();
    m_from = GenerateStartingTile();
    m_to = CreateTile();
    ConnectTiles();
    int iterations = 10;
    while(iterations > 0){
      m_from = m_to;
      m_to = CreateTile();
      ConnectTiles();
      iterations--;
    }
    GenerateFinishingTile();
    DungeonGeneratorEvents.CallOnFinishGeneratingDungeon();
  }


  private void ConnectTiles(){
    Transform connectFrom = GetRandomConnector(m_from);
    Transform connectTo = GetRandomConnector(m_to);
    if(connectTo != null && connectFrom != null){
      connectTo.SetParent(connectFrom);
      m_to.SetParent(connectTo);
      connectTo.localPosition = Vector3.zero;
      connectTo.localRotation = Quaternion.identity;
      connectTo.Rotate(0,180f,0);
      m_to.SetParent(transform);
      connectTo.SetParent(m_to.Find("Connectors"));
    }
  }

  private Transform GetRandomConnector(Transform p_tile){
    if(p_tile != null){
      List<Connector> connectors = p_tile.GetComponentsInChildren<Connector>().ToList().FindAll(c => !c.isConnected);
      if(connectors.Count > 0){
        int connectorIndex = UnityEngine.Random.Range(0, connectors.Count);
        connectors[connectorIndex].isConnected = true;
        return connectors[connectorIndex].transform;
      }else{
        return null;
      }

    }else{
      return null;
    }

  }

  Transform CreateTile(){
    int index = UnityEngine.Random.Range(0, m_prefabTiles.Length);
    GameObject tile = Instantiate(m_prefabTiles[index], Vector3.zero, Quaternion.identity) as GameObject;
    tile.name = m_prefabTiles[index].name;
    return tile.transform;
  }

  private Transform GenerateStartingTile(){
    //DestroyPreviousDungeon();
    int index = UnityEngine.Random.Range(0, m_startPrefabs.Length);
    GameObject startingTile = Instantiate(m_startPrefabs[index], Vector3.zero, Quaternion.identity) as GameObject;
    float yRotation = UnityEngine.Random.Range(0,4)*90f;
    startingTile.transform.Rotate(0,yRotation,0);
    //startingTile.transform.SetParent(this.transform);
    return startingTile.transform;
  }

  private void GenerateFinishingTile(){
  }

  [ContextMenu("Destroy Previous Dungeon")]
  private void DestroyPreviousDungeon(){
    try{
      if(transform.childCount > 0)
        DestroyImmediate(this.transform.GetChild(0).gameObject);
    }catch(Exception e){
      Debug.LogError(e);
    }
  }

}

public class DungeonGeneratorEvents{
  public static Action OnGenerateNewDungeon;
  public static Action OnFinishGeneratingNewDungeon;
  public static void CallOnGenerateNewDungeon () => OnGenerateNewDungeon?.Invoke();
  public static void CallOnFinishGeneratingDungeon () => OnFinishGeneratingNewDungeon?.Invoke();
}
