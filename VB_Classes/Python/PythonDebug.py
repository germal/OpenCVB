import numpy as np
import cv2 as cv
import sys

print('Select 3 tracking targets')

cv.namedWindow("tracking")
tracker = cv.MultiTracker_create()
init_once = False

def OpenCVCode(image, depth_colormap):
    if not init_once:
        bbox1 = cv.selectROI('tracking', image)
        bbox2 = cv.selectROI('tracking', image)
        bbox3 = cv.selectROI('tracking', image)

        ok = tracker.add(cv.TrackerMIL_create(), image, bbox1)
        ok = tracker.add(cv.TrackerMIL_create(), image, bbox2)
        ok = tracker.add(cv.TrackerMIL_create(), image, bbox3)
        init_once = True

    ok, boxes = tracker.update(image)
    print ok, boxes

    for newbox in boxes:
        p1 = (int(newbox[0]), int(newbox[1]))
        p2 = (int(newbox[0] + newbox[2]), int(newbox[1] + newbox[3]))
        cv.rectangle(image, p1, p2, (200,0,0))

    cv.imshow('tracking', image)
    k = cv.waitKey(1)

from PyStream import PyStreamRun
PyStreamRun(OpenCVCode, 'MultiTracker_PS.py')

