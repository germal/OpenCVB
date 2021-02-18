import numpy as np
import cv2 as cv
import sys
title_window = "MultiTracker_PS.py"
# https://docs.opencv.org/3.4/d8/d77/classcv_1_1MultiTracker.html
print('Select an object to be tracked by drawing a rectangle around it')

cv.namedWindow(title_window)
tracker = cv.MultiTracker_create()
init_once = False

def OpenCVCode(image, depth_colormap, frameCount):
    global init_once
    if not init_once:
        bbox1 = (100, 100, 100, 100) # cv.selectROI(title_window, image) # just selecting one object for now.  Others are possible.
        ok = tracker.add(cv.TrackerMIL_create(), image, bbox1)
        init_once = True

    ok, boxes = tracker.update(image)

    for newbox in boxes:
        p1 = (int(newbox[0]), int(newbox[1]))
        p2 = (int(newbox[0] + newbox[2]), int(newbox[1] + newbox[3]))
        cv.rectangle(image, p1, p2, (200,0,0))

    cv.imshow(title_window, image)

from PyStream import PyStreamRun
PyStreamRun(OpenCVCode, title_window)

