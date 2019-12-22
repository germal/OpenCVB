from __future__ import print_function
import cv2 as cv
import argparse
import numpy as np

## [Load image]
parser = argparse.ArgumentParser(description='Code for Histogram Equalization tutorial.')
parser.add_argument('--input', help='Path to input image.', default='PythonData/lena.jpg')
args = parser.parse_args()

src = cv.imread(cv.samples.findFile(args.input))
if src is None:
    print('Could not open or find the image:', args.input)
    exit(0)
## [Load image]

## [Convert to grayscale]
src = cv.cvtColor(src, cv.COLOR_BGR2GRAY)
## [Convert to grayscale]

## [Apply Histogram Equalization]
dst = cv.equalizeHist(src)
## [Apply Histogram Equalization]

images = np.vstack((src, dst))
cv.imshow("Original (Top) and Equalized Image (bottom)", images)

## [Wait until user exits the program]
cv.waitKey()
## [Wait until user exits the program]
