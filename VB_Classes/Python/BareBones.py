import ctypes
def Mbox(title, text, style):
    return ctypes.windll.user32.MessageBoxW(0, text, title, style)

print("testing the console log output...")
Mbox('Test', 'Python is present and the OpenCV interface from Python is working!', 1)