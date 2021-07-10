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

**Examples**
Will come later
