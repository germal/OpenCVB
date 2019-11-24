import cv2 as cv
import argparse


def main():
    bg = cv.imread('PythonData/fruits.jpg')
    obj = cv.imread('PythonData/baboon.jpg')
    generator = cv.bgsegm.createSyntheticSequenceGenerator(bg, obj)

    while True:
        frame, mask = generator.getNextFrame()
        cv.imshow('Generated frame', frame)
        cv.imshow('Generated mask', mask)
        k = cv.waitKey(int(1000.0 / 30))
        if k == 27:
            break


if __name__ == '__main__':
    main()