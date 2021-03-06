﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Collections;
using System.Drawing;

using Microsoft.Kinect;

using HandInput.Util;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

using Common.Logging;

using handinput;

namespace HandInput.Engine {
  /// <summary>
  /// Hand tracking based on Kinect skeleton.
  /// </summary>
  public class SimpleSkeletonHandTracker : IHandTracker {
    static readonly ILog Log = LogManager.GetCurrentClassLogger();
    static readonly int CamShiftIter = 4;
    static readonly int DefaultZDist = 1; // m
    static readonly float HandWidth = 0.095f; // m

    // Hand bounding box in detph image.
    public Rectangle HandRect { get; private set; }
    public Rectangle InitialHandRect { get; private set; }
    public Image<Gray, Byte> SmoothedDepth { get; private set; }
    public Image<Gray, Byte> HandMask { get; private set; }
    public Rectangle ShiftedRect {
      get {
        return shiftedRect.MinAreaRect();
      }
    }

    int width, height;
    CoordinateConverter mapper;
    PlayerDetector playerDetector;
    MCvConnectedComp connectedComp = new MCvConnectedComp();
    MCvBox2D shiftedRect = new MCvBox2D();
    bool useShiftedRect = true;

    public SimpleSkeletonHandTracker(int width, int height, Byte[] kinectParams,
                                     int bufferSize = 1) {
      mapper = new CoordinateConverter(kinectParams, HandInputParams.ColorImageFormat,
                                       HandInputParams.DepthImageFormat);
      Init(width, height);
    }

    public SimpleSkeletonHandTracker(int width, int height, CoordinateMapper coordMapper,
                                     int bufferSize = 1) {
      mapper = new CoordinateConverter(coordMapper, HandInputParams.ColorImageFormat,
                                       HandInputParams.DepthImageFormat);
      Init(width, height);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="depthFrame"></param>
    /// <param name="cf"></param>
    /// <param name="skeleton"></param>
    /// <returns>If skeleton is null, returns an empty result.</returns>
    public TrackingResult Update(short[] depthFrame, byte[] cf, Skeleton skeleton) {
      if (skeleton != null && depthFrame != null && cf != null) {
        float z = DefaultZDist;
        var rightHandJoint = SkeletonUtil.GetJoint(skeleton, JointType.HandRight);
        var rightHandDepthPos = mapper.MapSkeletonPointToDepthPoint(rightHandJoint.Position);
        z = rightHandJoint.Position.Z;
        // Relatively rough estimate.
        InitialHandRect = ComputeInitialRect(rightHandDepthPos, z);

        if (!InitialHandRect.IsEmpty) {
          playerDetector.UpdateMasks(depthFrame, cf, InitialHandRect, true, true);
          var depthImage = playerDetector.DepthImage;
          CvInvoke.cvSmooth(depthImage.Ptr, SmoothedDepth.Ptr, SMOOTH_TYPE.CV_MEDIAN, 5, 5, 0, 0);
          HandRect = FindBestBoundingBox(InitialHandRect);

          if (!HandRect.IsEmpty) {
            var handSkeletonPoint = SkeletonUtil.DepthToSkeleton(HandRect, SmoothedDepth.Data, 
                width, height, mapper);
            var relPos = SkeletonUtil.RelativePosToShoulder(handSkeletonPoint, skeleton);
            var angle = SkeletonUtil.PointDirection(handSkeletonPoint,
                SkeletonUtil.GetJoint(skeleton, JointType.ElbowRight).Position);
            var depthBBs = new List<Rectangle>();
            var colorBBs = new List<Rectangle>();
            depthBBs.Add(HandRect);
            colorBBs.Add(InitialHandRect);
            return new TrackingResult(new Some<Vector3D>(relPos), new Some<Vector3D>(angle), 
                SmoothedDepth, depthBBs, playerDetector.SkinImage, colorBBs);
          }
        }
      }
      return new TrackingResult();
    }

    void Init(int width, int height) {
      this.width = width;
      this.height = height;
      playerDetector = new PlayerDetector(width, height, mapper);
      SmoothedDepth = new Image<Gray, byte>(width, height);
    }

    /// <summary>
    /// Computes initial hand searching rectangle in the depth image. If the point is out side of 
    /// the frame boundary, an empty rectangle will be returned.
    /// </summary>
    /// <returns></returns>
    Rectangle ComputeInitialRect(DepthImagePoint point, float z) {
      var scaledHandWidth = DepthUtil.GetDepthImageLength(width, HandWidth, z) * 2;
      var left = Math.Max(0, (int)(point.X - scaledHandWidth / 2));
      left = Math.Min(left, width);
      var top = Math.Max(0, (int)(point.Y - scaledHandWidth / 2));
      top = Math.Min(top, height);
      // right and bottom are exclusive.
      var right = Math.Min(width, (int)(left + scaledHandWidth));
      var bottom = Math.Min(height, (int)(top + scaledHandWidth));
      var rectWidth = right - left;
      var rectHeight = bottom - top;
      if (rectWidth <= 0 || rectHeight <= 0)
        return Rectangle.Empty;
      return new Rectangle(left, top, right - left, bottom - top);
    }

    Rectangle FindBestBoundingBox(Rectangle initialRect) {
      if (!initialRect.IsEmpty) {
        CvInvoke.cvCamShift(SmoothedDepth.Ptr, initialRect, new MCvTermCriteria(CamShiftIter),
            out connectedComp, out shiftedRect);
        Rectangle rect;
        if (useShiftedRect) {
          rect = shiftedRect.MinAreaRect();
        } else {
          rect = connectedComp.rect;
        }
        if (rect.Width > 0 && rect.Height > 0)
          return rect;
      }
      return Rectangle.Empty;
    }
  }
}
