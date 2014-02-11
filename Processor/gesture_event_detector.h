#pragma once
#include "pcheader.h"

namespace handinput {
  class GestureEventDetector {
  public:
    GestureEventDetector() {};
    void Detect(const std::string& gesture, const std::string& stage, 
        json_spirit::mObject* result);
    void Reset();
  private:
    std::string prev_stage_, prev_event_, prev_gesture_;
  };
}