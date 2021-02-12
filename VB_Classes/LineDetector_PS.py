from lsd_ctypes import *
import cv2 as cv
import numpy as np
title_window = 'LineDetector_PS.py'

def OpenCVCode(imgRGB, depth_colormap, frameCount):
    global initialized
    cv.imshow("imgRGB", imgRGB)

from PyStream import PyStreamRun
cv.namedWindow(title_window)
PyStreamRun(OpenCVCode, 'LineDetector_PS.py')