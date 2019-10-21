#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include <string>
#include <opencv2/ml.hpp>
#include "time.h"
#include <iomanip> 
#include <sstream>
#include <random>
#include <intrin.h>

using namespace std;
using namespace cv;

#ifdef _DEBUG
const int cityMultiplier = 100;
const int puzzleMultiplier = 1;
#else
const int puzzleMultiplier = 1000;
const int cityMultiplier = 10000;
#endif

class CitySolver
{
public:
	std::vector<Point2f> cityPositions;
	std::vector<int> cityOrder;
	RNG rng;
	double currentTemperature = 100.0;
	char outMsg[512];
	int d0, d1, d2, d3;
private:

public:
	CitySolver()
	{
		rng = RNG(__rdtsc()); // When called in rapid succession, the rdtsc will prevent duplication when multi-threading...
		// rng = theRNG(); // To see the duplication problem with MT, uncomment this line.  Watch all the energy levels will be the same!
		currentTemperature = 100.0f;
	}
	/** Give energy value for a state of system.*/
	double energy() const
	{
		double e = 0;
		for (size_t i = 0; i < cityOrder.size(); i++)
		{
			e += norm(cityPositions[i] - cityPositions[cityOrder[i]]);
		}
		return e;
	}

	/** Function which change the state of system (random perturbation).*/
	void changeState()
	{
		d0 = rng.uniform(0, static_cast<int>(cityPositions.size()));
		d1 = cityOrder[d0];
		d2 = cityOrder[d1];
		d3 = cityOrder[d2];

		cityOrder[d0] = d2;
		cityOrder[d2] = d1;
		cityOrder[d1] = d3;
	}

	/** Function to reverse to the previous state.*/
	void reverseState()
	{
		cityOrder[d0] = d1;
		cityOrder[d1] = d2;
		cityOrder[d2] = d3;
	}
};

extern "C" __declspec(dllexport)
CitySolver *Annealing_Basics_Open(Point2f *cityPositions, int count)
{
	CitySolver *ts = new CitySolver();
	ts->cityPositions.assign(cityPositions, cityPositions + count);
	return ts;
}

extern "C" __declspec(dllexport)
void Annealing_Basics_Close(CitySolver *ts)
{
	delete ts;
}

extern "C" __declspec(dllexport)
char *Annealing_Basics_Run(CitySolver *ts, int *cityOrder, int count)
{
	ts->cityOrder.assign(cityOrder, cityOrder + count);
	int changesApplied = ml::simulatedAnnealingSolver(*ts, ts->currentTemperature, ts->currentTemperature*0.97, 0.99, cityMultiplier * count, &ts->currentTemperature, ts->rng);
	copy(ts->cityOrder.begin(), ts->cityOrder.end(), cityOrder);

	string msg = " changesApplied=" + to_string(changesApplied) + " temp=" + to_string(ts->currentTemperature) + " result = " + to_string(ts->energy());
	strcpy_s(ts->outMsg, msg.c_str());
	return ts->outMsg;
}










class puzzleSolver
{
public:
	std::vector<Rect> puzzlePieces;
	std::vector<int> puzzleOrder;
	int puzzleCount;
	RNG rng;
	double currentTemperature = 100.0;
	char outMsg[512];
	int d0, d1, d2, d3;
	Mat rgb;
	cv::Vec3i state;
private:

public:
	puzzleSolver()
	{
		rng = RNG(__rdtsc()); 
		currentTemperature = 100.0f;
	}
	/** Give energy value for a state of system.*/
	double energy() const
	{
		double e = 0;
		int first = state.val[1];
		int second = state.val[2];
#pragma omp parallel for
		for (int i = 0; i < puzzleOrder.size(); i++)
		{
			Mat m1, m2, ccoeff;
			Rect roi1 = puzzlePieces[i];
			Rect roi2 = puzzlePieces[puzzleOrder[i]];
			if (state.val[0] == 0)
			{
				m1 = rgb(roi1).col(first);
				m2 = rgb(roi2).col(second);
			}
			else {
				m1 = rgb(roi1).row(first);
				m2 = rgb(roi2).row(second);
			}
			cv::matchTemplate(m1, m2, ccoeff, TM_CCOEFF_NORMED);
			double invert = 1 - ccoeff.at<float>(0, 0);
#pragma omp atomic 
			e += invert;
		}
		return e;
	}

	/** Function which change the state of system (random perturbation).*/
	void changeState()
	{
		d0 = rng.uniform(0, static_cast<int>(puzzleOrder.size()));
		d1 = puzzleOrder[d0];
		d2 = puzzleOrder[d1];
		d3 = puzzleOrder[d2];

		puzzleOrder[d0] = d2;
		puzzleOrder[d2] = d1;
		puzzleOrder[d1] = d3;
	}

	/** Function to reverse to the previous state.*/
	void reverseState()
	{
		puzzleOrder[d0] = d1;
		puzzleOrder[d1] = d2;
		puzzleOrder[d2] = d3;
	}
};

extern "C" __declspec(dllexport)
puzzleSolver *Puzzle_PieceCorrelation_Open(Rect *puzzlePieces, int count)
{
	puzzleSolver *ts = new puzzleSolver();
	ts->puzzlePieces.assign(puzzlePieces, puzzlePieces + count);
	return ts;
}

extern "C" __declspec(dllexport)
void Puzzle_PieceCorrelation_Close(puzzleSolver *ts)
{
	delete ts;
}

extern "C" __declspec(dllexport)
char *Puzzle_PieceCorrelation_Run(puzzleSolver *ts, int *puzzleOrder, int *rgbPtr, int rows, int cols, cv::Vec3i state)
{
	ts->state = state;
	ts->rgb = Mat(rows, cols, CV_8UC3, rgbPtr);
	ts->puzzleOrder.assign(puzzleOrder, puzzleOrder + ts->puzzlePieces.size());
	int changesApplied = ml::simulatedAnnealingSolver(*ts, ts->currentTemperature, ts->currentTemperature*0.97, 0.99, puzzleMultiplier * ts->puzzlePieces.size(),
													  &ts->currentTemperature, ts->rng);
	copy(ts->puzzleOrder.begin(), ts->puzzleOrder.end(), puzzleOrder);

	string msg = " changesApplied=" + to_string(changesApplied) + " temp=" + to_string(ts->currentTemperature) + " result = " + to_string(ts->energy());
	strcpy_s(ts->outMsg, msg.c_str());
	return ts->outMsg;
}

