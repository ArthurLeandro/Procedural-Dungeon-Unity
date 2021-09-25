using UnityEngine;

[System.Serializable]
public class Tile {
  public Transform tile;
  public Transform origin;
  public Connector connector;

  public Tile(Transform p_tile, Transform p_origin){
    tile = p_tile;
    origin = p_origin;
  }
}
