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
    temp = 'c:/temp/testLSD.txt' # os.path.abspath(str(np.random.randint(1, 1000000)) + 'ntl.txt').replace('\\', '/')

    lens = len(gray64)
    gray64 = (ctypes.c_double * lens)(*gray64)
    lsdlib.lsdGet(gray64, ctypes.c_int(rows), ctypes.c_int(cols), temp)

    fp = open(temp, 'r')
    cnt = fp.read().strip().split(' ')
    fp.close()
    os.remove(temp)
    print(gray.shape)

    count = int(cnt[0])
    dim = int(cnt[1])
    lines = np.array([float(each) for each in cnt[2:]])
    lines = lines.reshape(count, dim)

from PyStream import PyStreamRun
cv.namedWindow(title_window)
PyStreamRun(OpenCVCode, 'LineDetector_PS.py')