# Udon Catmull-Rom Splines
A simple spline system that works with Udon.
Based on https://github.com/JPBotelho/Catmull-Rom-Splines
## Requirements
- VRCSDK 3 2021.06.03 or later (earlier versions are untested but may work)
- Udon Sharp v0.19.12 or later (earlier versions are untested but may work) https://github.com/MerlinVR/UdonSharp

## Documentation
**Editor**

Add CatmullRomSplines to any GameObject. To create a spline you need to add at least 3 transforms to the Control Point Transforms public variable. If you enable Preview Spline, the generated spline will update in real time when the Control Point Transforms variable updates or if a point is moved.
Resolution controls the amount of interpolation steps between control points.
When you finish editing the spline, push the save button in the spline inspector to serialise the data to the Udon Behaviour.
The spline data will be stored in the Positions, Tangents and Normals arrays and can be used at runtime.

**Runtime**

Since you can't use AddComponent in Udon, you will need to prepare a prefab of an empty GameObject with the CatmullRomSplines script added. To generate a spline, use either `SetupCatmullRom` or `SetupCatmullRomVector` and supply it with an array of Transforms or Vectors. This will generate the spline data. To update the spline data at runtime, use `UpdateControlPointTransforms` or `UpdateControlPointVectors` and supply new data.  `UpdateResolution` can be used to change the resolution or toggle the closed loop setting and will reprocess the spline data. When using these at runtime, be careful to not call any of these unless absolutely necessary, or reduce the resolution / control point count since Udon is quite slow.
`GetWorldSpacePosition` returns an interpolated world space position for t on the spline. It can be used to (for example) make objects follow the spline.
Takes about 0.03ms, depending on the way the spline is set up and the distance between control points.

**Examples**

SplineTester is a very simple script that uses GetWorldSpacePosition(float t) to make an object move along the spline smoothly. Add a reference to a CatmullRomSpline and set dt so that 0 < dt < 1.

If you need help or find a bug, please let me know: BocuD#8400
