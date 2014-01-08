﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

using Emgu.CV;
using Emgu.CV.Structure;

using HandInput.Util;
using System.Drawing;

namespace HandInput.Engine {
  public class TrackingResult {
    /// <summary>
    /// Relative postion of the right hand with respect to the center of the shoulder.
    /// X axis is rightward, Y axis is upward, Z axis is away from the camera.
    /// </summary>
    public Option<Vector3D> RelPos { get; private set; }
    public Image<Gray, Byte> DepthImage { get; private set; }
    // Can be null.
    public Image<Gray, Byte> ColorImage { get; private set; }
    public List<Rectangle> DepthBoundingBoxes { get; private set; }
    public List<Rectangle> ColorBoundingBoxes { get; private set; }

    public TrackingResult() {
      RelPos = new None<Vector3D>();
      DepthBoundingBoxes = new List<Rectangle>();
    }

    public TrackingResult(Option<Vector3D> relPos, Image<Gray, Byte> smoothedDepth,
        List<Rectangle> depthBox, Image<Gray, Byte> skin = null, 
        List<Rectangle> colorBox = null) {
      RelPos = relPos;
      DepthImage = smoothedDepth;
      DepthBoundingBoxes = depthBox;
      ColorImage = skin;

      if (colorBox == null)
        colorBox = new List<Rectangle>();
      ColorBoundingBoxes = colorBox;
    }
  }
}
