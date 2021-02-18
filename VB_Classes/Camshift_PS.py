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

import sys
import mmap
import array
import argparse
import numpy as np
import cv2 as cv
import os, time
from time import sleep
title_window = 'Camshift_PS.py'

class App(object):
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
        for i in range(bin_count):
            h = int(self.hist[i])
            cv.rectangle(img, (i*bin_w+2, 255), ((i+1)*bin_w-2, 255-h), (int(180.0*i/bin_count), 255, 255), -1)
        return img

    def Open(self):
        cv.namedWindow('camshift')
        cv.setMouseCallback('camshift', self.onmouse)

        self.selection = None
        self.drag_start = None
        self.show_backproj = False
        self.track_window = None
        self.initialized = False
        self.both = None
        self.vis = None
        self.img = None
        print(__doc__)
        from PyStream import PyStreamRun
        PyStreamRun(self.OpenCVCode, title_window)

    def OpenCVCode(self, vis, depth_colormap, frameCount):
        if self.initialized == False:
            self.both = np.empty((vis.shape[0], vis.shape[1]*2, 3), vis.dtype)
            self.img = vis
            self.initialized = True

        hsv = cv.cvtColor(vis, cv.COLOR_BGR2HSV)
        mask = cv.inRange(hsv, np.array((0., 60., 32.)), np.array((180., 255., 255.)))

        if self.selection:
            self.img = np.zeros(vis.shape, np.uint8)
            x0, y0, x1, y1 = self.selection
            hsv_roi = hsv[y0:y1, x0:x1]
            mask_roi = mask[y0:y1, x0:x1]
            hist = cv.calcHist( [hsv_roi], [0], mask_roi, [32], [0, 180] )
            cv.normalize(hist, hist, 0, 255, cv.NORM_MINMAX)
            self.hist = hist.reshape(-1)
            self.img = self.show_hist(self.img)

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

        graph = cv.cvtColor(self.img, cv.COLOR_HSV2BGR)
        both = cv.hconcat([vis, graph])
        cv.imshow('camshift', both)

        ch = cv.waitKey(1)
        if ch == ord('b'):
            self.show_backproj = not self.show_backproj

App().Open()

