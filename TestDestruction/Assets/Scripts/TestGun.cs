using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TestGun : MonoBehaviour
{
	Camera cam;
	float yAxis;

	// Start is called before the first frame update
	void Start()
	{
		cam = Camera.main;

		Cursor.lockState = CursorLockMode.Locked;

	}

	// Update is called once per frame
	void Update()
	{
		Vector2 mouseMove = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

		yAxis -= mouseMove.y;

		yAxis = Mathf.Clamp(yAxis, -80, 80);

		cam.transform.Rotate(transform.up * mouseMove.x);

		cam.transform.rotation = Quaternion.Euler(yAxis, cam.transform.rotation.eulerAngles.y, 0);





		if (Input.GetKeyDown(KeyCode.Mouse0))
		{
			if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit raycastHit, 100f))
			{
				Debug.DrawRay(cam.transform.position, cam.transform.forward, Color.red, 3f);
				if (raycastHit.transform.GetComponent<DestructableWall>() == null) return;
				GameObject gameObject = raycastHit.transform.gameObject;


				int nPoints = 3;
				Vector2[] points = new Vector2[nPoints];

				Ray rayOnWall = new Ray(cam.transform.position, cam.transform.forward);


				Vector3 planeNormal = gameObject.transform.forward;
				Vector3 planeCenter = gameObject.transform.position;
				Vector3 diff = planeCenter - rayOnWall.origin;
				float denominator = Vector3.Dot(rayOnWall.direction, planeNormal);
				float t = Vector3.Dot(diff, planeNormal) / denominator;
				Vector3 hitpoint3 = rayOnWall.origin + rayOnWall.direction * t;

				float radiansInterval = Mathf.PI * 2 / nPoints;
				float randomStartRadians = UnityEngine.Random.Range(0f, Mathf.PI);

				Vector2 hitpointLocal2 = gameObject.transform.InverseTransformPoint(hitpoint3);
				for (int i = 0; i < nPoints; ++i)
				{
					points[i] = hitpointLocal2 + GetUnitOnCircle(randomStartRadians + radiansInterval * i, .2f);
				}



				raycastHit.transform.GetComponent<DestructableWall>().Cut(points.ToList());


			}
		}
	}

	private Vector2 GetUnitOnCircle(float radians, float radius)
	{
		return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
	}

}
