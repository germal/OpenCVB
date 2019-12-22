import cv2 as cv
import numpy as np
def OpenCVCode(imgRGB, depth_colormap):
    images = np.vstack((imgRGB, depth_colormap))
    cv.imshow("RGB and Depth Images", images)

from PyStream import PyStreamRun
PyStreamRun(OpenCVCode, 'PyStream_RGBDepth.py')
