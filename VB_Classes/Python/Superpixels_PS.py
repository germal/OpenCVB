import numpy as np
import cv2 as cv
import common
import sys
title_window = "Superpixels_PS.py"

def OpenCVCode(imgRGB, depth_colormap):
    global seeds, display_mode, num_superpixels, prior, num_levels, num_histogram_bins, frameCount, color_img
    converted_img = cv.cvtColor(imgRGB, cv.COLOR_BGR2HSV)
    height,width,channels = converted_img.shape
    num_superpixels_new = cv.getTrackbarPos('Number of Superpixels', title_window)
    num_iterations = cv.getTrackbarPos('Iterations', title_window)

    if frameCount == 0:
        color_img = np.zeros((height,width,3), np.uint8)
        color_img[:] = (0, 0, 255)

    if not seeds or num_superpixels_new != num_superpixels:
        num_superpixels = num_superpixels_new
        seeds = cv.ximgproc.createSuperpixelSEEDS(width, height, channels,
                num_superpixels, num_levels, prior, num_histogram_bins)

    seeds.iterate(converted_img, num_iterations)

    # retrieve the segmentation result
    labels = seeds.getLabels()

    # labels output: use the last x bits to determine the color
    num_label_bits = 2
    labels &= (1<<num_label_bits)-1
    labels *= 1<<(16-num_label_bits)

    mask = seeds.getLabelContourMask(False)

    # stitch foreground & background together
    mask_inv = cv.bitwise_not(mask)
    result_bg = cv.bitwise_and(imgRGB, imgRGB, mask=mask_inv)
    result_fg = cv.bitwise_and(color_img, color_img, mask=mask)
    result = cv.add(result_bg, result_fg)

    if display_mode == 0:
        cv.imshow(title_window, result)
    elif display_mode == 1:
        cv.imshow(title_window, mask)
    else:
        cv.imshow(title_window, labels)

    ch = cv.waitKey(1)
    if ch & 0xff == ord(' '):
        display_mode = (display_mode + 1) % 2
    frameCount += 1

if __name__ == '__main__':
    cv.namedWindow(title_window)
    cv.createTrackbar('Number of Superpixels', title_window, 400, 1000, common.nothing)
    cv.createTrackbar('Iterations', title_window, 4, 12, common.nothing)

    frameCount = 0
    seeds = None
    display_mode = 0
    num_superpixels = 400
    prior = 2
    num_levels = 4
    num_histogram_bins = 5
    from PyStream import PyStreamRun
    PyStreamRun(OpenCVCode, 'Superpixels_PS.py')
