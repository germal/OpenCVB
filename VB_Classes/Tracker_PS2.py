import cv2 as cv
import sys
from PyStream2 import PyStreamRun
from PyStream2 import getDrawRect

title_window = 'Tracker_PS2.py'

# Set up tracker.
# Instead of MIL, you can also use
tracker_types = ['BOOSTING', 'MIL','KCF', 'TLD', 'MEDIANFLOW', 'GOTURN', 'MOSSE', 'CSRT']
tracker_type = tracker_types[2]
if tracker_type == 'BOOSTING':
    tracker = cv.TrackerBoosting_create()
if tracker_type == 'MIL':
    tracker = cv.TrackerMIL_create()
if tracker_type == 'KCF':
    tracker = cv.TrackerKCF_create()
if tracker_type == 'TLD':
    tracker = cv.TrackerTLD_create()
if tracker_type == 'MEDIANFLOW':
    tracker = cv.TrackerMedianFlow_create()
if tracker_type == 'GOTURN':
    tracker = cv.TrackerGOTURN_create()
if tracker_type == 'MOSSE':
    tracker = cv.TrackerMOSSE_create()
if tracker_type == "CSRT":
    tracker = cv.TrackerCSRT_create()    
trackerInitialized = False


def OpenCVCode(imgRGB, frameCount):
    global trackerInitialized
    rect = getDrawRect()

    # when the width of the drawRect is nonzero, then there is something to track
    if rect[3] != 0:
        if trackerInitialized == False:
            trackerInitialized = True
            # Initialize tracker with first imgRGB and bounding box
            ok = tracker.init(imgRGB, rect)

        # Update tracker
        ok, rect = tracker.update(imgRGB)
 
        # Draw bounding box
        if ok:
            # Tracking success
            p1 = (int(rect[0]), int(rect[1]))
            p2 = (int(rect[0] + rect[2]), int(rect[1] + rect[3]))
            cv.rectangle(imgRGB, p1, p2, (255,0,0), 2, 1)
            # Display tracker type on imgRGB
            cv.putText(imgRGB, tracker_type + " Tracker", (40,100), cv.FONT_HERSHEY_SIMPLEX, 0.75, (50,170,50),2)
        else :
            # Tracking failure
            cv.putText(imgRGB, "Tracking failure detected", (100,80), cv.FONT_HERSHEY_SIMPLEX, 0.75,(0,0,255),2)

PyStreamRun(OpenCVCode, title_window)