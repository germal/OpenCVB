import ctypes
def Mbox(title, text, style):
    return ctypes.windll.user32.MessageBoxW(0, text, title, style)

title_window = 'BareBones.py'
print("testing the console log output...")
Mbox('Barebones', 'Python is present and the OpenCV interface from Python is working!', 1)