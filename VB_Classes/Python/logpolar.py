#!/usr/bin/env python

'''
plots image as logPolar and linearPolar

https://github.com/opencv/opencv/blob/master/samples/python/logpolar.py

Usage:
    logpolar.py

Keys:
    ESC    - exit
'''

# Python 2/3 compatibility
from __future__ import print_function

import numpy as np
import cv2 as cv
title_window = 'logpolar.py'

def main():
    import sys
    try:
        fn = sys.argv[1]
    except IndexError:
        fn = 'PythonData/fruits.jpg'

    img = cv.imread(cv.samples.findFile(fn))
    if img is None:
        print('Failed to load image file:', fn)
        sys.exit(1)

    img2 = cv.logPolar(img, (img.shape[0]/2, img.shape[1]/2), 40, cv.WARP_FILL_OUTLIERS)
    img3 = cv.linearPolar(img, (img.shape[0]/2, img.shape[1]/2), 40, cv.WARP_FILL_OUTLIERS)

    cv.imshow('before', img)
    images = np.vstack((img2, img3))
    cv.imshow('Logpolar (top) and linearpolar', images)

    cv.waitKey(0)
    print('Done')


if __name__ == '__main__':
    print(__doc__)
    main()
    cv.destroyAllWindows()
