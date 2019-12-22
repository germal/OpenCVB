'''
example to show optical flow estimation using DISOpticalFlow

USAGE: dis_opt_flow.py [<video_source>]

Keys:
 1  - toggle HSV flow visualization
 2  - toggle glitch
 3  - toggle spatial propagation of flow vectors
 4  - toggle temporal propagation of flow vectors
ESC - exit
'''
import numpy as np
import cv2 as cv
import sys

def draw_flow(img, flow, step=16):
    h, w = img.shape[:2]
    y, x = np.mgrid[step/2:h:step, step/2:w:step].reshape(2,-1).astype(int)
    fx, fy = flow[y,x].T
    lines = np.vstack([x, y, x+fx, y+fy]).T.reshape(-1, 2, 2)
    lines = np.int32(lines + 0.5)
    vis = cv.cvtColor(img, cv.COLOR_GRAY2BGR)
    cv.polylines(vis, lines, 0, (0, 255, 0))
    for (x1, y1), (x2, y2) in lines:
        cv.circle(vis, (x1, y1), 1, (0, 255, 0), -1)
    return vis


def draw_hsv(flow):
    h, w = flow.shape[:2]
    fx, fy = flow[:,:,0], flow[:,:,1]
    ang = np.arctan2(fy, fx) + np.pi
    v = np.sqrt(fx*fx+fy*fy)
    hsv = np.zeros((h, w, 3), np.uint8)
    hsv[...,0] = ang*(180/np.pi/2)
    hsv[...,1] = 255
    hsv[...,2] = np.minimum(v*4, 255)
    bgr = cv.cvtColor(hsv, cv.COLOR_HSV2BGR)
    return bgr


def warp_flow(img, flow):
    h, w = flow.shape[:2]
    flow = -flow
    flow[:,:,0] += np.arange(w)
    flow[:,:,1] += np.arange(h)[:,np.newaxis]
    res = cv.remap(img, flow, None, cv.INTER_LINEAR)
    return res


def OpenCVCode(imgRGB, depth_colormap):
    global show_hsv, show_glitch, use_spatial_propagation, use_temporal_propagation, cur_glitch, prevgray, inst, flow, initialized 
    if initialized == False:
        initialized = True
        prevgray = cv.cvtColor(imgRGB, cv.COLOR_BGR2GRAY)
        cur_glitch = imgRGB.copy()
    gray = cv.cvtColor(imgRGB, cv.COLOR_BGR2GRAY)
    if flow is not None and use_temporal_propagation:
        #warp previous flow to get an initial approximation for the current flow:
        flow = inst.calc(prevgray, gray, warp_flow(flow,flow))
    else:
        flow = inst.calc(prevgray, gray, None)
    prevgray = gray

    cv.imshow('flow', draw_flow(gray, flow))
    if show_hsv:
        cv.imshow('flow HSV', draw_hsv(flow))
    if show_glitch:
        cur_glitch = warp_flow(cur_glitch, flow)
        cv.imshow('glitch', cur_glitch)

    ch = 0xFF & cv.waitKey(5)
    if ch == 27:
        exit
    if ch == ord('1'):
        show_hsv = not show_hsv
        print('HSV flow visualization is', ['off', 'on'][show_hsv])
    if ch == ord('2'):
        show_glitch = not show_glitch
        if show_glitch:
            cur_glitch = imgRGB.copy()
        print('glitch is', ['off', 'on'][show_glitch])
    if ch == ord('3'):
        use_spatial_propagation = not use_spatial_propagation
        inst.setUseSpatialPropagation(use_spatial_propagation)
        print('spatial propagation is', ['off', 'on'][use_spatial_propagation])
    if ch == ord('4'):
        use_temporal_propagation = not use_temporal_propagation
        print('temporal propagation is', ['off', 'on'][use_temporal_propagation])

if __name__ == '__main__':
    print(__doc__)
    initialized = False
    prevgray = None
    show_hsv = False
    show_glitch = False
    use_spatial_propagation = False
    use_temporal_propagation = True
    cur_glitch = None
    inst = cv.DISOpticalFlow.create(cv.DISOPTICAL_FLOW_PRESET_MEDIUM)
    inst.setUseSpatialPropagation(use_spatial_propagation)

    flow = None

from PyStream import PyStreamRun
PyStreamRun(OpenCVCode, 'PyStream_OpticalFlow.py')

