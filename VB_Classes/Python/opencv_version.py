import numpy as np
import cv2 as cv
import sys
import ctypes
def Mbox(title, text, style):
    return ctypes.windll.user32.MessageBoxW(0, text, title, style)

print(cv.getBuildInformation())
print("Welcome to OpenCV")
print('Done')
Mbox('OpenCVB', 'Completed', 1)
