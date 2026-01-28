using System;
namespace CS4620IS.Components;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public enum PathSegmentBrushState
{
    One,
    Two,
    Three
}

public class PathSegmentBrush
{
    public Vector3 ClampedPointForAngle;
    public PathSegment ClampedSegment;
    private PathSegment P1ClampedSegment;
    private Vector3 p3ClampedPoint;
    private PathSegmentBrushState state = PathSegmentBrushState.One;
    private int? StateOneSnappedSegment = null;
    private int? StateOneSnappedPoint = null;
    private int? StateThreeSnappedSegment = null;
    private int? StateThreeSnappedPoint = null;
    private int P1ClampedSegmentPathIndex;
}