import cv2 as cv

alpha_slider_max = 100
title_window = 'AddWeighted_Trackbar_PS.py - Linear Blend'
saveAlpha = 50
    
def on_trackbar(val):
    global saveAlpha # force the callback to reference the global variable.
    saveAlpha = val 

def OpenCVCode(imgRGB, depth_colormap, frameCount):
    alpha = saveAlpha / alpha_slider_max
    beta = ( 1.0 - alpha )
    dst1 = cv.addWeighted(imgRGB, alpha, depth_colormap, beta, 0.0)
    cv.imshow(title_window, dst1)

cv.namedWindow(title_window)
trackbar_name = 'Alpha'
cv.createTrackbar(trackbar_name, title_window , saveAlpha, alpha_slider_max, on_trackbar)
on_trackbar(saveAlpha)
from PyStream import PyStreamRun
PyStreamRun(OpenCVCode, 'AddWeighted_Trackbar_PS.py')
