import cv2 as cv

alpha_slider_max = 100
title_window = 'Linear Blend'
saveAlpha = 0
    
## [on_trackbar]
def on_trackbar(val):
    global saveAlpha # force the callback to reference the global variable.
    saveAlpha = val 
## [on_trackbar]

cv.namedWindow(title_window)
## [create_trackbar]
trackbar_name = 'Alpha'
cv.createTrackbar(trackbar_name, title_window , 0, alpha_slider_max, on_trackbar)
on_trackbar(saveAlpha)
## [create_trackbar]

def OpenCVCode(imgRGB, depth_colormap):
    alpha = saveAlpha / alpha_slider_max
    beta = ( 1.0 - alpha )
    dst = cv.addWeighted(imgRGB, alpha, depth_colormap, beta, 0.0)
    cv.imshow(title_window, dst)

from PyStream import PyStreamRun
PyStreamRun(OpenCVCode, 'PyStreamAddWeighted_Trackbar.py')
