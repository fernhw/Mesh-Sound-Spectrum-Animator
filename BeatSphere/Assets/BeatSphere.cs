

using UnityEngine;

public class BeatSphere:MonoBehaviour {
    
	#region Variables:

	const float 
        HALF_PI = Mathf.PI * .5f,
        PI = Mathf.PI;

    const int 
        SAMPLESIZE = 1024;

    public AudioSource listenTo;

    public int
        ringCount = 100, //Subdivisions Y
        ringDetail = 100, //Subdivisions X.Z
        smoothEffect = 0; //A smooth Effect, memory intense.

    public float
        spectrumDisplay = 7480.864f, //Order of times the spectrum is displayed accross all vectors
        spectrumIntensity = 0.57f,
        sampleIntensity = 2.56f,
        PatternRotationSpeed = 1.51f,
        fromCilinderToSphere = 1,
        spectrumAnimationSmoothness = 12.03f,
        sampleAnimationSmoothness = 4.65f,
        changeSpeed = .001f,
        sizeForSphere = 4.66f;

	Mesh sphereMesh;

	Vector3[] vertices;

	float[]
	   spectrumArray,
	   sampleArray,
	   smoothedSpectrumArray,
	   smoothedSampleArray;

	int[] 
        triangleBuffer,
		trianglesFromVertexIndexes; //to make the faces

    float
        currentPatternLoation = 0,
        sampleRate;

	int
        vertexPoolIndex = 0;

    #endregion

    #region Main:

    void Start() {
        spectrumArray = new float[SAMPLESIZE];
        sampleArray = new float[SAMPLESIZE];
        smoothedSpectrumArray = new float[SAMPLESIZE];
        smoothedSampleArray = new float[SAMPLESIZE];

        //Setting the first number of the spectrum and sample date for smoothing.
        for(int i = 0; i<SAMPLESIZE; i++){
            smoothedSpectrumArray[i] = 0f;
            smoothedSampleArray[i] = 0f;
        }
    }

    void Update() {
        //Animating the SpectrumDisplay and the Scaling number.
        spectrumDisplay += Time.deltaTime*changeSpeed;
        currentPatternLoation += Time.deltaTime*PatternRotationSpeed;

        spectrumArray = new float[SAMPLESIZE];
        sampleArray = new float[SAMPLESIZE];
        listenTo.GetSpectrumData(spectrumArray, 0, FFTWindow.BlackmanHarris);
        listenTo.GetOutputData(sampleArray, 0);
        for (int i = 0; i<SAMPLESIZE; i++) {
            smoothedSpectrumArray[i] = smoothedSpectrumArray[i] + (spectrumArray[i] - smoothedSpectrumArray[i])*Time.deltaTime*spectrumAnimationSmoothness;
            smoothedSampleArray[i] = smoothedSampleArray[i] + (sampleArray[i] - smoothedSampleArray[i])*Time.deltaTime*sampleAnimationSmoothness;
        }
        GenerateOrb();
    }

    /// <summary>
    /// Self explanatory
    /// </summary>
    void GenerateOrb() {
        //Needs atleast 3 subdivisions or errors occur.
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

    #region  Mesh Constructor Logic:

    /// <summary>
    /// Create all the vectors in the sphere this is also the home of the animations.
    /// </summary>
    Vector3[] GetVertexBuffer() {

        int vertexBufferSize = (ringCount + 1) * ringDetail;
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
        for (ri = 0; ri < ringCount + 1; ri++) {  //ringcount +1 For more subdivs
            float ringPercentage = ri / (ringCountF);
            float ringYUnscaledPosition = Mathf.Cos((ringPercentage) * PI);
            float ringYUnscaledPositionY = Mathf.Sin((ringPercentage) * PI);
            float ringSize = Mathf.Abs(ringYUnscaledPositionY) * sizeForSphere;
            float ringSizeVariant = (ringSize * fromCilinderToSphere + sizeForSphere * (1 - fromCilinderToSphere));
			ringY = ringYUnscaledPosition * sizeForSphere;
            for (di = 0; di < ringDetail; di++) {
                float ringDetailPercentage = di / ringDetailF;
                float ringDetailAngle = ringDetailPercentage * PI * 2;
                ringX = Mathf.Cos(ringDetailAngle) * ringSizeVariant;
                ringZ = Mathf.Sin(ringDetailAngle) * ringSizeVariant;
                vertexBuffer[sphereVertexIndex] = new Vector3(ringX, ringY, ringZ);
                sphereVertexIndex++;
            }
        }

		// Vector creating post-processing. 
        // ANIMATIONS, AND SMOOTHING SCRIPT
        // 1) Sound spectrum based animations.
        // 2) Face Smoothing script

		if (Application.isPlaying) {
            sphereVertexIndex = 0;

            // ANIMATION:
            // Spectrum animation added on top of the current shape.

            for (ri = 0; ri < ringCount + 1; ri++) {  //ringcount +1 For more subdivs      
                for (di = 0; di < ringDetail; di++) {
                    float positionInSphere = (float)sphereVertexIndex / (float)vertexBufferSize;
                    float spectrumProjection = smoothedSpectrumArray[Mathf.FloorToInt((float)sphereVertexIndex * spectrumDisplay + currentPatternLoation) % SAMPLESIZE] * spectrumIntensity;
                    float sampleProjection = smoothedSampleArray[Mathf.FloorToInt(positionInSphere * SAMPLESIZE + currentPatternLoation) % SAMPLESIZE] * sampleIntensity;
                    vertexBuffer[sphereVertexIndex] = vertexBuffer[sphereVertexIndex] + vertexBuffer[sphereVertexIndex] * (spectrumProjection * 5) + vertexBuffer[sphereVertexIndex] * (sampleProjection * .1f);
                    sphereVertexIndex++;
                }
            }

            // SMOOTHING:
            // Smoothing algorithm, it evens faces for the whole mesh
            // Warning: it's very buggy

            for (int repeat = 0; repeat < smoothEffect; repeat++) {
                sphereVertexIndex = 0;
                for (ri = 0; ri < (ringCount); ri++) {
                    for (di = 0; di < ringDetail; di++) {
                        int ringDetailIndex = di;
                        int ringDetailIndexPlusOneFixed = (di + 1) % ringDetail;
                        int ringIndex = ri;
                        int ringIndexPlusOneFixed = (ri + 1) % ringCount;
                        int upperRight = ringDetailIndexPlusOneFixed + (ringDetail) * ringIndex;
                        int lowerLeft = ringDetailIndex + (ringDetail) * ringIndexPlusOneFixed;

                        vertexBuffer[sphereVertexIndex] = (vertexBuffer[sphereVertexIndex] + vertexBuffer[lowerLeft]) / 2;
                        vertexBuffer[sphereVertexIndex] = (vertexBuffer[sphereVertexIndex] + vertexBuffer[upperRight]) / 2;
                        sphereVertexIndex++;
                    }
                }
            }


        }
        return vertexBuffer;
    }

	/// <summary>
	/// Draw the triangles from the vertexes, so the 3D shape can happen.
	/// </summary>
	int[] GetTriangleBuffer() {
        // restart vertexPool this variable is class based.
        vertexPoolIndex = 0;

        // Setting array's fixed size.
        int size = ringCount*ringDetail*6;
        trianglesFromVertexIndexes = new int[size];

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
        return trianglesFromVertexIndexes;
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
        trianglesFromVertexIndexes[vertexPoolIndex] = id;
        vertexPoolIndex++;
    }

	/// <summary>
	/// Uvs are not working 100% yet.
	/// </summary>
	Vector2[] GetUvBuffer() {
		int uvBufferSize = (ringCount + 1) * ringDetail;

		Vector2[] uvBuffer = new Vector2[uvBufferSize];
		int sphereVertexIndex = 0;
		for (int r = 0; r < ringCount + 1; r++) { //r=Rings, +1 For more subdivs
			uvBuffer[sphereVertexIndex] = new Vector2(0, 1);
			sphereVertexIndex++;
		}
		return uvBuffer;
	}
    #endregion

    #region Gizmo Logic:
	#if UNITY_EDITOR
	    private void OnDrawGizmos() {
				if (!Application.isPlaying) {
					GenerateOrb();
				}
			}
	#endif
	#endregion
}
