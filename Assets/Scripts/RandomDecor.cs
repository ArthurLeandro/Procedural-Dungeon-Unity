using UnityEngine;
public class RandomDecor : MonoBehaviour
{
	GameObject[] m_decorPrefabs;

	void Start()
	{
		DungeonGeneratorEvents.OnFinishGeneratingNewDungeon += GenerateDecoration;
	}

	void OnDisable()
	{
		DungeonGeneratorEvents.OnFinishGeneratingNewDungeon -= GenerateDecoration;
	}

	void Destroy()
	{
		DungeonGeneratorEvents.OnFinishGeneratingNewDungeon -= GenerateDecoration;
	}

	void GenerateDecoration()
	{
		if (m_decorPrefabs.Length > 0)
		{
			int decorationIndex = UnityEngine.Random.Range(0, m_decorPrefabs.Length);
			GameObject go = Instantiate(m_decorPrefabs[decorationIndex], transform.position, Quaternion.identity, transform) as GameObject;
			go.name = m_decorPrefabs[decorationIndex].name;
		}
	}

}