from __future__ import print_function
import cv2 as cv
import argparse

max_value = 255
max_type = 4
max_binary_value = 255
trackbar_type = 'Type: \n 0: Binary \n 1: Binary Inverted \n 2: Truncate \n 3: To Zero \n 4: To Zero Inverted'
trackbar_value = 'Value'
title_window = 'Threshold.py'

## [Threshold_Demo]
def Threshold_Demo(val):
    #0: Binary
    #1: Binary Inverted
    #2: Threshold Truncated
    #3: Threshold to Zero
    #4: Threshold to Zero Inverted
    threshold_type = cv.getTrackbarPos(trackbar_type, title_window)
    threshold_value = cv.getTrackbarPos(trackbar_value, title_window)
    _, dst = cv.threshold(src_gray, threshold_value, max_binary_value, threshold_type )
    cv.imshow(title_window, dst)
## [Threshold_Demo]

parser = argparse.ArgumentParser(description='Code for Basic Thresholding Operations tutorial.')
parser.add_argument('--input', help='Path to input image.', default='PythonData/stuff.jpg')
args = parser.parse_args()

## [load]
# Load an image
src = cv.imread(cv.samples.findFile(args.input))
if src is None:
    print('Could not open or find the image: ', args.input)
    exit(0)
# Convert the image to Gray
src_gray = cv.cvtColor(src, cv.COLOR_BGR2GRAY)
## [load]

## [window]
# Create a window to display results
cv.namedWindow(title_window)
## [window]

## [trackbar]
# Create Trackbar to choose type of Threshold
cv.createTrackbar(trackbar_type, title_window , 3, max_type, Threshold_Demo)
# Create Trackbar to choose Threshold value
cv.createTrackbar(trackbar_value, title_window , 0, max_value, Threshold_Demo)
## [trackbar]

# Call the function to initialize
Threshold_Demo(0)
# Wait until user finishes program
cv.waitKey()
