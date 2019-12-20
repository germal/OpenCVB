import numpy as np
import cv2 as cv

# built-in modules
import sys
def set_scale(val):
    global hist_scale # force the callback to reference the global variable.
    hist_scale = val 

def OpenCVCode(imgRGB, depth_colormap):
    global hsv_map, hist_scale, h, s
    cv.imshow('imgRGB', imgRGB)
    small = cv.pyrDown(imgRGB)
    hsv = cv.cvtColor(small, cv.COLOR_BGR2HSV)
    dark = hsv[...,2] < 32
    hsv[dark] = 0
    h = cv.calcHist([hsv], [0, 1], None, [180, 256], [0, 180, 0, 256])

    h = np.clip(h*0.005*hist_scale, 0, 1)
    vis = hsv_map*h[:,:,np.newaxis] / 255.0
    cv.imshow('hist', vis)
    cv.waitKey(1)

hsv_map = np.zeros((180, 256, 3), np.uint8)
h, s = np.indices(hsv_map.shape[:2])
hsv_map[:,:,0] = h
hsv_map[:,:,1] = s
hsv_map[:,:,2] = 255
hsv_map = cv.cvtColor(hsv_map, cv.COLOR_HSV2BGR)
#cv.imshow('hsv_map', hsv_map)

cv.namedWindow('hist', 0)
hist_scale = 10

cv.createTrackbar('scale', 'hist', hist_scale, 32, set_scale)

from PyStream import PyStreamRun
PyStreamRun(OpenCVCode, 'PyStreamcolor_histogram.py')
