Imports cv = OpenCvSharp
Imports OpenCvSharp.Aruco.CvAruco
Public Class Aruco_Basics : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Show how to use the Aruco markers and rotate the image accordingly."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static src = cv.Cv2.ImRead(ocvb.parms.HomeDir + "Data/aruco_markers_photo.jpg")
        Static detectorParameters = cv.Aruco.DetectorParameters.Create()
        detectorParameters.CornerRefinementMethod = cv.Aruco.CornerRefineMethod.Subpix
        detectorParameters.CornerRefinementWinSize = 9
        Dim dictionary = cv.Aruco.CvAruco.GetPredefinedDictionary(cv.Aruco.PredefinedDictionaryName.Dict4X4_1000)
        Dim corners()() As cv.Point2f = Nothing
        Dim ids() As Int32 = Nothing
        Dim rejectedPoints()() As cv.Point2f = Nothing
        'cv.Aruco.CvAruco.DetectMarkers(src, dictionary, corners, ids, detectorParameters, rejectedPoints)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class