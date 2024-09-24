using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Clipper2Lib;
using EarcutNet;
using GK;
using UnityEngine;
using Unity.Mathematics;


#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(DestructableWall)), CanEditMultipleObjects]
public class UIDestructableWall : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		GUILayoutOption[] option = new GUILayoutOption[]
		{
			GUILayout.Height(20)
		};

		DestructableWall myScript = (DestructableWall)target;
		if (GUILayout.Button("Generate New Mesh", option)) myScript.Cut();


	}
}
#endif

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class DestructableWall : MonoBehaviour
{
	private MeshFilter _wallMeshFilter;
	private MeshRenderer _wallMeshRenderer;
	private Mesh _wallMesh;

	private List<Vector3> _wallVertices = new()
	{
		new Vector3(0,0,0),
		new Vector3(50,0,0),
		new Vector3(50,50,0),
		new Vector3(0,50,0)
	};

	private List<int> _wallTris = new()
	{
		0,2,3,
		0,1,2
	};

	// ! public | Must be clockwise to cut.
	[SerializeField]
	public List<Vector2> TestHole = new List<Vector2>()
	{
			new Vector2(15,15),
			new Vector2(35,15),
			new Vector2(25,35),

	};

	private PathsD _mainObject = new PathsD();





	void Awake()
	{
		_wallMeshFilter = GetComponent<MeshFilter>();
		_wallMeshRenderer = GetComponent<MeshRenderer>();


		Mesh _mesh = new Mesh();
		_mesh.vertices = _wallVertices.ToArray();
		_mesh.triangles = _wallTris.ToArray();
		_wallMeshFilter.mesh = _mesh;
	}

	// Start is called before the first frame update
	void Start()
	{
		Cut();



	}

	public void Cut()
	{
		(List<double> vertices, List<int> holeIndexes) = GetFlattenedMeshData(CutMesh(_wallVertices, TestHole));
		Mesh mesh = TriangulateMesh(vertices, holeIndexes);

		_wallVertices = mesh.vertices.ToList();
		_wallTris = mesh.triangles.ToList();

		_wallMeshFilter.mesh = mesh;
	}

	// TODO cannot cut in a hole. if any points are over a hole, it dies.
	public static PolyTreeD CutMesh(List<Vector3> vertices, List<Vector2> holeVertices, FillRule fillRule = FillRule.Positive, ClipType clipType = ClipType.Difference)
	{
		if (vertices.Count < 3) throw new Exception("Cannot work with invalid object");
		if (holeVertices.Count < 3) throw new Exception("Cannot work with invalid hole");

		// Clipping stage. this cuts the (2D) object.
		ClipperD clipper = new ClipperD();

		// add our subject which is the plane.
		// TODO to fix issue, the holes must be in a reverse order, and we must use pathsD not pathD. https://angusj.com/clipper2/Docs/Units/Clipper/Types/PathsD.htm
		PathsD subject = new PathsD();

		PathD discard = new PathD();

		// Somtthing with holeVertices use this to get the holes of the mesh.
		// We have the verticies and the hole data points, and theey are in order so we can generate a poly shape.
		// This need to use the same system as the ear cutting with doubble for the verticies and int for when holes starty.
		// We dont need anything more complext than just one layer than holes as there would be a floating mesh, this would need to be removed any way.

		// Maybe just take in a pathsD, then its fine. do the calc for the points somewhere else.

		for (int mainObjectVert = 0; mainObjectVert < vertices.Count; mainObjectVert++)
		{
			discard.Add(new PointD(vertices[mainObjectVert].x, vertices[mainObjectVert].y));
		}

		subject.Add(discard);

		clipper.AddSubject(subject);


		// create a new hole path. (points that are marked for removal)


		PathD hole = new PathD();

		for (int point = 0; point < holeVertices.Count; point++)
		{
			hole.Add(new PointD(holeVertices[point].x, holeVertices[point].y));
		}

		clipper.AddClip(hole);



		PolyTreeD Solution = new PolyTreeD();

		// Use positive.
		clipper.Execute(clipType, fillRule, Solution);

		return Solution;



	}

	private static (List<double>, List<int>) GetFlattenedMeshData(PolyTreeD Solution)
	{
		List<double> returnedVerticies = new List<double>();
		List<int> returnedHoles = new List<int>();


		int verticesCounter = 0;



		foreach (PolyPathD poly in Solution)
		{

			for (int i = poly.Polygon.Count - 1; i >= 0; i--)
			{
				returnedVerticies.Add(poly.Polygon[i].x);
				returnedVerticies.Add(poly.Polygon[i].y);
				verticesCounter++;
			}

			foreach (PolyPathD holeInPoly in poly)
			{

				returnedHoles.Add(verticesCounter);

				for (int i = 0; i < holeInPoly.Polygon.Count; i++)
				{
					returnedVerticies.Add(holeInPoly.Polygon[i].x);
					returnedVerticies.Add(holeInPoly.Polygon[i].y);
					verticesCounter++;
				}
			}
		}

		return (returnedVerticies, returnedHoles);
	}

	// data is all points including holes, hole indices are when in the list of points is a hole.
	public static Mesh TriangulateMesh(IList<double> data, IList<int> holeIndices)
	{
		List<int> triangles = new List<int>();
		List<Vector3> vertices = new List<Vector3>();
		triangles = Earcut.Tessellate(data, holeIndices);

		vertices.Clear();


		for (int i = 0; i < data.Count; i += 2)
		{
			vertices.Add(new Vector3((float)data[i], (float)data[i + 1], 0));
		}

		Mesh returnedMesh = new Mesh();

		returnedMesh.vertices = vertices.ToArray();
		returnedMesh.triangles = triangles.ToArray();

		return returnedMesh;
	}
}
