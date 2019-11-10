#!/usr/bin/env python

'''
Camshift tracker
================

This is a demo that shows mean-shift based tracking
You select a color objects such as your face and it tracks it.
This reads the pipe connected to OpenCVB

http://www.robinhewitt.com/research/track/camshift.html

Usage:
------
    camshift.py 

    To initialize tracking, select the object with mouse

Keys:
-----
    ESC   - exit
    b     - toggle back-projected probability visualization
'''

# Python 2/3 compatibility
from __future__ import print_function
import sys
PY3 = sys.version_info[0] == 3

if PY3:
    xrange = range

import mmap
import array
import argparse
import numpy as np
import cv2 as cv
import os, time
import psutil
from time import sleep
import ctypes
def Mbox(title, text, style):
    return ctypes.windll.user32.MessageBoxW(0, text, title, style)

class App(object):
    def __init__(self, video_src):
        cv.namedWindow('camshift')
        cv.setMouseCallback('camshift', self.onmouse)

        self.selection = None
        self.drag_start = None
        self.show_backproj = False
        self.track_window = None

    def onmouse(self, event, x, y, flags, param):
        if event == cv.EVENT_LBUTTONDOWN:
            self.drag_start = (x, y)
            self.track_window = None
        if self.drag_start:
            xmin = min(x, self.drag_start[0])
            ymin = min(y, self.drag_start[1])
            xmax = max(x, self.drag_start[0])
            ymax = max(y, self.drag_start[1])
            self.selection = (xmin, ymin, xmax, ymax)
        if event == cv.EVENT_LBUTTONUP:
            self.drag_start = None
            self.track_window = (xmin, ymin, xmax - xmin, ymax - ymin)

    def show_hist(self, img):
        bin_count = self.hist.shape[0]
        bin_w = int(img.shape[1] / bin_count)
        for i in xrange(bin_count):
            h = int(self.hist[i])
            cv.rectangle(img, (i*bin_w+2, 255), ((i+1)*bin_w-2, 255-h), (int(180.0*i/bin_count), 255, 255), -1)
        return img

    def run(self):
        frameCount = -1
        while True:
            mm.seek(0)
            arrayDoubles = array.array('d', mm.read(MemMapLength))
            rgbBufferSize = int(arrayDoubles[1])
            rows = int(arrayDoubles[2])
            cols = int(arrayDoubles[3])

            if rows > 0:
                if arrayDoubles[0] == frameCount:
                    sleep(0.001)
                else:
                    frameCount = arrayDoubles[0] 
                    rgb = pipeIn.read(int(rgbBufferSize))
                    rgbSize = rows, cols, 3
                    vis = np.array(np.frombuffer(rgb, np.uint8).reshape(rgbSize))
                    hsv = cv.cvtColor(vis, cv.COLOR_BGR2HSV)
                    if frameCount == 0:
                        img = np.zeros(vis.shape, np.uint8)
                        both = np.empty((vis.shape[0], vis.shape[1]*2, 3), vis.dtype)
                    mask = cv.inRange(hsv, np.array((0., 60., 32.)), np.array((180., 255., 255.)))

                if self.selection:
                    img = np.zeros(vis.shape, np.uint8)
                    x0, y0, x1, y1 = self.selection
                    hsv_roi = hsv[y0:y1, x0:x1]
                    mask_roi = mask[y0:y1, x0:x1]
                    hist = cv.calcHist( [hsv_roi], [0], mask_roi, [32], [0, 180] )
                    cv.normalize(hist, hist, 0, 255, cv.NORM_MINMAX)
                    self.hist = hist.reshape(-1)
                    img = self.show_hist(img)

                    vis_roi = vis[y0:y1, x0:x1]
                    cv.bitwise_not(vis_roi, vis_roi)
                    vis[mask == 0] = 0

                if self.track_window and self.track_window[2] > 0 and self.track_window[3] > 0:
                    self.selection = None
                    prob = cv.calcBackProject([hsv], [0], self.hist, [0, 180], 1)
                    prob &= mask
                    term_crit = ( cv.TERM_CRITERIA_EPS | cv.TERM_CRITERIA_COUNT, 10, 1 )
                    track_box, self.track_window = cv.CamShift(prob, self.track_window, term_crit)

                    if self.show_backproj:
                        vis[:] = prob[...,np.newaxis]
                    try:
                        cv.ellipse(vis, track_box, (0, 0, 255), 2)
                    except:
                        print(track_box)

                graph = cv.cvtColor(img, cv.COLOR_HSV2BGR)
                both = cv.hconcat([vis, graph])
                cv.imshow("camshift", both)

                ch = cv.waitKey(1)
                if ch == 27:
                    break
                if ch == ord('b'):
                    self.show_backproj = not self.show_backproj

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description='Pass in length of MemMap region.', formatter_class=argparse.ArgumentDefaultsHelpFormatter)
    parser.add_argument('--MemMapLength', type=int, default=0, help='The number of bytes are in the memory mapped file.')
    parser.add_argument('--pipeName', default='', help='The name of the input pipe for image data.')
    args = parser.parse_args()

    pid = 0 # pid of any spawned task
    MemMapLength = args.MemMapLength
    if MemMapLength == 0:
        MemMapLength = 400 # these values have been generously padded (on both sides) but if they grow...
        args.pipeName = 'OpenCVBImages0' # we always start with 0 and since it is only invoked once, 0 is all it will ever be.
        ocvb = os.getcwd() + '\\..\\..\\bin\Debug\OpenCVB.exe' # 
        if os.path.exists(ocvb):
            pid = os.spawnv(os.P_NOWAIT, ocvb, 'Camshift_Python')
    
    pipeName = '\\\\.\\pipe\\' + args.pipeName 
    while True:
        try:
            pipeIn = open(pipeName, 'rb')
            break
        except Exception as exception:
            time.sleep(0.1) # sleep for a bit to wait for OpenCVB to start...
            
    print(args.pipeName)
    mm = mmap.mmap(0, MemMapLength, tagname='Python_MemMap')
    frameCount = -1

    print(__doc__)
    import sys
    try:
        video_src = sys.argv[1]
    except:
        video_src = 0
    App(video_src).run()
    if args.MemMapLength == 0:
        for proc in psutil.process_iter():
            # check whether the process name matches
            if proc.name() == "OpenCVB.exe":
                proc.kill()