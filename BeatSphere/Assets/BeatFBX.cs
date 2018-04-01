
/** 
 * Copyright (c) 2011-2017 Fernando Holguín Weber , and Studio Libeccio - All Rights Reserved
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of the Beat Sphere 
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


/// <summary>
/// Turn an FBX into a disco abomination.
/// </summary>
public class BeatFBX:MonoBehaviour {
    
	#region Variables:

	const float 
        HALF_PI = Mathf.PI * .5f,
        PI = Mathf.PI,
        SPECTRUM_MOD = 5f,
        SAMPLE_MOD = .1f;

    const int 
        SAMPLESIZE_SOUND = 1024;

    public AudioSource listenTo;
    public Mesh inputFBXMesh;

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

	float[]
	   spectrumArray,
	   sampleArray,
	   smoothedSpectrumArray,
	   smoothedSampleArray;

    float
    currentPatternLoation = 0;

    #endregion

    #region Main:

    void Start() {
        spectrumArray = new float[SAMPLESIZE_SOUND];
        sampleArray = new float[SAMPLESIZE_SOUND];
        smoothedSpectrumArray = new float[SAMPLESIZE_SOUND];
        smoothedSampleArray = new float[SAMPLESIZE_SOUND];

        //Setting the first number of the spectrum and sample date for smoothing.
        for(int i = 0; i<SAMPLESIZE_SOUND; i++){
            smoothedSpectrumArray[i] = 0f;
            smoothedSampleArray[i] = 0f;
        }
    }

    void Update() {
        //Animating the SpectrumDisplay and the Scaling number.
        SpectrumAnimation();
        GenerateSpectrumMesh();
    }

    Mesh spectrumMesh;
    void SpectrumAnimation(){
		spectrumDisplay += Time.deltaTime * changeSpeed;
		currentPatternLoation += Time.deltaTime * PatternRotationSpeed;

		spectrumArray = new float[SAMPLESIZE_SOUND];
		sampleArray = new float[SAMPLESIZE_SOUND];
		listenTo.GetSpectrumData(spectrumArray, 0, FFTWindow.BlackmanHarris);
		listenTo.GetOutputData(sampleArray, 0);

        // Smoothing out the spectrum and sample projections.
        // So we have it animated, they move too quickly.
		for (int i = 0; i < SAMPLESIZE_SOUND; i++) {
            
            // Holding objects of the arrays. in readable floats
			float spectrum = spectrumArray[i];
			float sample = sampleArray[i];
            float smoothedSpectrum = smoothedSpectrumArray[i];
			float smoothedSample = smoothedSampleArray[i];

            // Distance between them so we can decide how quickly we move to them.
			float distanceSpectrumToSmoothed = spectrum - smoothedSpectrum;
			float distanceSampleToSmoothed = sample - smoothedSample;

            // We mix the delta time qqqqqqq‹with an animation velocity
            // KEEP IN MIND. This is NOT good for dynamic framerates and it's
            // VERY UNSTABLE but since this is an excercise I did not create a 
            // framerate dynamic animation system. You can if you want to.
            // You could also even it out and cut the animation all together.
            // but what's the fun there.
			float spectrumAnimationStep = Time.deltaTime * spectrumAnimationSmoothness;
            float sampleAnimationStep = Time.deltaTime * sampleAnimationSmoothness;

            // We ADD it to the current smoothed spectrum again, this is why it's unstable.
			smoothedSpectrumArray[i] += distanceSpectrumToSmoothed * spectrumAnimationStep;
			smoothedSampleArray[i] += distanceSampleToSmoothed * sampleAnimationStep;
		}
    }

    /// <summary>
    /// Self explanatory
    /// </summary>
    void GenerateSpectrumMesh() {
        
		// Mesh times, create mesh access filter and renderer.
        spectrumMesh = new Mesh();
        MeshFilter spectrumMeshFilter = GetComponent<MeshFilter>() as MeshFilter;

		// Pass vertex Through TurnVertexBufferInSpectrumAnimation when creating
        // new mesh.
		spectrumMesh.vertices = TurnVertexBufferInSpectrumAnimation(inputFBXMesh.vertices);

        //We pass in everything else regularly.
        spectrumMesh.triangles = inputFBXMesh.triangles;
        spectrumMesh.uv = inputFBXMesh.uv;

        // Finish Mesh up.
        spectrumMeshFilter.mesh = spectrumMesh;
        spectrumMesh.RecalculateBounds();
        spectrumMesh.RecalculateNormals();
    }

    #endregion

    #region  Mesh Constructor Logic:

    /// <summary>
    /// Create all the vectors in the sphere this is also the home of the animations.
    /// </summary>
    Vector3[] TurnVertexBufferInSpectrumAnimation(Vector3[] originalBuffer) {

        int vertexBufferSize = originalBuffer.Length;

        Vector3[] vertexBuffer = originalBuffer;

        int
        //used to generate a single dimension list in nested for. 
        sphereVertexIndex = 0;//detailIndex     

		// Vector creating post-processing. 
        // ANIMATIONS, AND SMOOTHING SCRIPT
        // 1) Sound spectrum based animations.
        // 2) Face Smoothing script

		if (Application.isPlaying) {
            sphereVertexIndex = 0;

            // ANIMATION:
            // Spectrum animation added on top of the current shape.
            // Using nested for so it has the same ammount of vertexes.
            while (sphereVertexIndex < vertexBufferSize) {

                float positionInSphere = (float)sphereVertexIndex / (float)vertexBufferSize;

                // Te Area in which the spectrum affects the Vertex array

                /*** Area Definitions 
                * _________________________________________________________
                * 
                *   1024 Spectrums     ---> 1000000+ vertexes?
                *         .       .           .       .
                *     .  ...      ..      .  ...      ..
                *  ...........   ....  ...........   ....
                *.................... ................... 
                *   Pattern repeats  |
                *   itself.
                * 
                *                           .
                *                           .  ====>>>>
                *   .     .  . .  . . .     .   
                *   .     .  . .  . . . . . .. .  . .  .
                *  ..     .  . .. ... . . . .. .. . .. .
                *.........................................
                *   ==================> Extends and overlaps
                *                       revealing more pikes and
                *                       creating new shapes as it
                *                       continously overlaps.
                * 
                * _________________________________________________________
                * 
                * 
                * There are 1024 floats in the sound spectrum but there
                * are many more vertexes, in fact in the 100s of thousands 
                * if you want, Area definitions allow me to spread the
                * spectrum and scroll it to produce fantastic looking 
                * effects.
                * 
                * Patterns look cooler when [spectrumDisplay] is beyond 7300
                * the spread evens out and pretty numeric patterns emerge.
                * 
                */

                // Area definitions, spectrum and sample extended.
                float SpectrumAreaDefinition = sphereVertexIndex * spectrumDisplay;
                float SampleAreaDefinition = positionInSphere * SAMPLESIZE_SOUND;

                // We set out projection with their multipliers through
                // Scrolling system so they loop. if I reach 1026 we return to point 2.
                int indexInSpectrumArray = Mathf.FloorToInt(SpectrumAreaDefinition + currentPatternLoation) % SAMPLESIZE_SOUND;
                int indexInSampleArray = Mathf.FloorToInt(SampleAreaDefinition + currentPatternLoation) % SAMPLESIZE_SOUND;

                // We pick the sample and spectrums, and multiply them to their modifiers.
                // This makes the pikes look more intense, or less intense.
                float spectrumProjection = smoothedSpectrumArray[indexInSpectrumArray] * spectrumIntensity;
                float sampleProjection = smoothedSampleArray[indexInSampleArray] * sampleIntensity;

                // How much to add of sample and spectrum to make it look great.
                // I'm multiplying the position in the Vector3, since it's a sphere
                // it has values coming from the center, so if you multiply for let's
                // say 2, it goes twice as further from the center.
                Vector3 spectrumAdition = vertexBuffer[sphereVertexIndex] * (spectrumProjection * SPECTRUM_MOD);
                Vector3 sampleAdition = vertexBuffer[sphereVertexIndex] * (sampleProjection * SAMPLE_MOD);

                // We add them. spectrumAdition and sampleAdition go from
                // (0,0,0) to the position of the vertex multiplied.
                // they need to be added on top of the position so the
                // sphere keeps it's shape.
                vertexBuffer[sphereVertexIndex] += spectrumAdition + sampleAdition;

                //Actual linear index since I'm not using any index from the for.
                sphereVertexIndex++;
            }


        }
        return vertexBuffer;
    }

	
    #endregion

    #region Gizmo Logic:
	#if UNITY_EDITOR
	    private void OnDrawGizmos() {
				if (!Application.isPlaying) {
					GenerateSpectrumMesh();
				}
			}
	#endif
	#endregion
}
