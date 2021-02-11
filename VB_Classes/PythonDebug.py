import depthai as dai
import argparse
import mmap
import array
import cv2 as cv
import numpy as np
import os, time, sys
from time import sleep
import ctypes
def Mbox(title, text, style):
    return ctypes.windll.user32.MessageBoxW(0, text, title, style)

parser = argparse.ArgumentParser(description='Pass in length of MemMap region.', formatter_class=argparse.ArgumentDefaultsHelpFormatter)
parser.add_argument('--Width', type=int, default=1280, help='Image width expected by OpenCVB')
parser.add_argument('--Height', type=int, default=720, help='Image height expected by OpenCVB')
parser.add_argument('--pipeName', default='', help='The name of the input pipe for image data.')
args = parser.parse_args()

pipeName = '//./pipe/' + args.pipeName
pipeOut = open(pipeName, 'wb')

pipeline = dai.Pipeline()

left = pipeline.createMonoCamera()
left.setResolution(dai.MonoCameraProperties.SensorResolution.THE_720_P)
left.setBoardSocket(dai.CameraBoardSocket.LEFT)

right = pipeline.createMonoCamera()
right.setResolution(dai.MonoCameraProperties.SensorResolution.THE_720_P)
right.setBoardSocket(dai.CameraBoardSocket.RIGHT)

depth = pipeline.createStereoDepth()
depth.setConfidenceThreshold(200)
# Note: the rectified streams are horizontally mirrored by default
depth.setOutputRectified(True)
depth.setRectifyEdgeFillColor(0) # Black, to better see the cutout
left.out.link(depth.left)
right.out.link(depth.right)

# Define a source - color camera
cam_rgb = pipeline.createColorCamera()
cam_rgb.setPreviewSize(1280, 720)
cam_rgb.setBoardSocket(dai.CameraBoardSocket.RGB)
cam_rgb.setResolution(dai.ColorCameraProperties.SensorResolution.THE_1080_P)
cam_rgb.setInterleaved(False)

xout_depth = pipeline.createXLinkOut()
xout_depth.setStreamName("depth")
depth.disparity.link(xout_depth.input)

xout_left = pipeline.createXLinkOut()
xout_left.setStreamName("rect_left")
depth.rectifiedLeft.link(xout_left.input)

xout_right = pipeline.createXLinkOut()
xout_right.setStreamName('rect_right')
depth.rectifiedRight.link(xout_right.input)

# Create output
xout_rgb = pipeline.createXLinkOut()
xout_rgb.setStreamName("rgb")
cam_rgb.preview.link(xout_rgb.input)

device = dai.Device(pipeline)
device.startPipeline()

q_left = device.getOutputQueue(name="rect_left", maxSize=8, blocking=False)
q_right = device.getOutputQueue(name="rect_right", maxSize=8, blocking=False)
q_depth = device.getOutputQueue(name="depth", maxSize=8, blocking=False)
q_rgb = device.getOutputQueue(name="rgb", maxSize=4, blocking=True)

frame_left = None
frame_right = None
frame_manip = None
frame_depth = None
frame_rgb = None

while True:
    in_rgb = q_rgb.get()  # blocking call, will wait until a new data has arrived
    in_left = q_left.tryGet()
    in_right = q_right.tryGet()
    in_depth = q_depth.tryGet()

    if in_left is None: continue
    if in_right is None: continue 
    if in_depth is None: continue

    shape = (1, in_left.getHeight() * in_left.getWidth())
    #frame_left = in_left.getData().reshape(shape).astype(np.uint8)
    #frame_left = np.ascontiguousarray(frame_left)
    a = bytearray(in_left.getData().reshape(shape).astype(np.uint8))
    pipeOut.write(a)

    a = bytearray(in_right.getData().reshape(shape).astype(np.uint8))
    pipeOut.write(a)
    #shape = (in_right.getHeight(), in_right.getWidth())
    #frame_right = in_right.getData().reshape(shape).astype(np.uint8)
    #frame_right = np.ascontiguousarray(frame_right)

    a = bytearray(in_depth.getData().reshape(shape).astype(np.uint16))
    pipeOut.write(a)
    #frame_depth = in_depth.getData().reshape((in_depth.getHeight(), in_depth.getWidth())).astype(np.uint8)
    #frame_depth = np.ascontiguousarray(frame_depth)
    #frame_depth = cv.applyColorMap(frame_depth, cv.COLORMAP_HSV)

    #if frame_left is not None:
    #    cv.imshow("rectif_left", frame_left)

    #if frame_right is not None:
    #    cv.imshow("rectif_right", frame_right)

    #if frame_depth is not None:
    #    cv.imshow("depth", frame_depth)

    # data is originally represented as a flat 1D array, it needs to be converted into HxWxC form
    shape = (3, in_left.getHeight() * in_left.getWidth())
    a = bytearray(in_depth.getData().reshape(shape).astype(np.uint8))
    pipeOut.write(a)
    #shape = (3, in_rgb.getHeight(), in_rgb.getWidth())
    #frame_rgb = in_rgb.getData().reshape(shape).transpose(1, 2, 0).astype(np.uint8)
    #frame_rgb = np.ascontiguousarray(frame_rgb)
    # frame is transformed and ready to be shown
    #cv.imshow("rgb", frame_rgb)

    #if cv.waitKey(1) == ord('q'):
    #    break