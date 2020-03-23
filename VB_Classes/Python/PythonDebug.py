import ctypes
def Mbox(title, text, style):
    return ctypes.windll.user32.MessageBoxW(0, text, title, style)

title_window = 'BareBones.py'

print("Checking the packages used by the OpenCVB Python scripts.")
warningMsg = False
try:
    import numpy
except ImportError:
    print('You need to install numpy.')
    warningMsg = True

try:
    import cv2
except ImportError:
    print('You need to install opencv-python and opencv-contrib-python.')
    warningMsg = True

try:
    import sklearn
except ImportError:
    print('You need to install scikit-learn.')
    warningMsg = True

try:
    import matplotlib.pyplot as plt
except ImportError:
    print('You need to install matplotlib.')
    warningMsg = True

try:
    from OpenGL.GL import *
except ImportError:
    print('You need to install matplotlib.')
    warningMsg = True

try:
    import pygame
except ImportError:
    print('You need to install Pygame.')
    warningMsg = True

try:
    from cv2_rolling_ball import subtract_background_rolling_ball
except ImportError:
    print('You need to install opencv-rolling-ball.')
    warningMsg = True

if warningMsg:
    Mbox('Barebones', 'Needed packages are not present.  Review console log.', 1)
else:
    Mbox('Barebones', 'Python is present and all the necessary packages appear to be present.', 1)
cv2.waitKey(10000)