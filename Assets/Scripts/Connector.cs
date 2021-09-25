using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Connector : MonoBehaviour
{
	public Vector2 size;
	public bool isConnected = false;

	void OnDrawGizmos()
	{
		Gizmos.color = !isConnected ? Color.red : Color.cyan;
		Vector3 halfSize = size * .5f;
		Vector3 offset = transform.position + transform.up * halfSize.y;
		Vector3 top = transform.up * size.x;
		Vector3 side = transform.right * halfSize.x;
		Vector3 topRight = transform.position + top + side;
		Vector3 topLeft = transform.position + top - side;
		Vector3 botRight = transform.position + side;
		Vector3 botLeft = transform.position - side;
		Gizmos.DrawLine(offset, offset + transform.forward);
		Gizmos.DrawLine(topRight, topLeft);
		Gizmos.DrawLine(topLeft, botLeft);
		Gizmos.DrawLine(botLeft, botRight);
		Gizmos.DrawLine(botRight, topRight);
		Gizmos.DrawLine(topRight, offset);
		Gizmos.DrawLine(topLeft, offset);
		Gizmos.DrawLine(botLeft, offset);
		Gizmos.DrawLine(botRight, offset);
	}

}
