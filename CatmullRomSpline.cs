using UnityEditor;
using System;
using System.Diagnostics;
using UdonSharp;
using UdonSharpEditor;
using UnityEditorInternal;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[ExecuteInEditMode]
public class CatmullRomSpline : UdonSharpBehaviour
{
    [HideInInspector] public int resolution = 2; //Amount of points between control points. [Tesselation factor]
    [HideInInspector] public bool closedLoop;
    [HideInInspector] public bool previewSpline;
    
    [HideInInspector] public bool drawNormals;
    [HideInInspector] public bool drawTangents;
    [HideInInspector] public float normalExtrusion;
    [HideInInspector] public float tangentExtrusion;

    [HideInInspector] public bool save;
    
    public Transform[] controlPointTransforms;
    
    //These contain all the point data
    public Vector3[] controlPoints;
    public Vector3[] positions;
    public Vector3[] tangents;
    public Vector3[] normals;
    public float splineLength;
    public float[] segmentLength; //the length of an individual segment, array size will be positions.length - 1
    public float[] segmentTotalLength; //the length from position 0 to that segment
    /// <summary>
    /// Setup a spline based on a Transform array
    /// </summary>
    /// <param name="targetControlPoints">Control point transform array</param>
    /// <param name="targetResolution">Target resolution</param>
    /// <param name="targetClosedLoop">Close the spline</param>
    public void SetupCatmullRom(Transform[] targetControlPoints, int targetResolution, bool targetClosedLoop)
    {
        if (targetControlPoints == null || targetControlPoints.Length <= 2 || targetResolution < 2)
        {
            UnityEngine.Debug.LogError("Catmull Rom Error: Too few control points or resolution too small");
        }

        controlPoints = new Vector3[targetControlPoints.Length];
        for (int i = 0; i < targetControlPoints.Length; i++)
        {
            this.controlPoints[i] = targetControlPoints[i].position;
        }

        resolution = targetResolution;
        closedLoop = targetClosedLoop;

        GenerateSplinePoints();
    }
    
    /// <summary>
    /// Setup a spline based on a Vector3 array
    /// </summary>
    /// <param name="targetControlPoints">Control point vector3 array</param>
    /// <param name="targetResolution">Target resolution</param>
    /// <param name="targetClosedLoop">Close the spline</param>
    public void SetupCatmullRomVector(Vector3[] targetControlPoints, int targetResolution, bool targetClosedLoop)
    {
        if (targetControlPoints == null || targetControlPoints.Length <= 2 || targetResolution < 2)
        {
            UnityEngine.Debug.LogError("Catmull Rom Error: Too few control points or resolution too small");
        }

        controlPoints = targetControlPoints;

        resolution = targetResolution;
        closedLoop = targetClosedLoop;

        GenerateSplinePoints();
    }
    
    //Sets the length of the point array based on resolution/closed loop
    private void InitializeProperties()
    {
        int pointsToCreate;
        if (closedLoop)
        {
            pointsToCreate =
                resolution *
                controlPoints.Length; //Loops back to the beggining, so no need to adjust for arrays starting at 0
        }
        else
        {
            pointsToCreate = resolution * (controlPoints.Length - 1);
        }

        positions = new Vector3[pointsToCreate];
        tangents = new Vector3[pointsToCreate];
        normals = new Vector3[pointsToCreate];
    }

    //Math stuff to generate the spline points
    private void GenerateSplinePoints()
    {
        InitializeProperties();

        Vector3 p0, p1; //Start point, end point
        Vector3 m0, m1; //Tangents

        // First for loop goes through each individual control point and connects it to the next, so 0-1, 1-2, 2-3 and so on
        int closedAdjustment = closedLoop ? 0 : 1;
        for (int currentPoint = 0; currentPoint < controlPoints.Length - closedAdjustment; currentPoint++)
        {
            bool closedLoopFinalPoint = (closedLoop && currentPoint == controlPoints.Length - 1);

            p0 = controlPoints[currentPoint];

            if (closedLoopFinalPoint)
            {
                p1 = controlPoints[0];
            }
            else
            {
                p1 = controlPoints[currentPoint + 1];
            }

            // m0
            if (currentPoint == 0) // Tangent M[k] = (P[k+1] - P[k-1]) / 2
            {
                if (closedLoop)
                {
                    m0 = p1 - controlPoints[controlPoints.Length - 1];
                }
                else
                {
                    m0 = p1 - p0;
                }
            }
            else
            {
                m0 = p1 - controlPoints[currentPoint - 1];
            }

            // m1
            if (closedLoop)
            {
                if (currentPoint == controlPoints.Length - 1) //Last point case
                {
                    m1 = controlPoints[(currentPoint + 2) % controlPoints.Length] - p0;
                }
                else if (currentPoint == 0) //First point case
                {
                    m1 = controlPoints[currentPoint + 2] - p0;
                }
                else
                {
                    m1 = controlPoints[(currentPoint + 2) % controlPoints.Length] - p0;
                }
            }
            else
            {
                if (currentPoint < controlPoints.Length - 2)
                {
                    m1 = controlPoints[(currentPoint + 2) % controlPoints.Length] - p0;
                }
                else
                {
                    m1 = p1 - p0;
                }
            }

            m0 *= 0.5f; //Doing this here instead of  in every single above statement
            m1 *= 0.5f;

            float pointStep = 1.0f / resolution;

            if ((currentPoint == controlPoints.Length - 2 && !closedLoop) || closedLoopFinalPoint) //Final point
            {
                pointStep = 1.0f / (resolution - 1); // last point of last segment should reach p1
            }

            // Creates [resolution] points between this control point and the next
            for (int tesselatedPoint = 0; tesselatedPoint < resolution; tesselatedPoint++)
            {
                float t = tesselatedPoint * pointStep;
                
                Vector3 pointPosition = CalculatePosition(p0, p1, m0, m1, t);
                Vector3 pointTangent = CalculateTangent(p0, p1, m0, m1, t);
                Vector3 pointNormal = NormalFromTangent(pointTangent);

                positions[currentPoint * resolution + tesselatedPoint] = pointPosition;
                tangents[currentPoint * resolution + tesselatedPoint] = pointTangent;
                normals[currentPoint * resolution + tesselatedPoint] = pointNormal;
            }
        }
        
        //calculate total spline length
        splineLength = 0;
        segmentLength = new float[positions.Length - (closedLoop ? 0 : 1)];
        segmentTotalLength = new float[positions.Length];
        segmentTotalLength[0] = 0;
        
        for (int i = 1; i < positions.Length; i++)
        {
            segmentLength[i-1] = Vector3.Distance(positions[i-1], positions[i]);
            splineLength += segmentLength[i - 1];
            segmentTotalLength[i] = splineLength;
        }

        if (closedLoop)
        {
            segmentLength[positions.Length-1] = Vector3.Distance(positions[positions.Length-1], positions[0]);
            splineLength += segmentLength[positions.Length-1];
        }
        
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        if (save)
        {
            this.ApplyProxyModifications();
            save = false;
        }
#endif
    }
    
    public int lastWorldSpaceSegmentIndex = 0;

    public Vector3 GetWorldSpacePosition(float t)
    {
        if (t <= 0) return positions[0];
        if (t >= 1) return positions[positions.Length-1];
        
        //find position nearest to t
        float targetLength = t * splineLength;
        
        //guess based on targetLength
        //we know how many segments there are, so we can get a pretty close guess to the target from t

        int startSegment = (int) (t * segmentLength.Length);;
        int endSegment = 0;

        //if the segmentTotalLength for the guess is lower than our target, start iterating, otherwise, start iterating backwards
        if (segmentTotalLength[startSegment] < targetLength)
        {
            for (; startSegment < segmentLength.Length; startSegment++)
            {
                if (segmentTotalLength[startSegment] > targetLength)
                {
                    startSegment--;
                    endSegment = startSegment + 1;
                    break;
                }
            }
        }
        else
        {
            for (; startSegment > 0; startSegment--)
            {
                if (segmentTotalLength[startSegment] <= targetLength)
                {
                    endSegment = startSegment + 1;
                    break;
                }
            }
        }
        
        //loops.. are weird as fuck *sigh*
        if (startSegment == -1)
        {
            startSegment = 0;
            endSegment = 1;
        }

        //loops.. are weird as fuck *sigh*
        if (startSegment == segmentLength.Length - 1)
            endSegment = 0;

        lastWorldSpaceSegmentIndex = startSegment;
        
        t = targetLength - segmentTotalLength[startSegment];
        t /= segmentLength[startSegment];
        
        return Vector3.Lerp(positions[startSegment], positions[endSegment], t);
    }
    
    #region CALCULATE POINTS
    //Calculates curve position at t[0, 1]
    public Vector3 CalculatePosition(Vector3 start, Vector3 end, Vector3 tanPoint1, Vector3 tanPoint2, float t)
    {
        // Hermite curve formula:
        // (2t^3 - 3t^2 + 1) * p0 + (t^3 - 2t^2 + t) * m0 + (-2t^3 + 3t^2) * p1 + (t^3 - t^2) * m1
        Vector3 position = (2.0f * t * t * t - 3.0f * t * t + 1.0f) * start
                           + (t * t * t - 2.0f * t * t + t) * tanPoint1
                           + (-2.0f * t * t * t + 3.0f * t * t) * end
                           + (t * t * t - t * t) * tanPoint2;

        return position;
    }

    //Calculates tangent at t[0, 1]
    public Vector3 CalculateTangent(Vector3 start, Vector3 end, Vector3 tanPoint1, Vector3 tanPoint2, float t)
    {
        // Calculate tangents
        // p'(t) = (6t² - 6t)p0 + (3t² - 4t + 1)m0 + (-6t² + 6t)p1 + (3t² - 2t)m1
        Vector3 tangent = (6 * t * t - 6 * t) * start
                          + (3 * t * t - 4 * t + 1) * tanPoint1
                          + (-6 * t * t + 6 * t) * end
                          + (3 * t * t - 2 * t) * tanPoint2;

        return tangent.normalized;
    }

    //Calculates normal vector from tangent
    public Vector3 NormalFromTangent(Vector3 tangent)
    {
        return Vector3.Cross(tangent, Vector3.up).normalized / 2;
    }
    #endregion
    #region UPDATE SPLINE
    /// <summary>
    /// Updates spline control points with a new Transform array
    /// </summary>
    /// <param name="newControlPoints">New transform array</param>
    public void UpdateControlPointTransforms(Transform[] newControlPoints)
    {
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        this.UpdateProxy();
#endif
        
        if (newControlPoints == null || newControlPoints.Length <= 0)
        {
            UnityEngine.Debug.LogError("Invalid control points");
        }

        controlPoints = new Vector3[newControlPoints.Length];
        for (int i = 0; i < newControlPoints.Length; i++)
        {
            controlPoints[i] = newControlPoints[i].position;
        }

        GenerateSplinePoints();
    }
    /// <summary>
    /// Updates spline control points with the passed Vector3 array
    /// </summary>
    /// <param name="newControlPoints">New vector3 array</param>
    public void UpdateControlPointVectors(Vector3[] newControlPoints)
    {
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        this.UpdateProxy();
#endif
        
        if (newControlPoints == null || newControlPoints.Length <= 0)
        {
            UnityEngine.Debug.LogError("Invalid control points");
        }

        controlPoints = newControlPoints;

        GenerateSplinePoints();
    }

    /// <summary>
    /// Update the spline resolution or loop settings
    /// </summary>
    /// <param name="newResolution">New target resolution</param>
    /// <param name="newClosedLoop">New closed loop state</param>
    public void UpdateResolution(int newResolution, bool newClosedLoop)
    {
        if (newResolution < 2)
        {
            UnityEngine.Debug.LogError($"Invalid Resolution: {newResolution}. Make sure it's >= 1");
        }

        resolution = newResolution;
        closedLoop = newClosedLoop;

        GenerateSplinePoints();
    }
    #endregion
    #region DRAWING
    //Draws a line between every point and the next.
    public void DrawSpline(Color color)
    {
        for (int i = 0; i < positions.Length; i++)
        {
            if (i == positions.Length - 1 && closedLoop)
            {
                UnityEngine.Debug.DrawLine(positions[i], positions[0], color);
            }
            else if (i < positions.Length - 1)
            {
                UnityEngine.Debug.DrawLine(positions[i], positions[i + 1], color);
            }
        }
    }

    public void DrawNormals(float extrusion, Color color)
    {
        for (int i = 0; i < normals.Length; i++)
        {
            UnityEngine.Debug.DrawLine(positions[i], positions[i] + normals[i] * extrusion, color);
        }
    }

    public void DrawTangents(float extrusion, Color color)
    {
        for (int i = 0; i < tangents.Length; i++)
        {
            UnityEngine.Debug.DrawLine(positions[i], positions[i] + tangents[i] * extrusion, color);
        }
    }
    
#if !COMPILER_UDONSHARP && UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!previewSpline) return;
        
        UpdateControlPointTransforms(controlPointTransforms);
        UpdateResolution(resolution, closedLoop);
        
        DrawSpline(Color.white);
                
        if (drawNormals)
            DrawNormals(normalExtrusion, Color.red);

        if (drawTangents)
            DrawTangents(tangentExtrusion, Color.cyan);
    }
#endif
    #endregion
}

#if !COMPILER_UDONSHARP && UNITY_EDITOR
[CustomEditor(typeof(CatmullRomSpline))]
public class CatmullRomSplineInspector : Editor
{
    public override void OnInspectorGUI()
    {
        // Draws the default convert to UdonBehaviour button, program asset field, sync settings, etc.
        if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

        CatmullRomSpline catmullRomSpline = (CatmullRomSpline)target;
        catmullRomSpline.UpdateProxy();

        EditorGUI.BeginChangeCheck();
        
        EditorGUILayout.LabelField("", EditorStyles.label);
        EditorGUILayout.LabelField("Spline Options", EditorStyles.boldLabel);
        
        bool closedLoop = EditorGUILayout.Toggle("Closed loop", catmullRomSpline.closedLoop);
        int resolution = EditorGUILayout.IntSlider("Resolution", catmullRomSpline.resolution, 2, 100);
        
        bool drawNormals = catmullRomSpline.drawNormals;
        bool drawTangents = catmullRomSpline.drawTangents;
        
        float normalExtrusion = catmullRomSpline.normalExtrusion; 
        float tangentExtrusion = catmullRomSpline.tangentExtrusion;

        //EditorGUILayout.LabelField("", EditorStyles.label);
        EditorGUILayout.LabelField("", EditorStyles.label);
        EditorGUILayout.LabelField("Preview Options", EditorStyles.boldLabel);
        bool previewSpline = EditorGUILayout.Toggle("Preview spline", catmullRomSpline.previewSpline);

        if (previewSpline)
        {
            EditorGUILayout.LabelField("If changing these settings doesn't immediatly update the preview, try alt-tab", EditorStyles.helpBox);

            drawNormals = EditorGUILayout.Toggle("Draw normals", drawNormals);
            if (drawNormals)
                normalExtrusion = EditorGUILayout.Slider("Normal length", normalExtrusion, 0, 3);

            drawTangents = EditorGUILayout.Toggle("Draw tangents", drawTangents);
            if (drawTangents)
                tangentExtrusion = EditorGUILayout.Slider("Tangent length", tangentExtrusion, 0, 3);
        }

        bool save = GUILayout.Button("Save Spline Changes");
        
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(catmullRomSpline, "Modify CatmullRomSpline values");
            
            catmullRomSpline.closedLoop = closedLoop;
            catmullRomSpline.resolution = resolution;

            catmullRomSpline.previewSpline = previewSpline;
            
            catmullRomSpline.drawNormals = drawNormals;
            catmullRomSpline.drawTangents = drawTangents;
            
            catmullRomSpline.normalExtrusion = normalExtrusion;
            catmullRomSpline.tangentExtrusion = tangentExtrusion;

            catmullRomSpline.save = save;

            // else
            // {
            //     catmullRomSpline.SetupCatmullRom(controlPoints, resolution, closedLoop);
            // }

            catmullRomSpline.ApplyProxyModifications();
        }
        
        EditorGUILayout.LabelField("", EditorStyles.label);
        EditorGUILayout.LabelField("Spline Data", EditorStyles.boldLabel);
        base.OnInspectorGUI();
    }
}
#endif