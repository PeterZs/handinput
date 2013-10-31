﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Drawing;

using handinput;
using HandInput.Util;

namespace HandInput.Engine {
  public class RecognitionEngine {

    MProcessor processor;
    bool reset = true;
    String modelFile;

    public RecognitionEngine(String modelFile) {
      this.modelFile = modelFile;
      processor = new MProcessor(Parameters.FeatureImageWidth, Parameters.FeatureImageWidth, 
                                 modelFile);
    }

    public void Update(TrackingResult result, bool visualize = false) {
      if (result.RelPos.IsSome && result.BoundingBox.IsSome) {
        var pos = result.RelPos.Value;
        var image = result.SmoothedDepth;
        image.ROI = result.BoundingBox.Value;
        processor.Update((float)pos.X, (float)pos.Y, (float)pos.Z, image.Ptr, visualize);
        image.ROI = Rectangle.Empty;
        reset = false;
      } else {
        if (!reset) {
          reset = true;
          processor.Reset();
        }
      }
    }
  }
}