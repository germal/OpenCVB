import cv2 as cv
import numpy as np
def OpenCVCode(imgRGB, depth_colormap):
    images = np.vstack((imgRGB, depth_colormap))
    cv.imshow("RGB and Depth Images", images)
    cv.waitKey(1)

from PipeStream import pipeStreamRun
pipeStreamRun(OpenCVCode)
