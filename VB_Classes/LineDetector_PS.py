from lsd_ctypes import *
import cv2 as cv
import numpy as np
# https://github.com/primetang/pylsd
title_window = 'LineDetector_PS.py'

def OpenCVCode(imgRGB, depth_colormap, frameCount):
    gray = cv.cvtColor(imgRGB, cv.COLOR_BGR2GRAY)
    rows, cols = gray.shape
    gray64 = np.float64(gray)
    gray64 = gray64.reshape(1, gray.shape[0] * gray.shape[1]).tolist()[0]

    lens = len(gray64)
    gray64 = (ctypes.c_double * lens)(*gray64)
    lsdlib.lsdGet(gray64, ctypes.c_int(rows), ctypes.c_int(cols))

    fp = open("c:/temp/ntuples.txt", 'r')
    cnt = fp.read().strip().split(' ')
    fp.close()
    os.remove("c:/temp/ntuples.txt")

    count = int(cnt[0])
    dim = int(cnt[1])
    lines = np.array([float(each) for each in cnt[2:]])
    lines = lines.reshape(count, dim)
    for i in range(lines.shape[0]):
        pt1 = (int(lines[i, 0]), int(lines[i, 1]))
        pt2 = (int(lines[i, 2]), int(lines[i, 3]))
        width = lines[i, 4]
        cv.line(imgRGB, pt1, pt2, (0, 0, 255), int(np.ceil(width / 2)))
    cv.imshow("LineDetector_PS.py", imgRGB)

from PyStream import PyStreamRun
cv.namedWindow(title_window)
PyStreamRun(OpenCVCode, 'LineDetector_PS.py')