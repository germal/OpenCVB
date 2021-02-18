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


def getDrawRect():
    global drawRect
    return drawRect


def PyStreamRun(OpenCVCode, scriptName):
    global drawRect
    drawRect = (0,0,0,0)
    parser = argparse.ArgumentParser(description='Pass in data', formatter_class=argparse.ArgumentDefaultsHelpFormatter)
    parser.add_argument('--MemMapLength', type=int, default=0, help='The number of bytes are in the memory mapped file.')
    parser.add_argument('--pipeName', default='', help='The name of the input pipe for image data.')
    args = parser.parse_args()

    # When the PythonDebug project runs a Python script, this code will start OpenCVB.exe and invoke the script.
    MemMapLength = args.MemMapLength
    if MemMapLength == 0:
        MemMapLength = 400 # these values have been generously padded (on both sides) but if they grow...
        args.pipeName = 'PyStream2Way0' # we always start with 0 and since it is only invoked once, 0 is all it will ever be.
        ocvb = os.getcwd() + '/../bin/Debug/OpenCVB.exe'
        if os.path.exists(ocvb):
            tupleArg = (' ', scriptName)
            pid = os.spawnv(os.P_NOWAIT, ocvb, tupleArg) # OpenCVB.exe will be run with this .py script

    pipeName = '\\\\.\\pipe\\' + args.pipeName

    while True:
        try:
            pipeIn = open(pipeName, 'rb')
            pipeOut = open(pipeName + 'Results', 'wb')
            break
        except Exception as exception:
            time.sleep(0.1) # sleep for a bit to wait for OpenCVB to start...
    try: 
        mm = mmap.mmap(0, MemMapLength, tagname='Python_MemMap')
        frameCount = -1
        while True:
            mm.seek(0)
            arrayDoubles = array.array('d', mm.read(MemMapLength))
            rgbBufferSize = int(arrayDoubles[1])
            rows = int(arrayDoubles[3])
            cols = int(arrayDoubles[4])
            # this is the task.drawRect in OpenCVB
            drawRect = (int(arrayDoubles[5]),int(arrayDoubles[6]),int(arrayDoubles[7]),int(arrayDoubles[8]))

            if rows > 0:
                if arrayDoubles[0] == frameCount:
                    sleep(0.001)
                else:
                    frameCount = arrayDoubles[0] 
                    rgb = pipeIn.read(int(rgbBufferSize))
                    rgbSize = rows, cols, 3
                    try:
                        imgRGB = np.array(np.frombuffer(rgb, np.uint8).reshape(rgbSize))
                    except:
                        print("Unable to reshape the RGB data")
                        sys.exit()
                    OpenCVCode(imgRGB, frameCount)
                    shape = (3, rows, cols)
                    pipeOut.write(np.asarray(imgRGB))
                    
    except Exception as exception:
        print(exception)
        Mbox('PyStream2.py', 'Failure - see console output', 1)    