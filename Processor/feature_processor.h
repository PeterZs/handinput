#pragma once
#include "pcheader.h"
#include "hog_descriptor.h"

namespace handinput {
  class PROCESSOR_API FeatureProcessor {
  public:
    // w: feature image patch width.
    // h: feature image patch height.
    FeatureProcessor(int w, int h);
    float* Compute(cv::Mat& image);
    cv::Mat Visualize(cv::Mat& orig_image, int zoom_factor);
    ~FeatureProcessor(void) {}

  private:
    static const int kCellSize = 4;
    static const int kNBins = 9;
    std::unique_ptr<HOGDescriptor> hog_; 
    int w_, h_;
    std::unique_ptr<cv::Mat> scaled_image_, double_image_;
    // HOG descriptor.
    std::unique_ptr<float[]> descriptor_;
  };

}