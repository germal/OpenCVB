import pygame
from pygame.locals import *

from OpenGL.GL import *
from OpenGL.GLU import *

import cv2 as cv
import numpy as np

def OpenCVCode(imgRGB, depth_colormap):
    global initialized, surface, screen
    if initialized == False:
        initialized = True
        pygame.init()
        pygame.display.set_caption("OpenCVB - Python_SurfaceBlit.py")
        display = imgRGB.shape[1], imgRGB.shape[0]
        screen = pygame.display.set_mode(display)
        surface = pygame.Surface(display)
    try:
        #if event.type == pygame.QUIT:
        #    pygame.quit()
        #    quit()
        surface = pygame.image.frombuffer(imgRGB, (imgRGB.shape[1], imgRGB.shape[0]), "RGB")
        screen.blit(surface, (0, 0))
        pygame.display.flip()
        pygame.time.wait(1)

    except Exception as exception:
        print(exception)
        cv.waitKey(10000)

# some initialization code
initialized = False
screen = None
surface = None

from PipeStream import pipeStreamRun
pipeStreamRun(OpenCVCode)