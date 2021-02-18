import cv2 as cv
title_window = 'Depth_RGB_PS.py'
import numpy as np
def OpenCVCode(imgRGB, depth_colormap, frameCount):
    images = np.vstack((imgRGB, depth_colormap))
    cv.imshow("RGB and Depth Images", images)

from PyStream import PyStreamRun
PyStreamRun(OpenCVCode, title_window)
