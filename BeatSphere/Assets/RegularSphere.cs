
/** 
 * Copyright (c) 2011-2017 Fernando Holguín Weber , and Studio Libeccio - All Rights Reserved
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of the ProInputSystem 
 * and associated documentation files (the "Software"), to deal in the Software without restriction,
 * including without limitation the rights to use, copy, modify, merge, publish, distribute,
 * sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial 
 * portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
 * NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 * 
 * Fernando Holguín Weber, <contact@fernhw.com>,<http://fernhw.com>,<@fern_hw>
 * Studio Libeccio, <@studiolibeccio> <studiolibeccio.com>
 * 
 */

using UnityEngine;

public class RegularSphere:MonoBehaviour {

	#region Variables
 
    const float 
        HALF_PI = Mathf.PI * .5f,
        PI = Mathf.PI;

    public int
        ringCount = 36,
        ringDetail = 36;

    public float 
        sizeForSphere = 1;

    Mesh 
        sphereMesh;

    int[] 
        triangleCoords; //to make the faces

    float
        oldSize = 0;

    int 
        vertexPoolIndex = 0, //used in generator
	    oldRingCount,
		oldRingDetail;


	#endregion

	#region Main:

	void Start() {
        GenerateOrb();
    }

    void Update() {
        GenerateOrb();
    }

	/// <summary>
	/// Self explanatory
	/// </summary>
	void GenerateOrb(bool forceRender = false) {
        
        //Avoid redrawing mesh if there is no changes
        if (oldRingCount == ringCount && oldRingDetail == ringDetail &&
            System.Math.Abs(oldSize - sizeForSphere) < Mathf.Epsilon)
            return;

        oldRingCount = ringCount;
        oldRingDetail = ringDetail;
        oldSize = sizeForSphere;
        //You need atleast 3 faces otherwise errors
        if (ringCount < 3)
            ringCount = 3;

        if (ringDetail < 3)
            ringDetail = 3;

        //Mesh times, create mesh access filter and renderer.
        sphereMesh = new Mesh();
        MeshFilter sphereMeshFilter = GetComponent<MeshFilter>() as MeshFilter;
        MeshRenderer sphereRenderer = GetComponent<MeshRenderer>() as MeshRenderer;

        sphereMesh.vertices = GetVertexBuffer();
        sphereMesh.triangles = GetTriangleBuffer();

		// Open to create UV maps someday maybe.
		//sphereMesh.uv = GetUvBuffer();

		// Finish Mesh up.
		sphereMeshFilter.mesh = sphereMesh;
        sphereMesh.RecalculateBounds();
        sphereMesh.RecalculateNormals();
    }

    #endregion

    #region Mesh Constructor Logic:

    /// <summary>
    /// Create all the vectors in the sphere.
    /// </summary>
    Vector3[] GetVertexBuffer() {

        int vertexBufferSize = (ringCount+1)*ringDetail;
        Vector3[] vertexBuffer = new Vector3[vertexBufferSize];

        float
			// float versions for math
			ringCountF = (float)ringCount,
            ringDetailF = (float)ringDetail,
            //Coordinates
            ringY,ringX,ringZ;

        int
			//used to generate a single dimension list in nested for. 
			sphereVertexIndex = 0, 
            ri, //ringIndex
            di; //detailIndex
		
        //DRAWING:
        //Generate all the Vertices around the sphere.
		for (ri = 0; ri < ringCount+1; ri++) {  //ringcount +1 For more subdivs
            float ringPercentage = ri / (ringCountF);
            float ringYUnscaledPosition = Mathf.Cos((ringPercentage) * PI);
            float ringYUnscaledPositionY = Mathf.Sin((ringPercentage) * PI);
            float ringSize = Mathf.Abs(ringYUnscaledPositionY) * sizeForSphere;
			ringY = ringYUnscaledPosition * sizeForSphere;
            for (di = 0; di < ringDetail; di++) {
                float ringDetailPercentage = di / ringDetailF;
                float ringDetailAngle = ringDetailPercentage * PI * 2;
                ringX = Mathf.Cos(ringDetailAngle) * ringSize;
                ringZ = Mathf.Sin(ringDetailAngle) * ringSize;
                vertexBuffer[sphereVertexIndex] = new Vector3(ringX, ringY, ringZ);
                sphereVertexIndex++;
            }
        }
        return vertexBuffer;
    }

	/// <summary>
	/// Draw the triangles from the vertexes
	/// </summary>
	int[] GetTriangleBuffer() {
		// restart vertexPool this variable is class based.
		vertexPoolIndex = 0;

		// Setting array's fixed size.
		int size = ringCount*ringDetail*6;
        triangleCoords = new int[size];

        for (int r = 0; r < (ringCount); r++) {
            for (int d = 0; d < ringDetail; d++) {
                int ringDetailIndex = d;
                int ringDetailIndexPlusOneFixed = (d+1)%ringDetail;
                int ringIndex = r;
                int ringIndexPlusOneFixed = (r+1);
                int upperLeft = ringDetailIndex + (ringDetail) * ringIndex;
                int upperRight = ringDetailIndexPlusOneFixed + (ringDetail) * ringIndex;
                int lowerLeft = ringDetailIndex + (ringDetail) * ringIndexPlusOneFixed;
                int lowerRight = ringDetailIndexPlusOneFixed + (ringDetail) * ringIndexPlusOneFixed;
                AddQuadToVertex(upperLeft, upperRight, lowerLeft, lowerRight);
            }
        }
        return triangleCoords;
    }

	/**
     * upperLeft           upperRight
     *      o-----------------o
     *      |  .              |
     *      |    .            |
     *      |       .         |
     *      |          .      |
     *      |             .   |
     *      |                .|
     *      o-----------------o
     * lowerLeft           lowerRight
     * 
     * */

	void AddQuadToVertex(int upperLeft, int upperRight, int lowerLeft, int lowerRight) {
        //face 1
        AddVertexToPool(lowerRight);
        AddVertexToPool(lowerLeft);
        AddVertexToPool(upperLeft);
        //face 2
        AddVertexToPool(upperLeft);
        AddVertexToPool(upperRight);
        AddVertexToPool(lowerRight);
    }

    void AddVertexToPool(int id) {
        triangleCoords[vertexPoolIndex] = id;
        vertexPoolIndex++;
    }

	/// <summary>
	/// Uvs are not working 100% yet.
	/// </summary>
	Vector2[] GetUvBuffer() {
        int uvBufferSize = (ringCount+1)*ringDetail;

        Vector2[] uvBuffer = new Vector2[uvBufferSize];
        int sphereVertexIndex = 0;
        for (int r = 0; r < ringCount+1; r++) { //r=Rings, +1 For more subdivs
            uvBuffer[sphereVertexIndex] = new Vector2(0, 1);
            sphereVertexIndex++;
        }
        return uvBuffer;
    }
    #endregion

    #region Gizmo Logic:

    //Orb is drawn through Gizmos when using editor, but built through
    //Update, and start when in player.
#if UNITY_EDITOR
    private void OnDrawGizmos() {
        if (!Application.isPlaying) {
            GenerateOrb();
        }
    }
#endif


    //Old Gizmo script, left it here if anyone wants to use it.
    /// <summary>
    /// It creates gizmos out of the polygon logic in both a Vertex and
    /// triangle buffer, great to test a shape before going into making
    /// normals.

    void DrawGizmos(int[] vertexCollection, Vector3[] ORBvects) {
        int vertexSize = vertexCollection.Length;
        int triCount = 0;
        bool gizmoColor = false;
        for (int k = 0; k < vertexSize; k+=3) {
            gizmoColor = !gizmoColor;
            if (gizmoColor) {
                Gizmos.color = Color.red;
            }
            else {
                Gizmos.color = Color.yellow;
            }
            Gizmos.DrawLine(ORBvects[vertexCollection[k]], ORBvects[vertexCollection[k + 1]]);
            Gizmos.DrawLine(ORBvects[vertexCollection[k+1]], ORBvects[vertexCollection[k + 2]]);
            Gizmos.DrawLine(ORBvects[vertexCollection[k+2]], ORBvects[vertexCollection[k]]);
            triCount++;
        }
    }
    # endregion

}